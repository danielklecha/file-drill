Push-Location "$PSScriptRoot\.."

# Run dotnet list package to get outdated package versions
$jsonOutput = dotnet list package --include-transitive --outdated --format json
$parsedJson = $jsonOutput | ConvertFrom-Json

# Initialize an empty dictionary
$packageVersions = @{}

# Iterate over each project
foreach ($project in $parsedJson.projects) {
    foreach ($framework in $project.frameworks) {
        foreach ($package in $framework.topLevelPackages + $framework.transitivePackages) {
            if (-not $packageVersions.ContainsKey($package.id)) {
                $packageVersions[$package.id] = [PSCustomObject]@{
                    resolvedVersion = $package.resolvedVersion
                    latestVersion   = if ($package.latestVersion -match "(?i)not found") { "" } else { $package.latestVersion }
                    isUpdated       = $false
                }
            }
        }
    }
}

# Check if any package has an empty latestVersion
if ($packageVersions.Values.Where({ $_.latestVersion -eq "" }).Count -gt 0) {
    $jsonOutput = dotnet list package --include-transitive --outdated --include-prerelease --format json
    $parsedJson = $jsonOutput | ConvertFrom-Json
    # Update only packages with empty latestVersion
    foreach ($project in $parsedJson.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in $framework.topLevelPackages + $framework.transitivePackages) {
                if ($packageVersions.ContainsKey($package.id) -and $packageVersions[$package.id].latestVersion -eq "") {
                    $packageVersions[$package.id].latestVersion = $package.latestVersion
                }
            }
        }
    }
}

# Determine the script's directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Path to the XML file in the parent directory
$xmlFilePath = Join-Path -Path (Split-Path -Parent $scriptDir) -ChildPath "Directory.Packages.props"

# Load the XML file
[xml]$xml = Get-Content $xmlFilePath

# Iterate over the dictionary to update or add PackageVersion elements
foreach ($packageId in $packageVersions.Keys) {
    $found = $false
    # Update the existing package version
    foreach ($package in $xml.Project.ItemGroup.PackageVersion) {
        if ($package.Include -eq $packageId) {
            $found = $true
			$version = if($packageVersions[$packageId].latestVersion -eq "") { $packageVersions[$packageId].resolvedVersion } else { $packageVersions[$packageId].latestVersion }			
			if($package.Version -ne $version) {
				$package.Version = $version
				$packageVersions[$packageId].isUpdated = $true	
			}
            break
        }
    }
    
    # Add a new package version if it wasn't found
    if (-not $found) {
        $newPackage = $xml.CreateElement("PackageVersion")
        $newPackage.SetAttribute("Include", $packageId)
        $newPackage.SetAttribute("Version", $packageVersions[$packageId].latestVersion)
        $xml.Project.ItemGroup.AppendChild($newPackage) | Out-Null
        $packageVersions[$packageId].isUpdated = $true
    }
}

# Save the modified XML file
$xml.Save($xmlFilePath)

# Output the dictionary as a table
$packageVersions.GetEnumerator() | ForEach-Object {
    [PSCustomObject]@{
        "Package ID"      = $_.Key
        "Resolved Version" = $_.Value.resolvedVersion
        "Latest Version"   = $_.Value.latestVersion
        "Is Updated"       = $_.Value.isUpdated
    }
} | Format-Table -AutoSize

Pop-Location