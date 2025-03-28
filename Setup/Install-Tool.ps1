Push-Location
Set-Location "$PSScriptRoot\.."

try {
    Write-Host "Starting the restore process..." -ForegroundColor Cyan
    dotnet restore --configfile .\NuGet.Config FileDrill.sln

    Write-Host "Starting the build process..." -ForegroundColor Cyan
    dotnet build --no-restore --configuration Release FileDrill.sln

    Write-Host "Packing the tool..." -ForegroundColor Cyan
    dotnet pack --no-restore --no-build --configuration Release FileDrill.sln

    Write-Host "Installing the tool globally..." -ForegroundColor Cyan
    dotnet tool update --global --no-cache --configfile .\Setup\NuGet.Config file-drill

    Write-Host "Installation complete!" -ForegroundColor Green
}
finally {
    Pop-Location
    Read-Host -Prompt "Press Enter to exit..."
}
