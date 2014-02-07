using System;
using System.Collections.Generic;
using System.IO;

namespace Toggl.Phoebe.Bugsnag.IO
{
    public class CombiningStream : Stream
    {
        private readonly byte[] sepData;
        private readonly List<Stream> streams = new List<Stream> ();
        private long pos;

        public CombiningStream (string separator) : this (System.Text.Encoding.UTF8.GetBytes (separator))
        {
        }

        public CombiningStream (byte[] separator = null)
        {
            sepData = separator ?? new byte[0];
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                foreach (var stream in streams) {
                    stream.Dispose ();
                }
                streams.Clear ();
            }

            base.Dispose (disposing);
        }

        public void Add (string data, System.Text.Encoding enc = null)
        {
            if (enc == null) {
                enc = System.Text.Encoding.UTF8;
            }
            Add (new MemoryStream (enc.GetBytes (data)));
        }

        public void Add (Stream stream)
        {
            if (Position != 0)
                throw new InvalidOperationException ("Cannot inject streams after reading has begun.");

            streams.Add (stream);
        }

        public override void Flush ()
        {
        }

        public override int Read (byte[] buffer, int offset, int count)
        {
            int total = 0;

            while (total < count) {
                int length = ReadSegment (buffer, offset + total, count - total);
                if (length == 0)
                    break;
                total += length;
            }

            return total;
        }

        private int ReadSegment (byte[] buffer, int offset, int count)
        {
            long totalLength = 0;

            bool firstStream = true;
            foreach (var stream in streams) {
                if (!firstStream) {
                    // Need to insert separator
                    if (pos < totalLength + sepData.LongLength) {
                        var startIndex = pos - totalLength;
                        int length = (int)Math.Min (count, sepData.LongLength - startIndex);
                        if (length > 0) {
                            pos += length;
                            Array.Copy (sepData, startIndex, buffer, offset, length);
                            return length;
                        }
                    }
                    totalLength += sepData.LongLength;
                }

                // Need to copy from stream
                if (pos < totalLength + stream.Length) {
                    var startIndex = pos - totalLength;
                    int length = (int)Math.Min (count, stream.Length - startIndex);
                    if (length > 0) {
                        pos += length;
                        return stream.Read (buffer, offset, length);
                    }
                }
                totalLength += stream.Length;

                firstStream = false;
            }

            // Nothing to copy
            return 0;
        }

        public override long Seek (long offset, SeekOrigin origin)
        {
            throw new NotSupportedException ("This stream doesn't support seeking.");
        }

        public override void SetLength (long value)
        {
            throw new NotSupportedException ("This stream is read-only.");
        }

        public override void Write (byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException ("This stream is read-only.");
        }

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return false; }
        }

        public override long Length {
            get {
                long length = 0;
                if (streams.Count > 0)
                    length += (streams.Count - 1) * sepData.LongLength;
                foreach (var stream in streams) {
                    length += stream.Length;
                }
                return length;
            }
        }

        public override long Position {
            get { return pos; }
            set {
                throw new NotSupportedException ("This stream doesn't support seeking.");
            }
        }
    }
}
