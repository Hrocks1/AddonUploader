$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $root 'SourceCode\AddonUploader.csproj'
$localDotnet = Join-Path $root '.sdk\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& $dotnet publish $source -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $root 'Publish')
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item (Join-Path $root 'Publish\AddonUploader.exe') (Join-Path $root 'AddonUploader.exe') -Force
Write-Host "Created: $(Join-Path $root 'AddonUploader.exe')"
