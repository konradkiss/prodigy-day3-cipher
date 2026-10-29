# Review of src/SafeCipher.cs (after the Dispose fix)

One run of the review prompt (in README.md) in a fresh `claude -p` session, Claude Code 2.1.312, on 28 October 2026. The folder held only src/ with the code comments removed. Tools: Read, Glob, Grep, WebSearch, WebFetch. 139 s, 16 turns.

---

I found two issues in `src/SafeCipher.cs`, both with sources. I changed nothing and ran no tests; the tests below are the ones I'd write to prove each issue.

**Your four questions**
- **Read:** no. It uses AES-GCM with a fresh random 96-bit nonce per message.
- **Change:** no. Decrypt checks the 16-byte tag. Microsoft Learn says a bad tag throws `AuthenticationTagMismatchException` and the output buffer "is cleared".
- **Replay:** yes (finding 1).
- **Get the key:** no path in this file. `ToString()` hides the key, the class makes its own copy of the key bytes, and `Dispose()` wipes them.

---

### Finding 1: A captured ciphertext is accepted again, and can be swapped for another one
**`src/SafeCipher.cs:38`** (and `:27`)

**What an attacker can do:** Decrypt has no freshness check: no timestamp, sequence number or record of nonces already seen. It also passes no associated data, so a ciphertext isn't tied to any context. An attacker who captures one valid ciphertext can:
- send it again as many times as they like, and it decrypts every time;
- put it where a different ciphertext made with the same key belongs (another record, field, user or session), and it decrypts with no error.

Neither of these breaks secrecy or integrity. They break freshness and context, and the class does nothing to stop them. If callers are meant to add a timestamp or sequence number themselves, nothing in `ICipher` says so.

**Source:**
- OWASP MASWE-0009 names "Authenticating messages without a timestamp, nonce, or sequence number" as a weakness. Its fix is "Include and verify a timestamp, nonce, or sequence number in authenticated messages."
- OWASP WebSocket Security Cheat Sheet: "include timestamps or nonces in messages and reject duplicates".

**Test that proves it:**
```csharp
var c = new SafeCipher(new SecretKey(RandomNumberGenerator.GetBytes(32)));
var ct = c.Encrypt("pay 100 to bob");
Assert.Equal("pay 100 to bob", c.Decrypt(ct));
Assert.Throws<CryptographicException>(() => c.Decrypt(ct)); // fails today: the replay is accepted

var a = c.Encrypt("role=user");   // e.g. stored for user A
var b = c.Encrypt("role=admin");  // e.g. stored for user B
Assert.Throws<CryptographicException>(() => c.Decrypt(b) /* in A's slot */); // fails today: the swap is accepted
```

---

### Finding 2: No limit on how many messages one key encrypts with random nonces
**`src/SafeCipher.cs:23`** (and `:26`)

**What an attacker can do:** The nonce is 12 random bytes, which NIST calls the RBG-based construction. Nothing counts how many times one key encrypts, and nothing stops it. Once a key has encrypted far more than 2³² messages, the chance of two messages sharing a nonce rises sharply. If that happens, an attacker learns the XOR of the two plaintexts and can forge messages that pass the tag check. This only matters for long-lived keys with very high volume, but it breaks a "shall" requirement.

**Source:** NIST SP 800-38D:
- §8.3, for RBG-based IVs: "The total number of invocations of the authenticated encryption function shall not exceed 2³², including all IV lengths and all instances of the authenticated encryption function with the given key."
- §8: if an IV repeats, "the implementation may be vulnerable to the forgery attacks… In practice, this requirement is almost as important as the secrecy of the key."

**Test that proves it:** Call `Encrypt` 2³² + 1 times on one `SafeCipher` and assert that the last call throws or forces a new key. It won't, because the class has no counter, so the test fails. This is a long-running test. Reading the code also confirms there is no invocation count anywhere in the class.

---

**Not reported:** `Decrypt` on input shorter than 28 bytes or on `null` throws `ArgumentOutOfRangeException` or `NullReferenceException`, not a cryptographic exception. That's a robustness problem, not a read, change, replay or key issue, and I have no source tying it to an attack.

Sources:
- [NIST SP 800-38D (GCM), §8 and §8.3](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38d.pdf)
- [Microsoft Learn: AesGcm.Decrypt](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.decrypt)
- [OWASP MASWE-0009: Improper Use of Message Authentication Code](https://mas.owasp.org/MASWE/MASVS-CRYPTO/MASWE-0009/)
- [OWASP WebSocket Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/WebSocket_Security_Cheat_Sheet.html)
