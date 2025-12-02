# Plugin Development Guide

This guide provides comprehensive instructions for developing plugins for Arcana Windows. The **FlowChartModule** (`plugins/FlowChartModule/`) serves as the reference implementation.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Plugin Architecture](#plugin-architecture)
- [Project Structure](#project-structure)
- [Step-by-Step Development](#step-by-step-development)
  - [Step 1: Create Project](#step-1-create-project)
  - [Step 2: Create Plugin Class](#step-2-create-plugin-class)
  - [Step 3: Create NavGraph](#step-3-create-navgraph)
  - [Step 4: Create ViewModel (MVVM UDF)](#step-4-create-viewmodel-mvvm-udf)
  - [Step 5: Create Views](#step-5-create-views)
  - [Step 6: Create Localization](#step-6-create-localization)
  - [Step 7: Create Plugin Manifest](#step-7-create-plugin-manifest)
- [Build and Package](#build-and-package)
- [Installation](#installation)
- [Testing](#testing)
- [Best Practices](#best-practices)
- [API Reference](#api-reference)

---

## Quick Start

```powershell
# Clone the FlowChartModule as a template
cd plugins
Copy-Item -Recurse FlowChartModule MyNewPlugin

# Rename files and update namespaces
# Then build and install
cd MyNewPlugin
.\build.ps1
.\install.ps1
```

---

## Plugin Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      ARCANA APP HOST                            │
├─────────────────────────────────────────────────────────────────┤
│  IPluginContext                                                 │
│  ├── NavGraph (INavGraph)    → Type-safe navigation            │
│  ├── Navigation              → Low-level navigation service    │
│  ├── Commands                → Command registration            │
│  ├── Menus                   → Menu registration               │
│  ├── Views                   → View registration               │
│  ├── Events                  → Event aggregator                │
│  ├── MessageBus              → Cross-plugin messaging          │
│  ├── SharedState             → Shared state store              │
│  ├── Localization            → i18n service                    │
│  └── Logger                  → Plugin-specific logging         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        YOUR PLUGIN                              │
├─────────────────────────────────────────────────────────────────┤
│  MyPlugin : PluginBase                                          │
│  ├── Metadata                → Plugin info (id, version, etc.) │
│  ├── OnActivateAsync()       → Initialize NavGraph, load i18n  │
│  ├── RegisterContributions() → Register views, menus, commands │
│  └── Nav (MyPluginNavGraph)  → Type-safe navigation wrapper    │
├─────────────────────────────────────────────────────────────────┤
│  MyPluginNavGraph                                               │
│  ├── Routes (constants)      → View ID constants               │
│  ├── ToXxx() methods         → Type-safe navigation actions    │
│  └── Args records            → Navigation parameters           │
├─────────────────────────────────────────────────────────────────┤
│  MyViewModel : ReactiveViewModelBase                            │
│  ├── In (Input)              → User actions                    │
│  ├── Out (Output)            → Read-only state for binding     │
│  └── Fx (Effect)             → Side effects (dialogs, nav)     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

Reference: `plugins/FlowChartModule/`

```
plugins/
└── FlowChartModule/
    ├── Arcana.Plugin.FlowChart.csproj    # Project file
    ├── FlowChartPlugin.cs                 # Main plugin class
    ├── plugin.json                        # Plugin manifest
    │
    ├── Navigation/
    │   └── FlowChartNavGraph.cs          # Type-safe navigation
    │
    ├── ViewModels/
    │   └── FlowChartEditorViewModel.cs   # MVVM UDF pattern
    │
    ├── Views/
    │   ├── FlowChartEditorPage.xaml      # WinUI 3 XAML
    │   └── FlowChartEditorPage.xaml.cs   # Code-behind
    │
    ├── Models/
    │   ├── Diagram.cs                     # Domain models
    │   ├── DiagramNode.cs
    │   └── DiagramEdge.cs
    │
    ├── Services/
    │   └── DiagramSerializer.cs          # Business logic
    │
    ├── Controls/
    │   └── FlowChartCanvas.cs            # Custom controls
    │
    ├── locales/
    │   ├── en-US.json                    # English
    │   ├── zh-TW.json                    # Traditional Chinese
    │   └── ja-JP.json                    # Japanese
    │
    ├── Tests/
    │   ├── Arcana.Plugin.FlowChart.Tests.csproj
    │   ├── DiagramTests.cs
    │   └── DiagramSerializerTests.cs
    │
    ├── build.ps1                          # Build & package script
    ├── install.ps1                        # Development install script
    └── README.md                          # Plugin documentation
```

---

## Step-by-Step Development

### Step 1: Create Project

Create a new class library project:

```xml
<!-- MyPlugin.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <Platforms>x86;x64;ARM64</Platforms>
    <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>14.0</LangVersion>
    <UseWinUI>true</UseWinUI>
    <EnableMsixTooling>false</EnableMsixTooling>
    <WindowsSdkPackageVersion>10.0.19041.48</WindowsSdkPackageVersion>
    <OutputType>Library</OutputType>

    <!-- Plugin Info -->
    <AssemblyName>Arcana.Plugin.MyPlugin</AssemblyName>
    <RootNamespace>Arcana.Plugin.MyPlugin</RootNamespace>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240227000" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1742" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference Arcana plugin contracts -->
    <ProjectReference Include="..\..\src\Arcana.Plugins.Contracts\Arcana.Plugins.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Arcana.Plugins\Arcana.Plugins.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Include locales and manifest in output -->
    <Content Include="locales\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

### Step 2: Create Plugin Class

Reference: `plugins/FlowChartModule/FlowChartPlugin.cs`

```csharp
using Arcana.Plugin.MyPlugin.Navigation;
using Arcana.Plugin.MyPlugin.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;

namespace Arcana.Plugin.MyPlugin;

public class MyPlugin : PluginBase
{
    private MyPluginNavGraph? _nav;

    /// <summary>
    /// Type-safe navigation for this plugin.
    /// </summary>
    public MyPluginNavGraph Nav => _nav ?? throw new InvalidOperationException("Plugin not activated");

    public override PluginMetadata Metadata => new()
    {
        Id = "mycompany.plugin.myplugin",        // Unique plugin ID
        Name = "My Plugin",                       // Display name
        Version = new Version(1, 0, 0),          // Semantic version
        Description = "My awesome plugin",        // Description
        Type = PluginType.Module,                // Plugin type
        Author = "My Company"                     // Author
    };

    protected override async Task OnActivateAsync(IPluginContext context)
    {
        // Step 1: Initialize type-safe NavGraph
        _nav = new MyPluginNavGraph(context.NavGraph);

        // Step 2: Load localization from external JSON files
        var localesPath = Path.Combine(context.PluginPath, "locales");
        await LoadExternalLocalizationAsync(localesPath);
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "MyMainPage",
            Title = L("myplugin.page.title"),      // L() for localized string
            TitleKey = "myplugin.page.title",      // Key for dynamic title updates
            Icon = "\uE8A5",                       // Segoe MDL2 icon
            Type = ViewType.Page,
            ViewClass = typeof(MyMainPage),
            Category = L("menu.tools")
        });

        // Register menu items
        RegisterMenuItems(
            // Parent menu
            new MenuItemDefinition
            {
                Id = "menu.tools.myplugin",
                Title = L("myplugin.menu.title"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.tools",
                Icon = "\uE8A5",
                Order = 20
            },
            // Child menu with command
            new MenuItemDefinition
            {
                Id = "menu.tools.myplugin.open",
                Title = L("myplugin.action.open"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.tools.myplugin",
                Icon = "\uE8E5",
                Order = 1,
                Command = "myplugin.open"
            },
            // Function tree entry
            new MenuItemDefinition
            {
                Id = "tree.myplugin",
                Title = L("myplugin.menu.title"),
                Location = MenuLocation.FunctionTree,
                Icon = "\uE8A5",
                Order = 60,
                Command = "myplugin.open"
            },
            // Quick access
            new MenuItemDefinition
            {
                Id = "quick.myplugin",
                Title = L("myplugin.action.open"),
                Location = MenuLocation.QuickAccess,
                Icon = "\uE8A5",
                Order = 20,
                Group = "tools",
                Command = "myplugin.open"
            }
        );

        // Register commands with type-safe navigation
        RegisterCommand("myplugin.open", async () =>
        {
            await Nav.ToMainPage();  // Type-safe navigation!
        });

        RegisterCommand("myplugin.openWithFile", async (string filePath) =>
        {
            await Nav.ToMainPage(new MyPluginNavGraph.PageArgs(filePath));
        });

        LogInfo("My plugin activated");
    }
}
```

### Step 3: Create NavGraph

Reference: `plugins/FlowChartModule/Navigation/FlowChartNavGraph.cs`

```csharp
using Arcana.Plugins.Contracts;

namespace Arcana.Plugin.MyPlugin.Navigation;

/// <summary>
/// Type-safe navigation graph for MyPlugin.
/// Wraps INavGraph to provide strongly-typed navigation methods.
/// </summary>
public sealed class MyPluginNavGraph
{
    private readonly INavGraph _nav;

    public MyPluginNavGraph(INavGraph nav) => _nav = nav;

    // ================================================================
    // Routes - View ID constants
    // ================================================================

    public static class Routes
    {
        public const string MainPage = "MyMainPage";
        public const string SettingsPage = "MySettingsPage";
        public const string DetailPage = "MyDetailPage";
    }

    // ================================================================
    // Type-Safe Navigation Methods
    // ================================================================

    /// <summary>
    /// Navigate to main page (new tab).
    /// </summary>
    public Task<bool> ToMainPage()
        => _nav.ToNewTab(Routes.MainPage);

    /// <summary>
    /// Navigate to main page with parameters (new tab).
    /// </summary>
    public Task<bool> ToMainPage(PageArgs args)
        => _nav.ToNewTab(Routes.MainPage, args);

    /// <summary>
    /// Navigate to settings page (new tab).
    /// </summary>
    public Task<bool> ToSettings()
        => _nav.ToNewTab(Routes.SettingsPage);

    /// <summary>
    /// Navigate to detail page within current tab.
    /// </summary>
    public Task<bool> ToDetail(int itemId)
        => _nav.To(Routes.DetailPage, itemId);

    /// <summary>
    /// Show dialog and return result.
    /// </summary>
    public Task<bool?> ShowConfirmDialog(string message)
        => _nav.ShowDialog<bool?>("ConfirmDialog", message);

    // ================================================================
    // Common Navigation (delegated)
    // ================================================================

    public Task<bool> Back() => _nav.Back();
    public Task<bool> Forward() => _nav.Forward();
    public Task Close() => _nav.Close();

    /// <summary>
    /// Cross-plugin navigation (use sparingly).
    /// </summary>
    public Task<bool> ToOrderDetail(int orderId)
        => _nav.ToNewTab("OrderDetailPage", orderId);

    // ================================================================
    // Navigation Arguments
    // ================================================================

    public record PageArgs(string? FilePath = null, bool ReadOnly = false);
    public record DetailArgs(int ItemId, string? Title = null);
}
```

### Step 4: Create ViewModel (MVVM UDF)

Reference: `plugins/FlowChartModule/ViewModels/FlowChartEditorViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using Arcana.Plugins.Contracts.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.Plugin.MyPlugin.ViewModels;

public partial class MyMainPageViewModel : ReactiveViewModelBase
{
    // ================================================================
    // Private State (Observable Properties)
    // ================================================================

    [ObservableProperty]
    private ObservableCollection<ItemModel> _items = new();

    [ObservableProperty]
    private ItemModel? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    // ================================================================
    // Input/Output/Effect (UDF Pattern)
    // ================================================================

    private Input? _input;
    private Output? _output;
    private Effect? _effect;

    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    // ================================================================
    // Lifecycle
    // ================================================================

    public override async Task InitializeAsync()
    {
        await LoadItemsAsync();
    }

    public override void Cleanup()
    {
        _effect?.Dispose();
        base.Cleanup();
    }

    // ================================================================
    // Private Actions
    // ================================================================

    private async Task LoadItemsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Simulate loading data
            await Task.Delay(500);

            Items.Clear();
            Items.Add(new ItemModel("1", "Item 1"));
            Items.Add(new ItemModel("2", "Item 2"));
            Items.Add(new ItemModel("3", "Item 3"));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Fx.ShowError.Emit(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveItemAsync(ItemModel item)
    {
        try
        {
            IsLoading = true;
            // Save logic...
            await Task.Delay(200);
            Fx.ShowSuccess.Emit("Item saved successfully");
        }
        catch (Exception ex)
        {
            Fx.ShowError.Emit($"Failed to save: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteItemAsync(ItemModel item)
    {
        Items.Remove(item);
        if (SelectedItem == item)
            SelectedItem = null;
        Fx.ShowSuccess.Emit("Item deleted");
    }

    private void Search(string query)
    {
        SearchQuery = query;
        // Filter logic...
    }

    // ================================================================
    // Input Class - User Actions
    // ================================================================

    #region Input
    public sealed class Input : IViewModelInput
    {
        private readonly MyMainPageViewModel _vm;
        internal Input(MyMainPageViewModel vm) => _vm = vm;

        /// <summary>Load all items.</summary>
        public Task LoadItems() => _vm.LoadItemsAsync();

        /// <summary>Save an item.</summary>
        public Task SaveItem(ItemModel item) => _vm.SaveItemAsync(item);

        /// <summary>Delete an item.</summary>
        public Task DeleteItem(ItemModel item) => _vm.DeleteItemAsync(item);

        /// <summary>Select an item.</summary>
        public void SelectItem(ItemModel? item) => _vm.SelectedItem = item;

        /// <summary>Search items.</summary>
        public void Search(string query) => _vm.Search(query);

        /// <summary>Clear error.</summary>
        public void ClearError() => _vm.ErrorMessage = null;
    }
    #endregion

    // ================================================================
    // Output Class - Read-Only State for Binding
    // ================================================================

    #region Output
    public sealed class Output : IViewModelOutput
    {
        private readonly MyMainPageViewModel _vm;
        internal Output(MyMainPageViewModel vm) => _vm = vm;

        /// <summary>Collection of items.</summary>
        public ObservableCollection<ItemModel> Items => _vm.Items;

        /// <summary>Currently selected item.</summary>
        public ItemModel? SelectedItem => _vm.SelectedItem;

        /// <summary>Loading indicator.</summary>
        public bool IsLoading => _vm.IsLoading;

        /// <summary>Whether there's an error.</summary>
        public bool HasError => !string.IsNullOrEmpty(_vm.ErrorMessage);

        /// <summary>Error message if any.</summary>
        public string? ErrorMessage => _vm.ErrorMessage;

        /// <summary>Current search query.</summary>
        public string SearchQuery => _vm.SearchQuery;

        /// <summary>Whether any items exist.</summary>
        public bool HasItems => _vm.Items.Count > 0;
    }
    #endregion

    // ================================================================
    // Effect Class - Side Effects
    // ================================================================

    #region Effect
    public sealed class Effect : IViewModelEffect, IDisposable
    {
        /// <summary>Show error message.</summary>
        public EffectSubject<string> ShowError { get; } = new();

        /// <summary>Show success message.</summary>
        public EffectSubject<string> ShowSuccess { get; } = new();

        /// <summary>Navigate to item detail.</summary>
        public EffectSubject<ItemModel> NavigateToDetail { get; } = new();

        /// <summary>Request file open dialog.</summary>
        public EffectSubject RequestFileOpen { get; } = new();

        public void Dispose()
        {
            ShowError.Dispose();
            ShowSuccess.Dispose();
            NavigateToDetail.Dispose();
            RequestFileOpen.Dispose();
        }
    }
    #endregion
}

// ================================================================
// Models
// ================================================================

public record ItemModel(string Id, string Name);
```

### Step 5: Create Views

**XAML (Views/MyMainPage.xaml):**

```xml
<Page
    x:Class="Arcana.Plugin.MyPlugin.Views.MyMainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Loaded="OnLoaded"
    Unloaded="OnUnloaded">

    <Grid Padding="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8" Margin="0,0,0,16">
            <Button Content="Refresh" Click="OnRefreshClick"/>
            <Button Content="Add" Click="OnAddClick"/>
            <TextBox x:Name="SearchBox"
                     PlaceholderText="Search..."
                     Width="200"
                     TextChanged="OnSearchTextChanged"/>
        </StackPanel>

        <!-- Content -->
        <Grid Grid.Row="1">
            <!-- Loading overlay -->
            <ProgressRing IsActive="{x:Bind _vm.Out.IsLoading, Mode=OneWay}"
                          HorizontalAlignment="Center"
                          VerticalAlignment="Center"/>

            <!-- Error message -->
            <InfoBar x:Name="ErrorBar"
                     IsOpen="{x:Bind _vm.Out.HasError, Mode=OneWay}"
                     Severity="Error"
                     Title="Error"
                     Message="{x:Bind _vm.Out.ErrorMessage, Mode=OneWay}"/>

            <!-- Item list -->
            <ListView ItemsSource="{x:Bind _vm.Out.Items, Mode=OneWay}"
                      SelectedItem="{x:Bind _vm.Out.SelectedItem, Mode=TwoWay}"
                      SelectionChanged="OnSelectionChanged">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Name}"/>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Grid>
    </Grid>
</Page>
```

**Code-behind (Views/MyMainPage.xaml.cs):**

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arcana.Plugin.MyPlugin.ViewModels;

namespace Arcana.Plugin.MyPlugin.Views;

public sealed partial class MyMainPage : Page
{
    private readonly MyMainPageViewModel _vm;
    private readonly List<IDisposable> _subscriptions = new();

    public MyMainPage()
    {
        InitializeComponent();

        // Get ViewModel from DI or create directly
        _vm = new MyMainPageViewModel();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to effects
        _subscriptions.Add(_vm.Fx.ShowError.Subscribe(ShowErrorDialog));
        _subscriptions.Add(_vm.Fx.ShowSuccess.Subscribe(ShowSuccessNotification));
        _subscriptions.Add(_vm.Fx.NavigateToDetail.Subscribe(NavigateToDetail));

        // Initialize ViewModel
        await _vm.InitializeAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Cleanup subscriptions
        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();

        _vm.Cleanup();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await _vm.In.LoadItems();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        // Add item logic
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.In.Search(SearchBox.Text);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is ItemModel item)
            _vm.In.SelectItem(item);
    }

    // Effect handlers
    private async void ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void ShowSuccessNotification(string message)
    {
        // Show notification (e.g., InfoBar or TeachingTip)
    }

    private void NavigateToDetail(ItemModel item)
    {
        // Navigate to detail page
    }
}
```

### Step 6: Create Localization

**locales/en-US.json:**

```json
{
  "myplugin.page.title": "My Plugin",
  "myplugin.menu.title": "My Plugin",
  "myplugin.action.open": "Open",
  "myplugin.action.save": "Save",
  "myplugin.action.delete": "Delete",
  "myplugin.status.saved": "Saved successfully",
  "myplugin.status.deleted": "Deleted successfully",
  "myplugin.error.loadFailed": "Failed to load data"
}
```

**locales/zh-TW.json:**

```json
{
  "myplugin.page.title": "我的插件",
  "myplugin.menu.title": "我的插件",
  "myplugin.action.open": "開啟",
  "myplugin.action.save": "儲存",
  "myplugin.action.delete": "刪除",
  "myplugin.status.saved": "儲存成功",
  "myplugin.status.deleted": "刪除成功",
  "myplugin.error.loadFailed": "載入資料失敗"
}
```

**locales/ja-JP.json:**

```json
{
  "myplugin.page.title": "マイプラグイン",
  "myplugin.menu.title": "マイプラグイン",
  "myplugin.action.open": "開く",
  "myplugin.action.save": "保存",
  "myplugin.action.delete": "削除",
  "myplugin.status.saved": "保存しました",
  "myplugin.status.deleted": "削除しました",
  "myplugin.error.loadFailed": "データの読み込みに失敗しました"
}
```

### Step 7: Create Plugin Manifest

Reference: `plugins/FlowChartModule/plugin.json`

```json
{
  "id": "mycompany.plugin.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "My awesome plugin description",
  "author": "My Company",
  "main": "Arcana.Plugin.MyPlugin.dll",
  "pluginClass": "Arcana.Plugin.MyPlugin.MyPlugin",
  "type": "module",
  "activationEvents": [
    "onCommand:myplugin.open",
    "onView:MyMainPage"
  ],
  "contributes": {
    "views": [
      {
        "id": "MyMainPage",
        "titleKey": "myplugin.page.title",
        "title": "My Plugin",
        "icon": "\uE8A5",
        "type": "page",
        "viewClass": "Arcana.Plugin.MyPlugin.Views.MyMainPage"
      }
    ],
    "menus": [
      {
        "id": "menu.tools.myplugin",
        "title": "My Plugin",
        "location": "mainMenu",
        "parentId": "menu.tools",
        "icon": "\uE8A5",
        "order": 20
      },
      {
        "id": "menu.tools.myplugin.open",
        "title": "Open",
        "location": "mainMenu",
        "parentId": "menu.tools.myplugin",
        "icon": "\uE8E5",
        "order": 1,
        "command": "myplugin.open"
      },
      {
        "id": "tree.myplugin",
        "title": "My Plugin",
        "location": "functionTree",
        "icon": "\uE8A5",
        "order": 60,
        "command": "myplugin.open"
      }
    ],
    "commands": [
      {
        "id": "myplugin.open",
        "title": "Open My Plugin"
      }
    ]
  },
  "locales": {
    "en-US": "locales/en-US.json",
    "zh-TW": "locales/zh-TW.json",
    "ja-JP": "locales/ja-JP.json"
  }
}
```

---

## Build and Package

### Build Script

Reference: `plugins/FlowChartModule/build.ps1`

```powershell
# build.ps1 - Build and package plugin
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir "Arcana.Plugin.MyPlugin.csproj"
$OutputDir = Join-Path $ScriptDir "bin\$Platform\$Configuration\net10.0-windows10.0.19041.0"
$PackageDir = Join-Path $ScriptDir "package"
$PluginName = "MyPlugin"

Write-Host "=== Building $PluginName ===" -ForegroundColor Cyan

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning..." -ForegroundColor Yellow
    if (Test-Path (Join-Path $ScriptDir "bin")) { Remove-Item -Recurse -Force (Join-Path $ScriptDir "bin") }
    if (Test-Path (Join-Path $ScriptDir "obj")) { Remove-Item -Recurse -Force (Join-Path $ScriptDir "obj") }
    if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
}

# Build
Write-Host "Building plugin..." -ForegroundColor Green
dotnet build $ProjectFile -c $Configuration -p:Platform=$Platform

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create package
Write-Host "Creating package..." -ForegroundColor Green
if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
New-Item -ItemType Directory -Path $PackageDir | Out-Null

$PluginDir = Join-Path $PackageDir $PluginName
New-Item -ItemType Directory -Path $PluginDir | Out-Null

# Copy files
Copy-Item (Join-Path $OutputDir "Arcana.Plugin.MyPlugin.dll") $PluginDir
Copy-Item (Join-Path $ScriptDir "plugin.json") $PluginDir

# Copy locales
$LocalesDir = Join-Path $PluginDir "locales"
New-Item -ItemType Directory -Path $LocalesDir | Out-Null
Copy-Item (Join-Path $ScriptDir "locales\*.json") $LocalesDir

# Create zip
$ZipPath = Join-Path $ScriptDir "$PluginName-v1.0.0-$Platform.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path "$PluginDir\*" -DestinationPath $ZipPath

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host "Package: $ZipPath" -ForegroundColor Yellow
```

### Run Build

```powershell
cd plugins/MyPlugin

# Build release package
.\build.ps1

# Build with clean
.\build.ps1 -Clean

# Build debug
.\build.ps1 -Configuration Debug
```

---

## Installation

### Development Installation

Reference: `plugins/FlowChartModule/install.ps1`

```powershell
# install.ps1 - Install plugin for development
param(
    [string]$Configuration = "Debug",
    [string]$Platform = "x64",
    [string]$TargetAppPath = "",
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$PluginName = "mycompany.plugin.myplugin"  # Must match plugin.json id
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Installing Plugin ===" -ForegroundColor Cyan

# Default target path
if ([string]::IsNullOrEmpty($TargetAppPath)) {
    $TargetAppPath = Join-Path $ScriptDir "..\..\src\Arcana.App\bin\$Platform\$Configuration\net10.0-windows10.0.19041.0\plugins\$PluginName"
}

# Run tests (optional)
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Green
    $TestProject = Join-Path $ScriptDir "Tests\Arcana.Plugin.MyPlugin.Tests.csproj"
    if (Test-Path $TestProject) {
        dotnet test $TestProject -c $Configuration --no-build
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed!" -ForegroundColor Red
            exit 1
        }
    }
}

# Build
if (-not $SkipBuild) {
    Write-Host "Building..." -ForegroundColor Green
    $ProjectFile = Join-Path $ScriptDir "Arcana.Plugin.MyPlugin.csproj"
    dotnet build $ProjectFile -c $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Copy files
Write-Host "Installing to: $TargetAppPath" -ForegroundColor Green
if (-not (Test-Path $TargetAppPath)) {
    New-Item -ItemType Directory -Path $TargetAppPath -Force | Out-Null
}

$SourceDir = Join-Path $ScriptDir "bin\$Platform\$Configuration\net10.0-windows10.0.19041.0"

Copy-Item (Join-Path $SourceDir "Arcana.Plugin.MyPlugin.dll") $TargetAppPath -Force
Copy-Item (Join-Path $SourceDir "Arcana.Plugin.MyPlugin.pdb") $TargetAppPath -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $SourceDir "plugin.json") $TargetAppPath -Force

$LocalesDest = Join-Path $TargetAppPath "locales"
if (-not (Test-Path $LocalesDest)) { New-Item -ItemType Directory -Path $LocalesDest -Force | Out-Null }
Copy-Item (Join-Path $SourceDir "locales\*") $LocalesDest -Force

Write-Host "=== Installation Complete ===" -ForegroundColor Cyan
Write-Host "Restart Arcana to load the plugin." -ForegroundColor Yellow
```

### Run Install

```powershell
cd plugins/MyPlugin

# Install for development (Debug)
.\install.ps1

# Install without tests
.\install.ps1 -SkipTests

# Install to custom path
.\install.ps1 -TargetAppPath "C:\MyApp\plugins\myplugin"

# Force overwrite
.\install.ps1 -Force
```

### User Installation (from ZIP)

```powershell
# Extract plugin zip to plugins folder
Expand-Archive -Path "MyPlugin-v1.0.0-x64.zip" -DestinationPath "$env:LOCALAPPDATA\Arcana\plugins\mycompany.plugin.myplugin"

# Or use the app's Plugin Manager UI
```

---

## Testing

### Test Project Structure

```
Tests/
├── Arcana.Plugin.MyPlugin.Tests.csproj
├── ViewModelTests.cs
├── ServiceTests.cs
└── IntegrationTests.cs
```

### Test Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Arcana.Plugin.MyPlugin.csproj" />
  </ItemGroup>
</Project>
```

### Example Tests

```csharp
using FluentAssertions;
using Xunit;

namespace Arcana.Plugin.MyPlugin.Tests;

public class MyMainPageViewModelTests
{
    [Fact]
    public async Task LoadItems_ShouldPopulateItems()
    {
        // Arrange
        var vm = new MyMainPageViewModel();

        // Act
        await vm.In.LoadItems();

        // Assert
        vm.Out.Items.Should().NotBeEmpty();
        vm.Out.IsLoading.Should().BeFalse();
        vm.Out.HasError.Should().BeFalse();
    }

    [Fact]
    public void SelectItem_ShouldUpdateSelectedItem()
    {
        // Arrange
        var vm = new MyMainPageViewModel();
        var item = new ItemModel("1", "Test");

        // Act
        vm.In.SelectItem(item);

        // Assert
        vm.Out.SelectedItem.Should().Be(item);
    }
}
```

---

## Best Practices

### Do's

1. **Use Type-Safe Navigation** - Always create a NavGraph wrapper
2. **Follow MVVM UDF Pattern** - Separate Input/Output/Effect
3. **Externalize Strings** - Use locales/*.json for all user-facing text
4. **Dispose Effects** - Clean up subscriptions in Cleanup()
5. **Handle Errors** - Use Effect.ShowError for user feedback
6. **Write Tests** - Cover ViewModels and Services

### Don'ts

1. **Don't Use Magic Strings** - Use Routes constants
2. **Don't Mutate Output** - Output is read-only, use Input for actions
3. **Don't Block UI** - Use async/await for all I/O operations
4. **Don't Hardcode Paths** - Use context.PluginPath for plugin resources
5. **Don't Ignore Localization** - Support multiple languages from the start

---

## API Reference

### IPluginContext

```csharp
public interface IPluginContext
{
    string PluginId { get; }
    string PluginPath { get; }
    string DataPath { get; }
    ILogger Logger { get; }
    INavGraph NavGraph { get; }               // Type-safe navigation
    INavigationService Navigation { get; }    // Low-level navigation
    ICommandService Commands { get; }
    IMenuRegistry Menus { get; }
    IViewRegistry Views { get; }
    IEventAggregator Events { get; }
    IMessageBus MessageBus { get; }
    ISharedStateStore SharedState { get; }
    ILocalizationService Localization { get; }
    IList<IDisposable> Subscriptions { get; }
    T? GetService<T>() where T : class;
}
```

### INavGraph

```csharp
public interface INavGraph
{
    Task<bool> To(string routeId, object? parameter = null);
    Task<bool> ToNewTab(string routeId, object? parameter = null);
    Task<bool> ToWithinTab(string parentRouteId, string routeId, object? parameter = null);
    Task<bool> Back();
    Task<bool> Forward();
    Task Close();
    Task<TResult?> ShowDialog<TResult>(string routeId, object? parameter = null);
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    string? CurrentRouteId { get; }
}
```

### PluginBase

```csharp
public abstract class PluginBase : IPlugin
{
    protected IPluginContext? Context { get; }

    // Override these
    public abstract PluginMetadata Metadata { get; }
    protected virtual Task OnActivateAsync(IPluginContext context);
    protected virtual void RegisterContributions(IPluginContext context);

    // Helper methods
    protected string L(string key);  // Localization
    protected void LogInfo(string message);
    protected void LogError(string message, Exception? ex = null);
    protected Task LoadExternalLocalizationAsync(string path);
    protected void RegisterView(ViewDefinition view);
    protected void RegisterMenuItems(params MenuItemDefinition[] items);
    protected void RegisterCommand(string id, Func<Task> handler);
    protected void RegisterCommand<T>(string id, Func<T, Task> handler);
}
```

---

## Related Documentation

- [VIEWMODEL_PATTERN.md](VIEWMODEL_PATTERN.md) - MVVM UDF Pattern Details
- [README.md](../README.md) - Main Project Documentation

---

## Sample Plugin

The **FlowChartModule** (`plugins/FlowChartModule/`) is a complete reference implementation demonstrating all patterns:

```powershell
# Build FlowChart plugin
cd plugins/FlowChartModule
.\build.ps1

# Install for development
.\install.ps1

# Package created at:
# plugins/FlowChartModule/FlowChartPlugin-v1.0.0-x64.zip
```
