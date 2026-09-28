# Addon Uploader

A Windows desktop app for packaging WoW addons and uploading releases to CurseForge and Wago.

Created with OpenAI Codex and Visual Studio Code.

## Run the app

Launch `AddonUploader.exe`. Project settings are stored in `%LOCALAPPDATA%\AddonUploader\projects.json`. CurseForge and Wago API tokens are global across all projects and stored separately in `global.json`. Tokens are encrypted with Windows DPAPI and tied to your Windows account. Tokens saved in older project settings are migrated when the app loads them.

## Set up a project

1. Choose the addon source folder containing its `.toc` file and addon files.
2. Enter the addon name and its CurseForge and/or Wago project ID.
3. Enter the API token for each platform you want to use. Tokens are shared by all projects.
4. For Wago, enter one supported patch version for each game flavor you want to target: Retail, MoP, Cataclysm, Wrath, Burning Crusade Classic (BCC), Classic Forever, and Classic Era. Leave unused flavors blank. Enter one patch number per field; do not use comma-separated values.
5. Choose the release type, enter a changelog, select the upload platforms, and click **Create ZIP and Upload**.

## Versioning and packages

**Bump Version** increments the last numeric component and preserves leading zeroes, for example `1.2.9` to `1.2.10` and `1.06` to `1.07`.

The app creates a ZIP with all addon files inside one root folder named after the selected source folder. It writes the selected version to each `.toc` file in the ZIP as `## Version: <version>`. After all selected uploads succeed, it updates the source `.toc` files to the same version and deletes the temporary ZIP. If an upload fails, the ZIP is kept for troubleshooting.

Temporary ZIP files are created under `%LOCALAPPDATA%\AddonUploader`.

## Source code and build

The C# source code is in `SourceCode`. Run `Build.ps1` from PowerShell to publish a self-contained Windows x64 executable. The script uses the bundled SDK in `.sdk\dotnet` when present, or falls back to a system-installed `dotnet` SDK. If you need to install the SDK, download the Windows x64 .NET 9 SDK from the official [Microsoft .NET 9 download page](https://dotnet.microsoft.com/en-us/download/dotnet/9.0). The build output is copied to `AddonUploader.exe` in the project folder. `CreateIcon.ps1` regenerates the multi-size `AddonUploader.ico`, which is embedded in the executable.

Uploads use the official [CurseForge Upload API](https://support.curseforge.com/support/solutions/articles/9000197321) and [Wago Addons API](https://docs.wago.io/).
