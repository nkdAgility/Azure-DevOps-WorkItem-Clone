# Install Choco Apps
# Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
$installedStuff = choco list -i 
if (($installedStuff -like "*7zip*").Count -eq 0) {
   Write-Output "Installing 7zip"
   choco install 7zip --confirm --accept-license -y
} else { Write-Output "Detected 7zip"}
if (($installedStuff -like "*gh*").Count -eq 0) {
    Write-Output "Installing gh"
    choco install gh --confirm --accept-license -y
} else { Write-Output "Detected gh"}
if (($installedStuff -like "*GitVersion.Portable*").Count -eq 0) {
    Write-Output "Installing GitVersion"
    choco install gitversion.portable --confirm --accept-license -y
} else { Write-Output "Detected GitVersion"}

# Install DotNetApps
dotnet tool install --global GitVersion.Tool

# Refresh environment
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine")
# Make `refreshenv` available right away, by defining the $env:ChocolateyInstall
# variable and importing the Chocolatey profile module.
# Note: Using `. $PROFILE` instead *may* work, but isn't guaranteed to.
$env:ChocolateyInstall = Convert-Path "$((Get-Command choco).Path)\..\.."   
Import-Module "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"
Update-SessionEnvironment

# Get Version Numbers
$versionInfo = dotnet-gitversion | ConvertFrom-Json
Write-Output "Version: $($versionInfo.SemVer)"

# Build
$dotnetversion = where dotnet | dotnet --version
dotnet restore
dotnet build
dotnet test

$versionText = "v$($versionInfo.SemVer)";
$ZipName = "ABBWorkItemClone-$versionText.zip"
$ZipFilePath = ".\output\$ZipName"

# Create Zip
if (Get-Item -Path ".\output" -ErrorAction SilentlyContinue) {
    Remove-Item -Path ".\output\" -Recurse -Force
}
New-Item -Name "output" -ItemType Directory

7z a -tzip  $ZipFilePath ".\ABB.WorkItemClone.ConsoleUI\bin\Debug\net8.0\**"

# Publish
#gh release create $versionText .\output\$ZipName --generate-notes --generate-notes --prerelease --discussion-category "General"