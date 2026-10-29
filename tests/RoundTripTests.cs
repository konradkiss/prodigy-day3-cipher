namespace Cipher.Tests;

// The tests everybody writes: what goes in comes back out.
public class RoundTripTests
{
    [Theory]
    [MemberData(nameof(Ciphers.Both), MemberType = typeof(Ciphers))]
    public void Short_message_comes_back(string cipher)
    {
        var c = Ciphers.Make(cipher);
        Assert.Equal("hello", c.Decrypt(c.Encrypt("hello")));
    }

    [Theory]
    [MemberData(nameof(Ciphers.Both), MemberType = typeof(Ciphers))]
    public void Empty_message_comes_back(string cipher)
    {
        var c = Ciphers.Make(cipher);
        Assert.Equal("", c.Decrypt(c.Encrypt("")));
    }

    [Theory]
    [MemberData(nameof(Ciphers.Both), MemberType = typeof(Ciphers))]
    public void Long_message_comes_back(string cipher)
    {
        var c = Ciphers.Make(cipher);
        var text = new string('x', 10_000);
        Assert.Equal(text, c.Decrypt(c.Encrypt(text)));
    }
}
