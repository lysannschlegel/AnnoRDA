using System;
using System.IO;

namespace AnnoRDA.IO.Encryption
{
    public enum EncryptionMode
    {
        Encrypt,
        Decrypt,
    }

    public class EncryptionStream : Stream
    {
        private readonly Stream baseStream;
        private readonly StreamAccessMode accessMode;
        private readonly bool leaveOpen;

        private readonly LinearCongruentialGenerator lcg;
        private byte? bufferedUnprocessedByte = null;
        private byte? bufferedProcessedByte = null;

        private bool disposed = false;

        public EncryptionStream(Stream baseStream, StreamAccessMode accessMode, bool leaveOpen = false)
        {
            if (baseStream == null) {
                throw new ArgumentNullException("baseStream");
            }

            switch (accessMode) {
                case StreamAccessMode.Read:
                    if (!baseStream.CanRead) {
                        throw new ArgumentException("baseStream must be readable for accessMode == Read", "baseStream");
                    }
                    break;
                case StreamAccessMode.Write:
                    if (!baseStream.CanWrite) {
                        throw new ArgumentException("baseStream must be writable for accessMode == Write", "baseStream");
                    }
                    break;
            }

            this.baseStream = baseStream;
            this.accessMode = accessMode;
            this.leaveOpen = leaveOpen;

            this.lcg = new LinearCongruentialGenerator(seed: 0x71C71C71);
        }

        public override bool CanRead {
            get { return this.accessMode == StreamAccessMode.Read; }
        }
        public override bool CanSeek {
            get { return false; }
        }
        public override bool CanTimeout {
            get { return this.baseStream.CanTimeout; }
        }
        public override bool CanWrite {
            get { return this.accessMode == StreamAccessMode.Write; }
        }
        public override long Length {
            get {
                long result = this.baseStream.Length;
                switch (this.accessMode) {
                    case StreamAccessMode.Read:
                        if (result % 2 == 1) {
                            ++result;
                        }
                        break;
                    case StreamAccessMode.Write:
                        result += this.bufferedUnprocessedByte.HasValue ? 1 : 0;
                        break;
                }
                return result;
            }
        }
        public override long Position {
            get {
                long result = this.baseStream.Position;
                switch (this.accessMode) {
                    case StreamAccessMode.Read:
                        result -= this.bufferedUnprocessedByte.HasValue ? 1 : 0;
                        result -= this.bufferedProcessedByte.HasValue ? 1 : 0;
                        break;
                    case StreamAccessMode.Write:
                        result += this.bufferedUnprocessedByte.HasValue ? 1 : 0;
                        break;
                }
                return result;
            }
            set { throw new NotSupportedException(); }
        }
        public override int ReadTimeout {
            get { return this.baseStream.ReadTimeout; }
            set { this.baseStream.ReadTimeout = value; }
        }
        public override int WriteTimeout {
            get { return this.baseStream.WriteTimeout; }
            set { this.baseStream.WriteTimeout = value; }
        }

        public override void Flush()
        {
            this.baseStream.Flush();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 ) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if (offset + count > buffer.Length) {
                throw new ArgumentException("offset + count must be <= buffer.Length", "buffer");
            }
            if (!this.CanRead) {
                throw new NotSupportedException();
            }

            if (count == 0) {
                return 0;
            }

            // Put buffered processed byte out first
            if (this.bufferedProcessedByte.HasValue) {
                // Read before handling the bufferedProcessedByte, in case Read() throws an exception
                int result = this.ReadIgnoringBufferedProcessedByte(buffer, offset + 1, count - 1);
                buffer[offset] = this.bufferedProcessedByte.Value;
                this.bufferedProcessedByte = null;
                return result + 1;

            } else {
                return this.ReadIgnoringBufferedProcessedByte(buffer, offset, count);
            }
        }
        private int ReadIgnoringBufferedProcessedByte(byte[] buffer, int offset, int count)
        {
            if (count == 0) {
                return 0;
            }

            // Find out how many bytes we should read...
            int rawCount = count;
            int rawOffset = 0;
            // We'll write the unprocessed byte to the start of the rawBuffer
            // So reserve space for it in the rawBuffer, but exclude it from the read count.
            if (this.bufferedUnprocessedByte.HasValue) {
                rawCount -= 1;
                rawOffset += 1;
            }
            // We must always process 2 bytes at a time. If the current rawBuffer size would be odd,
            // try to read one byte more. We'll buffer the excess byte in this.bufferedProcessedByte.
            if ((rawCount + rawOffset) % 2 != 0) {
                rawCount += 1;
            }

            // Read from base stream
            byte[] rawBuffer = new byte[rawCount + rawOffset];
            int rawRead = this.baseStream.Read(rawBuffer, rawOffset, rawCount);

            // If the end of the base stream is reached, we must still return the buffered
            // unprocessed byte.
            if (rawRead == 0) {
                if (this.bufferedUnprocessedByte.HasValue) {
                    buffer[offset] = this.bufferedUnprocessedByte.Value;
                    this.bufferedUnprocessedByte = null;
                    return 1;
                } else {
                    return 0;
                }
            }

            // Insert unprocessed byte to be processed now. We didn't do that before in case
            // baseStream.Read() throws an exception.
            if (this.bufferedUnprocessedByte.HasValue) {
                rawBuffer[0] = this.bufferedUnprocessedByte.Value;
                this.bufferedUnprocessedByte = null;
                rawRead += 1;
            }

            // process and write into target buffer
            int n;
            for (n = 0; (n + 1) < rawRead; n += 2) {
                short value = (short)(rawBuffer[n] + (rawBuffer[n + 1] << 8));

                this.lcg.MoveNext();
                short key = this.lcg.Current;
                short processedValue = (short)(value ^ key);

                buffer[offset + n] = (byte)processedValue;
                byte secondByte = (byte)(processedValue >> 8);

                // This might be the excess byte we read so we could process more, but it won't
                // fit into the caller's buffer. In that case, buffer it in this stream.
                if ((offset + count) > (offset + n + 1)) {
                    buffer[offset + n + 1] = secondByte;
                } else {
                    this.bufferedProcessedByte = secondByte;
                    return n + 1;
                }
            }

            if (n < rawRead) {
                // one byte couldn't be processed, save it for later
                this.bufferedUnprocessedByte = rawBuffer[n];
                return n;
            } else {
                return rawRead;
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if (offset + count > buffer.Length) {
                throw new ArgumentException("offset + count must be <= buffer.Length", "buffer");
            }
            if (!this.CanWrite) {
                throw new NotSupportedException();
            }

            if (count == 0) {
                return;
            }

            // Find out how many bytes we we get after processing
            int processedCount = count;
            // We'll write the unprocessed byte to the start of the processedBuffer, so reserve space for it.
            if (this.bufferedUnprocessedByte.HasValue) {
                processedCount += 1;
            }
            // We must always process 2 bytes at a time. If the current processedCount would be odd, we can
            // only write one byte less. We'll buffer the excess byte in this.bufferedUnprocessedByte.
            bool mustBufferExcessByte = false;
            if (processedCount % 2 != 0) {
                processedCount -= 1;
                mustBufferExcessByte = true;
            }
            // Reserve space
            byte[] processedBuffer = new byte[processedCount];

            // Process and write into target buffer
            // We must take into account the current bufferedUnprocessedByte. Pretend that it's at the start
            // of the buffer.
            int bufferOffsetForUnprocessedByte = this.bufferedUnprocessedByte.HasValue ? -1 : 0;
            int n;
            for (n = 0; (n + 1) < processedCount; n += 2) {
                byte firstByte = n == 0 && this.bufferedUnprocessedByte.HasValue ? this.bufferedUnprocessedByte.Value : buffer[n + offset + bufferOffsetForUnprocessedByte];
                byte secondByte = buffer[n + offset + bufferOffsetForUnprocessedByte + 1];
                short value = (short)(firstByte + (secondByte << 8));

                this.lcg.MoveNext();
                short key = this.lcg.Current;
                short processedValue = (short)(value ^ key);

                processedBuffer[n] = (byte)processedValue;
                processedBuffer[n + 1] = (byte)(processedValue >> 8);
            }

            // Write to base stream
            this.baseStream.Write(processedBuffer, 0, processedCount);

            // Buffer the unprocessed byte and clear the previous one (if any)
            if (mustBufferExcessByte) {
                this.bufferedUnprocessedByte = buffer[offset + count - 1];
            } else {
                this.bufferedUnprocessedByte = null;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) {
                return;
            }

            if (disposing) {
                // Write bufferedUnprocessedByte
                if (this.accessMode == StreamAccessMode.Write && this.bufferedUnprocessedByte.HasValue) {
                    byte[] buffer = new byte[] { this.bufferedUnprocessedByte.Value };
                    this.baseStream.Write(buffer, 0, 1);
                }

                if (!this.leaveOpen) {
                    this.baseStream.Dispose();
                }
            }

            this.disposed = true;
            base.Dispose(disposing);
        }
    }
}
