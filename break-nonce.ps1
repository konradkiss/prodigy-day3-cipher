# The same as break-nonce.sh: make the safe version reuse its nonce, run the tests, put the file back.
Set-Location $PSScriptRoot
$file = 'src/SafeCipher.cs'
$original = [IO.File]::ReadAllText("$PSScriptRoot/$file")
try {
    $broken = $original.Replace('var nonce = RandomNumberGenerator.GetBytes(NonceSize);', 'var nonce = new byte[NonceSize];        ')
    [IO.File]::WriteAllText("$PSScriptRoot/$file", $broken)
    Write-Host 'Changed:  var nonce = new byte[NonceSize];   (the same nonce for every message)'
    dotnet test --nologo -v q --filter 'DisplayName~safe' 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -match '\[FAIL\]|Passed!|Failed!' } |
        ForEach-Object { $_ -replace '.*\.([^.]+\(.*\)) \[FAIL\]$', 'Failed $1' }
}
finally {
    [IO.File]::WriteAllText("$PSScriptRoot/$file", $original)
    Write-Host "Restored: $file"
}
