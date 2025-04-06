#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
  dotnet publish ./tools/Build/Build.csproj --output ./tools/bin/Build --nologo --verbosity quiet
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  dotnet ./tools/bin/Build/Build.dll $args
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
  Pop-Location
}
