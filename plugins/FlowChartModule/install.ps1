# FlowChart Plugin Install Script
# This script builds and installs the plugin to the Arcana app's Plugins folder

param(
    [string]$Configuration = "Debug",  # Changed default to Debug for development
    [string]$Platform = "x64",
    [string]$TargetAppPath = "",
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$Force,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$PluginName = "arcana.plugin.flowchart"  # Must match plugin ID in plugin.json
$PluginVersion = "1.0.0"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== FlowChart Plugin Installer ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host ""

# Clean only mode
if ($Clean) {
    Write-Host "Cleaning build outputs..." -ForegroundColor Yellow

    $PluginProject = Join-Path $ScriptDir "Arcana.Plugin.FlowChart.csproj"
    dotnet clean $PluginProject -c $Configuration -p:Platform=$Platform --verbosity minimal

    # Also clean bin and obj folders
    $BinDir = Join-Path $ScriptDir "bin"
    $ObjDir = Join-Path $ScriptDir "obj"

    if (Test-Path $BinDir) {
        Remove-Item -Recurse -Force $BinDir
        Write-Host "  Removed: bin\" -ForegroundColor Gray
    }
    if (Test-Path $ObjDir) {
        Remove-Item -Recurse -Force $ObjDir
        Write-Host "  Removed: obj\" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "=== Clean Complete ===" -ForegroundColor Cyan
    exit 0
}

# Determine target path
if ([string]::IsNullOrEmpty($TargetAppPath)) {
    # Default to the main app's output directory
    # Note: folder name is 'plugins' (lowercase) to match app's expected path
    $TargetAppPath = Join-Path $ScriptDir "..\..\src\Arcana.App\bin\$Platform\$Configuration\net10.0-windows10.0.19041.0\plugins\$PluginName"
}

Write-Host "Target Path: $TargetAppPath" -ForegroundColor Yellow
Write-Host ""

# Step 1: Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "Step 1: Running unit tests..." -ForegroundColor Green
    $TestProject = Join-Path $ScriptDir "Tests\Arcana.Plugin.FlowChart.Tests.csproj"

    if (Test-Path $TestProject) {
        # Build test project first, then run tests
        dotnet build $TestProject -c $Configuration --verbosity minimal
        if ($LASTEXITCODE -eq 0) {
            dotnet test $TestProject --configuration $Configuration --no-build --verbosity minimal
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Tests failed! Use -SkipTests to skip testing." -ForegroundColor Red
                exit 1
            }
            Write-Host "All tests passed!" -ForegroundColor Green
        } else {
            Write-Host "Test project build failed, skipping tests." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Test project not found, skipping tests." -ForegroundColor Yellow
    }
} else {
    Write-Host "Step 1: Skipping tests" -ForegroundColor Yellow
}

# Step 2: Build the plugin (unless skipped)
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Step 2: Building plugin..." -ForegroundColor Green
    $PluginProject = Join-Path $ScriptDir "Arcana.Plugin.FlowChart.csproj"

    dotnet build $PluginProject -c $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build succeeded!" -ForegroundColor Green
} else {
    Write-Host "Step 2: Skipping build" -ForegroundColor Yellow
}

# Step 3: Copy files to target
Write-Host ""
Write-Host "Step 3: Installing plugin to: $TargetAppPath" -ForegroundColor Green

# Create target directory
if (-not (Test-Path $TargetAppPath)) {
    New-Item -ItemType Directory -Path $TargetAppPath -Force | Out-Null
    Write-Host "Created directory: $TargetAppPath"
} elseif (-not $Force) {
    Write-Host "Target directory exists. Use -Force to overwrite." -ForegroundColor Yellow
    $confirm = Read-Host "Continue and overwrite? (y/N)"
    if ($confirm -ne "y") {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Source directory
$SourceDir = Join-Path $ScriptDir "bin\$Platform\$Configuration\net10.0-windows10.0.19041.0"

if (-not (Test-Path $SourceDir)) {
    Write-Host "Build output not found at: $SourceDir" -ForegroundColor Red
    Write-Host "Please build the project first." -ForegroundColor Red
    exit 1
}

# Copy plugin DLL
$PluginDll = Join-Path $SourceDir "Arcana.Plugin.FlowChart.dll"
if (Test-Path $PluginDll) {
    Copy-Item $PluginDll $TargetAppPath -Force
    Write-Host "  Copied: Arcana.Plugin.FlowChart.dll"
}

# Copy PDB for debugging
$PluginPdb = Join-Path $SourceDir "Arcana.Plugin.FlowChart.pdb"
if (Test-Path $PluginPdb) {
    Copy-Item $PluginPdb $TargetAppPath -Force
    Write-Host "  Copied: Arcana.Plugin.FlowChart.pdb"
}

# Copy plugin.json
$PluginJson = Join-Path $SourceDir "plugin.json"
if (Test-Path $PluginJson) {
    Copy-Item $PluginJson $TargetAppPath -Force
    Write-Host "  Copied: plugin.json"
}

# Copy locales
$LocalesSource = Join-Path $SourceDir "locales"
if (Test-Path $LocalesSource) {
    $LocalesDest = Join-Path $TargetAppPath "locales"
    if (-not (Test-Path $LocalesDest)) {
        New-Item -ItemType Directory -Path $LocalesDest -Force | Out-Null
    }
    Copy-Item "$LocalesSource\*" $LocalesDest -Force
    Write-Host "  Copied: locales\"
}

# Copy Views (XAML files)
$ViewsSource = Join-Path $SourceDir "Views"
if (Test-Path $ViewsSource) {
    $ViewsDest = Join-Path $TargetAppPath "Views"
    if (-not (Test-Path $ViewsDest)) {
        New-Item -ItemType Directory -Path $ViewsDest -Force | Out-Null
    }
    Copy-Item "$ViewsSource\*" $ViewsDest -Force
    Write-Host "  Copied: Views\"
}

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Cyan
Write-Host "Plugin installed to: $TargetAppPath" -ForegroundColor Green
Write-Host ""
Write-Host "Restart the Arcana app to load the new plugin." -ForegroundColor Yellow
