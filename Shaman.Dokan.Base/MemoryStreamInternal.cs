using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaman.Dokan
{
    class MemoryStreamInternal : Stream
    {
        public MemoryStreamInternal(int length)
        {
            data = new byte[length];
        }
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public volatile int length;
        public volatile byte[] data;

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
                var newlength = length + count;
                if (newlength > data.Length)
                {
                    var newdata = new byte[Math.Max(newlength, data.Length * 2)];
                    Buffer.BlockCopy(data, 0, newdata, 0, length);
                    data = newdata;
                }

                Buffer.BlockCopy(buffer, offset, data, length, count);
                length = newlength;
            }
        }

        protected override void Dispose(bool disposing)
        {
            data = null;
        }
    }
}
