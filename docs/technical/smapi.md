&larr; [README](../README.md)

This file provides more technical documentation about SMAPI. If you only want to use or create
mods, this section isn't relevant to you; see the main README to use or create mods.

This document is about SMAPI itself; see also [mod build package](mod-package.md) and
[web services](web.md).

# Contents
* [Customisation](#customisation)
  * [Configuration file](#configuration-file)
  * [Command-line arguments](#command-line-arguments)
  * [Compile flags](#compile-flags)
* [Compile from source code](#compile-from-source-code)
  * [Main project](#main-project)
  * [Custom Harmony build](#custom-harmony-build)
* [Prepare a release](#prepare-a-release)
  * [On any platform](#on-any-platform)
  * [On Windows](#on-windows)
* [Release notes](#release-notes)

## Customisation
### Configuration file
You can customise some SMAPI behaviour by editing the `smapi-internal/config.json` file in your
game folder. See documentation in the file for more info.

### Command-line arguments
The SMAPI installer recognises three command-line arguments:

argument | purpose
-------- | -------
`--install` | Preselects the install action, skipping the prompt asking what the user wants to do.
`--uninstall` | Preselects the uninstall action, skipping the prompt asking what the user wants to do.
`--game-path "path"` | Specifies the full path to the folder containing the Stardew Valley executable, skipping automatic detection and any prompt to choose a path. If the path is not valid, the installer displays an error.
`--no-prompt` | Don't let the installer wait for user input (e.g. for cases where it's being run by a script). If the installer is unable to continue without user input, it'll fail instead.

SMAPI itself recognises five arguments, but these are meant for internal use or testing, and might
change without warning. **On Linux/macOS**, command-line arguments won't work; see _environment
variables_ below instead.

argument | purpose
-------- | -------
`--developer-mode`<br />`--developer-mode-off` | Enable or disable features intended for mod developers. Currently this only makes `TRACE`-level messages appear in the console.
`--no-terminal` | SMAPI won't log anything to the console. On Linux/macOS only, this will also prevent the launch script from trying to open a terminal window. (Messages will still be written to the log file.)
`--prefer-terminal-name` | On Linux/macOS only, the terminal with which to open the SMAPI console. For example, `--prefer-terminal-name=xterm` to use xterm regardless of which terminal is the default one.
`--use-current-shell` | On Linux/macOS only, the launch script won't try to open a terminal window. All console output will be sent to the shell running the launch script.
`--mods-path` | The path to search for mods, if not the standard `Mods` folder. This can be a path relative to the game folder (like `--mods-path "Mods (test)"`) or an absolute path.

### Environment variables
The above SMAPI arguments may not work on Linux/macOS due to the way the game launcher works. You
can set temporary environment variables instead. For example:
> SMAPI_MODS_PATH="Mods (multiplayer)" /path/to/StardewValley

environment variable | purpose
-------------------- | -------
`SMAPI_DEVELOPER_MODE` | Equivalent to `--developer-mode` and `--developer-mode-off` above. The value must be `true` or `false`.
`SMAPI_MODS_PATH` | Equivalent to `--mods-path` above.
`SMAPI_NO_TERMINAL` | Equivalent to `--no-terminal` above.
`$SMAPI_PREFER_TERMINAL_NAME` | Equivalent to `--prefer-terminal-name` above.
`SMAPI_USE_CURRENT_SHELL` | Equivalent to `--use-current-shell` above.

### Compile flags
SMAPI uses a small number of conditional compilation constants, which you can set by editing the
`<DefineConstants>` element in `SMAPI.csproj`. Supported constants:

flag | purpose
---- | -------
`SMAPI_FOR_WINDOWS` | Whether SMAPI is being compiled for Windows; if not set, the code assumes Linux/macOS. Set automatically in `common.targets`.

## Compile from source code
### Main project
Using an official SMAPI release is recommended for most users, but you can compile from source
directly if needed. Just open the project in an IDE like [Visual
Studio](https://visualstudio.microsoft.com/vs/community/) or [Rider](https://www.jetbrains.com/rider/),
and build the `SMAPI` project. The project will automatically adjust the build settings for your
current OS and Stardew Valley install path.

Rebuilding the solution in debug mode will copy the SMAPI files into your game folder. Starting
the `SMAPI` project with debugging from Visual Studio or Rider should launch SMAPI with the
debugger attached, so you can intercept errors and step through the code being executed.

### Custom Harmony build
SMAPI uses [a custom build of Harmony 2.2.2](https://github.com/Pathoschild/Harmony#readme), which
is included in the `build` folder. To use a different build, just replace `0Harmony.dll` in that
folder before compiling.

## Prepare a release
### On any platform
_This is the unified process that works on any platform. However, it needs a few extra steps on
Windows (e.g. running Steam in WSL); see ['On Windows'](#on-windows) below for an alternative quick
option._

#### First-time setup
1. On Windows only:
   1. [Install Windows Subsystem for Linux (WSL)](https://docs.microsoft.com/en-us/windows/wsl/install).
   2. Run `sudo apt update` in WSL to update the package list.
   3. The rest of the instructions below should be run in WSL.
2. Install the required software:
   1. Install the [.NET 5 SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu).  
      _For Ubuntu-based systems, you can run `lsb_release -a` to get the Ubuntu version number._
   2. [Install Steam](https://linuxconfig.org/how-to-install-steam-on-ubuntu-20-04-focal-fossa-linux).
   3. Launch `steam` and install the game like usual.
   4. Download and install your preferred IDE. For the [latest standalone Rider
      version](https://www.jetbrains.com/help/rider/Installation_guide.html#prerequisites):
      ```sh
      wget "<download url here>" -O rider-install.tar.gz
      sudo tar -xzvf rider-install.tar.gz -C /opt
      ln -s "/opt/JetBrains Rider-<version>/bin/rider.sh"
      ./rider.sh
      ```
3. Clone the SMAPI repo:
   ```sh
   git clone https://github.com/Pathoschild/SMAPI.git
   ```

### Launch the game
1. Run these commands to start Steam:
   ```sh
   export TERM=xterm
   steam
   ```
2. Launch the game through the Steam UI.

### Prepare the release
1. Run `build/unix/prepare-install-package.sh VERSION_HERE` to create the release package in the
   root `bin` folder.

   Make sure you use a [semantic version](https://semver.org). Recommended format:

   build type | format                   | example
   :--------- | :----------------------- | :------
   dev build  | `<version>-alpha.<date>` | `4.0.0-alpha.20251230`
   prerelease | `<version>-beta.<date>`  | `4.0.0-beta.20251230`
   release    | `<version>`              | `4.0.0`

### On Windows
_This is the alternative quick process for Windows only. This avoids needing Steam installed on WSL,
and can be used to create Windows-only builds without using WSL at all. See ['on any platform'](#on-any-platform)
above for the unified process._

#### First-time setup
1. Set up Windows Subsystem for Linux (WSL):
   1. [Install WSL](https://docs.microsoft.com/en-us/windows/wsl/install).
   2. Run `sudo apt update` in WSL to update the package list.
   3. The rest of the instructions below should be run in WSL.
2. Install the required software:
   1. Install the [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0).
   2. Install [Stardew Valley](https://www.stardewvalley.net/).
3. Clone the SMAPI repo:
   ```sh
   git clone https://github.com/Pathoschild/SMAPI.git
   ```

### Prepare the release
1. Run `build/windows/prepare-install-package.ps1 VERSION_HERE` in PowerShell to create the release
   package folders in the root `bin` folder.

   Make sure you use a [semantic version](https://semver.org). Recommended format:

   build type | format                   | example
   :--------- | :----------------------- | :------
   dev build  | `<version>-alpha.<date>` | `4.0.0-alpha.20251230`
   prerelease | `<version>-beta.<date>`  | `4.0.0-beta.20251230`
   release    | `<version>`              | `4.0.0`

2. Launch WSL and run this script:
   ```bash
   # edit to match the build created in steps 1
   # In WSL, `/mnt/c/example` accesses `C:\example` on the Windows filesystem.
   version="4.0.0"
   binFolder="/mnt/e/source/_Stardew/SMAPI/bin"
   build/windows/finalize-install-package.sh "$version" "$binFolder"
   ```

Note: to prepare a test Windows-only build, you can pass `--windows-only` in the first step and
skip the second one.

## Release notes
See [release notes](../release-notes.md).
