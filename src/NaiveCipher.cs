using System.Security.Cryptography;
using System.Text;

namespace Cipher;

// What an agent hands over when nobody says what "secure" means.
// It compiles, it works, and every round-trip test is green.
public record CipherOptions(string Key);

public class NaiveCipher(CipherOptions options) : ICipher
{
    private static readonly byte[] Iv = new byte[16];   // the same IV for every message

    private byte[] Key => SHA256.HashData(Encoding.UTF8.GetBytes(options.Key));

    public byte[] Encrypt(string message)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        return aes.EncryptCbc(Encoding.UTF8.GetBytes(message), Iv);
    }

    public string Decrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        return Encoding.UTF8.GetString(aes.DecryptCbc(data, Iv));
    }
}
