# FlowChart Plugin Development Script
# Quick commands for plugin development workflow

param(
    [Parameter(Position = 0)]
    [ValidateSet("build", "test", "run", "package", "clean", "help")]
    [string]$Command = "help",

    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Show-Help {
    Write-Host ""
    Write-Host "FlowChart Plugin Development Commands" -ForegroundColor Cyan
    Write-Host "======================================"
    Write-Host ""
    Write-Host "Usage: .\dev.ps1 <command> [-Configuration <config>] [-Platform <platform>]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  build    - Build the plugin project"
    Write-Host "  test     - Run unit tests"
    Write-Host "  run      - Build and run the TestHost app"
    Write-Host "  package  - Create a plugin package (zip)"
    Write-Host "  clean    - Clean build outputs"
    Write-Host "  help     - Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\dev.ps1 build                    # Build Debug|x64"
    Write-Host "  .\dev.ps1 build -Configuration Release"
    Write-Host "  .\dev.ps1 test"
    Write-Host "  .\dev.ps1 run                      # Launch TestHost"
    Write-Host "  .\dev.ps1 package -Configuration Release"
    Write-Host ""
}

function Invoke-Build {
    Write-Host "Building plugin ($Configuration|$Platform)..." -ForegroundColor Green
    $PluginProject = Join-Path $ScriptDir "Arcana.Plugin.FlowChart.csproj"
    dotnet build $PluginProject -c $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build succeeded!" -ForegroundColor Green
}

function Invoke-Test {
    Write-Host "Running tests..." -ForegroundColor Green
    $TestProject = Join-Path $ScriptDir "Tests\Arcana.Plugin.FlowChart.Tests.csproj"

    if (-not (Test-Path $TestProject)) {
        Write-Host "Test project not found." -ForegroundColor Yellow
        return
    }

    # Build tests first
    dotnet build $TestProject -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Test build failed!" -ForegroundColor Red
        exit 1
    }

    # Run tests
    dotnet test $TestProject -c $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Some tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "All tests passed!" -ForegroundColor Green
}

function Invoke-Run {
    Write-Host "Building and launching TestHost..." -ForegroundColor Green
    $TestHostProject = Join-Path $ScriptDir "TestHost\Arcana.Plugin.FlowChart.TestHost.csproj"

    if (-not (Test-Path $TestHostProject)) {
        Write-Host "TestHost project not found. Please create the TestHost project first." -ForegroundColor Red
        exit 1
    }

    # Build TestHost (which also builds the plugin)
    dotnet build $TestHostProject -c $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }

    # Run TestHost
    $ExePath = Join-Path $ScriptDir "TestHost\bin\$Platform\$Configuration\net10.0-windows10.0.19041.0\Arcana.Plugin.FlowChart.TestHost.exe"
    if (Test-Path $ExePath) {
        Write-Host "Launching TestHost..." -ForegroundColor Cyan
        Start-Process $ExePath
    } else {
        Write-Host "TestHost executable not found at: $ExePath" -ForegroundColor Red
        exit 1
    }
}

function Invoke-Package {
    Write-Host "Creating plugin package..." -ForegroundColor Green

    # Build first
    Invoke-Build

    # Run the existing build.ps1 script if it exists
    $BuildScript = Join-Path $ScriptDir "build.ps1"
    if (Test-Path $BuildScript) {
        & $BuildScript -Configuration $Configuration -Platform $Platform
    } else {
        # Manual packaging
        $SourceDir = Join-Path $ScriptDir "bin\$Platform\$Configuration\net10.0-windows10.0.19041.0"
        $PackageDir = Join-Path $ScriptDir "package"
        $Version = "1.0.0"
        $ZipName = "FlowChartPlugin-v$Version-$Platform.zip"

        # Clean package directory
        if (Test-Path $PackageDir) {
            Remove-Item $PackageDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null

        # Copy files to package
        $PluginDir = Join-Path $PackageDir "FlowChartPlugin"
        New-Item -ItemType Directory -Path $PluginDir -Force | Out-Null

        Copy-Item (Join-Path $SourceDir "Arcana.Plugin.FlowChart.dll") $PluginDir
        Copy-Item (Join-Path $SourceDir "plugin.json") $PluginDir

        $LocalesSource = Join-Path $SourceDir "locales"
        if (Test-Path $LocalesSource) {
            Copy-Item $LocalesSource $PluginDir -Recurse
        }

        $ViewsSource = Join-Path $SourceDir "Views"
        if (Test-Path $ViewsSource) {
            Copy-Item $ViewsSource $PluginDir -Recurse
        }

        # Create zip
        $ZipPath = Join-Path $ScriptDir $ZipName
        if (Test-Path $ZipPath) {
            Remove-Item $ZipPath -Force
        }
        Compress-Archive -Path "$PluginDir\*" -DestinationPath $ZipPath

        Write-Host "Package created: $ZipPath" -ForegroundColor Green
    }
}

function Invoke-Clean {
    Write-Host "Cleaning build outputs..." -ForegroundColor Green

    $DirsToClean = @(
        (Join-Path $ScriptDir "bin"),
        (Join-Path $ScriptDir "obj"),
        (Join-Path $ScriptDir "package"),
        (Join-Path $ScriptDir "TestHost\bin"),
        (Join-Path $ScriptDir "TestHost\obj"),
        (Join-Path $ScriptDir "Tests\bin"),
        (Join-Path $ScriptDir "Tests\obj")
    )

    foreach ($dir in $DirsToClean) {
        if (Test-Path $dir) {
            Remove-Item $dir -Recurse -Force
            Write-Host "  Removed: $dir"
        }
    }

    Write-Host "Clean complete!" -ForegroundColor Green
}

# Execute command
switch ($Command) {
    "build" { Invoke-Build }
    "test" { Invoke-Test }
    "run" { Invoke-Run }
    "package" { Invoke-Package }
    "clean" { Invoke-Clean }
    default { Show-Help }
}
