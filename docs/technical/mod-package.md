&larr; [SMAPI](../README.md)

The **mod build package** is an open-source NuGet package which automates the MSBuild configuration
for SMAPI mods and related tools. The package is fully compatible with Linux, macOS, and Windows.

## Contents
* [Use](#use)
* [Features](#features)
* [Configure](#configure)
  * [How to set options](#how-to-set-options)
  * [Available properties](#available-properties)
* [Bundle content packs](#bundle-content-packs)
* [Code warnings](#code-warnings)
* [FAQs](#faqs)
  * [How do I set the game path?](#custom-game-path)
  * [How do I change which files are included in the mod deploy/zip?](#how-do-i-change-which-files-are-included-in-the-mod-deployzip)
  * [Can I use the package for non-mod projects?](#can-i-use-the-package-for-non-mod-projects)
* [For SMAPI developers](#for-smapi-developers)
* [Release notes](#release-notes)

## Use
1. Create an empty library project.
2. Reference the [`Pathoschild.Stardew.ModBuildConfig` NuGet package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig).
3. [Write your code](https://stardewvalleywiki.com/Modding:Creating_a_SMAPI_mod).
4. Compile on any platform.
5. Run the game to play with your mod.

## Features
The package includes several features to simplify mod development (see [_configure_](#configure) to
change how these work):

* **Detect game path:**  
  The package automatically finds your game folder by scanning the default install paths and
  Windows registry. It adds two MSBuild properties for use in your `.csproj` file if needed:
  `$(GamePath)` and `$(GameModsPath)`.

* **Add assembly references:**  
  The package adds assembly references to MonoGame, SMAPI, Stardew Valley, and xTile. It
  automatically adjusts depending on which OS you're compiling it on. If you use
  [Harmony](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony), it can optionally add
  a reference to that too.

* **Copy files into the `Mods` folder:**  
  The package automatically copies your mod's DLL and PDB files, `manifest.json`, [`i18n`
  files](https://stardewvalleywiki.com/Modding:Translations) (if any), and the `assets` folder (if
  any) into the `Mods` folder when you rebuild the code, with a subfolder matching the mod's project
  name. That lets you try the mod in-game right after building it.

* **Create release zip:**  
  The package adds a zip file in your project's `bin` folder when you rebuild the code, in the
  format recommended for uploading to mod sites like Nexus Mods. This includes the same files as
  the previous feature.

* **Launch or debug mod:**  
  On Windows only, the package configures Visual Studio so you can launch the game and attach a
  debugger using _Debug > Start Debugging_ or _Debug > Start Without Debugging_. This lets you [set
  breakpoints](https://docs.microsoft.com/en-us/visualstudio/debugger/using-breakpoints?view=vs-2019)
  in your code while the game is running, or [make simple changes to the mod code without needing to
  restart the game](https://docs.microsoft.com/en-us/visualstudio/debugger/edit-and-continue?view=vs-2019).
  This is disabled on Linux/macOS due to limitations with the Mono wrapper.

* **Preconfigure common settings:**  
  The package automatically enables `.pdb` files (so error logs show line numbers to simplify
  debugging), and enables support for the simplified SDK-style `.csproj` format.

* **Add code warnings:**  
  The package runs code analysis on your mod and raises warnings for some common errors or
  pitfalls. See [_code warnings_](#code-warnings) for more info.

## Configure
### How to set options
You can configure the package by setting build properties, which are essentially tags like this:
```xml
<PropertyGroup>
    <ModFolderName>CustomModName</ModFolderName>
    <EnableModDeploy>false</EnableModDeploy>
</PropertyGroup>
```

There are two places you can put them:

* **Global properties** apply to every mod project you open on your computer. That's recommended
  for properties you want to set for all mods (e.g. a custom game path). Here's where to put them:

  1. Open the home folder on your computer (see instructions for
     [Linux](https://superuser.com/questions/409218/where-is-my-users-home-folder-in-ubuntu),
     [macOS](https://www.cnet.com/how-to/how-to-find-your-macs-home-folder-and-add-it-to-finder/),
     or [Windows](https://www.computerhope.com/issues/ch000109.htm)).
  2. Create a `stardewvalley.targets` file with this content:
     ```xml
     <Project>
        <PropertyGroup>
        </PropertyGroup>
     </Project>
     ```
  3. Add the properties between the `<PropertyGroup>` and `</PropertyGroup>`.

* **Project properties** apply to a specific project. This is mainly useful for mod-specific
  options like the mod name. Here's where to put them:

  1. Open the folder containing your mod's source code.
  2. Open the `.csproj` file in a text editor (Notepad is fine).
  3. Add the properties between the first `<PropertyGroup>` and `</PropertyGroup>` tags you find.

**Note:** you can't use a property before it's defined. That mainly means that when setting
`GameModsPath`, you'll need to either specify `GamePath` manually or put the full path in
`GameModsPath`.

### Available properties
These are the options you can set.

#### Common properties
property       | effect
-------------- | ------
`GamePath`     | The absolute path to the Stardew Valley folder. This is auto-detected, so you usually don't need to change it.
`GameModsPath` | The absolute path to the folder containing the game's installed mods (defaults to `$(GamePath)/Mods`), used when deploying the mod files.

#### Mod build properties
property          | effect
----------------- | ------
`EnableHarmony`   | Whether to add a reference to [Harmony](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony) (default `false`). This is only needed if you use Harmony.
`EnableModDeploy` | Whether to copy the mod files into your game's `Mods` folder (default `true`).
`EnableModZip`    | Whether to create a release-ready `.zip` file in the mod project's `bin` folder (default `true`).
`ModFolderName`   | The mod name for its folder under `Mods` and its release zip (defaults to the project name).
`ModZipPath`      | The folder path where the release zip is created (defaults to the project's `bin` folder).

#### Specialized properties
These properties should usually be left as-is.

property                | effect
----------------------- | ------
`EnableGameDebugging`   | Whether to configure the project so you can launch or debug the game through the _Debug_ menu in Visual Studio (default `true`). There's usually no reason to change this, unless it's a unit test project.
`IgnoreModFilePaths`    | A comma-delimited list of literal file paths to ignore, relative to the mod's `bin` folder. Paths are case-sensitive, but path delimiters are normalized automatically. For example, this ignores a set of tilesheets: `<IgnoreModFilePaths>assets/paths.png, assets/springobjects.png</IgnoreModFilePaths>`.
`IgnoreModFilePatterns` | A comma-delimited list of regex patterns matching files to ignore when deploying or zipping the mod files (default empty). For crossplatform compatibility, you should replace path delimiters with `[/\\]`. For example, this excludes all `.txt` and `.pdf` files, as well as the `assets/paths.png` file: `<IgnoreModFilePatterns>\.txt$, \.pdf$, assets[/\\]paths.png</IgnoreModFilePatterns>`.

#### `BundleExtraAssemblies`
_(Specialized)_ **Most mods should not change this option.**

By default (when this is _not_ enabled), only the mod files [normally considered part of the
mod](#Features) will be added to the release `.zip` and copied into the `Mods` folder (i.e.
"deployed"). That includes the assembly files (`*.dll`, `*.pdb`, and `*.xml`) for your mod project,
but any other DLLs won't be deployed.

Enabling this option will add _all_ dependencies to the build output, then deploy _some_ of them
depending on the comma-separated value(s) you set:

option       | result
------------ | ------
`ThirdParty` | Assembly files which don't match any other category.
`System`     | Assembly files whose names start with `Microsoft.*` or `System.*`.
`Game`       | Assembly files which are part of MonoGame, SMAPI, or Stardew Valley.
`All`        | Equivalent to `System, Game, ThirdParty`.

Most mods should omit the option. Some mods may need `ThirdParty` if they bundle third-party DLLs
with their mod. The other options are mainly useful for unit tests.

When enabling this option, you should **manually review which files get deployed** and use the
`IgnoreModFilePaths` or `IgnoreModFilePatterns` options to exclude files as needed.

## Bundle content packs
You can bundle any number of [content packs](https://stardewvalleywiki.com/Modding:Content_pack_frameworks)
with your main C# mod. They'll be grouped with the main mod into a parent folder automatically,
which will be copied to the `Mods` folder and included in the release zip.

To do that, add an `ItemGroup` with a `ContentPacks` line for each content pack you want to include:

```xml
<ItemGroup>
    <ContentPacks Include="content packs/[CP] SomeContentPack" Version="$(Version)" />
    <ContentPacks Include="content packs/[CP] AnotherContentPack" Version="$(Version)" />
</ItemGroup>
```

You can use these properties for each line:

property                | effect
----------------------- | ------
`Include`               | _(Required)_ The path to the content pack folder. This can be an absolute path, or relative to the current project.
`Version`               | _(Required)_ The expected version of the content pack. This should usually be the same version as your main mod, to keep update alerts in sync. The package will validate that the included content pack's manifest version matches.
`FolderName`            | _(Optional)_ The content pack folder name to create. Defaults to the folder name from `Include`.
`ValidateManifest`      | _(Optional)_ Whether to validate that the included mod has a valid `manifest.json` file and version. Default `true`.
`IgnoreModFilePaths`    | _(Optional)_ A list of file paths to ignore (relative to the content pack's directory); see `IgnoreModFilePaths` in the main settings. Default none.
`IgnoreModFilePatterns` | _(Optional)_ A list of file regex patterns to ignore (relative to the content pack's directory); see `IgnoreModFilePatterns` in the main settings. Default none.

## Code warnings
### Overview
The NuGet package adds code warnings in Visual Studio specific to Stardew Valley. For example:  
![](screenshots/code-analyzer-example.png)

You can [hide the warnings](https://visualstudiomagazine.com/articles/2017/09/01/hide-compiler-warnings.aspx)
if needed using the warning ID (shown under 'code' in the Error List).

See below for help with specific warnings.

### Avoid net field
Warning text:
> '{{expression}}' is a {{net type}} field; consider using the {{property name}} property instead.

Your code accesses a net field, but the game has an equivalent non-net property.

Suggested fix: access the suggested property name instead.

### Avoid obsolete field
Warning text:
> The '{{old field}}' field is obsolete and should be replaced with '{{new field}}'.

Your code accesses a field which is obsolete or no longer works. Use the suggested field instead.

### Wrong processor architecture
Warning text:
> The target platform should be set to 'Any CPU' for compatibility with both 32-bit and 64-bit
> versions of Stardew Valley (currently set to '{{current platform}}').

Mods can be used in either 32-bit or 64-bit mode. Your project's target platform isn't set to the
default 'Any CPU', so it won't work in both. You can fix it by [setting the target platform to
'Any CPU'](https://docs.microsoft.com/en-ca/visualstudio/ide/how-to-configure-projects-to-target-platforms).

## FAQs
### How do I set the game path?<span id="custom-game-path"></span>
The package detects where your game is installed automatically, so you usually don't need to set it
manually. If it can't find your game or you have multiple installs, you can specify the path
yourself.

To do that:

1. Get the full folder path containing the Stardew Valley executable.
2. See [_configure_](#configure) to add this property:
   ```xml
   <PropertyGroup>
       <GamePath>PATH_HERE</GamePath>
   </PropertyGroup>
   ```
3. Replace `PATH_HERE` with your game's folder path (don't add quotes).

The configuration will check your custom path first, then fall back to the default paths (so it'll
still compile on a different computer).

### How do I change which files are included in the mod deploy/zip?
* For normal files, you can [add/remove them in the build output](https://stackoverflow.com/a/10828462/262123).
* For assembly files (`*.dll`, `*.exe`, `*.pdb`, or `*.xml`), see the
  [`BundleExtraAssemblies` option](#configure).
* To exclude a file which the package copies by default, see the [`IgnoreModFilePaths` or
  `IgnoreModFilePatterns` options](#configure).

### Can I use the package for non-mod projects?
Yep, this works in unit tests and framework projects too. Just disable the mod-related package
features (see [_configure_](#configure)):

```xml
<EnableGameDebugging>false</EnableGameDebugging>
<EnableModDeploy>false</EnableModDeploy>
<EnableModZip>false</EnableModZip>
```

To copy referenced DLLs into your build output for unit tests, add this too:
```xml
<BundleExtraAssemblies>All</BundleExtraAssemblies>
```

## For SMAPI developers
The mod build package consists of three projects:

project                                           | purpose
------------------------------------------------- | ----------------
`StardewModdingAPI.ModBuildConfig`                | Configures the build (references, deploying the mod files, setting up debugging, etc).
`StardewModdingAPI.ModBuildConfig.Analyzer`       | Adds C# analyzers which show code warnings in Visual Studio.
`StardewModdingAPI.ModBuildConfig.Analyzer.Tests` | Unit tests for the C# analyzers.

The NuGet package is generated automatically in `StardewModdingAPI.ModBuildConfig`'s `bin` folder
when you compile it.

## See also
* [Release notes](mod-package-release-notes.md)
