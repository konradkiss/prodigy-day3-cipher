namespace Cipher.Tests;

// The tests nobody writes unless the contract asks for them.
public class PropertyTests
{
    [Theory]
    [MemberData(nameof(Ciphers.Both), MemberType = typeof(Ciphers))]
    public void Same_message_twice_looks_different(string cipher)
    {
        var c = Ciphers.Make(cipher);
        Assert.NotEqual(c.Encrypt("Pay Bob $100.00"), c.Encrypt("Pay Bob $100.00"));
    }

    [Theory]
    [MemberData(nameof(Ciphers.Both), MemberType = typeof(Ciphers))]
    public void Changed_message_is_refused(string cipher)
    {
        var c = Ciphers.Make(cipher);
        var data = c.Encrypt("Order 1001: pay Bob $100.00");
        data[5] ^= 0x08;                                // someone flips one bit on the way
        var error = Record.Exception(() => c.Decrypt(data));
        Assert.True(error is not null, $"Decrypted without complaint: {(error is null ? c.Decrypt(data) : "")}");
    }

    [Theory]
    [MemberData(nameof(Ciphers.Both), MemberType = typeof(Ciphers))]
    public void Key_never_shows_up_in_a_log(string cipher)
    {
        var logLine = $"Cipher: {Ciphers.Settings(cipher)}";
        Assert.DoesNotContain(Ciphers.Secret, logLine);
    }
}
