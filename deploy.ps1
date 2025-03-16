# Build and Package Script for EMC Lab Station Reservation System
# This script builds the Svelte frontend (outputs to emc_api/wwwroot), 
# compiles the emc_api as self-contained Windows x64 application, and packages it to the desktop

# Set error action preference
$ErrorActionPreference = "Stop"

# Display start message
Write-Host "Starting build and package process..." -ForegroundColor Cyan

# Step 1: Build Svelte Frontend (will output to emc_api/wwwroot based on svelte.config.js)
Write-Host "Building Svelte frontend..." -ForegroundColor Green
Set-Location -Path $PSScriptRoot
pnpm install
pnpm build

# Check if frontend build was successful
if (-not $?) {
    Write-Host "Frontend build failed! Exiting script." -ForegroundColor Red
    exit 1
}
Write-Host "Frontend build completed successfully (output to emc_api/wwwroot)." -ForegroundColor Green

# Step 2: Build and publish emc_api as self-contained Windows x64 application
Write-Host "Building emc_api as self-contained Windows x64 application..." -ForegroundColor Green
Set-Location -Path "$PSScriptRoot\emc_api"

# Build .NET project as self-contained Windows x64 application
if (Test-Path -Path "*.csproj") {
    dotnet publish -c Release -r win-x64 --self-contained true -o "$PSScriptRoot\publish"
} else {
    Write-Host "Unable to find .NET project file (*.csproj). Build failed!" -ForegroundColor Red
    exit 1
}

# Check if API build was successful
if (-not $?) {
    Write-Host "API build failed! Exiting script." -ForegroundColor Red
    exit 1
}
Write-Host "API build completed successfully." -ForegroundColor Green

# Step 3: Package and copy to desktop
Set-Location -Path $PSScriptRoot

# Create timestamp for package name
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$packageName = "emc_system_$timestamp.zip"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$packagePath = "$desktopPath\$packageName"

# Copy README if exists
if (Test-Path -Path "README.md") {
    Copy-Item -Path "README.md" -Destination "$PSScriptRoot\publish\" -Force
}

# Make sure no processes are still holding onto files
Write-Host "Waiting for file handles to be released..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Kill any dotnet processes that might be holding files
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*$PSScriptRoot*" } | ForEach-Object { 
    Write-Host "Terminating process that might be locking files: $($_.Id)" -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue 
}

# Wait a moment for processes to fully terminate
Start-Sleep -Seconds 2

# Use robust ZIP creation method
Write-Host "Packaging to desktop as $packageName..." -ForegroundColor Green
try {
    # First method - Try standard PowerShell method
    Compress-Archive -Path "$PSScriptRoot\publish\*" -DestinationPath $packagePath -Force -ErrorAction Stop
} 
catch {
    Write-Host "Standard compression failed, trying alternative method..." -ForegroundColor Yellow
    
    # Alternative method using .NET's ZipFile class
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory("$PSScriptRoot\publish", $packagePath)
    
    if (-not $?) {
        # If that also fails, try 7zip if installed
        $7zipPath = "C:\Program Files\7-Zip\7z.exe"
        if (Test-Path $7zipPath) {
            Write-Host "Trying with 7-Zip..." -ForegroundColor Yellow
            & $7zipPath a -tzip $packagePath "$PSScriptRoot\publish\*" -r
        } else {
            Write-Host "Alternative compression methods failed. Consider installing 7-Zip or manually zipping the files." -ForegroundColor Red
            # Don't delete the publish folder in this case so manual zipping is possible
            exit 1
        }
    }
}

# Verify package creation
if (Test-Path -Path $packagePath) {
    Write-Host "Package created successfully at: $packagePath" -ForegroundColor Green
    
    # Clean up publish directory
    Remove-Item -Path "$PSScriptRoot\publish" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "Build and package process completed!" -ForegroundColor Cyan
} else {
    Write-Host "Package creation failed! The publish directory remains at: $PSScriptRoot\publish" -ForegroundColor Red
    Write-Host "Please manually zip the contents of this directory." -ForegroundColor Yellow
    exit 1
} 