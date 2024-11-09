#
#
# This is the PowerShell equivalent of ../unix/set-smapi-version.sh.
# When making changes, both scripts should be updated.
#
#


. "$PSScriptRoot\lib\in-place-regex.ps1"

# get version number
$version=$args[0]
if (!$version) {
    $version = Read-Host "SMAPI release version (like '4.0.0')"
}

# move to SMAPI root
cd "$PSScriptRoot/../.."

# apply changes
In-Place-Regex -Path "build/common.targets" -Search "<Version>.+</Version>" -Replace "<Version>$version</Version>"
In-Place-Regex -Path "src/SMAPI/Constants.cs" -Search "RawApiVersion = `".+?`";" -Replace "RawApiVersion = `"$version`";"
ForEach ($modName in "ConsoleCommands","SaveBackup") {
    In-Place-Regex -Path "src/SMAPI.Mods.$modName/manifest.json" -Search "`"(Version|MinimumApiVersion)`": `".+?`"" -Replace "`"`$1`": `"$version`""
}
