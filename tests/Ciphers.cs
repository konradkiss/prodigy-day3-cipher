using System.Security.Cryptography;
using System.Text;

namespace Cipher.Tests;

// Both versions, so every test runs against each of them.
public static class Ciphers
{
    public const string Secret = "correct-horse-battery-staple";

    public static TheoryData<string> Both => ["naive", "safe"];

    public static ICipher Make(string name) => name == "naive"
        ? new NaiveCipher(new CipherOptions(Secret))
        : new SafeCipher(new SecretKey(Encoding.UTF8.GetBytes(Secret.PadRight(32)[..32])));

    // What ends up in a log line like logger.LogInformation("Cipher: {Settings}", settings)
    public static object Settings(string name) => name == "naive"
        ? new CipherOptions(Secret)
        : new SecretKey(Encoding.UTF8.GetBytes(Secret.PadRight(32)[..32]));
}
