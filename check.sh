#!/usr/bin/env bash
set -euo pipefail

# 1. Style: the formatter decides, not the reviewer
dotnet format --verify-no-changes

# 2. Packages: a package with a known vulnerability stops the build (NuGet audit)
dotnet restore -warnaserror:NU1901,NU1902,NU1903,NU1904

# 3. Analyzers: a warning is an error
dotnet build --no-restore -warnaserror

# 4. Tests: unit and integration
dotnet test --no-build
