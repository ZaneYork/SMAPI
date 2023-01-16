function In-Place-Regex {
    param (
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Search,
        [Parameter(Mandatory)][string]$Replace
    )

    $content = (Get-Content "$Path" -Encoding UTF8)
    $content = ($content -replace "$Search", "$Replace")
    [System.IO.File]::WriteAllLines((Get-Item "$Path").FullName, $content)
}
