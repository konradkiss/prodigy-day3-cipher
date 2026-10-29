# Session 3 demo: green is not safe

The C# demo from session 3 of [agentcode.page](https://agentcode.page/3), "Guaranteed quality?". It needs the .NET 10 SDK and nothing else.

A small library with two versions of the same job, encrypting a message:

- `src/NaiveCipher.cs`: what an agent hands over when nobody says what "secure" means. AES-CBC with the same IV for every message, no tamper check, the key in a `record` (whose generated `ToString()` prints it).
- `src/SafeCipher.cs`: AES-GCM with a new nonce per message, the tag that refuses any change, and a `SecretKey` type that never prints and wipes itself.

Every test runs against both versions (`naive`, `safe`). The three property tests in `tests/PropertyTests.cs` come from one requirement: nobody can read, change or replay a message, or get the key.

## Run it

On Windows, in PowerShell:

```powershell
dotnet test --filter "FullyQualifiedName~RoundTrip"   # 6 of 6 green: both versions "work"
dotnet test --filter "DisplayName~naive"              # 3 green, 3 red: the property tests
dotnet test --filter "DisplayName~safe"               # 6 of 6 green
.\break-nonce.ps1                                     # the safe version reuses its nonce: 1 red, then the file is restored
.\check.ps1                                           # the four checks; the tests fail, by design
```

On macOS and Linux, use `./break-nonce.sh` and `./check.sh`; the output is the same. If Windows refuses to run a script, start it with `powershell -ExecutionPolicy Bypass -File .\check.ps1`.

`check.ps1` and `check.sh` run four checks (format, NuGet audit, build with warnings as errors, tests) and stop at the first that fails. You can copy them into your own repository.

Measured on 28 October 2026 with .NET SDK 10.0.401 and xUnit 2.9.3. The naive version decrypts a message with one flipped bit as `…Bob $900.00` (it was `$100.00`) without an error.

## The AI review

`reviews/` holds the full answers of an AI reviewer, one fresh `claude -p` session per version, with the code comments removed first because they name the flaws. The prompt:

```text
You are reviewing in a fresh session. You have not seen how this code was written.

Question: can an attacker read, change or replay a message, or get the key?
A finding is: file and line, what an attacker can do, and the test that would prove it.
Report only findings you can back with a source (Microsoft Learn, OWASP, NIST).
Do not fix anything. If you find nothing, say "nothing found" and stop.

Review src/NaiveCipher.cs, then, in a new session, src/SafeCipher.cs.
```

On the naive version it found two of the three planted flaws and missed the key in a log line. On the safe version it found a real bug: after `Dispose`, the key's bytes were zeros and still used. `3-safe-after-fix.md` is the run after the fix. Both versions accept a captured message a second time (replay): it was in the requirement, and no test checks it.
