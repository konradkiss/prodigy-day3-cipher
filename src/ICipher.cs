namespace Cipher;

/// Encrypts a message so that only someone with the key can read it.
public interface ICipher
{
    byte[] Encrypt(string message);
    string Decrypt(byte[] data);
}
