# Review of src/SafeCipher.cs

One run of the review prompt (in README.md) in a fresh `claude -p` session, Claude Code 2.1.312, on 28 October 2026. The folder held only src/ with the code comments removed. Tools: Read, Glob, Grep, WebSearch, WebFetch. 128 s, 16 turns.

---

I found three issues in `src/SafeCipher.cs`. None of the tests below has been run: the call to check for the .NET SDK was blocked, so they are written out but not executed.

**Short answers:**
- **Read or change a message:** not while the key is intact. `AesGcm.Decrypt` checks the 16-byte tag, throws on a mismatch and clears the output buffer, so a flipped bit is rejected. The exception is finding 1.
- **Replay a message:** yes (finding 3).
- **Get the key:** not from the code itself, but after `Dispose` the key becomes all zeros, which every attacker knows (finding 1).

---

### 1. A disposed key turns into an all-zero key, and the cipher keeps using it (`SafeCipher.cs:10`, used at `:23` and `:34`)

- **What an attacker can do:** `SecretKey.Dispose()` zeroes the byte array. `SafeCipher` holds the same array and has no "disposed" check, so later `Encrypt` calls run under an all-zero AES key. `AesGcm` only rejects keys of the wrong length (not 16, 24 or 32 bytes), not weak values like all zeros. Anyone can then decrypt those messages with `new byte[32]` and forge new ones that pass the tag check.
- **Related:** the constructor at `:6` stores the caller's array without copying it. Any code that still holds that array can change or wipe the key while it is in use.
- **Test that proves it:**
  ```csharp
  var key = new SecretKey(RandomNumberGenerator.GetBytes(32));
  var c = new SafeCipher(key);
  key.Dispose();
  var ct = c.Encrypt("secret");
  var attacker = new SafeCipher(new SecretKey(new byte[32]));
  Assert.Equal("secret", attacker.Decrypt(ct));        // passes today
  ```
- **Sources:**
  - Microsoft Learn, [Dispose Pattern](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern): "DO throw an ObjectDisposedException from any member that cannot be used after the object has been disposed of."
  - Microsoft Learn, [AesGcm constructor](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.-ctor): the only key exception listed is for a length other than 16, 24 or 32 bytes.

### 2. Nothing limits how many messages one key encrypts with random nonces (`SafeCipher.cs:20`)

- **What an attacker can do:** this only matters at very high volume. Nonces are 96 random bits and nothing counts uses per key. Past NIST's limit, the chance of two messages getting the same nonce rises. One repeated nonce lets an attacker forge messages and XOR two plaintexts together.
- **Test that proves it:** a brute-force collision isn't practical to show. The test checks that the limit is enforced: count `Encrypt` calls per key and assert that call 2³²+1 throws. Today no counter exists, so the test cannot pass.
- **Source:** NIST [SP 800-38D](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38d.pdf):
  - §8.3: "The total number of invocations of the authenticated encryption function shall not exceed 2³², including all IV lengths and all instances of the authenticated encryption function with the given key." This applies to random (RBG-based) IVs.
  - §8: "if even one IV is ever repeated, then the implementation may be vulnerable to the forgery attacks…"

### 3. Captured messages are accepted again, and in any context (`SafeCipher.cs:28–37`)

- **What an attacker can do:** `Decrypt` keeps no record of what it has already accepted. A captured ciphertext decrypts successfully every time it is sent again. No associated data is passed to `Encrypt` or `Decrypt` (lines 24 and 35), so a ciphertext isn't tied to a sender, recipient, field or session. It can be moved from one context to another and still pass the tag check. This matters if callers rely on `ICipher` to reject repeated or misdirected messages.
- **Test that proves it:**
  ```csharp
  var c = new SafeCipher(new SecretKey(RandomNumberGenerator.GetBytes(32)));
  var ct = c.Encrypt("pay 100");
  Assert.Equal("pay 100", c.Decrypt(ct));
  Assert.Throws<CryptographicException>(() => c.Decrypt(ct)); // fails today: replay is accepted
  ```
- **Sources:**
  - NIST [glossary, "replay attack"](https://csrc.nist.gov/glossary/term/replay_attack), from SP 800-53 Rev. 5: "An attack in which the Attacker is able to replay previously captured messages…"
  - NIST SP 800-38D §7.2, Algorithm 5: authenticated decryption takes only the IV, ciphertext, associated data and tag. It keeps no state, so GCM alone cannot detect a replay.

---

I left out short inputs under 28 bytes: they throw `ArgumentOutOfRangeException` instead of a crypto exception, but that doesn't let anyone read, change, replay or get the key. I made no changes to any files.
