# Install Choco Apps
# Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
choco install 7zip --confirm --accept-license -y
choco install gh --confirm --accept-license -y

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine")

# Install DotNetApps
dotnet tool install --global GitVersion.Tool

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

# Create Zip
if (Get-Item -Path ".\output" -ErrorAction SilentlyContinue) {
    Remove-Item -Path ".\output\" -Recurse -Force
}
New-Item -Name "output" -ItemType Directory

7z a -tzip  $ZipName ".\ABB.WorkItemClone.ConsoleUI\bin\Debug\net8.0\**"

# Publish
gh release create $versionText .\output\$ZipName --generate-notes --generate-notes --prerelease --discussion-category "General"