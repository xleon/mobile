using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toggl.Phoebe
{
    public sealed class LogStore
    {
        private const long FileTrimThresholdSize = 1024 * 1024;
        private const long FileTrimTargetSize = 512 * 1024;
        private const string FileName = "diagnostics.log";
        private const string FileTempName = "diagnostics.log.tmp";

        private readonly object syncRoot = new Object ();
        private readonly Queue<string> writeQueue = new Queue<string> ();
        private readonly string logDir;
        private bool isProcessing;
        private TaskCompletionSource<byte[]> compressionTcs;

        public LogStore ()
        {
            logDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
        }

        public Task<byte[]> Compress ()
        {
            Task<byte[]> ret;

            lock (syncRoot) {
                if (compressionTcs == null) {
                    compressionTcs = new TaskCompletionSource<byte[]> ();
                }
                ret = compressionTcs.Task;
            }

            EnsureProcessing ();

            return ret;
        }

        public void Record (Logger.Level level, string tag, string message, Exception exc)
        {
            var sb = new StringBuilder ();
            sb.AppendFormat ("[{0}] {1} - {2}: {3}", Time.Now, level, tag, message);
            if (exc != null) {
                var lines = exc.ToString ().Replace ("\r\n", "\n").Split ('\n', '\r');
                foreach (var line in lines) {
                    sb.AppendLine ();
                    sb.Append ('\t');
                    sb.Append (line);
                }
            }

            Record (sb.ToString ());
        }

        private void Record (string data)
        {
            lock (syncRoot) {
                writeQueue.Enqueue (data);
            }

            EnsureProcessing ();
        }

        private void EnsureProcessing ()
        {
            lock (syncRoot) {
                if (isProcessing) {
                    return;
                }

                isProcessing = true;
            }

            ThreadPool.QueueUserWorkItem (Process);
        }

        private void Process (object state)
        {
            var logFile = new FileInfo (Path.Combine (logDir, FileName));

            // Always start out by trimming the logs
            TrimLog (logFile);

            while (true) {
                var shouldCompress = false;
                var shouldFlush = false;

                // Determine what to do:
                lock (syncRoot) {
                    if (compressionTcs != null) {
                        shouldCompress = true;
                    } else if (writeQueue.Count > 0) {
                        shouldFlush = true;
                    } else {
                        // Nothing left to do, shutdown worker thread
                        isProcessing = false;
                        break;
                    }
                }

                // Do the actual work outside of lock
                if (shouldCompress) {
                    CompressLogs (logFile);
                } else if (shouldFlush) {
                    FlushLogs (logFile);
                }
            }
        }

        private void TrimLog (FileInfo logFile)
        {
            // Check if we need to trim the log file:
            if (logFile.Exists && logFile.Length >= FileTrimThresholdSize) {
                var tmpFile = new FileInfo (Path.Combine (logDir, FileTempName));
                try {
                    if (tmpFile.Exists) {
                        tmpFile.Delete ();
                    }
                    File.Move (logFile.FullName, tmpFile.FullName);
                    logFile.Refresh ();
                    tmpFile.Refresh ();

                    // Copy data over to new file
                    using (var tmpReader = new StreamReader (tmpFile.FullName, Encoding.UTF8))
                    using (var logWriter = new StreamWriter (logFile.FullName, false, Encoding.UTF8)) {
                        // Skip to where we can start copying
                        tmpReader.BaseStream.Seek (-FileTrimTargetSize, SeekOrigin.End);
                        tmpReader.DiscardBufferedData ();
                        tmpReader.ReadLine ();

                        string line;
                        while ((line = tmpReader.ReadLine ()) != null) {
                            logWriter.WriteLine (line);
                        }
                    }
                } catch (SystemException ex) {
                    Console.WriteLine ("Failed to trim log file.");
                    Console.WriteLine (ex);

                    // Make sure that the log file is deleted so we can start a new one
                    try {
                        logFile.Delete ();
                    } catch (SystemException ex2) {
                        Console.WriteLine ("Failed to clean up log file.");
                        Console.WriteLine (ex2);
                    }
                } finally {
                    try {
                        tmpFile.Delete ();
                    } catch (SystemException ex) {
                        Console.WriteLine ("Failed to clean up temporary log file.");
                        Console.WriteLine (ex);
                    }
                }
            }
        }

        private void FlushLogs (FileInfo logFile)
        {
            // Flush queue to log file:
            try {
                using (var logWriter = new StreamWriter (logFile.FullName, true, Encoding.UTF8)) {
                    while (true) {
                        String data;

                        // Get data to write:
                        lock (syncRoot) {
                            if (writeQueue.Count < 1) {
                                return;
                            }

                            data = writeQueue.Dequeue ();
                        }

                        // Write data to file:
                        try {
                            logWriter.WriteLine (data);
                        } catch (SystemException ex2) {
                            // Couldn't write to log file.. nothing to do really.
                            Console.WriteLine ("Failed to append to log file.");
                            Console.WriteLine (ex2);
                        }
                    }
                }
            } catch (SystemException ex) {
                // Probably opening of the log file failed, clean up
                Console.WriteLine ("Failed to open the log file.");
                Console.WriteLine (ex);

                lock (syncRoot) {
                    writeQueue.Clear ();
                }
            }
        }

        private void CompressLogs (FileInfo logFile)
        {
            byte[] data = null;

            using (var logStream = logFile.OpenRead ())
            using (var buffer = new MemoryStream ())
            using (var zipStream = new GZipStream (buffer, CompressionMode.Compress)) {
                logStream.CopyTo (zipStream);
                zipStream.Close ();

                data = buffer.ToArray ();
            }

            lock (syncRoot) {
                compressionTcs.SetResult (data);
                compressionTcs = null;
            }
        }
    }
}
