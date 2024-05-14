using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace RateLimitAppDemo.MyRateLimiting
{
    public class CustomRateLimitLease : RateLimitLease
{
    private readonly bool _isAllowed;
    private readonly Dictionary<string, object> _metadata;

    public CustomRateLimitLease(bool isAllowed, Dictionary<string, object> metadata)
    {
        _isAllowed = isAllowed;
        _metadata = metadata;
    }

    public override bool IsAcquired => _isAllowed;

    public override IEnumerable<string> MetadataNames => _metadata.Keys;

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        return _metadata.TryGetValue(metadataName, out metadata);
    }

    public bool TryGetMetadata<T>(string metadataName, out T metadata)
    {
        if (_metadata.TryGetValue(metadataName, out var value) && value is T typedValue)
        {
            metadata = typedValue;
            return true;
        }

        metadata = default(T); // Use default(T) for explicit conversion
        return false;
    }
}
}