using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

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
        private bool isWriting;

        public LogStore ()
        {
            logDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
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

            EnsureWriting ();
        }

        private void EnsureWriting ()
        {
            lock (syncRoot) {
                if (isWriting)
                    return;

                isWriting = true;
            }

            ThreadPool.QueueUserWorkItem (delegate {
                // Check if we need to trim the log file:
                var logFile = new FileInfo (Path.Combine (logDir, FileName));
                if (logFile.Exists && logFile.Length >= FileTrimThresholdSize) {
                    var tmpFile = new FileInfo (Path.Combine (logDir, FileTempName));
                    try {
                        if (tmpFile.Exists)
                            tmpFile.Delete ();
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

                // Flush queue to log file:
                try {
                    using (var logWriter = new StreamWriter (logFile.FullName, true, Encoding.UTF8)) {
                        while (true) {
                            String data;

                            // Get data to write:
                            lock (syncRoot) {
                                if (writeQueue.Count < 1) {
                                    isWriting = false;
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
                        isWriting = false;
                        writeQueue.Clear ();
                    }
                }
            });
        }
    }
}
