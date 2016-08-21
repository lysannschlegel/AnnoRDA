using System;
using System.IO;

namespace AnnoRDA.IO
{
    public class SubStream : Stream
    {
        private readonly Stream source;
        private readonly long offset;
        private readonly long length;
        private readonly bool leaveOpen;

        private bool disposed = false;

        public SubStream(Stream source, long offset, long length, bool leaveOpen = false)
        {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (!source.CanRead) {
                throw new ArgumentException("The source must support reading", "source");
            }
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset cannot be negative.", "offset");
            }
            if (length < 0) {
                throw new ArgumentOutOfRangeException("length cannot be negative.", "length");
            }
            if (source.Length < offset) {
                System.Diagnostics.Trace.TraceError("The substream offset is beyond the bounds of the source stream.");
            }

            this.source = source;
            this.offset = offset;
            this.length = length;
            this.leaveOpen = leaveOpen;

            if (source.Position != offset) {
                if (!source.CanSeek) {
                    throw new ArgumentException("The source must support seeking", "source");
                }
                this.Position = 0;
            }
        }

        public override bool CanRead {
            get {
                return this.source.CanRead;
            }
        }
        public override bool CanSeek {
            get {
                return this.source.CanSeek;
            }
        }
        public override bool CanTimeout {
            get {
                return this.source.CanTimeout;
            }
        }
        public override bool CanWrite {
            get {
                return false;
            }
        }
        public override long Length {
            get {
                return Math.Min(this.length, Math.Max(0, this.source.Length - this.offset));
            }
        }
        public override long Position {
            get {
                return Math.Max(0, this.source.Position - this.offset);
            }
            set {
                this.Seek(value, SeekOrigin.Begin);
            }
        }
        public override int ReadTimeout {
            get {
                return this.source.ReadTimeout;
            }
            set {
                this.source.ReadTimeout = value;
            }
        }
        public override int WriteTimeout {
            get {
                return this.source.WriteTimeout;
            }
            set {
                this.source.WriteTimeout = value;
            }
        }

    #if !NETSTANDARD
        public override IAsyncResult BeginRead(byte[] buffer, int bufferOffset, int count, AsyncCallback callback, object state)
        {
            count = GetMaxReadableBytes(count);
            return this.source.BeginRead(buffer, bufferOffset, count, callback, state);
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.source.EndRead(asyncResult);
        }
    #endif
        public override void Flush()
        {
            this.source.Flush();
        }
        public override int Read(byte[] buffer, int bufferOffset, int count)
        {
            count = GetMaxReadableBytes(count);
            return this.source.Read(buffer, bufferOffset, count);
        }
        public override long Seek(long seekOffset, SeekOrigin origin)
        {
            switch (origin) {
                case SeekOrigin.Begin:
                    long newPos = this.offset + seekOffset;
                    this.source.Seek(newPos, SeekOrigin.Begin);
                    return this.Position;

                case SeekOrigin.Current:
                    return this.Seek(this.Position + seekOffset, SeekOrigin.Begin);

                case SeekOrigin.End:
                    return this.Seek(this.Length + seekOffset, SeekOrigin.Begin);
            }
            return this.Position;
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException("SubStreams are not writable.");
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("SubStreams are not writable.");
        }
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) {
                return;
            }

            if (disposing) {
                if (!this.leaveOpen) {
                    this.source.Dispose();
                }
            }

            this.disposed = true;
            base.Dispose(disposing);
        }


        private int GetMaxReadableBytes(int count)
        {
            if (this.Length - this.Position < count) {
                count = (int)(this.Length - this.Position);
            }
            return count;
        }
    }
}
