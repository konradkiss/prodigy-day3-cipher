using System.Security.Cryptography;
using System.Text;

namespace Cipher;

// The same job, with three decisions written into the contract.
public sealed class SecretKey(byte[] bytes) : IDisposable
{
    private readonly byte[] bytes = [.. bytes];                         // its own copy
    private bool disposed;

    internal byte[] Bytes => disposed ? throw new ObjectDisposedException(nameof(SecretKey)) : bytes;
    public override string ToString() => "SecretKey(***)";            // never prints the key
    public void Dispose() { CryptographicOperations.ZeroMemory(bytes); disposed = true; } // wipes it, then refuses
}

public class SafeCipher(SecretKey key) : ICipher
{
    private const int NonceSize = 12, TagSize = 16;

    public byte[] Encrypt(string message)
    {
        var plain = Encoding.UTF8.GetBytes(message);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);       // new for every message
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];
        using var gcm = new AesGcm(key.Bytes, TagSize);
        gcm.Encrypt(nonce, plain, cipher, tag);                      // the tag detects any change
        return [.. nonce, .. cipher, .. tag];
    }

    public string Decrypt(byte[] data)
    {
        var nonce = data[..NonceSize];
        var tag = data[^TagSize..];
        var cipher = data[NonceSize..^TagSize];
        var plain = new byte[cipher.Length];
        using var gcm = new AesGcm(key.Bytes, TagSize);
        gcm.Decrypt(nonce, cipher, tag, plain);                      // throws if anything changed
        return Encoding.UTF8.GetString(plain);
    }
}
