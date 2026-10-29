#!/usr/bin/env bash
# The check we have seen fail: make the safe version reuse its nonce, run the tests, put the file back.
set -u
cd "$(dirname "$0")"
bak=$(mktemp); cp src/SafeCipher.cs "$bak"
trap 'cp "$bak" src/SafeCipher.cs' EXIT
sed -i.tmp 's/var nonce = RandomNumberGenerator.GetBytes(NonceSize);/var nonce = new byte[NonceSize];        /' src/SafeCipher.cs && rm -f src/SafeCipher.cs.tmp
echo "Changed:  var nonce = new byte[NonceSize];   (the same nonce for every message)"
dotnet test --nologo -v q --filter "DisplayName~safe" 2>&1 | grep -E "\[FAIL\]|Passed!|Failed!" | sed -E 's/.*\.([^.]+\(.*\)) \[FAIL\]$/Failed \1/'
echo "Restored: src/SafeCipher.cs"
