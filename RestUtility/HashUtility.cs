using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace RestUtility;

public static class HashUtility
{
    private static readonly Dictionary<Type, Func<HashAlgorithm>> _hashAlgorithmCache = new();

    public static IEnumerable<byte> GetHashBytes<T>(string data)
        where T : HashAlgorithm
    {
        return GetHashBytes<T>(data, Encoding.UTF8);
    }

    public static IEnumerable<byte> GetHashBytes<T>(string data, Encoding encoding)
        where T : HashAlgorithm
    {
        if (encoding is null)
            throw new ArgumentNullException(nameof(encoding));

        return GetHashBytes<T>(encoding.GetBytes(data));
    }

    public static IEnumerable<byte> GetHashBytes<T>(byte[] data)
        where T : HashAlgorithm
    {
        if (!_hashAlgorithmCache.TryGetValue(typeof(T), out var algorithm))
        {
            MethodInfo? methodInfo = typeof(T).GetMethod("Create", Array.Empty<Type>());
            var func = (Func<HashAlgorithm>)Delegate.CreateDelegate(typeof(Func<HashAlgorithm>), methodInfo!);
            _hashAlgorithmCache[typeof(T)] = func;
            algorithm = func;
        }

        using HashAlgorithm? csp = algorithm.Invoke();
        return csp.ComputeHash(data);
    }
}