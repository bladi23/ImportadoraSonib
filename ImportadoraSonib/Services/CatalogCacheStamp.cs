using System.Threading;

namespace ImportadoraSonib.Services;

public class CatalogCacheStamp
{
    private long _v = 0;
    public long Value => Interlocked.Read(ref _v);
    public long Bump() => Interlocked.Increment(ref _v);
}
