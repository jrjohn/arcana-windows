# FlowChart Plugin Build Script
# This script builds the plugin and creates a distributable zip package

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir "Arcana.Plugin.FlowChart.csproj"
$OutputDir = Join-Path $ScriptDir "bin\$Platform\$Configuration\net10.0-windows10.0.19041.0"
$PackageDir = Join-Path $ScriptDir "package"
$PluginName = "FlowChartPlugin"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FlowChart Plugin Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    $binDir = Join-Path $ScriptDir "bin"
    $objDir = Join-Path $ScriptDir "obj"
    if (Test-Path $binDir) { Remove-Item -Recurse -Force $binDir }
    if (Test-Path $objDir) { Remove-Item -Recurse -Force $objDir }
    if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
}

# Fix .NET 10 SDK missing PRI Tasks DLL issue
Write-Host "Checking PRI Tasks DLL..." -ForegroundColor Gray
$sdkVersion = "18"
$targetDir = "C:\Program Files\Microsoft Visual Studio\$sdkVersion\Community\MSBuild\Microsoft\VisualStudio\v18.0\AppxPackage"
$targetDll = Join-Path $targetDir "Microsoft.Build.Packaging.Pri.Tasks.dll"

if (-not (Test-Path $targetDll)) {
    # Try to find the DLL in Visual Studio installation
    $vsPaths = @(
        "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\v18.0\AppxPackage",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Microsoft\VisualStudio\v18.0\AppxPackage",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Microsoft\VisualStudio\v18.0\AppxPackage",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Microsoft\VisualStudio\v18.0\AppxPackage"
    )

    $sourceDll = $null
    foreach ($vsPath in $vsPaths) {
        $dllPath = Join-Path $vsPath "Microsoft.Build.Packaging.Pri.Tasks.dll"
        if (Test-Path $dllPath) {
            $sourceDll = $dllPath
            Write-Host "Found PRI Tasks DLL: $sourceDll" -ForegroundColor Gray
            break
        }
    }

    if ($sourceDll) {
        Write-Host "Copying PRI Tasks DLL to .NET SDK folder..." -ForegroundColor Yellow
        try {
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }
            Copy-Item $sourceDll $targetDir -Force
            Write-Host "PRI Tasks DLL copied successfully." -ForegroundColor Green
        } catch {
            Write-Host "Warning: Could not copy DLL (run as Administrator): $_" -ForegroundColor Yellow
            Write-Host "Trying to build anyway..." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Warning: PRI Tasks DLL not found in Visual Studio." -ForegroundColor Yellow
    }
} else {
    Write-Host "PRI Tasks DLL already exists." -ForegroundColor Gray
}

# Build the project
Write-Host ""
Write-Host "Building plugin..." -ForegroundColor Green
dotnet build $ProjectFile `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:GeneratePriFile=false `
    -p:MrtCoreGeneratePriFileEnabled=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build succeeded!" -ForegroundColor Green
Write-Host ""

# Create package directory
Write-Host "Creating package..." -ForegroundColor Green
if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
New-Item -ItemType Directory -Path $PackageDir | Out-Null

$PluginDir = Join-Path $PackageDir $PluginName
New-Item -ItemType Directory -Path $PluginDir | Out-Null

# Copy required files
Write-Host "Copying files..."

# Copy main DLL
Copy-Item (Join-Path $OutputDir "Arcana.Plugin.FlowChart.dll") $PluginDir

# Copy plugin.json
Copy-Item (Join-Path $ScriptDir "plugin.json") $PluginDir

# Copy locales
$LocalesDir = Join-Path $PluginDir "locales"
New-Item -ItemType Directory -Path $LocalesDir | Out-Null
Copy-Item (Join-Path $ScriptDir "locales\*.json") $LocalesDir

# Copy dependencies (if any, excluding framework assemblies)
$DepsJson = Join-Path $OutputDir "Arcana.Plugin.FlowChart.deps.json"
if (Test-Path $DepsJson) {
    Copy-Item $DepsJson $PluginDir
}

# Copy required NuGet package DLLs
Write-Host "Copying dependencies..."
$dependencies = @(
    "CommunityToolkit.Mvvm.dll",
    "CommunityToolkit.WinUI.UI.Controls.dll",
    "CommunityToolkit.WinUI.UI.Controls.Core.dll",
    "CommunityToolkit.WinUI.UI.Controls.Primitives.dll"
)

foreach ($dep in $dependencies) {
    $depPath = Join-Path $OutputDir $dep
    if (Test-Path $depPath) {
        Copy-Item $depPath $PluginDir
        Write-Host "  Copied: $dep" -ForegroundColor Gray
    }
}

# Create zip package
Write-Host "Creating zip package..." -ForegroundColor Green
$ZipPath = Join-Path $ScriptDir "$PluginName-v1.0.0-$Platform.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath }

# Zip the contents directly (not the folder itself)
Compress-Archive -Path "$PluginDir\*" -DestinationPath $ZipPath

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Plugin package created at:"
Write-Host "  $ZipPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "To install, extract the zip to:"
Write-Host "  %LOCALAPPDATA%\Arcana\plugins\" -ForegroundColor Yellow
Write-Host ""
