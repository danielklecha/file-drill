Push-Location
Set-Location "$PSScriptRoot\.."
try {
	dotnet build --configuration Release FileDrill.sln
	dotnet pack --no-build --configuration Release FileDrill.sln
	dotnet tool update --global --add-source .\FileDrill\bin\Release --no-cache FileDrill
	Read-Host -Prompt "Press Enter to exit..."
}
finally {
	Pop-Location
}
	