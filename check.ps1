# The four checks of check.sh, for Windows PowerShell 5.1 and PowerShell 7.
# A failing dotnet command does not stop PowerShell, so each step checks its exit code.
$steps =
    { dotnet format --verify-no-changes },                        # 1. style
    { dotnet restore -warnaserror:NU1901,NU1902,NU1903,NU1904 },  # 2. packages
    { dotnet build --no-restore -warnaserror },                   # 3. analyzers
    { dotnet test --no-build }                                    # 4. tests
foreach ($step in $steps) {
    & $step
    if ($LASTEXITCODE -ne 0) { Write-Host "Failed: $step"; exit $LASTEXITCODE }
}
