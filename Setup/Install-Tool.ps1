Push-Location
Set-Location "$PSScriptRoot\.."

try {
    Write-Host "Starting the build process..." -ForegroundColor Cyan
    dotnet build --configuration Release FileDrill.sln

    Write-Host "Packing the tool..." -ForegroundColor Cyan
    dotnet pack --no-build --configuration Release FileDrill.sln

    Write-Host "Installing the tool globally..." -ForegroundColor Cyan
    dotnet tool update --global --add-source .\FileDrill\bin\Release --no-cache file-drill

    Write-Host "Installation complete!" -ForegroundColor Green
    Read-Host -Prompt "Press Enter to exit..."
}
finally {
    Pop-Location
}
