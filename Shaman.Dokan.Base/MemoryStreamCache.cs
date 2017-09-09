using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaman.Dokan
{
    public class MemoryStreamCache<TKey>
    {
        private Action<TKey, Stream> load;
        public MemoryStreamCache(Action<TKey, Stream> load)
        {
            this.load = load;
        }

        public MemoryStreamCache(Func<TKey, Stream> getStream)
        {
            this.load = (key, stream) =>
            {
                using (var s = getStream(key))
                {
                    s.CopyTo(stream);
                }
            };
        }

        private Dictionary<TKey, MemoryStreamManager> streams = new Dictionary<TKey, MemoryStreamManager>();
        public Stream OpenStream(TKey item, long? size, bool onlyIfAlreadyAvailable = false)
        {
            lock (streams)
            {
                streams.TryGetValue(item, out var ms);
                if (ms != null)
                {
                    lock (ms)
                    {
                        if (!ms.IsDisposed)
                        {
                            return ms.CreateStream();
                        }
                    }
                }

                if (onlyIfAlreadyAvailable) return null;

                ms = new MemoryStreamManager(stream => load(item, stream), size);
                streams[item] = ms;
                return ms.CreateStream();

                //Console.WriteLine("  (" + access + ")");
            }
        }

        public long? TryGetLength(TKey item)
        {
            lock (streams)
            {
                if (streams.TryGetValue(item, out var manager))
                {
                    return manager.Length;
                }
            }
            return null;
        }


        private byte[] sharedGarbageBuffer = new byte[32 * 1024];

        public long GetLength(TKey item)
        {
            lock (streams)
            {
                using (var s = OpenStream(item, null))
                {
                    var manager = streams[item];
                    if (manager.Length != null) return manager.Length.Value;

                    long len = 0;
                    while (true)
                    {
                        var q = s.Read(sharedGarbageBuffer, 0, sharedGarbageBuffer.Length);
                        if (q == 0) break;
                        len += q;
                    }
                    return len;
                }
            }
        }
    }
}
