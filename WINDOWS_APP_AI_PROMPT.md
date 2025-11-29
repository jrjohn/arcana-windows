# Windows App Development AI Prompt

## Project: Arcana Windows

A Windows desktop application built with C# and WinUI3, following Clean Architecture with plugin-based extensibility, **Local-First data management**, and enterprise-grade patterns.

---

## Core Design Philosophy: Local-First Architecture (本地優先架構)

This application is designed with a **Local-First** approach, where:

### Primary: Local Data Store (主要：本地資料存儲)
- **SQLite database is the single source of truth** for all application data
- All CRUD operations write directly to local database first
- UI always reads from local database - ensuring instant response times
- Application works fully offline without any degradation of core features
- Local storage handles all data persistence, caching, and state management

### Secondary: Server/Cloud API (輔助：伺服器/雲端 API)
- Server API is **optional** and serves as a synchronization target
- Background sync pushes local changes to server when online
- Server data is pulled and merged into local database
- Conflict resolution happens locally with configurable strategies
- Application functions completely without server connectivity
- Cloud services are used for:
  - Multi-device synchronization (可選)
  - Backup and restore (可選)
  - Shared/collaborative features (可選)
  - Analytics and telemetry (可選)

### Data Flow Pattern (資料流模式)
```
┌─────────────────────────────────────────────────────────────────────┐
│                         APPLICATION                                  │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────────────┐ │
│  │    UI       │───▶│  ViewModel  │───▶│      Repository         │ │
│  │   Layer     │◀───│   (MVVM)    │◀───│   (Local-First)         │ │
│  └─────────────┘    └─────────────┘    └───────────┬─────────────┘ │
│                                                     │               │
│                           ┌─────────────────────────┼───────────┐   │
│                           │                         ▼           │   │
│                           │    ┌─────────────────────────────┐  │   │
│                           │    │   LOCAL DATABASE (SQLite)   │  │   │
│                           │    │   ══════════════════════    │  │   │
│                           │    │   • Primary Data Store      │  │   │
│                           │    │   • Single Source of Truth  │  │   │
│                           │    │   • Always Available        │  │   │
│                           │    │   • Instant Read/Write      │  │   │
│                           │    └─────────────────────────────┘  │   │
│                           │                   │                 │   │
│                           │    Background     │ Sync            │   │
│                           │    (When Online)  ▼ Service         │   │
│                           │    ┌─────────────────────────────┐  │   │
│                           │    │   CHANGE QUEUE (SyncQueue)  │  │   │
│                           │    │   • Pending creates         │  │   │
│                           │    │   • Pending updates         │  │   │
│                           │    │   • Pending deletes         │  │   │
│                           │    └─────────────┬───────────────┘  │   │
│                           │                  │                  │   │
│                           └──────────────────│──────────────────┘   │
│                                              │                      │
├──────────────────────────────────────────────│──────────────────────┤
│                     NETWORK BOUNDARY         │                      │
├──────────────────────────────────────────────│──────────────────────┤
│                                              ▼                      │
│                           ┌─────────────────────────────┐           │
│                           │   SERVER/CLOUD API (可選)   │           │
│                           │   ═══════════════════════   │           │
│                           │   • Sync target (同步目標)  │           │
│                           │   • Backup (備份)           │           │
│                           │   • Multi-device (多設備)   │           │
│                           │   • Collaboration (協作)    │           │
│                           └─────────────────────────────┘           │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Benefits (主要優點)
1. **Instant Performance** - No network latency for any operation
2. **Full Offline Support** - Works without internet connection
3. **Data Ownership** - User data stays on their device
4. **Reliability** - No dependency on server availability
5. **Privacy** - Sensitive data can remain local only
6. **Cost Effective** - Reduced server infrastructure needs

---

## Plugin-Everything Architecture (一切皆插件架構)

The application follows a **"Plugin-Everything"** design philosophy where **ALL functionality beyond the core shell is implemented as plugins**. This includes menus, functions, views, services, and features.

### Core Principle: Minimal Shell + Maximum Plugins

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION SHELL (最小核心)                        │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  • Window Management (視窗管理)                                        │  │
│  │  • Plugin Host & Lifecycle (插件宿主與生命週期)                         │  │
│  │  • Shared Context & Message Bus (共享上下文與訊息匯流排)                │  │
│  │  • Navigation Shell (導航框架)                                         │  │
│  │  • Theme Engine (主題引擎)                                             │  │
│  │  • Local Database (本地資料庫)                                         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                    │                                         │
│                    ┌───────────────┼───────────────┐                        │
│                    ▼               ▼               ▼                        │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐   │
│  │   PLUGIN: Menu      │ │   PLUGIN: Customer  │ │   PLUGIN: Reports   │   │
│  │   (菜單插件)         │ │   (客戶管理插件)     │ │   (報表插件)         │   │
│  ├─────────────────────┤ ├─────────────────────┤ ├─────────────────────┤   │
│  │ • Main Menu         │ │ • Customer List     │ │ • Report Designer   │   │
│  │ • Context Menus     │ │ • Customer Detail   │ │ • Report Viewer     │   │
│  │ • Toolbar Items     │ │ • Customer CRUD     │ │ • Export Formats    │   │
│  └─────────────────────┘ └─────────────────────┘ └─────────────────────┘   │
│                    │               │               │                        │
│                    └───────────────┼───────────────┘                        │
│                                    ▼                                        │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │              SHARED CONTEXT & MESSAGE BUS (共享上下文)                  │  │
│  │  • IPluginContext - Shared state and services                         │  │
│  │  • IMessageBus - Plugin-to-plugin communication                       │  │
│  │  • IEventAggregator - Application-wide events                         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### What's in the Shell vs Plugins

| Shell (核心) | Plugin (插件) |
|-------------|--------------|
| Window frame & chrome | All menus (main, context, toolbar) |
| Plugin loader & lifecycle | All function tree nodes |
| Navigation container | All views and pages |
| Theme engine | All business features |
| Message bus infrastructure | All data operations |
| Local database engine | Entity definitions & repositories |
| Authentication framework | Auth providers (Azure AD, etc.) |
| Settings framework | Settings UI & storage |

### Plugin Types (插件類型)

```csharp
/// <summary>
/// All plugin types supported by the system.
/// 系統支援的所有插件類型
/// </summary>
public enum PluginType
{
    // UI Contribution Plugins (UI 貢獻插件)
    Menu,               // Main menu, context menus, toolbar
    FunctionTree,       // Function tree nodes
    View,               // Pages, dialogs, panels
    Widget,             // Dashboard widgets, status bar items
    Theme,              // Color themes, styles

    // Feature Plugins (功能插件)
    Module,             // Complete business modules (Customer, Order, etc.)
    Service,            // Background services
    DataSource,         // Data providers
    Export,             // Export formats (Excel, PDF, CSV)
    Import,             // Import handlers
    Print,              // Print templates

    // Integration Plugins (整合插件)
    Auth,               // Authentication providers
    Sync,               // Sync providers
    Analytics,          // Analytics providers
    Notification,       // Notification channels

    // Extension Plugins (擴展插件)
    EntityExtension,    // Extend existing entities
    ViewExtension,      // Extend existing views
    Workflow,           // Workflow definitions
}
```

---

## Core Architecture

### Technology Stack

| Category | Technology | Notes |
|----------|-----------|-------|
| **Language** | C# 12 | Latest .NET 8 |
| **UI Framework** | WinUI 3 | Windows App SDK |
| **Architecture** | Clean Architecture + MVVM | Layered with plugin support |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection | Built-in .NET DI |
| **Database** | Entity Framework Core + SQLite | Local persistence |
| **Networking** | HttpClient + Refit | REST API client |
| **Async** | async/await, IAsyncEnumerable | Native C# async |
| **Navigation** | Frame + NavigationView | WinUI navigation |
| **Background Work** | BackgroundTaskBuilder / HostedService | Windows background tasks |
| **Logging** | Serilog | Structured logging |
| **Validation** | FluentValidation | Input validation |
| **Serialization** | System.Text.Json | JSON handling |
| **Testing** | xUnit, Moq, FluentAssertions | Unit testing |

---

## Project Structure

```
Arcana.Windows/
├── src/
│   ├── Arcana.App/                           # Main WinUI3 Application
│   │   ├── App.xaml.cs                       # Application entry point
│   │   ├── MainWindow.xaml.cs                # Main window shell
│   │   ├── Views/                            # XAML views (pages)
│   │   │   ├── HomePage.xaml
│   │   │   ├── UserListPage.xaml
│   │   │   └── UserDetailPage.xaml
│   │   ├── ViewModels/                       # ViewModels with MVVM
│   │   │   ├── HomeViewModel.cs
│   │   │   ├── UserListViewModel.cs
│   │   │   └── UserDetailViewModel.cs
│   │   ├── Controls/                         # Reusable UI controls
│   │   ├── Converters/                       # Value converters
│   │   ├── Styles/                           # XAML styles and themes
│   │   └── Navigation/                       # Navigation service
│   │
│   ├── Arcana.Core/                          # Core/Shared Layer
│   │   ├── Analytics/                        # Analytics tracking
│   │   │   ├── IAnalyticsTracker.cs
│   │   │   ├── AnalyticsManager.cs
│   │   │   └── AnalyticsEvent.cs
│   │   ├── Common/                           # Cross-cutting concerns
│   │   │   ├── AppError.cs                   # Error hierarchy
│   │   │   ├── ErrorCode.cs                  # Error code system
│   │   │   ├── Result.cs                     # Result<T> pattern
│   │   │   └── INetworkMonitor.cs            # Connectivity abstraction
│   │   ├── Extensions/                       # Extension methods
│   │   └── Logging/                          # Logging abstractions
│   │
│   ├── Arcana.Domain/                        # Domain Layer (Business Logic)
│   │   ├── Entities/                         # Domain entities
│   │   │   └── User.cs
│   │   ├── ValueObjects/                     # Value objects
│   │   │   └── EmailAddress.cs
│   │   ├── Services/                         # Domain services
│   │   │   ├── IUserService.cs
│   │   │   └── UserService.cs
│   │   └── Validation/                       # Validators
│   │       └── UserValidator.cs
│   │
│   ├── Arcana.Data/                          # Data Layer
│   │   ├── Local/                            # Local database
│   │   │   ├── AppDbContext.cs               # EF Core DbContext
│   │   │   ├── Entities/                     # Database entities
│   │   │   │   ├── UserEntity.cs
│   │   │   │   └── UserChangeEntity.cs
│   │   │   └── Migrations/                   # EF migrations
│   │   ├── Remote/                           # Network layer
│   │   │   ├── IApiService.cs                # API interface (Refit)
│   │   │   ├── Dto/                          # Data transfer objects
│   │   │   │   ├── UserDto.cs
│   │   │   │   └── CreateUserRequest.cs
│   │   │   └── UserNetworkDataSource.cs
│   │   ├── Repository/                       # Repository pattern
│   │   │   ├── IDataRepository.cs
│   │   │   ├── OfflineFirstDataRepository.cs
│   │   │   └── CachingDataRepository.cs
│   │   ├── Cache/                            # Caching layer
│   │   │   ├── ICacheEventBus.cs
│   │   │   ├── CacheEventBus.cs
│   │   │   └── CacheInvalidationEvent.cs
│   │   └── Mappers/                          # Entity mappers
│   │
│   ├── Arcana.Sync/                          # Synchronization Layer
│   │   ├── ISynchronizer.cs
│   │   ├── SyncManager.cs
│   │   ├── SyncStatus.cs
│   │   └── ISyncable.cs
│   │
│   ├── Arcana.Plugins/                       # Plugin Architecture
│   │   ├── Core/                             # Plugin infrastructure
│   │   │   ├── IPlugin.cs                    # Plugin interface
│   │   │   ├── PluginBase.cs                 # Base class
│   │   │   ├── PluginManager.cs              # Plugin loader/manager
│   │   │   ├── PluginContext.cs              # Runtime context
│   │   │   └── PluginMetadata.cs             # Plugin info
│   │   ├── Contracts/                        # Plugin contracts
│   │   │   ├── IDataSourcePlugin.cs          # Custom data sources
│   │   │   ├── IAnalyticsPlugin.cs           # Analytics providers
│   │   │   ├── IAuthPlugin.cs                # Auth providers
│   │   │   └── IExportPlugin.cs              # Export formats
│   │   └── Hosting/                          # Plugin hosting
│   │       ├── PluginHost.cs
│   │       └── PluginAssemblyLoadContext.cs
│   │
│   └── Arcana.Infrastructure/                # Infrastructure Layer
│       ├── DependencyInjection/              # DI configuration
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── PluginServiceExtensions.cs
│       ├── Settings/                         # App settings
│       │   ├── ISettingsService.cs
│       │   └── SettingsService.cs
│       └── Platform/                         # Windows-specific
│           ├── NetworkMonitor.cs
│           └── BackgroundSyncTask.cs
│
├── plugins/                                  # External plugins folder
│   ├── Arcana.Plugin.Firebase/               # Firebase analytics plugin
│   ├── Arcana.Plugin.AzureAD/                # Azure AD auth plugin
│   └── Arcana.Plugin.Excel/                  # Excel export plugin
│
├── tests/
│   ├── Arcana.Domain.Tests/
│   ├── Arcana.Data.Tests/
│   └── Arcana.App.Tests/
│
├── docs/
│   └── architecture/
│
└── Arcana.Windows.sln                        # Solution file
```

---

## Architectural Patterns

### 1. Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│  Presentation Layer: WinUI3 XAML + ViewModels           │
│  (MVVM Pattern - INotifyPropertyChanged)                │
├─────────────────────────────────────────────────────────┤
│  Domain Layer: Services + Validators + Entities         │
│  (Business logic, framework-independent)                │
├─────────────────────────────────────────────────────────┤
│  Data Layer: Offline-First Repository + Caching         │
│  (EF Core SQLite as source of truth)                    │
├─────────────────────────────────────────────────────────┤
│  Core Layer: Analytics, Error Handling, Logging         │
│  (Cross-cutting concerns)                               │
├─────────────────────────────────────────────────────────┤
│  Plugin Layer: Extensible components via contracts      │
│  (Isolated assemblies, hot-reload capable)              │
└─────────────────────────────────────────────────────────┘
```

### 2. MVVM with Commands and Messaging

```csharp
// ViewModel base class
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

// Example ViewModel with Input/Output/Effect pattern
public class UserListViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IAnalyticsTracker _analytics;

    // Output (State)
    private ObservableCollection<User> _users = new();
    public ObservableCollection<User> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // Commands (Input)
    public IAsyncRelayCommand LoadUsersCommand { get; }
    public IAsyncRelayCommand<User> DeleteUserCommand { get; }
    public IRelayCommand<User> NavigateToDetailCommand { get; }

    // Effects (One-time events)
    public event EventHandler<string>? ShowError;
    public event EventHandler<User>? NavigateToDetail;

    public UserListViewModel(IUserService userService, IAnalyticsTracker analytics)
    {
        _userService = userService;
        _analytics = analytics;

        LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
        DeleteUserCommand = new AsyncRelayCommand<User>(DeleteUserAsync);
        NavigateToDetailCommand = new RelayCommand<User>(u => NavigateToDetail?.Invoke(this, u!));
    }

    private async Task LoadUsersAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _userService.GetUsersAsync();
            if (result.IsSuccess)
            {
                Users = new ObservableCollection<User>(result.Value);
                _analytics.TrackEvent("users_loaded", new { count = Users.Count });
            }
            else
            {
                ShowError?.Invoke(this, result.Error.Message);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 3. Repository Pattern with Decorator

```csharp
// Repository interface
public interface IDataRepository
{
    IAsyncEnumerable<User> GetUsersStream();
    Task<Result<User>> GetUserByIdAsync(int id);
    Task<Result<(List<User> Users, int TotalPages)>> GetUsersPageAsync(int page, int pageSize = 10);
    Task<Result<User>> CreateUserAsync(User user);
    Task<Result<User>> UpdateUserAsync(User user);
    Task<Result<bool>> DeleteUserAsync(int id);
    Task<Result<bool>> SyncAsync();
    void InvalidateCache();
}

// Offline-first implementation
public class OfflineFirstDataRepository : IDataRepository, ISyncable
{
    private readonly AppDbContext _dbContext;
    private readonly IApiService _apiService;
    private readonly INetworkMonitor _networkMonitor;

    public async IAsyncEnumerable<User> GetUsersStream()
    {
        // Always return from local DB first
        await foreach (var user in _dbContext.Users.AsAsyncEnumerable())
        {
            yield return user.ToDomain();
        }
    }

    public async Task<Result<User>> CreateUserAsync(User user)
    {
        // Save locally immediately
        var entity = user.ToEntity();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync();

        if (await _networkMonitor.IsOnlineAsync())
        {
            // Sync to server
            await SyncToServerAsync(entity);
        }
        else
        {
            // Queue for later sync
            await QueueChangeAsync(entity, ChangeType.Create);
        }

        return Result<User>.Success(entity.ToDomain());
    }
}

// Caching decorator
public class CachingDataRepository : IDataRepository
{
    private readonly IDataRepository _inner;
    private readonly ICacheEventBus _cacheEventBus;
    private readonly MemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachingDataRepository(IDataRepository inner, ICacheEventBus cacheEventBus)
    {
        _inner = inner;
        _cacheEventBus = cacheEventBus;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });

        _cacheEventBus.OnInvalidation += HandleCacheInvalidation;
    }

    public async Task<Result<(List<User> Users, int TotalPages)>> GetUsersPageAsync(int page, int pageSize = 10)
    {
        var cacheKey = $"users_page_{page}_{pageSize}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return Result<(List<User>, int)>.Success(((List<User>, int))cached);
        }

        var result = await _inner.GetUsersPageAsync(page, pageSize);

        if (result.IsSuccess)
        {
            _cache.Set(cacheKey, result.Value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                Size = 1
            });
        }

        return result;
    }
}
```

### 4. Plugin Architecture (VS Code-Style Extension System)

The plugin system follows VS Code's extension architecture with contribution points, activation events, and a rich API surface.

#### 4.1 Plugin Manifest (plugin.json) - Like VS Code's package.json

```json
{
  "id": "arcana.plugin.github",
  "name": "GitHub Integration",
  "version": "1.2.0",
  "publisher": "arcana-team",
  "description": "GitHub integration for Arcana",
  "icon": "assets/icon.png",
  "license": "MIT",
  "repository": "https://github.com/arcana/plugin-github",

  "engines": {
    "arcana": "^1.0.0"
  },

  "main": "Arcana.Plugin.GitHub.dll",
  "activationEvents": [
    "onStartup",
    "onCommand:github.connect",
    "onView:github.repositories",
    "onUri:arcana://github",
    "workspaceContains:.github"
  ],

  "contributes": {
    "commands": [
      {
        "command": "github.connect",
        "title": "Connect to GitHub",
        "icon": "$(github)",
        "category": "GitHub"
      },
      {
        "command": "github.createPR",
        "title": "Create Pull Request",
        "icon": "$(git-pull-request)",
        "category": "GitHub"
      }
    ],

    "menus": {
      "commandPalette": [
        { "command": "github.connect", "when": "!github.connected" },
        { "command": "github.createPR", "when": "github.connected" }
      ],
      "navigation": [
        { "command": "github.connect", "group": "tools" }
      ],
      "context": [
        { "command": "github.viewFile", "when": "resourceScheme == file", "group": "github" }
      ]
    },

    "viewsContainers": {
      "sidebar": [
        {
          "id": "github",
          "title": "GitHub",
          "icon": "assets/github.svg"
        }
      ]
    },

    "views": {
      "github": [
        { "id": "github.repositories", "name": "Repositories" },
        { "id": "github.pullRequests", "name": "Pull Requests" },
        { "id": "github.issues", "name": "Issues" }
      ]
    },

    "configuration": {
      "title": "GitHub",
      "properties": {
        "github.token": {
          "type": "string",
          "description": "GitHub Personal Access Token",
          "secret": true
        },
        "github.defaultBranch": {
          "type": "string",
          "default": "main",
          "description": "Default branch name"
        },
        "github.autoFetch": {
          "type": "boolean",
          "default": true,
          "description": "Automatically fetch repository updates"
        }
      }
    },

    "keybindings": [
      {
        "command": "github.createPR",
        "key": "ctrl+shift+p",
        "when": "github.connected"
      }
    ],

    "themes": [
      {
        "label": "GitHub Dark",
        "uiTheme": "vs-dark",
        "path": "./themes/github-dark.json"
      }
    ],

    "languages": [
      {
        "id": "github-actions",
        "extensions": [".yml", ".yaml"],
        "filenames": ["action.yml", "action.yaml"],
        "configuration": "./language-configuration.json"
      }
    ],

    "snippets": [
      {
        "language": "yaml",
        "path": "./snippets/github-actions.json"
      }
    ],

    "taskDefinitions": [
      {
        "type": "github",
        "properties": {
          "workflow": { "type": "string", "description": "Workflow file name" }
        }
      }
    ]
  },

  "extensionDependencies": [
    "arcana.plugin.git"
  ],

  "capabilities": {
    "untrustedWorkspaces": { "supported": true },
    "virtualWorkspaces": { "supported": "limited" }
  }
}
```

#### 4.2 Core Plugin Interfaces

```csharp
// ============================================
// BASE PLUGIN INTERFACE (Like VS Code Extension)
// ============================================

public interface IExtension : IAsyncDisposable
{
    /// <summary>Plugin manifest loaded from plugin.json</summary>
    ExtensionManifest Manifest { get; }

    /// <summary>Current activation state</summary>
    ExtensionState State { get; }

    /// <summary>Called when plugin is activated (lazy activation)</summary>
    Task ActivateAsync(IExtensionContext context);

    /// <summary>Called when plugin is deactivated</summary>
    Task DeactivateAsync();
}

public enum ExtensionState
{
    NotLoaded,
    Loaded,
    Activating,
    Active,
    Deactivating,
    Deactivated,
    Error
}

// ============================================
// EXTENSION CONTEXT (What plugins can access)
// ============================================

public interface IExtensionContext
{
    /// <summary>Unique extension identifier</summary>
    string ExtensionId { get; }

    /// <summary>Extension version</summary>
    Version ExtensionVersion { get; }

    /// <summary>Path to extension installation directory</summary>
    string ExtensionPath { get; }

    /// <summary>Path for extension-specific data storage</summary>
    string GlobalStoragePath { get; }

    /// <summary>Path for workspace-specific data</summary>
    string WorkspaceStoragePath { get; }

    /// <summary>Persistent storage for extension state</summary>
    IExtensionMemento GlobalState { get; }

    /// <summary>Workspace-scoped state storage</summary>
    IExtensionMemento WorkspaceState { get; }

    /// <summary>Secret storage (encrypted)</summary>
    ISecretStorage Secrets { get; }

    /// <summary>Extension's subscriptions (auto-disposed)</summary>
    IList<IDisposable> Subscriptions { get; }

    /// <summary>Logger for this extension</summary>
    ILogger Logger { get; }

    /// <summary>Access to extension API</summary>
    IExtensionApi Api { get; }
}

// ============================================
// EXTENSION API (Host services for plugins)
// ============================================

public interface IExtensionApi
{
    // Command System
    ICommandService Commands { get; }

    // UI Components
    IWindowService Window { get; }
    IStatusBarService StatusBar { get; }
    INotificationService Notifications { get; }
    IQuickPickService QuickPick { get; }
    ITreeViewService TreeView { get; }
    IWebViewService WebView { get; }

    // Workspace & Documents
    IWorkspaceService Workspace { get; }
    IDocumentService Documents { get; }

    // Configuration
    IConfigurationService Configuration { get; }

    // Environment
    IEnvironmentService Environment { get; }

    // Extension-to-Extension Communication
    IExtensionBus ExtensionBus { get; }

    // Tasks & Progress
    ITaskService Tasks { get; }
    IProgressService Progress { get; }

    // Data & Storage
    IDataService Data { get; }

    // Authentication
    IAuthenticationService Authentication { get; }

    // Theming
    IThemeService Theme { get; }

    // Language Features
    ILanguageService Languages { get; }
}

// ============================================
// COMMAND SERVICE (Like VS Code Commands API)
// ============================================

public interface ICommandService
{
    /// <summary>Register a command handler</summary>
    IDisposable RegisterCommand(string commandId, Func<object?[], Task> handler);

    /// <summary>Register a command with typed arguments</summary>
    IDisposable RegisterCommand<T>(string commandId, Func<T, Task> handler);

    /// <summary>Execute a command by ID</summary>
    Task<object?> ExecuteCommandAsync(string commandId, params object?[] args);

    /// <summary>Get all registered commands</summary>
    IReadOnlyList<string> GetCommands();

    /// <summary>Check if command exists</summary>
    bool HasCommand(string commandId);
}

// ============================================
// WINDOW SERVICE (UI Interactions)
// ============================================

public interface IWindowService
{
    /// <summary>Show information message</summary>
    Task<string?> ShowInformationMessageAsync(string message, params string[] actions);

    /// <summary>Show warning message</summary>
    Task<string?> ShowWarningMessageAsync(string message, params string[] actions);

    /// <summary>Show error message</summary>
    Task<string?> ShowErrorMessageAsync(string message, params string[] actions);

    /// <summary>Show input box</summary>
    Task<string?> ShowInputBoxAsync(InputBoxOptions options);

    /// <summary>Show quick pick (dropdown selection)</summary>
    Task<T?> ShowQuickPickAsync<T>(IEnumerable<QuickPickItem<T>> items, QuickPickOptions? options = null);

    /// <summary>Show multi-select quick pick</summary>
    Task<IReadOnlyList<T>> ShowQuickPickManyAsync<T>(IEnumerable<QuickPickItem<T>> items, QuickPickOptions? options = null);

    /// <summary>Show file open dialog</summary>
    Task<string[]?> ShowOpenDialogAsync(OpenDialogOptions options);

    /// <summary>Show file save dialog</summary>
    Task<string?> ShowSaveDialogAsync(SaveDialogOptions options);

    /// <summary>Show folder picker</summary>
    Task<string?> ShowFolderPickerAsync(FolderPickerOptions options);

    /// <summary>Create output channel for logging</summary>
    IOutputChannel CreateOutputChannel(string name);

    /// <summary>Create terminal</summary>
    ITerminal CreateTerminal(TerminalOptions options);

    /// <summary>Active color theme</summary>
    IColorTheme ActiveColorTheme { get; }

    /// <summary>Theme changed event</summary>
    event EventHandler<IColorTheme> OnDidChangeActiveColorTheme;
}

public record InputBoxOptions(
    string? Title = null,
    string? Prompt = null,
    string? PlaceHolder = null,
    string? Value = null,
    bool Password = false,
    Func<string, string?>? ValidateInput = null
);

public record QuickPickItem<T>(
    string Label,
    T Value,
    string? Description = null,
    string? Detail = null,
    string? IconPath = null,
    bool Picked = false
);

public record QuickPickOptions(
    string? Title = null,
    string? PlaceHolder = null,
    bool MatchOnDescription = false,
    bool MatchOnDetail = false,
    bool CanPickMany = false
);

// ============================================
// TREE VIEW SERVICE (Sidebar panels)
// ============================================

public interface ITreeViewService
{
    /// <summary>Register a tree data provider</summary>
    IDisposable RegisterTreeDataProvider<T>(string viewId, ITreeDataProvider<T> provider) where T : class;

    /// <summary>Create a tree view</summary>
    ITreeView<T> CreateTreeView<T>(string viewId, TreeViewOptions<T> options) where T : class;

    /// <summary>Reveal an item in the tree</summary>
    Task RevealAsync<T>(string viewId, T element, RevealOptions? options = null) where T : class;
}

public interface ITreeDataProvider<T> where T : class
{
    /// <summary>Get root elements or children</summary>
    Task<IReadOnlyList<T>> GetChildrenAsync(T? element);

    /// <summary>Get parent of an element</summary>
    Task<T?> GetParentAsync(T element);

    /// <summary>Get tree item representation</summary>
    TreeItem GetTreeItem(T element);

    /// <summary>Data changed event</summary>
    event EventHandler<TreeDataChangedEventArgs<T>?> OnDidChangeTreeData;
}

public record TreeItem(
    string Label,
    TreeItemCollapsibleState CollapsibleState = TreeItemCollapsibleState.None,
    string? Description = null,
    string? Tooltip = null,
    string? IconPath = null,
    string? ContextValue = null,
    Command? Command = null
);

public enum TreeItemCollapsibleState { None, Collapsed, Expanded }

// ============================================
// WEBVIEW SERVICE (Embedded web content)
// ============================================

public interface IWebViewService
{
    /// <summary>Create a webview panel</summary>
    IWebViewPanel CreateWebViewPanel(
        string viewType,
        string title,
        WebViewPanelOptions options
    );

    /// <summary>Register a webview view provider (sidebar/panel)</summary>
    IDisposable RegisterWebViewViewProvider(
        string viewId,
        IWebViewViewProvider provider
    );
}

public interface IWebViewPanel : IDisposable
{
    string ViewType { get; }
    string Title { get; set; }
    IWebView WebView { get; }
    bool Visible { get; }
    bool Active { get; }

    void Reveal(ViewColumn column = ViewColumn.Active);
    event EventHandler OnDidDispose;
    event EventHandler<bool> OnDidChangeViewState;
}

public interface IWebView
{
    /// <summary>HTML content to display</summary>
    string Html { get; set; }

    /// <summary>Post message to webview</summary>
    Task<bool> PostMessageAsync(object message);

    /// <summary>Message received from webview</summary>
    event EventHandler<object> OnDidReceiveMessage;

    /// <summary>Convert local resource path to webview URI</summary>
    Uri AsWebViewUri(string localPath);

    /// <summary>Content Security Policy source for local resources</summary>
    string CspSource { get; }
}

// ============================================
// STATUS BAR SERVICE
// ============================================

public interface IStatusBarService
{
    /// <summary>Create a status bar item</summary>
    IStatusBarItem CreateStatusBarItem(StatusBarAlignment alignment = StatusBarAlignment.Left, int priority = 0);

    /// <summary>Set temporary status message</summary>
    IDisposable SetStatusMessage(string text, int hideAfterMs = 5000);
}

public interface IStatusBarItem : IDisposable
{
    string? Text { get; set; }
    string? Tooltip { get; set; }
    string? Color { get; set; }
    string? BackgroundColor { get; set; }
    string? Command { get; set; }
    bool Visible { get; set; }

    void Show();
    void Hide();
}

public enum StatusBarAlignment { Left, Right }

// ============================================
// CONFIGURATION SERVICE
// ============================================

public interface IConfigurationService
{
    /// <summary>Get configuration value</summary>
    T? Get<T>(string section, string key, T? defaultValue = default);

    /// <summary>Update configuration value</summary>
    Task UpdateAsync<T>(string section, string key, T value, ConfigurationTarget target = ConfigurationTarget.User);

    /// <summary>Check if configuration has a value</summary>
    bool Has(string section, string key);

    /// <summary>Configuration changed event</summary>
    event EventHandler<ConfigurationChangeEvent> OnDidChangeConfiguration;

    /// <summary>Get workspace configuration</summary>
    IWorkspaceConfiguration GetWorkspaceConfiguration(string? resource = null);
}

public enum ConfigurationTarget { User, Workspace, WorkspaceFolder }

public record ConfigurationChangeEvent(IReadOnlyList<string> AffectedKeys)
{
    public bool AffectsConfiguration(string section) =>
        AffectedKeys.Any(k => k.StartsWith(section));
}

// ============================================
// AUTHENTICATION SERVICE
// ============================================

public interface IAuthenticationService
{
    /// <summary>Get existing session or prompt for login</summary>
    Task<AuthenticationSession?> GetSessionAsync(
        string providerId,
        IReadOnlyList<string> scopes,
        GetSessionOptions? options = null);

    /// <summary>Register an authentication provider</summary>
    IDisposable RegisterAuthenticationProvider(
        string id,
        string label,
        IAuthenticationProvider provider);

    /// <summary>Session changed event</summary>
    event EventHandler<AuthenticationSessionsChangeEvent> OnDidChangeSessions;
}

public interface IAuthenticationProvider
{
    Task<IReadOnlyList<AuthenticationSession>> GetSessionsAsync(IReadOnlyList<string>? scopes = null);
    Task<AuthenticationSession> CreateSessionAsync(IReadOnlyList<string> scopes);
    Task RemoveSessionAsync(string sessionId);
    event EventHandler<AuthenticationProviderSessionChangeEvent> OnDidChangeSessions;
}

public record AuthenticationSession(
    string Id,
    string AccessToken,
    AccountInfo Account,
    IReadOnlyList<string> Scopes
);

public record AccountInfo(string Id, string Label);

// ============================================
// EXTENSION BUS (Extension-to-Extension Communication)
// ============================================

public interface IExtensionBus
{
    /// <summary>Get API exported by another extension</summary>
    Task<T?> GetExtensionApiAsync<T>(string extensionId) where T : class;

    /// <summary>Export API for other extensions</summary>
    void ExportApi<T>(T api) where T : class;

    /// <summary>Publish event to all extensions</summary>
    Task PublishAsync<T>(string eventName, T data);

    /// <summary>Subscribe to events from other extensions</summary>
    IDisposable Subscribe<T>(string eventName, Func<T, Task> handler);

    /// <summary>Check if extension is active</summary>
    bool IsExtensionActive(string extensionId);

    /// <summary>Get extension by ID</summary>
    IExtension? GetExtension(string extensionId);

    /// <summary>All installed extensions</summary>
    IReadOnlyList<IExtension> AllExtensions { get; }
}

// ============================================
// PROGRESS SERVICE
// ============================================

public interface IProgressService
{
    /// <summary>Show progress with cancellation support</summary>
    Task<T> WithProgressAsync<T>(
        ProgressOptions options,
        Func<IProgress<ProgressReport>, CancellationToken, Task<T>> task);

    /// <summary>Show progress without return value</summary>
    Task WithProgressAsync(
        ProgressOptions options,
        Func<IProgress<ProgressReport>, CancellationToken, Task> task);
}

public record ProgressOptions(
    ProgressLocation Location,
    string? Title = null,
    bool Cancellable = false
);

public enum ProgressLocation { Notification, Window, StatusBar }

public record ProgressReport(
    string? Message = null,
    int? Increment = null
);

// ============================================
// LANGUAGE SERVICE (For editor extensions)
// ============================================

public interface ILanguageService
{
    /// <summary>Register completion provider</summary>
    IDisposable RegisterCompletionItemProvider(
        DocumentSelector selector,
        ICompletionItemProvider provider,
        params string[] triggerCharacters);

    /// <summary>Register hover provider</summary>
    IDisposable RegisterHoverProvider(
        DocumentSelector selector,
        IHoverProvider provider);

    /// <summary>Register definition provider</summary>
    IDisposable RegisterDefinitionProvider(
        DocumentSelector selector,
        IDefinitionProvider provider);

    /// <summary>Register code action provider</summary>
    IDisposable RegisterCodeActionProvider(
        DocumentSelector selector,
        ICodeActionProvider provider);

    /// <summary>Register diagnostic collection</summary>
    IDiagnosticCollection CreateDiagnosticCollection(string name);
}

public record DocumentSelector(string Language, string? Scheme = null, string? Pattern = null);
```

#### 4.3 Extension Host & Manager

```csharp
// ============================================
// EXTENSION MANAGER (Like VS Code Extension Host)
// ============================================

public class ExtensionManager : IExtensionManager, IAsyncDisposable
{
    private readonly ILogger<ExtensionManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configuration;
    private readonly Dictionary<string, ExtensionHost> _extensionHosts = new();
    private readonly Dictionary<string, ExtensionManifest> _manifests = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _activationLocks = new();

    // Contribution registries
    private readonly CommandRegistry _commandRegistry;
    private readonly MenuRegistry _menuRegistry;
    private readonly ViewRegistry _viewRegistry;
    private readonly ConfigurationRegistry _configurationRegistry;
    private readonly KeybindingRegistry _keybindingRegistry;
    private readonly ThemeRegistry _themeRegistry;

    public IReadOnlyList<IExtension> Extensions => _extensionHosts.Values
        .Select(h => h.Extension)
        .ToList();

    public async Task<IReadOnlyList<ExtensionManifest>> DiscoverExtensionsAsync(string extensionsPath)
    {
        var manifests = new List<ExtensionManifest>();

        foreach (var dir in Directory.GetDirectories(extensionsPath))
        {
            var manifestPath = Path.Combine(dir, "plugin.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ExtensionManifest>(json, _jsonOptions);

                if (manifest != null)
                {
                    manifest.ExtensionPath = dir;
                    manifests.Add(manifest);
                    _manifests[manifest.Id] = manifest;

                    // Process contributions (register commands, views, etc.)
                    await ProcessContributionsAsync(manifest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load manifest from {Path}", manifestPath);
            }
        }

        return manifests;
    }

    private async Task ProcessContributionsAsync(ExtensionManifest manifest)
    {
        var contributions = manifest.Contributes;
        if (contributions == null) return;

        // Register commands (but don't activate yet)
        foreach (var cmd in contributions.Commands ?? [])
        {
            _commandRegistry.RegisterCommand(cmd.Command, manifest.Id, cmd);
        }

        // Register menus
        foreach (var (location, items) in contributions.Menus ?? new())
        {
            foreach (var item in items)
            {
                _menuRegistry.RegisterMenuItem(location, item, manifest.Id);
            }
        }

        // Register views
        foreach (var (containerId, views) in contributions.Views ?? new())
        {
            foreach (var view in views)
            {
                _viewRegistry.RegisterView(containerId, view, manifest.Id);
            }
        }

        // Register view containers
        foreach (var (location, containers) in contributions.ViewsContainers ?? new())
        {
            foreach (var container in containers)
            {
                _viewRegistry.RegisterViewContainer(location, container, manifest.Id);
            }
        }

        // Register configuration schema
        if (contributions.Configuration != null)
        {
            _configurationRegistry.RegisterConfiguration(manifest.Id, contributions.Configuration);
        }

        // Register keybindings
        foreach (var kb in contributions.Keybindings ?? [])
        {
            _keybindingRegistry.RegisterKeybinding(kb, manifest.Id);
        }

        // Register themes
        foreach (var theme in contributions.Themes ?? [])
        {
            _themeRegistry.RegisterTheme(theme, manifest.Id, manifest.ExtensionPath);
        }
    }

    public async Task ActivateExtensionAsync(string extensionId, string? activationReason = null)
    {
        if (!_manifests.TryGetValue(extensionId, out var manifest))
        {
            throw new ExtensionNotFoundException(extensionId);
        }

        // Use lock to prevent concurrent activation
        var activationLock = _activationLocks.GetOrAdd(extensionId, _ => new SemaphoreSlim(1, 1));
        await activationLock.WaitAsync();

        try
        {
            // Already activated?
            if (_extensionHosts.TryGetValue(extensionId, out var existingHost) &&
                existingHost.Extension.State == ExtensionState.Active)
            {
                return;
            }

            // Activate dependencies first
            foreach (var depId in manifest.ExtensionDependencies ?? [])
            {
                await ActivateExtensionAsync(depId, $"dependency of {extensionId}");
            }

            _logger.LogInformation(
                "Activating extension {ExtensionId} (reason: {Reason})",
                extensionId,
                activationReason ?? "explicit");

            // Create isolated host
            var host = new ExtensionHost(manifest, _serviceProvider, _logger);
            await host.LoadAsync();

            // Create context
            var context = new ExtensionContext(
                extensionId: manifest.Id,
                extensionVersion: Version.Parse(manifest.Version),
                extensionPath: manifest.ExtensionPath,
                globalStoragePath: GetGlobalStoragePath(manifest.Id),
                workspaceStoragePath: GetWorkspaceStoragePath(manifest.Id),
                api: CreateExtensionApi(manifest.Id),
                logger: _logger
            );

            // Activate
            await host.ActivateAsync(context);

            _extensionHosts[extensionId] = host;

            ExtensionActivated?.Invoke(this, new ExtensionActivatedEventArgs(manifest, activationReason));
        }
        finally
        {
            activationLock.Release();
        }
    }

    public async Task ActivateByEventAsync(string activationEvent, object? eventData = null)
    {
        var extensionsToActivate = _manifests.Values
            .Where(m => m.ActivationEvents?.Contains(activationEvent) == true ||
                        m.ActivationEvents?.Contains("*") == true)
            .ToList();

        foreach (var manifest in extensionsToActivate)
        {
            if (!IsExtensionActive(manifest.Id))
            {
                await ActivateExtensionAsync(manifest.Id, activationEvent);
            }
        }
    }

    public bool IsExtensionActive(string extensionId) =>
        _extensionHosts.TryGetValue(extensionId, out var host) &&
        host.Extension.State == ExtensionState.Active;

    public async Task DeactivateExtensionAsync(string extensionId)
    {
        if (_extensionHosts.TryGetValue(extensionId, out var host))
        {
            await host.DeactivateAsync();
            _extensionHosts.Remove(extensionId);

            ExtensionDeactivated?.Invoke(this, extensionId);
        }
    }

    public event EventHandler<ExtensionActivatedEventArgs>? ExtensionActivated;
    public event EventHandler<string>? ExtensionDeactivated;
}

// ============================================
// EXTENSION HOST (Isolated plugin runtime)
// ============================================

public class ExtensionHost : IAsyncDisposable
{
    private readonly ExtensionManifest _manifest;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly PluginAssemblyLoadContext _loadContext;

    private IExtension? _extension;
    private IExtensionContext? _context;

    public IExtension Extension => _extension ?? throw new InvalidOperationException("Extension not loaded");

    public ExtensionHost(ExtensionManifest manifest, IServiceProvider serviceProvider, ILogger logger)
    {
        _manifest = manifest;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loadContext = new PluginAssemblyLoadContext(manifest.ExtensionPath);
    }

    public async Task LoadAsync()
    {
        var assemblyPath = Path.Combine(_manifest.ExtensionPath, _manifest.Main);
        var assembly = _loadContext.LoadFromAssemblyPath(assemblyPath);

        // Find the extension class
        var extensionType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsAbstract);

        if (extensionType == null)
        {
            throw new InvalidOperationException($"No IExtension implementation found in {_manifest.Main}");
        }

        _extension = (IExtension)ActivatorUtilities.CreateInstance(_serviceProvider, extensionType);
    }

    public async Task ActivateAsync(IExtensionContext context)
    {
        _context = context;

        try
        {
            await _extension!.ActivateAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate extension {ExtensionId}", _manifest.Id);
            throw;
        }
    }

    public async Task DeactivateAsync()
    {
        if (_extension != null)
        {
            try
            {
                await _extension.DeactivateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating extension {ExtensionId}", _manifest.Id);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DeactivateAsync();

        if (_extension != null)
        {
            await _extension.DisposeAsync();
        }

        // Dispose all subscriptions
        if (_context != null)
        {
            foreach (var sub in _context.Subscriptions)
            {
                sub.Dispose();
            }
        }

        // Unload the assembly
        _loadContext.Unload();
    }
}

// ============================================
// PLUGIN ASSEMBLY LOAD CONTEXT (Isolation)
// ============================================

public class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;

    public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(Path.Combine(pluginPath, "plugin.json"));
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from plugin directory first
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Fall back to default context (shared assemblies)
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
```

#### 4.4 Activation Events

```csharp
// ============================================
// ACTIVATION EVENT SYSTEM
// ============================================

public class ActivationEventService : IActivationEventService
{
    private readonly IExtensionManager _extensionManager;
    private readonly ILogger<ActivationEventService> _logger;

    public ActivationEventService(
        IExtensionManager extensionManager,
        ILogger<ActivationEventService> logger)
    {
        _extensionManager = extensionManager;
        _logger = logger;
    }

    /// <summary>Trigger activation event and activate matching extensions</summary>
    public async Task FireAsync(string activationEvent, object? data = null)
    {
        _logger.LogDebug("Firing activation event: {Event}", activationEvent);
        await _extensionManager.ActivateByEventAsync(activationEvent, data);
    }

    /// <summary>
    /// Supported activation events (like VS Code):
    /// - onStartup: Activate on app startup
    /// - onCommand:{commandId}: Activate when command is invoked
    /// - onView:{viewId}: Activate when view is opened
    /// - onUri:{scheme}: Activate on URI scheme (deep linking)
    /// - onLanguage:{languageId}: Activate for language files
    /// - onFileSystem:{scheme}: Activate for file system provider
    /// - workspaceContains:{pattern}: Activate if workspace contains file
    /// - onDebug: Activate when debugging starts
    /// - onDebugResolve:{type}: Activate for debug type
    /// - onAuthenticationRequest:{providerId}: Activate for auth provider
    /// - *: Always activate (use sparingly!)
    /// </summary>
    public static class Events
    {
        public const string OnStartup = "onStartup";
        public static string OnCommand(string commandId) => $"onCommand:{commandId}";
        public static string OnView(string viewId) => $"onView:{viewId}";
        public static string OnUri(string scheme) => $"onUri:{scheme}";
        public static string OnLanguage(string languageId) => $"onLanguage:{languageId}";
        public static string WorkspaceContains(string pattern) => $"workspaceContains:{pattern}";
        public const string OnDebug = "onDebug";
        public static string OnDebugResolve(string type) => $"onDebugResolve:{type}";
        public static string OnAuthenticationRequest(string providerId) => $"onAuthenticationRequest:{providerId}";
        public const string Always = "*";
    }
}

// Integration with command execution
public class CommandService : ICommandService
{
    private readonly Dictionary<string, CommandRegistration> _commands = new();
    private readonly IActivationEventService _activationEvents;
    private readonly IExtensionManager _extensionManager;

    public async Task<object?> ExecuteCommandAsync(string commandId, params object?[] args)
    {
        // Fire activation event before executing
        await _activationEvents.FireAsync(ActivationEventService.Events.OnCommand(commandId));

        if (!_commands.TryGetValue(commandId, out var registration))
        {
            throw new CommandNotFoundException(commandId);
        }

        // Ensure extension is activated
        if (!string.IsNullOrEmpty(registration.ExtensionId) &&
            !_extensionManager.IsExtensionActive(registration.ExtensionId))
        {
            await _extensionManager.ActivateExtensionAsync(
                registration.ExtensionId,
                $"command:{commandId}");
        }

        return await registration.Handler(args);
    }
}
```

#### 4.5 Sample Extension Implementation

```csharp
// ============================================
// SAMPLE EXTENSION: GitHub Integration
// ============================================

// plugins/Arcana.Plugin.GitHub/GitHubExtension.cs
public class GitHubExtension : IExtension
{
    private IExtensionContext _context = null!;
    private GitHubClient? _client;
    private IStatusBarItem? _statusBarItem;
    private readonly List<IDisposable> _disposables = new();

    public ExtensionManifest Manifest { get; private set; } = null!;
    public ExtensionState State { get; private set; } = ExtensionState.NotLoaded;

    public async Task ActivateAsync(IExtensionContext context)
    {
        _context = context;
        State = ExtensionState.Activating;

        var api = context.Api;

        // Register commands
        _disposables.Add(api.Commands.RegisterCommand("github.connect", ConnectAsync));
        _disposables.Add(api.Commands.RegisterCommand("github.disconnect", DisconnectAsync));
        _disposables.Add(api.Commands.RegisterCommand("github.createPR", CreatePullRequestAsync));
        _disposables.Add(api.Commands.RegisterCommand("github.viewIssues", ViewIssuesAsync));

        // Create status bar item
        _statusBarItem = api.StatusBar.CreateStatusBarItem(StatusBarAlignment.Left, 100);
        _statusBarItem.Text = "$(github) GitHub";
        _statusBarItem.Tooltip = "Click to connect to GitHub";
        _statusBarItem.Command = "github.connect";
        _statusBarItem.Show();
        _disposables.Add(_statusBarItem);

        // Register tree data provider
        var repoProvider = new GitHubRepositoryTreeProvider(this);
        _disposables.Add(api.TreeView.RegisterTreeDataProvider("github.repositories", repoProvider));

        var prProvider = new GitHubPullRequestTreeProvider(this);
        _disposables.Add(api.TreeView.RegisterTreeDataProvider("github.pullRequests", prProvider));

        // Check for existing token and auto-connect
        var token = await context.Secrets.GetAsync("github.token");
        if (!string.IsNullOrEmpty(token))
        {
            await ConnectWithTokenAsync(token);
        }

        // Subscribe to configuration changes
        _disposables.Add(new DisposableAction(() =>
            api.Configuration.OnDidChangeConfiguration -= OnConfigurationChanged));
        api.Configuration.OnDidChangeConfiguration += OnConfigurationChanged;

        // Export API for other extensions
        api.ExtensionBus.ExportApi<IGitHubApi>(new GitHubApi(this));

        State = ExtensionState.Active;
        _context.Logger.LogInformation("GitHub extension activated");
    }

    private async Task ConnectAsync(object?[] args)
    {
        var api = _context.Api;

        // Show authentication options
        var choice = await api.Window.ShowQuickPickAsync(new[]
        {
            new QuickPickItem<string>("Personal Access Token", "pat", "Use a GitHub PAT"),
            new QuickPickItem<string>("OAuth (Browser)", "oauth", "Sign in via browser")
        });

        if (choice == null) return;

        if (choice == "pat")
        {
            var token = await api.Window.ShowInputBoxAsync(new InputBoxOptions
            {
                Title = "GitHub Token",
                Prompt = "Enter your GitHub Personal Access Token",
                Password = true,
                ValidateInput = input => string.IsNullOrEmpty(input) ? "Token is required" : null
            });

            if (!string.IsNullOrEmpty(token))
            {
                await _context.Secrets.SetAsync("github.token", token);
                await ConnectWithTokenAsync(token);
            }
        }
        else
        {
            // Use built-in auth provider
            var session = await api.Authentication.GetSessionAsync(
                "github",
                new[] { "repo", "user" },
                new GetSessionOptions { CreateIfNone = true });

            if (session != null)
            {
                await ConnectWithTokenAsync(session.AccessToken);
            }
        }
    }

    private async Task ConnectWithTokenAsync(string token)
    {
        try
        {
            _client = new GitHubClient(new ProductHeaderValue("Arcana"))
            {
                Credentials = new Credentials(token)
            };

            var user = await _client.User.Current();

            _statusBarItem!.Text = $"$(github) {user.Login}";
            _statusBarItem.Tooltip = $"Connected as {user.Name ?? user.Login}";
            _statusBarItem.Command = "github.showMenu";

            // Set context for when clauses
            await _context.Api.Commands.ExecuteCommandAsync(
                "setContext",
                "github.connected",
                true);

            await _context.Api.Window.ShowInformationMessageAsync(
                $"Connected to GitHub as {user.Login}");
        }
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, "Failed to connect to GitHub");
            await _context.Api.Window.ShowErrorMessageAsync(
                $"Failed to connect: {ex.Message}");
        }
    }

    private async Task CreatePullRequestAsync(object?[] args)
    {
        if (_client == null)
        {
            await _context.Api.Window.ShowWarningMessageAsync(
                "Please connect to GitHub first",
                "Connect").ContinueWith(async result =>
            {
                if (result.Result == "Connect")
                {
                    await ConnectAsync(Array.Empty<object?>());
                }
            });
            return;
        }

        // Show progress
        await _context.Api.Progress.WithProgressAsync(
            new ProgressOptions(ProgressLocation.Notification, "Creating Pull Request", true),
            async (progress, token) =>
            {
                progress.Report(new ProgressReport("Gathering branch info..."));

                // Implementation...

                progress.Report(new ProgressReport("Creating PR...", 50));

                // Create PR via API...

                await _context.Api.Window.ShowInformationMessageAsync(
                    "Pull Request created successfully!",
                    "View on GitHub");
            });
    }

    public async Task DeactivateAsync()
    {
        State = ExtensionState.Deactivating;

        // Cleanup
        _client = null;

        await _context.Api.Commands.ExecuteCommandAsync(
            "setContext",
            "github.connected",
            false);

        State = ExtensionState.Deactivated;
        _context.Logger.LogInformation("GitHub extension deactivated");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }
}

// Tree Data Provider
public class GitHubRepositoryTreeProvider : ITreeDataProvider<GitHubRepoItem>
{
    private readonly GitHubExtension _extension;

    public event EventHandler<TreeDataChangedEventArgs<GitHubRepoItem>?>? OnDidChangeTreeData;

    public GitHubRepositoryTreeProvider(GitHubExtension extension)
    {
        _extension = extension;
    }

    public async Task<IReadOnlyList<GitHubRepoItem>> GetChildrenAsync(GitHubRepoItem? element)
    {
        if (element == null)
        {
            // Root level - return repositories
            var repos = await _extension.GetRepositoriesAsync();
            return repos.Select(r => new GitHubRepoItem(r)).ToList();
        }

        // Return branches for a repository
        var branches = await _extension.GetBranchesAsync(element.Repository);
        return branches.Select(b => new GitHubRepoItem(element.Repository, b)).ToList();
    }

    public Task<GitHubRepoItem?> GetParentAsync(GitHubRepoItem element) =>
        Task.FromResult<GitHubRepoItem?>(null);

    public TreeItem GetTreeItem(GitHubRepoItem element)
    {
        if (element.Branch == null)
        {
            return new TreeItem(
                element.Repository.Name,
                TreeItemCollapsibleState.Collapsed,
                Description: element.Repository.Private ? "Private" : "Public",
                IconPath: "$(repo)",
                ContextValue: "repository"
            );
        }

        return new TreeItem(
            element.Branch.Name,
            TreeItemCollapsibleState.None,
            IconPath: element.Branch.Name == element.Repository.DefaultBranch
                ? "$(git-branch)"
                : "$(git-merge)",
            ContextValue: "branch"
        );
    }

    public void Refresh() => OnDidChangeTreeData?.Invoke(this, null);
}

// API exported to other extensions
public interface IGitHubApi
{
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync();
    Task<Repository?> GetRepositoryAsync(string owner, string name);
    Task<PullRequest> CreatePullRequestAsync(NewPullRequest pr);
    bool IsConnected { get; }
}
```

#### 4.6 Extension Marketplace & Installation

```csharp
// ============================================
// EXTENSION MARKETPLACE SERVICE
// ============================================

public interface IExtensionGalleryService
{
    /// <summary>Search for extensions</summary>
    Task<ExtensionSearchResult> SearchAsync(string query, int page = 1, int pageSize = 20);

    /// <summary>Get extension details</summary>
    Task<ExtensionDetails?> GetExtensionAsync(string extensionId);

    /// <summary>Install extension from gallery</summary>
    Task<InstallResult> InstallAsync(string extensionId, string? version = null, IProgress<int>? progress = null);

    /// <summary>Uninstall extension</summary>
    Task UninstallAsync(string extensionId);

    /// <summary>Check for updates</summary>
    Task<IReadOnlyList<ExtensionUpdate>> CheckForUpdatesAsync();

    /// <summary>Update extension</summary>
    Task<InstallResult> UpdateAsync(string extensionId, IProgress<int>? progress = null);
}

public record ExtensionSearchResult(
    IReadOnlyList<ExtensionSummary> Extensions,
    int TotalCount,
    int Page,
    int PageSize
);

public record ExtensionSummary(
    string Id,
    string Name,
    string Publisher,
    string Version,
    string Description,
    string IconUrl,
    int InstallCount,
    double Rating,
    DateTime LastUpdated
);

public record ExtensionDetails(
    string Id,
    string Name,
    string Publisher,
    string Version,
    string Description,
    string FullDescription,
    string IconUrl,
    string RepositoryUrl,
    string LicenseUrl,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    int InstallCount,
    double Rating,
    int RatingCount,
    IReadOnlyList<ExtensionVersion> Versions,
    ExtensionManifest Manifest
);

public class ExtensionGalleryService : IExtensionGalleryService
{
    private readonly HttpClient _httpClient;
    private readonly IExtensionManager _extensionManager;
    private readonly string _extensionsPath;
    private readonly ILogger<ExtensionGalleryService> _logger;

    private const string GalleryBaseUrl = "https://marketplace.arcana.dev/api";

    public async Task<InstallResult> InstallAsync(
        string extensionId,
        string? version = null,
        IProgress<int>? progress = null)
    {
        try
        {
            progress?.Report(0);

            // Get extension info
            var extension = await GetExtensionAsync(extensionId);
            if (extension == null)
            {
                return InstallResult.Failure($"Extension '{extensionId}' not found");
            }

            version ??= extension.Version;
            progress?.Report(10);

            // Download VSIX package
            var packageUrl = $"{GalleryBaseUrl}/extensions/{extensionId}/{version}/download";
            var packagePath = Path.Combine(Path.GetTempPath(), $"{extensionId}-{version}.vsix");

            using (var response = await _httpClient.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var downloadedBytes = 0L;

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(packagePath);

                var buffer = new byte[81920];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var downloadProgress = (int)(10 + (downloadedBytes * 60 / totalBytes));
                        progress?.Report(downloadProgress);
                    }
                }
            }

            progress?.Report(70);

            // Extract to extensions folder
            var extensionPath = Path.Combine(_extensionsPath, extensionId);
            if (Directory.Exists(extensionPath))
            {
                Directory.Delete(extensionPath, recursive: true);
            }

            ZipFile.ExtractToDirectory(packagePath, extensionPath);
            progress?.Report(90);

            // Cleanup temp file
            File.Delete(packagePath);

            // Reload extension
            await _extensionManager.DiscoverExtensionsAsync(_extensionsPath);
            progress?.Report(100);

            _logger.LogInformation("Installed extension {ExtensionId} v{Version}", extensionId, version);

            return InstallResult.Success(extensionId, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install extension {ExtensionId}", extensionId);
            return InstallResult.Failure(ex.Message);
        }
    }
}
```

#### 4.7 Project Structure for Plugin System

```
Arcana.Windows/
├── src/
│   ├── Arcana.Plugins/                       # Plugin Infrastructure
│   │   ├── Abstractions/                     # Core interfaces
│   │   │   ├── IExtension.cs
│   │   │   ├── IExtensionContext.cs
│   │   │   ├── IExtensionApi.cs
│   │   │   └── IExtensionManager.cs
│   │   │
│   │   ├── Api/                              # Extension API implementations
│   │   │   ├── CommandService.cs
│   │   │   ├── WindowService.cs
│   │   │   ├── StatusBarService.cs
│   │   │   ├── TreeViewService.cs
│   │   │   ├── WebViewService.cs
│   │   │   ├── ConfigurationService.cs
│   │   │   ├── AuthenticationService.cs
│   │   │   ├── ProgressService.cs
│   │   │   ├── LanguageService.cs
│   │   │   └── ExtensionBus.cs
│   │   │
│   │   ├── Contribution/                     # Contribution point registries
│   │   │   ├── CommandRegistry.cs
│   │   │   ├── MenuRegistry.cs
│   │   │   ├── ViewRegistry.cs
│   │   │   ├── ConfigurationRegistry.cs
│   │   │   ├── KeybindingRegistry.cs
│   │   │   └── ThemeRegistry.cs
│   │   │
│   │   ├── Hosting/                          # Plugin hosting
│   │   │   ├── ExtensionManager.cs
│   │   │   ├── ExtensionHost.cs
│   │   │   ├── PluginAssemblyLoadContext.cs
│   │   │   ├── ExtensionContext.cs
│   │   │   └── ActivationEventService.cs
│   │   │
│   │   ├── Manifest/                         # Manifest models
│   │   │   ├── ExtensionManifest.cs
│   │   │   ├── ContributionPoints.cs
│   │   │   ├── CommandContribution.cs
│   │   │   ├── MenuContribution.cs
│   │   │   ├── ViewContribution.cs
│   │   │   └── ConfigurationContribution.cs
│   │   │
│   │   ├── Gallery/                          # Marketplace
│   │   │   ├── IExtensionGalleryService.cs
│   │   │   ├── ExtensionGalleryService.cs
│   │   │   └── Models/
│   │   │
│   │   └── Storage/                          # Extension state
│   │       ├── ExtensionMemento.cs
│   │       ├── SecretStorage.cs
│   │       └── ExtensionStorageService.cs
│   │
│   └── Arcana.Plugins.Contracts/             # Shared contracts (referenced by plugins)
│       ├── IDataSourcePlugin.cs
│       ├── IAnalyticsPlugin.cs
│       ├── IAuthPlugin.cs
│       ├── IExportPlugin.cs
│       ├── IThemePlugin.cs
│       └── ILanguagePlugin.cs
│
├── plugins/                                  # Built-in plugins
│   ├── Arcana.Plugin.GitHub/
│   │   ├── plugin.json
│   │   ├── GitHubExtension.cs
│   │   ├── Services/
│   │   ├── TreeProviders/
│   │   └── assets/
│   │
│   ├── Arcana.Plugin.AzureAD/
│   │   ├── plugin.json
│   │   └── ...
│   │
│   └── Arcana.Plugin.Excel/
│       ├── plugin.json
│       └── ...
│
└── extensions/                               # User-installed plugins (runtime)
    └── ...
```

---

## Extension Point Design Reference (Eclipse & VS Code Patterns)

This section provides a comprehensive design reference for extension points, comparing patterns from Eclipse (OSGi/Equinox) and VS Code, adapted for C# and WinUI3.

### Comparison: Eclipse vs VS Code Extension Models

| Aspect | Eclipse (OSGi) | VS Code | Arcana (Recommended) |
|--------|---------------|---------|---------------------|
| **Manifest** | plugin.xml + MANIFEST.MF | package.json | plugin.json |
| **Extension Points** | Declarative XML schemas | Contribution Points (JSON) | Contribution Points (JSON) |
| **Lazy Loading** | Bundle activation | Activation Events | Activation Events |
| **Isolation** | OSGi classloaders | Process isolation (Extension Host) | AssemblyLoadContext |
| **Communication** | OSGi services, Extension Registry | Commands, Events, API export | Commands, Events, Extension Bus |
| **Dependencies** | Require-Bundle, Import-Package | extensionDependencies | extensionDependencies |
| **Lifecycle** | start/stop (BundleActivator) | activate/deactivate | ActivateAsync/DeactivateAsync |

---

### 1. Eclipse-Style Extension Points

Eclipse uses a powerful declarative extension point system where the host defines extension point schemas and plugins contribute extensions.

#### 1.1 Extension Point Schema Definition (Eclipse Pattern)

```csharp
// ============================================
// EXTENSION POINT REGISTRY (Eclipse-Style)
// ============================================

/// <summary>
/// Defines an extension point that plugins can contribute to.
/// Similar to Eclipse's plugin.xml extension-point element.
/// </summary>
public interface IExtensionPoint
{
    /// <summary>Unique identifier (e.g., "arcana.views", "arcana.commands")</summary>
    string Id { get; }

    /// <summary>Human-readable name</summary>
    string Name { get; }

    /// <summary>Schema defining valid contributions</summary>
    ExtensionPointSchema Schema { get; }

    /// <summary>Namespace/category</summary>
    string Namespace { get; }
}

/// <summary>
/// Schema definition for extension point contributions.
/// Similar to Eclipse's .exsd schema files.
/// </summary>
public class ExtensionPointSchema
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<SchemaElement> Elements { get; init; } = Array.Empty<SchemaElement>();
}

public class SchemaElement
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ElementCardinality Cardinality { get; init; } = ElementCardinality.ZeroOrMore;
    public IReadOnlyList<SchemaAttribute> Attributes { get; init; } = Array.Empty<SchemaAttribute>();
    public IReadOnlyList<SchemaElement> Children { get; init; } = Array.Empty<SchemaElement>();
}

public class SchemaAttribute
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public AttributeType Type { get; init; } = AttributeType.String;
    public bool Required { get; init; } = false;
    public string? DefaultValue { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }
}

public enum ElementCardinality { One, ZeroOrOne, ZeroOrMore, OneOrMore }
public enum AttributeType { String, Boolean, Integer, Class, Resource, Identifier }

// ============================================
// EXTENSION POINT DEFINITIONS
// ============================================

public static class CoreExtensionPoints
{
    /// <summary>
    /// Extension point for contributing commands.
    /// Plugins contribute command handlers that can be invoked by ID.
    /// </summary>
    public static readonly ExtensionPointSchema Commands = new()
    {
        Id = "arcana.commands",
        Name = "Commands",
        Description = "Contributes executable commands to the application",
        Elements = new[]
        {
            new SchemaElement
            {
                Name = "command",
                Description = "A command contribution",
                Cardinality = ElementCardinality.ZeroOrMore,
                Attributes = new[]
                {
                    new SchemaAttribute { Name = "id", Type = AttributeType.Identifier, Required = true },
                    new SchemaAttribute { Name = "name", Type = AttributeType.String, Required = true },
                    new SchemaAttribute { Name = "description", Type = AttributeType.String },
                    new SchemaAttribute { Name = "category", Type = AttributeType.String },
                    new SchemaAttribute { Name = "icon", Type = AttributeType.Resource },
                    new SchemaAttribute { Name = "handler", Type = AttributeType.Class, Required = true,
                        Description = "Fully qualified class name implementing ICommandHandler" },
                    new SchemaAttribute { Name = "enabledWhen", Type = AttributeType.String,
                        Description = "Context expression for when command is enabled" }
                }
            }
        }
    };

    /// <summary>
    /// Extension point for contributing views (panels, sidebars).
    /// </summary>
    public static readonly ExtensionPointSchema Views = new()
    {
        Id = "arcana.views",
        Name = "Views",
        Description = "Contributes view panels to the application",
        Elements = new[]
        {
            new SchemaElement
            {
                Name = "category",
                Description = "A view category (container)",
                Cardinality = ElementCardinality.ZeroOrMore,
                Attributes = new[]
                {
                    new SchemaAttribute { Name = "id", Type = AttributeType.Identifier, Required = true },
                    new SchemaAttribute { Name = "name", Type = AttributeType.String, Required = true },
                    new SchemaAttribute { Name = "icon", Type = AttributeType.Resource }
                },
                Children = new[]
                {
                    new SchemaElement
                    {
                        Name = "view",
                        Description = "A view contribution",
                        Cardinality = ElementCardinality.ZeroOrMore,
                        Attributes = new[]
                        {
                            new SchemaAttribute { Name = "id", Type = AttributeType.Identifier, Required = true },
                            new SchemaAttribute { Name = "name", Type = AttributeType.String, Required = true },
                            new SchemaAttribute { Name = "class", Type = AttributeType.Class, Required = true },
                            new SchemaAttribute { Name = "icon", Type = AttributeType.Resource },
                            new SchemaAttribute { Name = "allowMultiple", Type = AttributeType.Boolean, DefaultValue = "false" }
                        }
                    }
                }
            }
        }
    };

    /// <summary>
    /// Extension point for contributing menu items.
    /// </summary>
    public static readonly ExtensionPointSchema Menus = new()
    {
        Id = "arcana.menus",
        Name = "Menus",
        Description = "Contributes menu items to various locations",
        Elements = new[]
        {
            new SchemaElement
            {
                Name = "menuContribution",
                Description = "Menu contribution to a specific location",
                Cardinality = ElementCardinality.ZeroOrMore,
                Attributes = new[]
                {
                    new SchemaAttribute { Name = "locationUri", Type = AttributeType.String, Required = true,
                        Description = "URI identifying the menu location (e.g., 'menu:file', 'toolbar:main', 'popup:editor')" }
                },
                Children = new[]
                {
                    new SchemaElement
                    {
                        Name = "command",
                        Description = "Command reference",
                        Attributes = new[]
                        {
                            new SchemaAttribute { Name = "commandId", Type = AttributeType.Identifier, Required = true },
                            new SchemaAttribute { Name = "label", Type = AttributeType.String },
                            new SchemaAttribute { Name = "icon", Type = AttributeType.Resource },
                            new SchemaAttribute { Name = "visibleWhen", Type = AttributeType.String }
                        }
                    },
                    new SchemaElement
                    {
                        Name = "separator",
                        Description = "Menu separator",
                        Attributes = new[]
                        {
                            new SchemaAttribute { Name = "name", Type = AttributeType.String }
                        }
                    },
                    new SchemaElement
                    {
                        Name = "menu",
                        Description = "Submenu",
                        Attributes = new[]
                        {
                            new SchemaAttribute { Name = "id", Type = AttributeType.Identifier, Required = true },
                            new SchemaAttribute { Name = "label", Type = AttributeType.String, Required = true },
                            new SchemaAttribute { Name = "icon", Type = AttributeType.Resource }
                        }
                    }
                }
            }
        }
    };

    /// <summary>
    /// Extension point for contributing editors.
    /// </summary>
    public static readonly ExtensionPointSchema Editors = new()
    {
        Id = "arcana.editors",
        Name = "Editors",
        Description = "Contributes document editors",
        Elements = new[]
        {
            new SchemaElement
            {
                Name = "editor",
                Description = "An editor contribution",
                Cardinality = ElementCardinality.ZeroOrMore,
                Attributes = new[]
                {
                    new SchemaAttribute { Name = "id", Type = AttributeType.Identifier, Required = true },
                    new SchemaAttribute { Name = "name", Type = AttributeType.String, Required = true },
                    new SchemaAttribute { Name = "class", Type = AttributeType.Class, Required = true },
                    new SchemaAttribute { Name = "icon", Type = AttributeType.Resource },
                    new SchemaAttribute { Name = "extensions", Type = AttributeType.String,
                        Description = "Comma-separated file extensions (e.g., '.txt,.md')" },
                    new SchemaAttribute { Name = "contentTypes", Type = AttributeType.String,
                        Description = "Comma-separated content type IDs" },
                    new SchemaAttribute { Name = "default", Type = AttributeType.Boolean, DefaultValue = "false" }
                }
            }
        }
    };

    /// <summary>
    /// Extension point for contributing preferences/settings pages.
    /// </summary>
    public static readonly ExtensionPointSchema Preferences = new()
    {
        Id = "arcana.preferences",
        Name = "Preferences",
        Description = "Contributes preference pages",
        Elements = new[]
        {
            new SchemaElement
            {
                Name = "page",
                Description = "A preference page",
                Cardinality = ElementCardinality.ZeroOrMore,
                Attributes = new[]
                {
                    new SchemaAttribute { Name = "id", Type = AttributeType.Identifier, Required = true },
                    new SchemaAttribute { Name = "name", Type = AttributeType.String, Required = true },
                    new SchemaAttribute { Name = "class", Type = AttributeType.Class, Required = true },
                    new SchemaAttribute { Name = "category", Type = AttributeType.Identifier,
                        Description = "Parent page ID for hierarchical organization" },
                    new SchemaAttribute { Name = "icon", Type = AttributeType.Resource }
                }
            }
        }
    };
}
```

#### 1.2 Extension Registry (Eclipse Pattern)

```csharp
// ============================================
// EXTENSION REGISTRY (Central Repository)
// ============================================

/// <summary>
/// Central registry for all extension points and extensions.
/// Similar to Eclipse's IExtensionRegistry.
/// </summary>
public interface IExtensionRegistry
{
    // Extension Point Management
    void RegisterExtensionPoint(IExtensionPoint extensionPoint);
    IExtensionPoint? GetExtensionPoint(string extensionPointId);
    IReadOnlyList<IExtensionPoint> GetExtensionPoints();
    IReadOnlyList<IExtensionPoint> GetExtensionPoints(string @namespace);

    // Extension Management
    void RegisterExtension(string extensionPointId, IExtensionContribution contribution);
    IReadOnlyList<IExtensionContribution> GetExtensions(string extensionPointId);
    IExtensionContribution? GetExtension(string extensionPointId, string extensionId);

    // Configuration Elements (like Eclipse's IConfigurationElement)
    IReadOnlyList<IConfigurationElement> GetConfigurationElements(string extensionPointId);
    IReadOnlyList<IConfigurationElement> GetConfigurationElements(string extensionPointId, string elementName);

    // Events
    event EventHandler<ExtensionRegistryEventArgs> ExtensionAdded;
    event EventHandler<ExtensionRegistryEventArgs> ExtensionRemoved;
}

/// <summary>
/// Represents an extension contribution from a plugin.
/// </summary>
public interface IExtensionContribution
{
    string ExtensionPointId { get; }
    string ContributorId { get; }
    string ExtensionId { get; }
    IReadOnlyList<IConfigurationElement> ConfigurationElements { get; }
}

/// <summary>
/// Represents a configuration element within an extension.
/// Similar to Eclipse's IConfigurationElement.
/// </summary>
public interface IConfigurationElement
{
    string Name { get; }
    string? Value { get; }
    IConfigurationElement? Parent { get; }
    IReadOnlyList<IConfigurationElement> Children { get; }

    // Attribute access
    string? GetAttribute(string name);
    T? GetAttribute<T>(string name);
    IReadOnlyDictionary<string, string> GetAttributes();

    // Executable extension creation (lazy instantiation)
    T CreateExecutableExtension<T>(string attributeName) where T : class;
    Task<T> CreateExecutableExtensionAsync<T>(string attributeName) where T : class;

    // Contributor info
    string ContributorId { get; }
    IExtensionContribution DeclaringExtension { get; }
}

// ============================================
// EXTENSION REGISTRY IMPLEMENTATION
// ============================================

public class ExtensionRegistry : IExtensionRegistry
{
    private readonly Dictionary<string, IExtensionPoint> _extensionPoints = new();
    private readonly Dictionary<string, List<IExtensionContribution>> _extensions = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExtensionRegistry> _logger;

    public ExtensionRegistry(IServiceProvider serviceProvider, ILogger<ExtensionRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Register core extension points
        RegisterCoreExtensionPoints();
    }

    private void RegisterCoreExtensionPoints()
    {
        RegisterExtensionPoint(new ExtensionPoint("arcana.commands", "Commands", CoreExtensionPoints.Commands));
        RegisterExtensionPoint(new ExtensionPoint("arcana.views", "Views", CoreExtensionPoints.Views));
        RegisterExtensionPoint(new ExtensionPoint("arcana.menus", "Menus", CoreExtensionPoints.Menus));
        RegisterExtensionPoint(new ExtensionPoint("arcana.editors", "Editors", CoreExtensionPoints.Editors));
        RegisterExtensionPoint(new ExtensionPoint("arcana.preferences", "Preferences", CoreExtensionPoints.Preferences));
    }

    public void RegisterExtensionPoint(IExtensionPoint extensionPoint)
    {
        _extensionPoints[extensionPoint.Id] = extensionPoint;
        _extensions[extensionPoint.Id] = new List<IExtensionContribution>();

        _logger.LogInformation("Registered extension point: {ExtensionPointId}", extensionPoint.Id);
    }

    public void RegisterExtension(string extensionPointId, IExtensionContribution contribution)
    {
        if (!_extensions.TryGetValue(extensionPointId, out var extensions))
        {
            throw new InvalidOperationException($"Unknown extension point: {extensionPointId}");
        }

        // Validate contribution against schema
        var extensionPoint = _extensionPoints[extensionPointId];
        ValidateContribution(extensionPoint.Schema, contribution);

        extensions.Add(contribution);

        _logger.LogInformation(
            "Registered extension {ExtensionId} to {ExtensionPointId} from {ContributorId}",
            contribution.ExtensionId,
            extensionPointId,
            contribution.ContributorId);

        ExtensionAdded?.Invoke(this, new ExtensionRegistryEventArgs(extensionPointId, contribution));
    }

    public IReadOnlyList<IConfigurationElement> GetConfigurationElements(string extensionPointId)
    {
        if (!_extensions.TryGetValue(extensionPointId, out var extensions))
        {
            return Array.Empty<IConfigurationElement>();
        }

        return extensions
            .SelectMany(e => e.ConfigurationElements)
            .ToList();
    }

    public IReadOnlyList<IConfigurationElement> GetConfigurationElements(string extensionPointId, string elementName)
    {
        return GetConfigurationElements(extensionPointId)
            .Where(e => e.Name == elementName)
            .ToList();
    }

    // ... other interface implementations

    public event EventHandler<ExtensionRegistryEventArgs>? ExtensionAdded;
    public event EventHandler<ExtensionRegistryEventArgs>? ExtensionRemoved;
}

/// <summary>
/// Configuration element implementation with lazy executable creation.
/// </summary>
public class ConfigurationElement : IConfigurationElement
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, string> _attributes;
    private readonly List<IConfigurationElement> _children;

    public string Name { get; }
    public string? Value { get; }
    public IConfigurationElement? Parent { get; }
    public IReadOnlyList<IConfigurationElement> Children => _children;
    public string ContributorId { get; }
    public IExtensionContribution DeclaringExtension { get; }

    public string? GetAttribute(string name) =>
        _attributes.TryGetValue(name, out var value) ? value : null;

    public IReadOnlyDictionary<string, string> GetAttributes() => _attributes;

    /// <summary>
    /// Creates an instance of the class specified in the given attribute.
    /// This enables lazy loading - the plugin is only activated when needed.
    /// </summary>
    public T CreateExecutableExtension<T>(string attributeName) where T : class
    {
        var className = GetAttribute(attributeName)
            ?? throw new InvalidOperationException($"Attribute '{attributeName}' not found");

        // This triggers plugin activation if not already active
        var type = ResolveType(className);
        return (T)ActivatorUtilities.CreateInstance(_serviceProvider, type);
    }

    public async Task<T> CreateExecutableExtensionAsync<T>(string attributeName) where T : class
    {
        // Ensure contributing plugin is activated first
        var extensionManager = _serviceProvider.GetRequiredService<IExtensionManager>();
        await extensionManager.ActivateExtensionAsync(ContributorId, $"executable extension: {attributeName}");

        return CreateExecutableExtension<T>(attributeName);
    }

    private Type ResolveType(string className)
    {
        // Resolve type from the contributing plugin's assembly
        var extensionManager = _serviceProvider.GetRequiredService<IExtensionManager>();
        return extensionManager.ResolveType(ContributorId, className);
    }
}
```

#### 1.3 Eclipse-Style Plugin Manifest

```xml
<!-- Eclipse-style plugin.xml (for reference, we use JSON) -->
<?xml version="1.0" encoding="UTF-8"?>
<?eclipse version="3.4"?>
<plugin>
   <extension point="org.eclipse.ui.views">
      <category
            id="com.example.category"
            name="Example Views">
      </category>
      <view
            id="com.example.views.sample"
            name="Sample View"
            category="com.example.category"
            class="com.example.views.SampleView"
            icon="icons/sample.png">
      </view>
   </extension>

   <extension point="org.eclipse.ui.commands">
      <command
            id="com.example.commands.sampleCommand"
            name="Sample Command"
            description="A sample command"
            categoryId="com.example.category">
      </command>
   </extension>

   <extension point="org.eclipse.ui.handlers">
      <handler
            commandId="com.example.commands.sampleCommand"
            class="com.example.handlers.SampleHandler">
         <enabledWhen>
            <with variable="selection">
               <instanceof value="com.example.model.SampleElement"/>
            </with>
         </enabledWhen>
      </handler>
   </extension>
</plugin>
```

```json
// Equivalent Arcana plugin.json (Eclipse-inspired)
{
  "id": "arcana.plugin.sample",
  "version": "1.0.0",

  "extensions": [
    {
      "point": "arcana.views",
      "elements": [
        {
          "category": {
            "id": "sample.category",
            "name": "Sample Views"
          }
        },
        {
          "view": {
            "id": "sample.views.main",
            "name": "Sample View",
            "category": "sample.category",
            "class": "SampleViewProvider",
            "icon": "assets/sample.png"
          }
        }
      ]
    },
    {
      "point": "arcana.commands",
      "elements": [
        {
          "command": {
            "id": "sample.commands.doSomething",
            "name": "Do Something",
            "description": "Performs a sample action",
            "handler": "SampleCommandHandler",
            "enabledWhen": "selection.type == 'SampleElement'"
          }
        }
      ]
    },
    {
      "point": "arcana.menus",
      "elements": [
        {
          "menuContribution": {
            "locationUri": "menu:tools",
            "items": [
              {
                "command": {
                  "commandId": "sample.commands.doSomething",
                  "label": "Do Something",
                  "icon": "$(gear)"
                }
              }
            ]
          }
        }
      ]
    }
  ]
}
```

---

### 2. VS Code-Style Contribution Points

VS Code uses a simpler, JSON-based contribution point system that's easier to understand but equally powerful.

#### 2.1 Contribution Point Definitions

```csharp
// ============================================
// VS CODE-STYLE CONTRIBUTION POINTS
// ============================================

/// <summary>
/// Defines all contribution points available for extensions.
/// Each contribution point has a JSON schema for validation.
/// </summary>
public static class ContributionPoints
{
    // ===== COMMANDS =====
    public static readonly ContributionPointDefinition Commands = new()
    {
        Id = "commands",
        Description = "Contributes commands that can be invoked via command palette or keybindings",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["command", "title"],
            "properties": {
              "command": { "type": "string", "description": "Unique command identifier" },
              "title": { "type": "string", "description": "Human-readable title" },
              "category": { "type": "string", "description": "Category for grouping" },
              "icon": { "type": "string", "description": "Icon reference" },
              "enablement": { "type": "string", "description": "When expression" }
            }
          }
        }
        """
    };

    // ===== MENUS =====
    public static readonly ContributionPointDefinition Menus = new()
    {
        Id = "menus",
        Description = "Contributes menu items to various locations",
        JsonSchema = """
        {
          "type": "object",
          "properties": {
            "commandPalette": { "$ref": "#/definitions/menuItems" },
            "navigation": { "$ref": "#/definitions/menuItems" },
            "context": { "$ref": "#/definitions/menuItems" },
            "view/title": { "$ref": "#/definitions/menuItems" },
            "view/item/context": { "$ref": "#/definitions/menuItems" }
          },
          "definitions": {
            "menuItems": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["command"],
                "properties": {
                  "command": { "type": "string" },
                  "when": { "type": "string" },
                  "group": { "type": "string" },
                  "alt": { "type": "string" }
                }
              }
            }
          }
        }
        """
    };

    // ===== VIEWS =====
    public static readonly ContributionPointDefinition Views = new()
    {
        Id = "views",
        Description = "Contributes views to view containers",
        JsonSchema = """
        {
          "type": "object",
          "additionalProperties": {
            "type": "array",
            "items": {
              "type": "object",
              "required": ["id", "name"],
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" },
                "when": { "type": "string" },
                "icon": { "type": "string" },
                "contextualTitle": { "type": "string" },
                "visibility": { "enum": ["visible", "hidden", "collapsed"] }
              }
            }
          }
        }
        """
    };

    // ===== VIEWS CONTAINERS =====
    public static readonly ContributionPointDefinition ViewsContainers = new()
    {
        Id = "viewsContainers",
        Description = "Contributes view containers (sidebar sections)",
        JsonSchema = """
        {
          "type": "object",
          "properties": {
            "activitybar": {
              "type": "array",
              "items": {
                "type": "object",
                "required": ["id", "title", "icon"],
                "properties": {
                  "id": { "type": "string" },
                  "title": { "type": "string" },
                  "icon": { "type": "string" }
                }
              }
            },
            "panel": {
              "type": "array",
              "items": { "$ref": "#/properties/activitybar/items" }
            }
          }
        }
        """
    };

    // ===== CONFIGURATION =====
    public static readonly ContributionPointDefinition Configuration = new()
    {
        Id = "configuration",
        Description = "Contributes configuration settings",
        JsonSchema = """
        {
          "type": "object",
          "properties": {
            "title": { "type": "string" },
            "order": { "type": "integer" },
            "properties": {
              "type": "object",
              "additionalProperties": {
                "type": "object",
                "properties": {
                  "type": { "enum": ["string", "boolean", "number", "integer", "array", "object"] },
                  "default": {},
                  "description": { "type": "string" },
                  "enum": { "type": "array" },
                  "enumDescriptions": { "type": "array", "items": { "type": "string" } },
                  "minimum": { "type": "number" },
                  "maximum": { "type": "number" },
                  "scope": { "enum": ["application", "machine", "window", "resource", "language-overridable"] },
                  "order": { "type": "integer" }
                }
              }
            }
          }
        }
        """
    };

    // ===== KEYBINDINGS =====
    public static readonly ContributionPointDefinition Keybindings = new()
    {
        Id = "keybindings",
        Description = "Contributes keyboard shortcuts",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["command", "key"],
            "properties": {
              "command": { "type": "string" },
              "key": { "type": "string" },
              "mac": { "type": "string" },
              "linux": { "type": "string" },
              "win": { "type": "string" },
              "when": { "type": "string" },
              "args": {}
            }
          }
        }
        """
    };

    // ===== LANGUAGES =====
    public static readonly ContributionPointDefinition Languages = new()
    {
        Id = "languages",
        Description = "Contributes language declarations",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["id"],
            "properties": {
              "id": { "type": "string" },
              "aliases": { "type": "array", "items": { "type": "string" } },
              "extensions": { "type": "array", "items": { "type": "string" } },
              "filenames": { "type": "array", "items": { "type": "string" } },
              "filenamePatterns": { "type": "array", "items": { "type": "string" } },
              "firstLine": { "type": "string" },
              "configuration": { "type": "string" },
              "icon": {
                "type": "object",
                "properties": {
                  "light": { "type": "string" },
                  "dark": { "type": "string" }
                }
              }
            }
          }
        }
        """
    };

    // ===== GRAMMARS (Syntax Highlighting) =====
    public static readonly ContributionPointDefinition Grammars = new()
    {
        Id = "grammars",
        Description = "Contributes TextMate grammars for syntax highlighting",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["language", "scopeName", "path"],
            "properties": {
              "language": { "type": "string" },
              "scopeName": { "type": "string" },
              "path": { "type": "string" },
              "embeddedLanguages": {
                "type": "object",
                "additionalProperties": { "type": "string" }
              },
              "tokenTypes": {
                "type": "object",
                "additionalProperties": { "type": "string" }
              },
              "injectTo": { "type": "array", "items": { "type": "string" } }
            }
          }
        }
        """
    };

    // ===== THEMES =====
    public static readonly ContributionPointDefinition Themes = new()
    {
        Id = "themes",
        Description = "Contributes color themes",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["label", "uiTheme", "path"],
            "properties": {
              "id": { "type": "string" },
              "label": { "type": "string" },
              "uiTheme": { "enum": ["vs", "vs-dark", "hc-black", "hc-light"] },
              "path": { "type": "string" }
            }
          }
        }
        """
    };

    // ===== ICON THEMES =====
    public static readonly ContributionPointDefinition IconThemes = new()
    {
        Id = "iconThemes",
        Description = "Contributes file icon themes",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["id", "label", "path"],
            "properties": {
              "id": { "type": "string" },
              "label": { "type": "string" },
              "path": { "type": "string" }
            }
          }
        }
        """
    };

    // ===== SNIPPETS =====
    public static readonly ContributionPointDefinition Snippets = new()
    {
        Id = "snippets",
        Description = "Contributes code snippets",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["language", "path"],
            "properties": {
              "language": { "type": "string" },
              "path": { "type": "string" }
            }
          }
        }
        """
    };

    // ===== TASK DEFINITIONS =====
    public static readonly ContributionPointDefinition TaskDefinitions = new()
    {
        Id = "taskDefinitions",
        Description = "Contributes task types",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["type"],
            "properties": {
              "type": { "type": "string" },
              "required": { "type": "array", "items": { "type": "string" } },
              "properties": {
                "type": "object",
                "additionalProperties": {
                  "type": "object",
                  "properties": {
                    "type": { "type": "string" },
                    "description": { "type": "string" }
                  }
                }
              },
              "when": { "type": "string" }
            }
          }
        }
        """
    };

    // ===== PROBLEM MATCHERS =====
    public static readonly ContributionPointDefinition ProblemMatchers = new()
    {
        Id = "problemMatchers",
        Description = "Contributes problem matchers for parsing build output",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["name", "owner", "pattern"],
            "properties": {
              "name": { "type": "string" },
              "owner": { "type": "string" },
              "source": { "type": "string" },
              "severity": { "enum": ["error", "warning", "info"] },
              "fileLocation": { "type": ["string", "array"] },
              "pattern": {
                "oneOf": [
                  { "$ref": "#/definitions/patternType" },
                  { "type": "array", "items": { "$ref": "#/definitions/patternType" } }
                ]
              },
              "background": {
                "type": "object",
                "properties": {
                  "activeOnStart": { "type": "boolean" },
                  "beginsPattern": { "type": "string" },
                  "endsPattern": { "type": "string" }
                }
              }
            }
          }
        }
        """
    };

    // ===== DEBUGGERS =====
    public static readonly ContributionPointDefinition Debuggers = new()
    {
        Id = "debuggers",
        Description = "Contributes debug adapters",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["type", "label"],
            "properties": {
              "type": { "type": "string" },
              "label": { "type": "string" },
              "program": { "type": "string" },
              "runtime": { "type": "string" },
              "runtimeArgs": { "type": "array", "items": { "type": "string" } },
              "variables": { "type": "object" },
              "initialConfigurations": { "type": "array" },
              "configurationAttributes": { "type": "object" },
              "configurationSnippets": { "type": "array" },
              "languages": { "type": "array", "items": { "type": "string" } },
              "when": { "type": "string" }
            }
          }
        }
        """
    };

    // ===== BREAKPOINTS =====
    public static readonly ContributionPointDefinition Breakpoints = new()
    {
        Id = "breakpoints",
        Description = "Contributes breakpoint types",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["language"],
            "properties": {
              "language": { "type": "string" },
              "when": { "type": "string" }
            }
          }
        }
        """
    };

    // ===== WALKTHROUGHS =====
    public static readonly ContributionPointDefinition Walkthroughs = new()
    {
        Id = "walkthroughs",
        Description = "Contributes getting started walkthroughs",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["id", "title", "description", "steps"],
            "properties": {
              "id": { "type": "string" },
              "title": { "type": "string" },
              "description": { "type": "string" },
              "icon": { "type": "string" },
              "when": { "type": "string" },
              "featuredFor": { "type": "array", "items": { "type": "string" } },
              "steps": {
                "type": "array",
                "items": {
                  "type": "object",
                  "required": ["id", "title", "description"],
                  "properties": {
                    "id": { "type": "string" },
                    "title": { "type": "string" },
                    "description": { "type": "string" },
                    "media": {
                      "type": "object",
                      "properties": {
                        "image": { "type": "string" },
                        "altText": { "type": "string" },
                        "markdown": { "type": "string" }
                      }
                    },
                    "completionEvents": { "type": "array", "items": { "type": "string" } },
                    "when": { "type": "string" }
                  }
                }
              }
            }
          }
        }
        """
    };

    // ===== CUSTOM EDITORS =====
    public static readonly ContributionPointDefinition CustomEditors = new()
    {
        Id = "customEditors",
        Description = "Contributes custom editors for specific file types",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["viewType", "displayName", "selector"],
            "properties": {
              "viewType": { "type": "string" },
              "displayName": { "type": "string" },
              "selector": {
                "type": "array",
                "items": {
                  "type": "object",
                  "properties": {
                    "filenamePattern": { "type": "string" }
                  }
                }
              },
              "priority": { "enum": ["default", "option"] }
            }
          }
        }
        """
    };

    // ===== AUTHENTICATION PROVIDERS =====
    public static readonly ContributionPointDefinition Authentication = new()
    {
        Id = "authentication",
        Description = "Contributes authentication providers",
        JsonSchema = """
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["id", "label"],
            "properties": {
              "id": { "type": "string" },
              "label": { "type": "string" }
            }
          }
        }
        """
    };

    // Get all contribution points
    public static IReadOnlyList<ContributionPointDefinition> All => new[]
    {
        Commands, Menus, Views, ViewsContainers, Configuration,
        Keybindings, Languages, Grammars, Themes, IconThemes,
        Snippets, TaskDefinitions, ProblemMatchers, Debuggers,
        Breakpoints, Walkthroughs, CustomEditors, Authentication
    };
}

public record ContributionPointDefinition(
    string Id,
    string Description,
    string JsonSchema
)
{
    public ContributionPointDefinition() : this("", "", "{}") { }
}
```

#### 2.2 Contribution Processing

```csharp
// ============================================
// CONTRIBUTION PROCESSOR
// ============================================

/// <summary>
/// Processes and validates contributions from plugin manifests.
/// </summary>
public class ContributionProcessor
{
    private readonly Dictionary<string, IContributionHandler> _handlers = new();
    private readonly ILogger<ContributionProcessor> _logger;

    public ContributionProcessor(
        IEnumerable<IContributionHandler> handlers,
        ILogger<ContributionProcessor> logger)
    {
        _logger = logger;

        foreach (var handler in handlers)
        {
            _handlers[handler.ContributionPointId] = handler;
        }
    }

    /// <summary>
    /// Process all contributions from an extension manifest.
    /// </summary>
    public async Task ProcessContributionsAsync(
        ExtensionManifest manifest,
        CancellationToken cancellationToken = default)
    {
        var contributes = manifest.Contributes;
        if (contributes == null) return;

        foreach (var (pointId, contribution) in GetContributions(contributes))
        {
            if (_handlers.TryGetValue(pointId, out var handler))
            {
                try
                {
                    await handler.ProcessAsync(manifest.Id, contribution, cancellationToken);
                    _logger.LogDebug(
                        "Processed contribution {ContributionPoint} from {ExtensionId}",
                        pointId, manifest.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process contribution {ContributionPoint} from {ExtensionId}",
                        pointId, manifest.Id);
                }
            }
        }
    }

    private IEnumerable<(string, JsonElement)> GetContributions(JsonElement contributes)
    {
        foreach (var property in contributes.EnumerateObject())
        {
            yield return (property.Name, property.Value);
        }
    }
}

/// <summary>
/// Handler for a specific contribution point.
/// </summary>
public interface IContributionHandler
{
    string ContributionPointId { get; }
    Task ProcessAsync(string extensionId, JsonElement contribution, CancellationToken cancellationToken);
    Task UnprocessAsync(string extensionId, CancellationToken cancellationToken);
}

// ============================================
// CONTRIBUTION HANDLERS
// ============================================

public class CommandContributionHandler : IContributionHandler
{
    public string ContributionPointId => "commands";

    private readonly ICommandRegistry _commandRegistry;
    private readonly Dictionary<string, List<string>> _extensionCommands = new();

    public CommandContributionHandler(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public Task ProcessAsync(string extensionId, JsonElement contribution, CancellationToken cancellationToken)
    {
        var commands = new List<string>();

        foreach (var cmdElement in contribution.EnumerateArray())
        {
            var command = new CommandContribution
            {
                Command = cmdElement.GetProperty("command").GetString()!,
                Title = cmdElement.GetProperty("title").GetString()!,
                Category = cmdElement.TryGetProperty("category", out var cat) ? cat.GetString() : null,
                Icon = cmdElement.TryGetProperty("icon", out var icon) ? icon.GetString() : null,
                Enablement = cmdElement.TryGetProperty("enablement", out var en) ? en.GetString() : null
            };

            _commandRegistry.RegisterCommand(command, extensionId);
            commands.Add(command.Command);
        }

        _extensionCommands[extensionId] = commands;
        return Task.CompletedTask;
    }

    public Task UnprocessAsync(string extensionId, CancellationToken cancellationToken)
    {
        if (_extensionCommands.TryGetValue(extensionId, out var commands))
        {
            foreach (var commandId in commands)
            {
                _commandRegistry.UnregisterCommand(commandId);
            }
            _extensionCommands.Remove(extensionId);
        }
        return Task.CompletedTask;
    }
}

public class ViewContributionHandler : IContributionHandler
{
    public string ContributionPointId => "views";

    private readonly IViewRegistry _viewRegistry;

    public ViewContributionHandler(IViewRegistry viewRegistry)
    {
        _viewRegistry = viewRegistry;
    }

    public Task ProcessAsync(string extensionId, JsonElement contribution, CancellationToken cancellationToken)
    {
        foreach (var containerProp in contribution.EnumerateObject())
        {
            var containerId = containerProp.Name;

            foreach (var viewElement in containerProp.Value.EnumerateArray())
            {
                var view = new ViewContribution
                {
                    Id = viewElement.GetProperty("id").GetString()!,
                    Name = viewElement.GetProperty("name").GetString()!,
                    When = viewElement.TryGetProperty("when", out var when) ? when.GetString() : null,
                    Icon = viewElement.TryGetProperty("icon", out var icon) ? icon.GetString() : null,
                    ContainerId = containerId
                };

                _viewRegistry.RegisterView(view, extensionId);
            }
        }

        return Task.CompletedTask;
    }

    public Task UnprocessAsync(string extensionId, CancellationToken cancellationToken)
    {
        _viewRegistry.UnregisterExtensionViews(extensionId);
        return Task.CompletedTask;
    }
}

public class ConfigurationContributionHandler : IContributionHandler
{
    public string ContributionPointId => "configuration";

    private readonly IConfigurationRegistry _configRegistry;

    public ConfigurationContributionHandler(IConfigurationRegistry configRegistry)
    {
        _configRegistry = configRegistry;
    }

    public Task ProcessAsync(string extensionId, JsonElement contribution, CancellationToken cancellationToken)
    {
        var title = contribution.TryGetProperty("title", out var t) ? t.GetString() : extensionId;

        if (contribution.TryGetProperty("properties", out var properties))
        {
            foreach (var prop in properties.EnumerateObject())
            {
                var setting = ParseConfigurationProperty(prop.Name, prop.Value);
                _configRegistry.RegisterSetting(setting, extensionId);
            }
        }

        return Task.CompletedTask;
    }

    private ConfigurationSetting ParseConfigurationProperty(string key, JsonElement value)
    {
        return new ConfigurationSetting
        {
            Key = key,
            Type = value.TryGetProperty("type", out var type) ? type.GetString()! : "string",
            Default = value.TryGetProperty("default", out var def) ? def : default,
            Description = value.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Scope = value.TryGetProperty("scope", out var scope)
                ? Enum.Parse<ConfigurationScope>(scope.GetString()!, true)
                : ConfigurationScope.Window,
            EnumValues = value.TryGetProperty("enum", out var enumVals)
                ? enumVals.EnumerateArray().Select(e => e.GetString()!).ToList()
                : null
        };
    }

    public Task UnprocessAsync(string extensionId, CancellationToken cancellationToken)
    {
        _configRegistry.UnregisterExtensionSettings(extensionId);
        return Task.CompletedTask;
    }
}
```

---

### 3. Hybrid Approach: Best of Both Worlds

The recommended approach combines Eclipse's type-safe extension registry with VS Code's JSON simplicity.

#### 3.1 Unified Extension Point System

```csharp
// ============================================
// UNIFIED EXTENSION POINT SYSTEM
// ============================================

/// <summary>
/// Combines Eclipse's type-safety with VS Code's simplicity.
/// </summary>
public interface IUnifiedExtensionPoint<TContribution> where TContribution : class
{
    /// <summary>Unique identifier</summary>
    string Id { get; }

    /// <summary>JSON schema for manifest validation</summary>
    string JsonSchema { get; }

    /// <summary>All registered contributions</summary>
    IReadOnlyList<ExtensionContribution<TContribution>> Contributions { get; }

    /// <summary>Register a contribution programmatically</summary>
    IDisposable Register(TContribution contribution, string extensionId);

    /// <summary>Process contribution from JSON manifest</summary>
    Task ProcessJsonAsync(string extensionId, JsonElement json, CancellationToken ct = default);

    /// <summary>Contribution added event</summary>
    event EventHandler<ExtensionContribution<TContribution>> ContributionAdded;

    /// <summary>Contribution removed event</summary>
    event EventHandler<ExtensionContribution<TContribution>> ContributionRemoved;
}

public record ExtensionContribution<T>(
    string ExtensionId,
    T Contribution,
    bool FromManifest
) where T : class;

/// <summary>
/// Base implementation for extension points.
/// </summary>
public abstract class ExtensionPointBase<TContribution> : IUnifiedExtensionPoint<TContribution>
    where TContribution : class
{
    private readonly List<ExtensionContribution<TContribution>> _contributions = new();
    private readonly ILogger _logger;

    public abstract string Id { get; }
    public abstract string JsonSchema { get; }

    public IReadOnlyList<ExtensionContribution<TContribution>> Contributions =>
        _contributions.AsReadOnly();

    protected ExtensionPointBase(ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable Register(TContribution contribution, string extensionId)
    {
        var entry = new ExtensionContribution<TContribution>(extensionId, contribution, false);
        _contributions.Add(entry);

        OnContributionAdded(entry);
        ContributionAdded?.Invoke(this, entry);

        return new DisposableAction(() =>
        {
            _contributions.Remove(entry);
            OnContributionRemoved(entry);
            ContributionRemoved?.Invoke(this, entry);
        });
    }

    public async Task ProcessJsonAsync(string extensionId, JsonElement json, CancellationToken ct = default)
    {
        var contributions = await ParseJsonAsync(json, ct);

        foreach (var contribution in contributions)
        {
            var entry = new ExtensionContribution<TContribution>(extensionId, contribution, true);
            _contributions.Add(entry);

            OnContributionAdded(entry);
            ContributionAdded?.Invoke(this, entry);
        }
    }

    /// <summary>Parse JSON to strongly-typed contributions</summary>
    protected abstract Task<IReadOnlyList<TContribution>> ParseJsonAsync(JsonElement json, CancellationToken ct);

    /// <summary>Called when a contribution is added (for subclass processing)</summary>
    protected virtual void OnContributionAdded(ExtensionContribution<TContribution> contribution) { }

    /// <summary>Called when a contribution is removed (for subclass cleanup)</summary>
    protected virtual void OnContributionRemoved(ExtensionContribution<TContribution> contribution) { }

    public event EventHandler<ExtensionContribution<TContribution>>? ContributionAdded;
    public event EventHandler<ExtensionContribution<TContribution>>? ContributionRemoved;
}

// ============================================
// CONCRETE EXTENSION POINTS
// ============================================

public record CommandDefinition(
    string Id,
    string Title,
    string? Category = null,
    string? Icon = null,
    string? Enablement = null
);

public class CommandsExtensionPoint : ExtensionPointBase<CommandDefinition>
{
    private readonly ICommandService _commandService;

    public override string Id => "commands";

    public override string JsonSchema => ContributionPoints.Commands.JsonSchema;

    public CommandsExtensionPoint(ICommandService commandService, ILogger<CommandsExtensionPoint> logger)
        : base(logger)
    {
        _commandService = commandService;
    }

    protected override Task<IReadOnlyList<CommandDefinition>> ParseJsonAsync(JsonElement json, CancellationToken ct)
    {
        var commands = new List<CommandDefinition>();

        foreach (var element in json.EnumerateArray())
        {
            commands.Add(new CommandDefinition(
                Id: element.GetProperty("command").GetString()!,
                Title: element.GetProperty("title").GetString()!,
                Category: element.TryGetProperty("category", out var c) ? c.GetString() : null,
                Icon: element.TryGetProperty("icon", out var i) ? i.GetString() : null,
                Enablement: element.TryGetProperty("enablement", out var e) ? e.GetString() : null
            ));
        }

        return Task.FromResult<IReadOnlyList<CommandDefinition>>(commands);
    }

    protected override void OnContributionAdded(ExtensionContribution<CommandDefinition> contribution)
    {
        // Register placeholder command handler that activates extension on first invocation
        _commandService.RegisterPlaceholder(
            contribution.Contribution.Id,
            contribution.ExtensionId);
    }

    protected override void OnContributionRemoved(ExtensionContribution<CommandDefinition> contribution)
    {
        _commandService.Unregister(contribution.Contribution.Id);
    }
}

public record ViewDefinition(
    string Id,
    string Name,
    string ContainerId,
    string? When = null,
    string? Icon = null
);

public class ViewsExtensionPoint : ExtensionPointBase<ViewDefinition>
{
    public override string Id => "views";
    public override string JsonSchema => ContributionPoints.Views.JsonSchema;

    public ViewsExtensionPoint(ILogger<ViewsExtensionPoint> logger) : base(logger) { }

    protected override Task<IReadOnlyList<ViewDefinition>> ParseJsonAsync(JsonElement json, CancellationToken ct)
    {
        var views = new List<ViewDefinition>();

        foreach (var containerProp in json.EnumerateObject())
        {
            foreach (var viewElement in containerProp.Value.EnumerateArray())
            {
                views.Add(new ViewDefinition(
                    Id: viewElement.GetProperty("id").GetString()!,
                    Name: viewElement.GetProperty("name").GetString()!,
                    ContainerId: containerProp.Name,
                    When: viewElement.TryGetProperty("when", out var w) ? w.GetString() : null,
                    Icon: viewElement.TryGetProperty("icon", out var i) ? i.GetString() : null
                ));
            }
        }

        return Task.FromResult<IReadOnlyList<ViewDefinition>>(views);
    }
}

public record MenuItemDefinition(
    string Location,
    string CommandId,
    string? When = null,
    string? Group = null,
    int Order = 0
);

public class MenusExtensionPoint : ExtensionPointBase<MenuItemDefinition>
{
    private readonly IMenuService _menuService;

    public override string Id => "menus";
    public override string JsonSchema => ContributionPoints.Menus.JsonSchema;

    public MenusExtensionPoint(IMenuService menuService, ILogger<MenusExtensionPoint> logger)
        : base(logger)
    {
        _menuService = menuService;
    }

    protected override Task<IReadOnlyList<MenuItemDefinition>> ParseJsonAsync(JsonElement json, CancellationToken ct)
    {
        var items = new List<MenuItemDefinition>();

        foreach (var locationProp in json.EnumerateObject())
        {
            var order = 0;
            foreach (var itemElement in locationProp.Value.EnumerateArray())
            {
                items.Add(new MenuItemDefinition(
                    Location: locationProp.Name,
                    CommandId: itemElement.GetProperty("command").GetString()!,
                    When: itemElement.TryGetProperty("when", out var w) ? w.GetString() : null,
                    Group: itemElement.TryGetProperty("group", out var g) ? g.GetString() : null,
                    Order: order++
                ));
            }
        }

        return Task.FromResult<IReadOnlyList<MenuItemDefinition>>(items);
    }

    protected override void OnContributionAdded(ExtensionContribution<MenuItemDefinition> contribution)
    {
        _menuService.AddMenuItem(
            contribution.Contribution.Location,
            contribution.Contribution,
            contribution.ExtensionId);
    }

    protected override void OnContributionRemoved(ExtensionContribution<MenuItemDefinition> contribution)
    {
        _menuService.RemoveMenuItem(
            contribution.Contribution.Location,
            contribution.Contribution.CommandId);
    }
}
```

#### 3.2 Extension Point Registry

```csharp
// ============================================
// EXTENSION POINT REGISTRY
// ============================================

public interface IExtensionPointRegistry
{
    /// <summary>Register an extension point</summary>
    void Register<T>(IUnifiedExtensionPoint<T> extensionPoint) where T : class;

    /// <summary>Get extension point by ID</summary>
    IUnifiedExtensionPoint<T>? Get<T>(string id) where T : class;

    /// <summary>Get all extension points</summary>
    IReadOnlyList<object> GetAll();

    /// <summary>Process contributions from manifest</summary>
    Task ProcessManifestAsync(ExtensionManifest manifest, CancellationToken ct = default);
}

public class ExtensionPointRegistry : IExtensionPointRegistry
{
    private readonly Dictionary<string, object> _extensionPoints = new();
    private readonly ILogger<ExtensionPointRegistry> _logger;

    public ExtensionPointRegistry(
        IEnumerable<object> extensionPoints,
        ILogger<ExtensionPointRegistry> logger)
    {
        _logger = logger;

        foreach (dynamic ep in extensionPoints)
        {
            RegisterDynamic(ep);
        }
    }

    private void RegisterDynamic<T>(IUnifiedExtensionPoint<T> extensionPoint) where T : class
    {
        Register(extensionPoint);
    }

    public void Register<T>(IUnifiedExtensionPoint<T> extensionPoint) where T : class
    {
        _extensionPoints[extensionPoint.Id] = extensionPoint;
        _logger.LogInformation("Registered extension point: {Id}", extensionPoint.Id);
    }

    public IUnifiedExtensionPoint<T>? Get<T>(string id) where T : class
    {
        return _extensionPoints.TryGetValue(id, out var ep)
            ? ep as IUnifiedExtensionPoint<T>
            : null;
    }

    public IReadOnlyList<object> GetAll() => _extensionPoints.Values.ToList();

    public async Task ProcessManifestAsync(ExtensionManifest manifest, CancellationToken ct = default)
    {
        if (manifest.Contributes == null) return;

        foreach (var property in manifest.Contributes.Value.EnumerateObject())
        {
            if (_extensionPoints.TryGetValue(property.Name, out var extensionPoint))
            {
                try
                {
                    // Use reflection to call ProcessJsonAsync on the correct generic type
                    var method = extensionPoint.GetType().GetMethod("ProcessJsonAsync");
                    var task = (Task)method!.Invoke(extensionPoint, new object[] { manifest.Id, property.Value, ct })!;
                    await task;

                    _logger.LogDebug(
                        "Processed {ContributionPoint} contributions from {ExtensionId}",
                        property.Name, manifest.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process {ContributionPoint} from {ExtensionId}",
                        property.Name, manifest.Id);
                }
            }
        }
    }
}
```

---

### 4. When Expression Evaluator (Context System)

Both Eclipse and VS Code use "when" expressions to control visibility and enablement.

```csharp
// ============================================
// WHEN EXPRESSION EVALUATOR
// ============================================

/// <summary>
/// Context service for evaluating "when" expressions.
/// Expressions use a simple DSL similar to VS Code.
/// </summary>
public interface IContextKeyService
{
    /// <summary>Set a context key value</summary>
    void SetContext(string key, object? value);

    /// <summary>Remove a context key</summary>
    void RemoveContext(string key);

    /// <summary>Get a context key value</summary>
    T? GetContext<T>(string key);

    /// <summary>Evaluate a "when" expression</summary>
    bool Evaluate(string? expression);

    /// <summary>Context changed event</summary>
    event EventHandler<ContextChangedEventArgs> ContextChanged;
}

public class ContextKeyService : IContextKeyService
{
    private readonly Dictionary<string, object?> _context = new();
    private readonly WhenExpressionParser _parser = new();

    public void SetContext(string key, object? value)
    {
        var oldValue = _context.GetValueOrDefault(key);
        _context[key] = value;

        if (!Equals(oldValue, value))
        {
            ContextChanged?.Invoke(this, new ContextChangedEventArgs(key, oldValue, value));
        }
    }

    public void RemoveContext(string key)
    {
        if (_context.Remove(key, out var oldValue))
        {
            ContextChanged?.Invoke(this, new ContextChangedEventArgs(key, oldValue, null));
        }
    }

    public T? GetContext<T>(string key)
    {
        return _context.TryGetValue(key, out var value) && value is T typed
            ? typed
            : default;
    }

    /// <summary>
    /// Evaluates expressions like:
    /// - "editorFocus" (boolean context check)
    /// - "resourceExtname == '.cs'" (equality)
    /// - "resourceScheme == 'file' && editorFocus" (logical AND)
    /// - "isWindows || isMac" (logical OR)
    /// - "!isDebugging" (negation)
    /// - "listFocus && listHasSelectionOrFocus" (compound)
    /// - "resourceFilename =~ /.*\\.test\\.ts$/" (regex match)
    /// </summary>
    public bool Evaluate(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true;

        var ast = _parser.Parse(expression);
        return EvaluateNode(ast);
    }

    private bool EvaluateNode(WhenExpressionNode node)
    {
        return node switch
        {
            ContextKeyNode ck => EvaluateContextKey(ck.Key),
            EqualsNode eq => EvaluateEquals(eq.Key, eq.Value),
            NotEqualsNode neq => !EvaluateEquals(neq.Key, neq.Value),
            RegexNode rx => EvaluateRegex(rx.Key, rx.Pattern),
            InNode inNode => EvaluateIn(inNode.Key, inNode.Values),
            NotNode not => !EvaluateNode(not.Inner),
            AndNode and => EvaluateNode(and.Left) && EvaluateNode(and.Right),
            OrNode or => EvaluateNode(or.Left) || EvaluateNode(or.Right),
            TrueNode => true,
            FalseNode => false,
            _ => false
        };
    }

    private bool EvaluateContextKey(string key)
    {
        if (!_context.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            null => false,
            _ => true
        };
    }

    private bool EvaluateEquals(string key, string expected)
    {
        if (!_context.TryGetValue(key, out var value))
            return expected == "false" || expected == "";

        var actual = value?.ToString() ?? "";
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateRegex(string key, string pattern)
    {
        if (!_context.TryGetValue(key, out var value))
            return false;

        var actual = value?.ToString() ?? "";
        return Regex.IsMatch(actual, pattern);
    }

    private bool EvaluateIn(string key, IReadOnlyList<string> values)
    {
        if (!_context.TryGetValue(key, out var value))
            return false;

        var actual = value?.ToString() ?? "";
        return values.Contains(actual, StringComparer.OrdinalIgnoreCase);
    }

    public event EventHandler<ContextChangedEventArgs>? ContextChanged;
}

// Expression AST nodes
public abstract record WhenExpressionNode;
public record ContextKeyNode(string Key) : WhenExpressionNode;
public record EqualsNode(string Key, string Value) : WhenExpressionNode;
public record NotEqualsNode(string Key, string Value) : WhenExpressionNode;
public record RegexNode(string Key, string Pattern) : WhenExpressionNode;
public record InNode(string Key, IReadOnlyList<string> Values) : WhenExpressionNode;
public record NotNode(WhenExpressionNode Inner) : WhenExpressionNode;
public record AndNode(WhenExpressionNode Left, WhenExpressionNode Right) : WhenExpressionNode;
public record OrNode(WhenExpressionNode Left, WhenExpressionNode Right) : WhenExpressionNode;
public record TrueNode() : WhenExpressionNode;
public record FalseNode() : WhenExpressionNode;

/// <summary>
/// Built-in context keys (like VS Code's built-in contexts)
/// </summary>
public static class BuiltInContextKeys
{
    // Application state
    public const string IsWindows = "isWindows";
    public const string IsMac = "isMac";
    public const string IsLinux = "isLinux";
    public const string IsDevelopment = "isDevelopment";

    // Editor state
    public const string EditorFocus = "editorFocus";
    public const string EditorTextFocus = "editorTextFocus";
    public const string EditorHasSelection = "editorHasSelection";
    public const string EditorHasMultipleSelections = "editorHasMultipleSelections";
    public const string EditorReadonly = "editorReadonly";
    public const string EditorLangId = "editorLangId";

    // Resource state
    public const string ResourceScheme = "resourceScheme";
    public const string ResourceFilename = "resourceFilename";
    public const string ResourceExtname = "resourceExtname";
    public const string ResourceDirname = "resourceDirname";
    public const string ResourcePath = "resourcePath";

    // View state
    public const string ViewId = "view";
    public const string ViewItemId = "viewItem";
    public const string ActiveViewlet = "activeViewlet";
    public const string SideBarVisible = "sideBarVisible";
    public const string PanelVisible = "panelVisible";

    // List/Tree state
    public const string ListFocus = "listFocus";
    public const string ListHasSelectionOrFocus = "listHasSelectionOrFocus";
    public const string TreeItemHasChildren = "treeItemHasChildren";

    // Debug state
    public const string InDebugMode = "inDebugMode";
    public const string DebugState = "debugState";
    public const string DebugType = "debugType";

    // Search state
    public const string SearchViewletVisible = "searchViewletVisible";
    public const string HasSearchResults = "hasSearchResults";

    // SCM state
    public const string ScmProviderCount = "scmProviderCount";
    public const string ScmResourceGroupCount = "scmResourceGroupCount";

    // Terminal state
    public const string TerminalFocus = "terminalFocus";
    public const string TerminalIsOpen = "terminalIsOpen";
}
```

---

### 5. Quick Reference: Extension Point Summary

| Extension Point | Purpose | Key Properties |
|----------------|---------|----------------|
| `commands` | Executable commands | id, title, category, icon, enablement |
| `menus` | Menu contributions | location, command, when, group |
| `views` | Sidebar/panel views | id, name, when, icon |
| `viewsContainers` | View containers | id, title, icon |
| `configuration` | Settings | key, type, default, description, scope |
| `keybindings` | Keyboard shortcuts | command, key, when |
| `languages` | Language support | id, extensions, configuration |
| `grammars` | Syntax highlighting | language, scopeName, path |
| `themes` | Color themes | label, uiTheme, path |
| `iconThemes` | File icon themes | id, label, path |
| `snippets` | Code snippets | language, path |
| `taskDefinitions` | Task types | type, properties |
| `debuggers` | Debug adapters | type, program, languages |
| `customEditors` | Custom editors | viewType, selector |
| `walkthroughs` | Getting started | steps, media |
| `authentication` | Auth providers | id, label |

---

## Enterprise Application Design (ERP Client Pattern)

This section focuses on enterprise application patterns for ERP-style clients with MDI (Multiple Document Interface), function tree navigation, and Master-Detail CRUD operations.

### 1. Application Shell Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Title Bar: Arcana ERP - [Company Name] - [Current User]            _ □ X  │
├─────────────────────────────────────────────────────────────────────────────┤
│  Menu Bar: File | Edit | View | Modules | Tools | Window | Help            │
├─────────────────────────────────────────────────────────────────────────────┤
│  Toolbar: [Save] [Refresh] [Print] [Export] | [New] [Edit] [Delete] | ...  │
├────────────────┬────────────────────────────────────────────────────────────┤
│                │  Tab Bar: [Customers ×] [Orders ×] [Products ×] [+]       │
│   Function     ├────────────────────────────────────────────────────────────┤
│   Tree         │                                                            │
│   (階層式      │                                                            │
│    功能樹)     │           MDI Document Area                                │
│                │           (Master-Detail Views)                            │
│   ▼ Sales      │                                                            │
│     ├ Customers│                                                            │
│     ├ Orders   │                                                            │
│     └ Quotes   │                                                            │
│   ▼ Inventory  │                                                            │
│     ├ Products │                                                            │
│     ├ Stock    │                                                            │
│     └ Warehouse│                                                            │
│   ▼ Finance    │                                                            │
│     ├ Invoices │                                                            │
│     ├ Payments │                                                            │
│     └ Reports  │                                                            │
│   ▼ Settings   │                                                            │
│     ├ Users    │                                                            │
│     └ Config   │                                                            │
│                │                                                            │
├────────────────┴────────────────────────────────────────────────────────────┤
│  Status Bar: Ready | Records: 1,234 | User: Admin | Server: Connected | 🟢 │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. Function Tree (功能樹) Architecture

#### 2.1 Function Tree Data Model

```csharp
// ============================================
// FUNCTION TREE DATA MODEL (功能樹資料模型)
// ============================================

/// <summary>
/// Represents a node in the function tree.
/// 功能樹節點
/// </summary>
public record FunctionNode
{
    /// <summary>Unique identifier</summary>
    public required string Id { get; init; }

    /// <summary>Display name (支援多語系)</summary>
    public required string Name { get; init; }

    /// <summary>Localization key for i18n</summary>
    public string? LocalizationKey { get; init; }

    /// <summary>Icon (Segoe MDL2 Assets or custom)</summary>
    public string? Icon { get; init; }

    /// <summary>Parent node ID (null for root)</summary>
    public string? ParentId { get; init; }

    /// <summary>Display order within parent</summary>
    public int Order { get; init; }

    /// <summary>Node type</summary>
    public FunctionNodeType NodeType { get; init; }

    /// <summary>View/Page to open when clicked</summary>
    public string? ViewType { get; init; }

    /// <summary>View parameters</summary>
    public Dictionary<string, object>? Parameters { get; init; }

    /// <summary>Required permission to see this node</summary>
    public string? RequiredPermission { get; init; }

    /// <summary>Child nodes</summary>
    public IReadOnlyList<FunctionNode> Children { get; init; } = Array.Empty<FunctionNode>();

    /// <summary>Is node expanded by default</summary>
    public bool IsExpanded { get; init; }

    /// <summary>Badge count (e.g., pending items)</summary>
    public int? BadgeCount { get; init; }

    /// <summary>Keyboard shortcut</summary>
    public string? Shortcut { get; init; }

    /// <summary>Tooltip description</summary>
    public string? Description { get; init; }
}

public enum FunctionNodeType
{
    /// <summary>Category/folder node (群組節點)</summary>
    Category,

    /// <summary>Function that opens a view (功能節點)</summary>
    Function,

    /// <summary>Separator line</summary>
    Separator,

    /// <summary>External link</summary>
    Link,

    /// <summary>Recent items</summary>
    Recent,

    /// <summary>Favorites</summary>
    Favorite
}

/// <summary>
/// Function tree configuration from JSON or database.
/// 功能樹設定
/// </summary>
public class FunctionTreeConfiguration
{
    public string Version { get; set; } = "1.0";
    public IReadOnlyList<FunctionNode> Nodes { get; set; } = Array.Empty<FunctionNode>();
    public FunctionTreeSettings Settings { get; set; } = new();
}

public class FunctionTreeSettings
{
    /// <summary>Show icons in tree</summary>
    public bool ShowIcons { get; set; } = true;

    /// <summary>Allow drag & drop reordering</summary>
    public bool AllowReorder { get; set; } = false;

    /// <summary>Show search box</summary>
    public bool ShowSearch { get; set; } = true;

    /// <summary>Show favorites section</summary>
    public bool ShowFavorites { get; set; } = true;

    /// <summary>Show recent items</summary>
    public bool ShowRecent { get; set; } = true;

    /// <summary>Max recent items</summary>
    public int MaxRecentItems { get; set; } = 10;

    /// <summary>Collapse siblings when expanding</summary>
    public bool AutoCollapseSiblings { get; set; } = false;

    /// <summary>Remember expansion state</summary>
    public bool RememberExpansionState { get; set; } = true;

    /// <summary>Double-click to open (single click expand)</summary>
    public bool DoubleClickToOpen { get; set; } = false;
}
```

#### 2.2 Function Tree Service

```csharp
// ============================================
// FUNCTION TREE SERVICE (功能樹服務)
// ============================================

public interface IFunctionTreeService
{
    /// <summary>Get the complete function tree</summary>
    Task<FunctionTreeConfiguration> GetFunctionTreeAsync();

    /// <summary>Get visible nodes based on user permissions</summary>
    Task<IReadOnlyList<FunctionNode>> GetVisibleNodesAsync(string userId);

    /// <summary>Search functions by name</summary>
    Task<IReadOnlyList<FunctionNode>> SearchAsync(string query);

    /// <summary>Get recent items for user</summary>
    Task<IReadOnlyList<FunctionNode>> GetRecentAsync(string userId, int count = 10);

    /// <summary>Add to recent items</summary>
    Task AddToRecentAsync(string userId, string functionId);

    /// <summary>Get user's favorites</summary>
    Task<IReadOnlyList<FunctionNode>> GetFavoritesAsync(string userId);

    /// <summary>Toggle favorite status</summary>
    Task<bool> ToggleFavoriteAsync(string userId, string functionId);

    /// <summary>Get badge counts for all functions</summary>
    Task<Dictionary<string, int>> GetBadgeCountsAsync();

    /// <summary>Function tree changed event</summary>
    event EventHandler<FunctionTreeChangedEventArgs>? TreeChanged;
}

public class FunctionTreeService : IFunctionTreeService
{
    private readonly IFunctionTreeRepository _repository;
    private readonly IPermissionService _permissionService;
    private readonly IUserPreferenceService _preferenceService;
    private readonly IBadgeCountProvider _badgeProvider;
    private readonly ILogger<FunctionTreeService> _logger;

    private FunctionTreeConfiguration? _cachedTree;

    public async Task<IReadOnlyList<FunctionNode>> GetVisibleNodesAsync(string userId)
    {
        var tree = await GetFunctionTreeAsync();
        var permissions = await _permissionService.GetUserPermissionsAsync(userId);

        return FilterByPermissions(tree.Nodes, permissions);
    }

    private IReadOnlyList<FunctionNode> FilterByPermissions(
        IReadOnlyList<FunctionNode> nodes,
        IReadOnlySet<string> permissions)
    {
        var result = new List<FunctionNode>();

        foreach (var node in nodes)
        {
            // Check permission
            if (node.RequiredPermission != null &&
                !permissions.Contains(node.RequiredPermission))
            {
                continue;
            }

            // Filter children recursively
            var filteredChildren = FilterByPermissions(node.Children, permissions);

            // Include category only if it has visible children
            if (node.NodeType == FunctionNodeType.Category && filteredChildren.Count == 0)
            {
                continue;
            }

            result.Add(node with { Children = filteredChildren });
        }

        return result;
    }

    public async Task<IReadOnlyList<FunctionNode>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<FunctionNode>();

        var tree = await GetFunctionTreeAsync();
        var results = new List<FunctionNode>();

        SearchRecursive(tree.Nodes, query.ToLower(), results);

        return results.OrderBy(n => n.Name).ToList();
    }

    private void SearchRecursive(
        IReadOnlyList<FunctionNode> nodes,
        string query,
        List<FunctionNode> results)
    {
        foreach (var node in nodes)
        {
            if (node.NodeType == FunctionNodeType.Function &&
                (node.Name.ToLower().Contains(query) ||
                 node.Id.ToLower().Contains(query) ||
                 node.Description?.ToLower().Contains(query) == true))
            {
                results.Add(node);
            }

            SearchRecursive(node.Children, query, results);
        }
    }

    // ... other implementations
}
```

#### 2.3 Function Tree JSON Configuration

```json
{
  "version": "1.0",
  "settings": {
    "showIcons": true,
    "showSearch": true,
    "showFavorites": true,
    "showRecent": true,
    "maxRecentItems": 10,
    "doubleClickToOpen": false
  },
  "nodes": [
    {
      "id": "sales",
      "name": "Sales",
      "localizationKey": "Menu.Sales",
      "icon": "\uE719",
      "nodeType": "Category",
      "order": 1,
      "isExpanded": true,
      "children": [
        {
          "id": "sales.customers",
          "name": "Customers",
          "localizationKey": "Menu.Sales.Customers",
          "icon": "\uE77B",
          "nodeType": "Function",
          "viewType": "CustomerListView",
          "requiredPermission": "sales.customers.view",
          "shortcut": "Ctrl+Shift+C",
          "description": "Manage customer information"
        },
        {
          "id": "sales.orders",
          "name": "Orders",
          "localizationKey": "Menu.Sales.Orders",
          "icon": "\uE7BF",
          "nodeType": "Function",
          "viewType": "OrderListView",
          "requiredPermission": "sales.orders.view",
          "shortcut": "Ctrl+Shift+O"
        },
        {
          "id": "sales.quotes",
          "name": "Quotations",
          "localizationKey": "Menu.Sales.Quotes",
          "icon": "\uE8A1",
          "nodeType": "Function",
          "viewType": "QuoteListView",
          "requiredPermission": "sales.quotes.view"
        }
      ]
    },
    {
      "id": "inventory",
      "name": "Inventory",
      "localizationKey": "Menu.Inventory",
      "icon": "\uE773",
      "nodeType": "Category",
      "order": 2,
      "children": [
        {
          "id": "inventory.products",
          "name": "Products",
          "localizationKey": "Menu.Inventory.Products",
          "icon": "\uEB3E",
          "nodeType": "Function",
          "viewType": "ProductListView",
          "requiredPermission": "inventory.products.view"
        },
        {
          "id": "inventory.stock",
          "name": "Stock Levels",
          "localizationKey": "Menu.Inventory.Stock",
          "icon": "\uE74C",
          "nodeType": "Function",
          "viewType": "StockListView",
          "requiredPermission": "inventory.stock.view"
        },
        {
          "id": "inventory.separator1",
          "nodeType": "Separator"
        },
        {
          "id": "inventory.warehouse",
          "name": "Warehouses",
          "localizationKey": "Menu.Inventory.Warehouse",
          "icon": "\uE825",
          "nodeType": "Function",
          "viewType": "WarehouseListView",
          "requiredPermission": "inventory.warehouse.view"
        }
      ]
    },
    {
      "id": "finance",
      "name": "Finance",
      "localizationKey": "Menu.Finance",
      "icon": "\uE8C7",
      "nodeType": "Category",
      "order": 3,
      "children": [
        {
          "id": "finance.invoices",
          "name": "Invoices",
          "icon": "\uE9F9",
          "nodeType": "Function",
          "viewType": "InvoiceListView",
          "requiredPermission": "finance.invoices.view"
        },
        {
          "id": "finance.payments",
          "name": "Payments",
          "icon": "\uE8C7",
          "nodeType": "Function",
          "viewType": "PaymentListView",
          "requiredPermission": "finance.payments.view"
        },
        {
          "id": "finance.reports",
          "name": "Reports",
          "icon": "\uE9F9",
          "nodeType": "Category",
          "children": [
            {
              "id": "finance.reports.balance",
              "name": "Balance Sheet",
              "nodeType": "Function",
              "viewType": "BalanceSheetReport"
            },
            {
              "id": "finance.reports.pl",
              "name": "Profit & Loss",
              "nodeType": "Function",
              "viewType": "ProfitLossReport"
            }
          ]
        }
      ]
    },
    {
      "id": "settings",
      "name": "Settings",
      "localizationKey": "Menu.Settings",
      "icon": "\uE713",
      "nodeType": "Category",
      "order": 99,
      "children": [
        {
          "id": "settings.users",
          "name": "User Management",
          "icon": "\uE716",
          "nodeType": "Function",
          "viewType": "UserListView",
          "requiredPermission": "admin.users"
        },
        {
          "id": "settings.config",
          "name": "System Configuration",
          "icon": "\uE90F",
          "nodeType": "Function",
          "viewType": "SystemConfigView",
          "requiredPermission": "admin.config"
        }
      ]
    }
  ]
}
```

#### 2.4 Function Tree UI Component (WinUI3 XAML)

```xml
<!-- FunctionTreeControl.xaml -->
<UserControl
    x:Class="Arcana.App.Controls.FunctionTreeControl"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls"
    xmlns:local="using:Arcana.App.Controls">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Search -->
            <RowDefinition Height="Auto"/>  <!-- Favorites -->
            <RowDefinition Height="*"/>     <!-- Tree -->
        </Grid.RowDefinitions>

        <!-- Search Box -->
        <AutoSuggestBox
            Grid.Row="0"
            PlaceholderText="Search functions..."
            QueryIcon="Find"
            Margin="8"
            TextChanged="OnSearchTextChanged"
            QuerySubmitted="OnSearchSubmitted"/>

        <!-- Favorites Section (Collapsible) -->
        <Expander
            Grid.Row="1"
            Header="Favorites"
            IsExpanded="True"
            Margin="8,0"
            Visibility="{x:Bind ViewModel.HasFavorites, Mode=OneWay}">
            <ListView
                ItemsSource="{x:Bind ViewModel.Favorites, Mode=OneWay}"
                SelectionMode="Single"
                ItemClick="OnFunctionClicked"
                IsItemClickEnabled="True">
                <ListView.ItemTemplate>
                    <DataTemplate x:DataType="local:FunctionNode">
                        <Grid Padding="4" ColumnSpacing="8">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <FontIcon Glyph="{x:Bind Icon}" FontSize="16"/>
                            <TextBlock Grid.Column="1" Text="{x:Bind Name}"/>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Expander>

        <!-- Function Tree -->
        <TreeView
            Grid.Row="2"
            ItemsSource="{x:Bind ViewModel.RootNodes, Mode=OneWay}"
            SelectionMode="Single"
            ItemInvoked="OnTreeItemInvoked"
            Expanding="OnTreeItemExpanding"
            Collapsed="OnTreeItemCollapsed"
            Margin="8">

            <TreeView.ItemTemplate>
                <DataTemplate x:DataType="local:FunctionNodeViewModel">
                    <TreeViewItem
                        ItemsSource="{x:Bind Children, Mode=OneWay}"
                        IsExpanded="{x:Bind IsExpanded, Mode=TwoWay}">
                        <Grid Padding="4" ColumnSpacing="8">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>

                            <!-- Icon -->
                            <FontIcon
                                Glyph="{x:Bind Icon, Mode=OneWay}"
                                FontSize="16"
                                Foreground="{x:Bind IconColor, Mode=OneWay}"/>

                            <!-- Name -->
                            <TextBlock
                                Grid.Column="1"
                                Text="{x:Bind Name, Mode=OneWay}"
                                VerticalAlignment="Center"/>

                            <!-- Badge -->
                            <controls:InfoBadge
                                Grid.Column="2"
                                Value="{x:Bind BadgeCount, Mode=OneWay}"
                                Visibility="{x:Bind HasBadge, Mode=OneWay}"/>
                        </Grid>

                        <!-- Context Menu -->
                        <TreeViewItem.ContextFlyout>
                            <MenuFlyout>
                                <MenuFlyoutItem
                                    Text="Open"
                                    Icon="OpenFile"
                                    Click="OnOpenClicked"/>
                                <MenuFlyoutItem
                                    Text="Open in New Tab"
                                    Icon="NewWindow"
                                    Click="OnOpenInNewTabClicked"/>
                                <MenuFlyoutSeparator/>
                                <MenuFlyoutItem
                                    Text="{x:Bind FavoriteText, Mode=OneWay}"
                                    Icon="{x:Bind FavoriteIcon, Mode=OneWay}"
                                    Click="OnToggleFavoriteClicked"/>
                            </MenuFlyout>
                        </TreeViewItem.ContextFlyout>
                    </TreeViewItem>
                </DataTemplate>
            </TreeView.ItemTemplate>
        </TreeView>
    </Grid>
</UserControl>
```

```csharp
// FunctionTreeControl.xaml.cs
public sealed partial class FunctionTreeControl : UserControl
{
    public FunctionTreeViewModel ViewModel { get; }

    public event EventHandler<FunctionOpenRequestedEventArgs>? FunctionOpenRequested;

    public FunctionTreeControl()
    {
        ViewModel = App.Current.Services.GetRequiredService<FunctionTreeViewModel>();
        InitializeComponent();
    }

    private void OnTreeItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is FunctionNodeViewModel node &&
            node.NodeType == FunctionNodeType.Function)
        {
            FunctionOpenRequested?.Invoke(this, new FunctionOpenRequestedEventArgs(node.Node));
        }
    }

    private async void OnToggleFavoriteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item &&
            item.DataContext is FunctionNodeViewModel node)
        {
            await ViewModel.ToggleFavoriteAsync(node.Id);
        }
    }
}

public class FunctionOpenRequestedEventArgs : EventArgs
{
    public FunctionNode Function { get; }
    public bool OpenInNewTab { get; init; }

    public FunctionOpenRequestedEventArgs(FunctionNode function)
    {
        Function = function;
    }
}
```

---

### 3. MDI (Multiple Document Interface) System

#### 3.1 MDI Architecture

```csharp
// ============================================
// MDI DOCUMENT MANAGEMENT (多文件介面)
// ============================================

/// <summary>
/// Represents an open document/view in the MDI container.
/// 代表MDI容器中的一個開啟文件/視圖
/// </summary>
public interface IMdiDocument
{
    /// <summary>Unique document ID</summary>
    string DocumentId { get; }

    /// <summary>Display title for tab</summary>
    string Title { get; }

    /// <summary>Icon for tab</summary>
    string? Icon { get; }

    /// <summary>Has unsaved changes</summary>
    bool IsDirty { get; }

    /// <summary>Can be closed</summary>
    bool CanClose { get; }

    /// <summary>Document type identifier</summary>
    string DocumentType { get; }

    /// <summary>The actual view content</summary>
    object Content { get; }

    /// <summary>Save the document</summary>
    Task<bool> SaveAsync();

    /// <summary>Check if can close (prompt for save)</summary>
    Task<bool> CanCloseAsync();

    /// <summary>Called when document is activated</summary>
    void OnActivated();

    /// <summary>Called when document is deactivated</summary>
    void OnDeactivated();

    /// <summary>Document state changed</summary>
    event EventHandler<DocumentStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// MDI container service for managing documents.
/// MDI容器服務
/// </summary>
public interface IMdiService
{
    /// <summary>All open documents</summary>
    IReadOnlyList<IMdiDocument> Documents { get; }

    /// <summary>Currently active document</summary>
    IMdiDocument? ActiveDocument { get; }

    /// <summary>Open a new document</summary>
    Task<IMdiDocument> OpenDocumentAsync(string documentType, object? parameter = null);

    /// <summary>Open or activate existing document</summary>
    Task<IMdiDocument> OpenOrActivateDocumentAsync(string documentType, string documentId);

    /// <summary>Close a document</summary>
    Task<bool> CloseDocumentAsync(IMdiDocument document);

    /// <summary>Close a document by ID</summary>
    Task<bool> CloseDocumentAsync(string documentId);

    /// <summary>Close all documents</summary>
    Task<bool> CloseAllDocumentsAsync();

    /// <summary>Close all except specified</summary>
    Task<bool> CloseAllExceptAsync(string documentId);

    /// <summary>Activate a document</summary>
    void ActivateDocument(IMdiDocument document);

    /// <summary>Activate by ID</summary>
    void ActivateDocument(string documentId);

    /// <summary>Find document by ID</summary>
    IMdiDocument? FindDocument(string documentId);

    /// <summary>Find documents by type</summary>
    IReadOnlyList<IMdiDocument> FindDocumentsByType(string documentType);

    /// <summary>Save all dirty documents</summary>
    Task<bool> SaveAllAsync();

    /// <summary>Document opened event</summary>
    event EventHandler<MdiDocumentEventArgs>? DocumentOpened;

    /// <summary>Document closed event</summary>
    event EventHandler<MdiDocumentEventArgs>? DocumentClosed;

    /// <summary>Active document changed</summary>
    event EventHandler<MdiDocumentEventArgs>? ActiveDocumentChanged;
}

/// <summary>
/// Factory for creating document views.
/// 文件視圖工廠
/// </summary>
public interface IMdiDocumentFactory
{
    /// <summary>Can create documents of this type</summary>
    bool CanCreate(string documentType);

    /// <summary>Create a new document</summary>
    Task<IMdiDocument> CreateAsync(string documentType, object? parameter);
}
```

#### 3.2 MDI Service Implementation

```csharp
// ============================================
// MDI SERVICE IMPLEMENTATION
// ============================================

public class MdiService : IMdiService
{
    private readonly List<IMdiDocument> _documents = new();
    private readonly IEnumerable<IMdiDocumentFactory> _factories;
    private readonly ILogger<MdiService> _logger;
    private IMdiDocument? _activeDocument;

    public IReadOnlyList<IMdiDocument> Documents => _documents.AsReadOnly();
    public IMdiDocument? ActiveDocument => _activeDocument;

    public MdiService(
        IEnumerable<IMdiDocumentFactory> factories,
        ILogger<MdiService> logger)
    {
        _factories = factories;
        _logger = logger;
    }

    public async Task<IMdiDocument> OpenDocumentAsync(string documentType, object? parameter = null)
    {
        var factory = _factories.FirstOrDefault(f => f.CanCreate(documentType))
            ?? throw new InvalidOperationException($"No factory for document type: {documentType}");

        var document = await factory.CreateAsync(documentType, parameter);

        _documents.Add(document);
        document.StateChanged += OnDocumentStateChanged;

        _logger.LogInformation("Opened document {DocumentId} of type {DocumentType}",
            document.DocumentId, documentType);

        DocumentOpened?.Invoke(this, new MdiDocumentEventArgs(document));

        ActivateDocument(document);

        return document;
    }

    public async Task<IMdiDocument> OpenOrActivateDocumentAsync(string documentType, string documentId)
    {
        var existing = FindDocument(documentId);
        if (existing != null)
        {
            ActivateDocument(existing);
            return existing;
        }

        return await OpenDocumentAsync(documentType, documentId);
    }

    public async Task<bool> CloseDocumentAsync(IMdiDocument document)
    {
        if (!await document.CanCloseAsync())
        {
            return false;
        }

        _documents.Remove(document);
        document.StateChanged -= OnDocumentStateChanged;

        _logger.LogInformation("Closed document {DocumentId}", document.DocumentId);

        DocumentClosed?.Invoke(this, new MdiDocumentEventArgs(document));

        // Activate next document
        if (_activeDocument == document)
        {
            var next = _documents.LastOrDefault();
            if (next != null)
            {
                ActivateDocument(next);
            }
            else
            {
                _activeDocument = null;
                ActiveDocumentChanged?.Invoke(this, new MdiDocumentEventArgs(null));
            }
        }

        // Dispose if disposable
        if (document is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return true;
    }

    public async Task<bool> CloseAllDocumentsAsync()
    {
        // Close in reverse order (newest first)
        var documentsToClose = _documents.ToList();
        documentsToClose.Reverse();

        foreach (var doc in documentsToClose)
        {
            if (!await CloseDocumentAsync(doc))
            {
                return false; // User cancelled
            }
        }

        return true;
    }

    public void ActivateDocument(IMdiDocument document)
    {
        if (_activeDocument == document)
            return;

        _activeDocument?.OnDeactivated();

        _activeDocument = document;
        document.OnActivated();

        _logger.LogDebug("Activated document {DocumentId}", document.DocumentId);

        ActiveDocumentChanged?.Invoke(this, new MdiDocumentEventArgs(document));
    }

    public async Task<bool> SaveAllAsync()
    {
        var dirtyDocs = _documents.Where(d => d.IsDirty).ToList();

        foreach (var doc in dirtyDocs)
        {
            if (!await doc.SaveAsync())
            {
                return false;
            }
        }

        return true;
    }

    private void OnDocumentStateChanged(object? sender, DocumentStateChangedEventArgs e)
    {
        // Handle title changes, dirty state, etc.
        if (sender is IMdiDocument doc)
        {
            _logger.LogDebug("Document {DocumentId} state changed: {Property}",
                doc.DocumentId, e.PropertyName);
        }
    }

    public event EventHandler<MdiDocumentEventArgs>? DocumentOpened;
    public event EventHandler<MdiDocumentEventArgs>? DocumentClosed;
    public event EventHandler<MdiDocumentEventArgs>? ActiveDocumentChanged;
}
```

#### 3.3 MDI TabView Container (WinUI3)

```xml
<!-- MdiContainer.xaml -->
<UserControl
    x:Class="Arcana.App.Controls.MdiContainer"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls">

    <Grid>
        <controls:TabView
            x:Name="TabView"
            TabItemsSource="{x:Bind ViewModel.Documents, Mode=OneWay}"
            SelectedItem="{x:Bind ViewModel.ActiveDocument, Mode=TwoWay}"
            IsAddTabButtonVisible="False"
            TabCloseRequested="OnTabCloseRequested"
            SelectionChanged="OnSelectionChanged"
            CanDragTabs="True"
            CanReorderTabs="True"
            TabDroppedOutside="OnTabDroppedOutside">

            <controls:TabView.TabItemTemplate>
                <DataTemplate x:DataType="local:IMdiDocument">
                    <controls:TabViewItem
                        Header="{x:Bind Title, Mode=OneWay}"
                        IconSource="{x:Bind Icon, Mode=OneWay, Converter={StaticResource IconConverter}}"
                        IsClosable="{x:Bind CanClose, Mode=OneWay}">

                        <!-- Dirty indicator -->
                        <controls:TabViewItem.HeaderTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="4">
                                    <TextBlock Text="{Binding Title}"/>
                                    <TextBlock
                                        Text="●"
                                        Foreground="Orange"
                                        Visibility="{Binding IsDirty, Converter={StaticResource BoolToVisibility}}"
                                        ToolTipService.ToolTip="Unsaved changes"/>
                                </StackPanel>
                            </DataTemplate>
                        </controls:TabViewItem.HeaderTemplate>

                        <!-- Content -->
                        <ContentPresenter Content="{x:Bind Content, Mode=OneWay}"/>

                        <!-- Context Menu -->
                        <controls:TabViewItem.ContextFlyout>
                            <MenuFlyout>
                                <MenuFlyoutItem Text="Close" Click="OnCloseTab"/>
                                <MenuFlyoutItem Text="Close Others" Click="OnCloseOtherTabs"/>
                                <MenuFlyoutItem Text="Close All" Click="OnCloseAllTabs"/>
                                <MenuFlyoutSeparator/>
                                <MenuFlyoutItem Text="Close Tabs to the Right" Click="OnCloseTabsToRight"/>
                                <MenuFlyoutSeparator/>
                                <MenuFlyoutItem Text="Duplicate Tab" Click="OnDuplicateTab"/>
                                <MenuFlyoutItem Text="Pin Tab" Click="OnPinTab"/>
                            </MenuFlyout>
                        </controls:TabViewItem.ContextFlyout>
                    </controls:TabViewItem>
                </DataTemplate>
            </controls:TabView.TabItemTemplate>

            <!-- Empty state -->
            <controls:TabView.TabStripFooter>
                <StackPanel
                    Orientation="Horizontal"
                    Spacing="8"
                    Visibility="{x:Bind ViewModel.HasDocuments, Mode=OneWay, Converter={StaticResource InverseBoolToVisibility}}">
                    <TextBlock
                        Text="No documents open. Select a function from the tree."
                        Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                        VerticalAlignment="Center"
                        Margin="16,0"/>
                </StackPanel>
            </controls:TabView.TabStripFooter>
        </controls:TabView>

        <!-- Welcome screen when no tabs -->
        <Grid
            Visibility="{x:Bind ViewModel.HasDocuments, Mode=OneWay, Converter={StaticResource InverseBoolToVisibility}}"
            HorizontalAlignment="Center"
            VerticalAlignment="Center">
            <StackPanel Spacing="16" HorizontalAlignment="Center">
                <FontIcon Glyph="&#xE8A5;" FontSize="64" Opacity="0.5"/>
                <TextBlock
                    Text="Welcome to Arcana ERP"
                    Style="{StaticResource TitleTextBlockStyle}"/>
                <TextBlock
                    Text="Select a function from the menu to get started"
                    Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>

                <!-- Recent Items -->
                <TextBlock Text="Recent:" Margin="0,16,0,0"/>
                <ItemsRepeater ItemsSource="{x:Bind ViewModel.RecentItems}">
                    <ItemsRepeater.ItemTemplate>
                        <DataTemplate>
                            <Button
                                Content="{Binding Name}"
                                Click="OnRecentItemClicked"
                                Margin="0,4"/>
                        </DataTemplate>
                    </ItemsRepeater.ItemTemplate>
                </ItemsRepeater>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

---

### 4. Master-Detail CRUD Pattern

#### 4.1 Master-Detail View Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  [Tab: Customers ×]                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  Toolbar: [+ New] [Edit] [Delete] [Refresh] | [Export ▼] [Print] | [Filter]│
├─────────────────────────────────────────────────────────────────────────────┤
│  Filter Bar: [Search...] [Status: All ▼] [Date: ____] [Apply] [Clear]      │
├───────────────────────────────────┬─────────────────────────────────────────┤
│                                   │                                         │
│   Master List (Grid)              │   Detail Panel                          │
│   ┌─────────────────────────────┐ │   ┌───────────────────────────────────┐ │
│   │ ☐ │ Code    │ Name   │ ... │ │   │ Customer Details                  │ │
│   ├───┼─────────┼────────┼─────┤ │   ├───────────────────────────────────┤ │
│   │ ☐ │ C001    │ Acme   │ ... │ │   │ Code: C001                        │ │
│   │ ☑ │ C002    │ Beta   │ ... │ │   │ Name: Beta Corp                   │ │
│   │ ☐ │ C003    │ Gamma  │ ... │ │   │ Contact: John Smith               │ │
│   │ ☐ │ C004    │ Delta  │ ... │ │   │ Email: john@beta.com              │ │
│   │   │         │        │     │ │   │ Phone: +1-234-567-8900            │ │
│   │   │         │        │     │ │   │                                   │ │
│   │   │         │        │     │ │   │ ┌─ Tabs ─────────────────────────┐│ │
│   │   │         │        │     │ │   │ │ Info │ Orders │ Invoices │ ... ││ │
│   │   │         │        │     │ │   │ ├─────────────────────────────────┤│ │
│   │   │         │        │     │ │   │ │  Related orders list...        ││ │
│   │   │         │        │     │ │   │ │                                 ││ │
│   └─────────────────────────────┘ │   │ └─────────────────────────────────┘│ │
│   Page: [◀][1][2][3][▶] | 50/page │   │                                   │ │
│                                   │   │ [Edit] [Delete] [Print]           │ │
│                                   │   └───────────────────────────────────┘ │
└───────────────────────────────────┴─────────────────────────────────────────┘
```

#### 4.2 Base Master-Detail ViewModel

```csharp
// ============================================
// MASTER-DETAIL CRUD VIEW MODEL (主從式CRUD)
// ============================================

/// <summary>
/// Base ViewModel for Master-Detail CRUD operations.
/// 主從式CRUD基礎ViewModel
/// </summary>
public abstract class MasterDetailViewModel<TEntity, TKey> : ViewModelBase, IMdiDocument
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    protected readonly IRepository<TEntity, TKey> _repository;
    protected readonly IDialogService _dialogService;
    protected readonly ILogger _logger;

    // ========== Document Properties ==========
    public abstract string DocumentType { get; }
    public string DocumentId { get; protected set; }
    public virtual string Title => $"{EntityDisplayName} List";
    public virtual string? Icon => null;

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        protected set => SetProperty(ref _isDirty, value, onChanged: () =>
            StateChanged?.Invoke(this, new DocumentStateChangedEventArgs(nameof(IsDirty))));
    }

    public bool CanClose => true;
    public object Content => this; // The view binds to this ViewModel

    protected abstract string EntityDisplayName { get; }

    // ========== Master List Properties ==========
    private ObservableCollection<TEntity> _items = new();
    public ObservableCollection<TEntity> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    private TEntity? _selectedItem;
    public TEntity? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnSelectedItemChanged(value);
                UpdateCommandStates();
            }
        }
    }

    private List<TEntity> _selectedItems = new();
    public List<TEntity> SelectedItems
    {
        get => _selectedItems;
        set
        {
            if (SetProperty(ref _selectedItems, value))
            {
                UpdateCommandStates();
            }
        }
    }

    // ========== Loading & Paging ==========
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private int _totalPages = 1;
    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    private int _totalRecords;
    public int TotalRecords
    {
        get => _totalRecords;
        set => SetProperty(ref _totalRecords, value);
    }

    private int _pageSize = 50;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                _ = LoadDataAsync();
            }
        }
    }

    // ========== Filter Properties ==========
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private FilterCriteria _filterCriteria = new();
    public FilterCriteria FilterCriteria
    {
        get => _filterCriteria;
        set => SetProperty(ref _filterCriteria, value);
    }

    // ========== Commands ==========
    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand NewCommand { get; }
    public IAsyncRelayCommand EditCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand PrintCommand { get; }
    public IRelayCommand ApplyFilterCommand { get; }
    public IRelayCommand ClearFilterCommand { get; }
    public IRelayCommand FirstPageCommand { get; }
    public IRelayCommand PreviousPageCommand { get; }
    public IRelayCommand NextPageCommand { get; }
    public IRelayCommand LastPageCommand { get; }

    // ========== Constructor ==========
    protected MasterDetailViewModel(
        IRepository<TEntity, TKey> repository,
        IDialogService dialogService,
        ILogger logger)
    {
        _repository = repository;
        _dialogService = dialogService;
        _logger = logger;

        DocumentId = Guid.NewGuid().ToString();

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        NewCommand = new AsyncRelayCommand(CreateNewAsync, CanCreateNew);
        EditCommand = new AsyncRelayCommand(EditSelectedAsync, CanEditSelected);
        DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync, CanDeleteSelected);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsDirty);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        PrintCommand = new RelayCommand(Print);
        ApplyFilterCommand = new RelayCommand(ApplyFilter);
        ClearFilterCommand = new RelayCommand(ClearFilter);
        FirstPageCommand = new RelayCommand(() => GoToPage(1), () => CurrentPage > 1);
        PreviousPageCommand = new RelayCommand(() => GoToPage(CurrentPage - 1), () => CurrentPage > 1);
        NextPageCommand = new RelayCommand(() => GoToPage(CurrentPage + 1), () => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(() => GoToPage(TotalPages), () => CurrentPage < TotalPages);
    }

    // ========== Data Loading ==========
    public virtual async Task LoadDataAsync()
    {
        IsLoading = true;

        try
        {
            var query = BuildQuery();
            var result = await _repository.GetPagedAsync(query, CurrentPage, PageSize);

            Items = new ObservableCollection<TEntity>(result.Items);
            TotalRecords = result.TotalCount;
            TotalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);

            _logger.LogInformation("Loaded {Count} {EntityType} records (page {Page}/{Total})",
                result.Items.Count, EntityDisplayName, CurrentPage, TotalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {EntityType} data", EntityDisplayName);
            await _dialogService.ShowErrorAsync("Load Error", $"Failed to load data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected virtual QuerySpec<TEntity> BuildQuery()
    {
        var query = new QuerySpec<TEntity>();

        // Apply search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query.SearchText = SearchText;
            query.SearchFields = GetSearchFields();
        }

        // Apply filters
        query.Filters = FilterCriteria.ToFilters();

        // Apply sorting
        query.OrderBy = GetDefaultSortField();
        query.OrderDescending = GetDefaultSortDescending();

        return query;
    }

    protected abstract IReadOnlyList<string> GetSearchFields();
    protected abstract string GetDefaultSortField();
    protected virtual bool GetDefaultSortDescending() => false;

    // ========== CRUD Operations ==========
    protected virtual async Task CreateNewAsync()
    {
        var newEntity = CreateNewEntity();
        var result = await ShowEditDialogAsync(newEntity, isNew: true);

        if (result != null)
        {
            await _repository.AddAsync(result);
            Items.Insert(0, result);
            SelectedItem = result;
            TotalRecords++;

            _logger.LogInformation("Created new {EntityType}: {EntityId}",
                EntityDisplayName, result.Id);
        }
    }

    protected abstract TEntity CreateNewEntity();

    protected virtual async Task EditSelectedAsync()
    {
        if (SelectedItem == null) return;

        var result = await ShowEditDialogAsync(SelectedItem, isNew: false);

        if (result != null)
        {
            await _repository.UpdateAsync(result);

            // Update in list
            var index = Items.IndexOf(SelectedItem);
            if (index >= 0)
            {
                Items[index] = result;
            }

            SelectedItem = result;

            _logger.LogInformation("Updated {EntityType}: {EntityId}",
                EntityDisplayName, result.Id);
        }
    }

    protected abstract Task<TEntity?> ShowEditDialogAsync(TEntity entity, bool isNew);

    protected virtual async Task DeleteSelectedAsync()
    {
        if (SelectedItems.Count == 0) return;

        var count = SelectedItems.Count;
        var message = count == 1
            ? $"Are you sure you want to delete this {EntityDisplayName.ToLower()}?"
            : $"Are you sure you want to delete {count} {EntityDisplayName.ToLower()}s?";

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Confirm Delete",
            message,
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            foreach (var item in SelectedItems.ToList())
            {
                await _repository.DeleteAsync(item.Id);
                Items.Remove(item);
                TotalRecords--;
            }

            _logger.LogInformation("Deleted {Count} {EntityType} records", count, EntityDisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {EntityType}", EntityDisplayName);
            await _dialogService.ShowErrorAsync("Delete Error", $"Failed to delete: {ex.Message}");
        }
    }

    // ========== Command Can Execute ==========
    protected virtual bool CanCreateNew() => true;
    protected virtual bool CanEditSelected() => SelectedItem != null;
    protected virtual bool CanDeleteSelected() => SelectedItems.Count > 0;

    protected void UpdateCommandStates()
    {
        NewCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    // ========== Paging ==========
    private void GoToPage(int page)
    {
        CurrentPage = Math.Clamp(page, 1, TotalPages);
        _ = LoadDataAsync();

        FirstPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        LastPageCommand.NotifyCanExecuteChanged();
    }

    // ========== Filter ==========
    private void ApplyFilter()
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    private void ClearFilter()
    {
        SearchText = string.Empty;
        FilterCriteria = new FilterCriteria();
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    // ========== IMdiDocument Implementation ==========
    public virtual async Task<bool> SaveAsync()
    {
        // Override in derived class if needed
        IsDirty = false;
        return true;
    }

    public virtual async Task<bool> CanCloseAsync()
    {
        if (!IsDirty) return true;

        var result = await _dialogService.ShowSaveConfirmAsync(
            "Unsaved Changes",
            "Do you want to save changes before closing?");

        switch (result)
        {
            case SaveConfirmResult.Save:
                return await SaveAsync();
            case SaveConfirmResult.DontSave:
                return true;
            case SaveConfirmResult.Cancel:
            default:
                return false;
        }
    }

    public virtual void OnActivated()
    {
        _logger.LogDebug("{DocumentType} activated: {DocumentId}", DocumentType, DocumentId);
    }

    public virtual void OnDeactivated()
    {
        _logger.LogDebug("{DocumentType} deactivated: {DocumentId}", DocumentType, DocumentId);
    }

    public event EventHandler<DocumentStateChangedEventArgs>? StateChanged;

    // ========== Detail Selection ==========
    protected virtual void OnSelectedItemChanged(TEntity? item)
    {
        // Override to load detail data
    }

    // ========== Export & Print ==========
    protected virtual async Task ExportAsync()
    {
        // Show export options dialog
        var exportType = await _dialogService.ShowExportOptionsAsync();
        if (exportType == null) return;

        var data = Items.ToList();

        switch (exportType)
        {
            case ExportType.Excel:
                await ExportToExcelAsync(data);
                break;
            case ExportType.Csv:
                await ExportToCsvAsync(data);
                break;
            case ExportType.Pdf:
                await ExportToPdfAsync(data);
                break;
        }
    }

    protected virtual async Task ExportToExcelAsync(List<TEntity> data)
    {
        // Implementation
    }

    protected virtual async Task ExportToCsvAsync(List<TEntity> data)
    {
        // Implementation
    }

    protected virtual async Task ExportToPdfAsync(List<TEntity> data)
    {
        // Implementation
    }

    protected virtual void Print()
    {
        // Implementation
    }

    protected virtual Task RefreshAsync()
    {
        return LoadDataAsync();
    }
}
```

#### 4.3 Concrete Customer Master-Detail Example

```csharp
// ============================================
// CUSTOMER MASTER-DETAIL VIEWMODEL EXAMPLE
// ============================================

public class CustomerListViewModel : MasterDetailViewModel<Customer, int>
{
    private readonly ICustomerService _customerService;

    public override string DocumentType => "CustomerList";
    public override string Title => "Customers";
    public override string? Icon => "\uE77B";
    protected override string EntityDisplayName => "Customer";

    // Detail properties
    private CustomerDetailViewModel? _selectedCustomerDetail;
    public CustomerDetailViewModel? SelectedCustomerDetail
    {
        get => _selectedCustomerDetail;
        set => SetProperty(ref _selectedCustomerDetail, value);
    }

    // Filter options
    public ObservableCollection<string> StatusOptions { get; } = new()
    {
        "All", "Active", "Inactive", "Pending"
    };

    private string _selectedStatus = "All";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public CustomerListViewModel(
        IRepository<Customer, int> repository,
        ICustomerService customerService,
        IDialogService dialogService,
        ILogger<CustomerListViewModel> logger)
        : base(repository, dialogService, logger)
    {
        _customerService = customerService;
    }

    protected override IReadOnlyList<string> GetSearchFields() =>
        new[] { "Code", "Name", "ContactName", "Email", "Phone" };

    protected override string GetDefaultSortField() => "Name";

    protected override Customer CreateNewEntity() => new()
    {
        Code = GenerateCustomerCode(),
        Status = CustomerStatus.Active,
        CreatedAt = DateTime.UtcNow
    };

    private string GenerateCustomerCode()
    {
        return $"C{DateTime.Now:yyMMdd}{new Random().Next(1000, 9999)}";
    }

    protected override async Task<Customer?> ShowEditDialogAsync(Customer entity, bool isNew)
    {
        var dialog = new CustomerEditDialog(entity, isNew);
        var result = await _dialogService.ShowDialogAsync(dialog);

        return result == DialogResult.OK ? dialog.Customer : null;
    }

    protected override void OnSelectedItemChanged(Customer? item)
    {
        if (item != null)
        {
            SelectedCustomerDetail = new CustomerDetailViewModel(item, _customerService);
            _ = SelectedCustomerDetail.LoadAsync();
        }
        else
        {
            SelectedCustomerDetail = null;
        }
    }

    protected override QuerySpec<Customer> BuildQuery()
    {
        var query = base.BuildQuery();

        // Add status filter
        if (SelectedStatus != "All")
        {
            query.Filters.Add(new Filter("Status", SelectedStatus));
        }

        return query;
    }
}

/// <summary>
/// Detail view model for customer with related data.
/// </summary>
public class CustomerDetailViewModel : ViewModelBase
{
    private readonly Customer _customer;
    private readonly ICustomerService _customerService;

    public Customer Customer => _customer;

    // Related data tabs
    public ObservableCollection<Order> RecentOrders { get; } = new();
    public ObservableCollection<Invoice> RecentInvoices { get; } = new();
    public ObservableCollection<ContactPerson> Contacts { get; } = new();
    public ObservableCollection<Address> Addresses { get; } = new();
    public ObservableCollection<Note> Notes { get; } = new();

    // Statistics
    public decimal TotalRevenue { get; private set; }
    public int OrderCount { get; private set; }
    public decimal OutstandingBalance { get; private set; }

    public CustomerDetailViewModel(Customer customer, ICustomerService customerService)
    {
        _customer = customer;
        _customerService = customerService;
    }

    public async Task LoadAsync()
    {
        var details = await _customerService.GetCustomerDetailsAsync(_customer.Id);

        RecentOrders.Clear();
        foreach (var order in details.RecentOrders)
            RecentOrders.Add(order);

        RecentInvoices.Clear();
        foreach (var invoice in details.RecentInvoices)
            RecentInvoices.Add(invoice);

        Contacts.Clear();
        foreach (var contact in details.Contacts)
            Contacts.Add(contact);

        TotalRevenue = details.TotalRevenue;
        OrderCount = details.OrderCount;
        OutstandingBalance = details.OutstandingBalance;

        OnPropertyChanged(nameof(TotalRevenue));
        OnPropertyChanged(nameof(OrderCount));
        OnPropertyChanged(nameof(OutstandingBalance));
    }
}
```

#### 4.4 Master-Detail XAML View

```xml
<!-- CustomerListView.xaml -->
<UserControl
    x:Class="Arcana.App.Views.CustomerListView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls"
    xmlns:toolkit="using:CommunityToolkit.WinUI.UI.Controls">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Toolbar -->
            <RowDefinition Height="Auto"/>  <!-- Filter Bar -->
            <RowDefinition Height="*"/>     <!-- Content -->
            <RowDefinition Height="Auto"/>  <!-- Status Bar -->
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <CommandBar Grid.Row="0" DefaultLabelPosition="Right">
            <AppBarButton
                Icon="Add"
                Label="New"
                Command="{x:Bind ViewModel.NewCommand}"/>
            <AppBarButton
                Icon="Edit"
                Label="Edit"
                Command="{x:Bind ViewModel.EditCommand}"/>
            <AppBarButton
                Icon="Delete"
                Label="Delete"
                Command="{x:Bind ViewModel.DeleteCommand}"/>
            <AppBarSeparator/>
            <AppBarButton
                Icon="Refresh"
                Label="Refresh"
                Command="{x:Bind ViewModel.RefreshCommand}"/>
            <AppBarSeparator/>
            <AppBarButton Icon="Download" Label="Export">
                <AppBarButton.Flyout>
                    <MenuFlyout>
                        <MenuFlyoutItem Text="Export to Excel" Click="OnExportExcel"/>
                        <MenuFlyoutItem Text="Export to CSV" Click="OnExportCsv"/>
                        <MenuFlyoutItem Text="Export to PDF" Click="OnExportPdf"/>
                    </MenuFlyout>
                </AppBarButton.Flyout>
            </AppBarButton>
            <AppBarButton
                Icon="Print"
                Label="Print"
                Command="{x:Bind ViewModel.PrintCommand}"/>
        </CommandBar>

        <!-- Filter Bar -->
        <Grid Grid.Row="1" Padding="16,8" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300"/>
                <ColumnDefinition Width="150"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <AutoSuggestBox
                Grid.Column="0"
                PlaceholderText="Search customers..."
                QueryIcon="Find"
                Text="{x:Bind ViewModel.SearchText, Mode=TwoWay}"
                QuerySubmitted="OnSearchSubmitted"/>

            <ComboBox
                Grid.Column="1"
                Margin="8,0"
                ItemsSource="{x:Bind ViewModel.StatusOptions}"
                SelectedItem="{x:Bind ViewModel.SelectedStatus, Mode=TwoWay}"
                PlaceholderText="Status"/>

            <Button
                Grid.Column="2"
                Content="Apply"
                Command="{x:Bind ViewModel.ApplyFilterCommand}"/>

            <Button
                Grid.Column="4"
                Content="Clear"
                Command="{x:Bind ViewModel.ClearFilterCommand}"/>
        </Grid>

        <!-- Master-Detail Content -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" MinWidth="400"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="400" MinWidth="300"/>
            </Grid.ColumnDefinitions>

            <!-- Master List (DataGrid) -->
            <toolkit:DataGrid
                Grid.Column="0"
                ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
                SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}"
                AutoGenerateColumns="False"
                SelectionMode="Extended"
                IsReadOnly="True"
                CanUserSortColumns="True"
                CanUserReorderColumns="True"
                CanUserResizeColumns="True"
                GridLinesVisibility="Horizontal"
                AlternatingRowBackground="{ThemeResource LayerFillColorAltBrush}">

                <toolkit:DataGrid.Columns>
                    <toolkit:DataGridTextColumn
                        Header="Code"
                        Binding="{Binding Code}"
                        Width="100"/>
                    <toolkit:DataGridTextColumn
                        Header="Name"
                        Binding="{Binding Name}"
                        Width="*"/>
                    <toolkit:DataGridTextColumn
                        Header="Contact"
                        Binding="{Binding ContactName}"
                        Width="150"/>
                    <toolkit:DataGridTextColumn
                        Header="Email"
                        Binding="{Binding Email}"
                        Width="200"/>
                    <toolkit:DataGridTextColumn
                        Header="Phone"
                        Binding="{Binding Phone}"
                        Width="120"/>
                    <toolkit:DataGridTemplateColumn Header="Status" Width="100">
                        <toolkit:DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Border
                                    Background="{Binding Status, Converter={StaticResource StatusToBrush}}"
                                    CornerRadius="4"
                                    Padding="8,2">
                                    <TextBlock Text="{Binding Status}" FontSize="12"/>
                                </Border>
                            </DataTemplate>
                        </toolkit:DataGridTemplateColumn.CellTemplate>
                    </toolkit:DataGridTemplateColumn>
                </toolkit:DataGrid.Columns>
            </toolkit:DataGrid>

            <!-- Splitter -->
            <controls:GridSplitter
                Grid.Column="1"
                Width="8"
                ResizeBehavior="BasedOnAlignment"
                ResizeDirection="Columns"/>

            <!-- Detail Panel -->
            <Grid Grid.Column="2">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- Detail Header -->
                <StackPanel Grid.Row="0" Padding="16" Spacing="8">
                    <TextBlock
                        Text="{x:Bind ViewModel.SelectedCustomerDetail.Customer.Name, Mode=OneWay, FallbackValue='Select a customer'}"
                        Style="{StaticResource SubtitleTextBlockStyle}"/>
                    <TextBlock
                        Text="{x:Bind ViewModel.SelectedCustomerDetail.Customer.Code, Mode=OneWay}"
                        Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                </StackPanel>

                <!-- Detail Tabs -->
                <Pivot Grid.Row="1" Margin="0,8,0,0">
                    <!-- Info Tab -->
                    <PivotItem Header="Info">
                        <ScrollViewer Padding="16">
                            <StackPanel Spacing="12">
                                <local:LabeledField Label="Contact" Value="{x:Bind ViewModel.SelectedCustomerDetail.Customer.ContactName, Mode=OneWay}"/>
                                <local:LabeledField Label="Email" Value="{x:Bind ViewModel.SelectedCustomerDetail.Customer.Email, Mode=OneWay}"/>
                                <local:LabeledField Label="Phone" Value="{x:Bind ViewModel.SelectedCustomerDetail.Customer.Phone, Mode=OneWay}"/>
                                <local:LabeledField Label="Address" Value="{x:Bind ViewModel.SelectedCustomerDetail.Customer.Address, Mode=OneWay}"/>

                                <Border Height="1" Background="{ThemeResource DividerStrokeColorDefaultBrush}" Margin="0,8"/>

                                <TextBlock Text="Statistics" Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                    </Grid.ColumnDefinitions>

                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="Total Revenue" Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                                        <TextBlock Text="{x:Bind ViewModel.SelectedCustomerDetail.TotalRevenue, Mode=OneWay, Converter={StaticResource CurrencyConverter}}" Style="{StaticResource SubtitleTextBlockStyle}"/>
                                    </StackPanel>
                                    <StackPanel Grid.Column="1">
                                        <TextBlock Text="Orders" Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                                        <TextBlock Text="{x:Bind ViewModel.SelectedCustomerDetail.OrderCount, Mode=OneWay}" Style="{StaticResource SubtitleTextBlockStyle}"/>
                                    </StackPanel>
                                    <StackPanel Grid.Column="2">
                                        <TextBlock Text="Balance" Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                                        <TextBlock Text="{x:Bind ViewModel.SelectedCustomerDetail.OutstandingBalance, Mode=OneWay, Converter={StaticResource CurrencyConverter}}" Style="{StaticResource SubtitleTextBlockStyle}"/>
                                    </StackPanel>
                                </Grid>
                            </StackPanel>
                        </ScrollViewer>
                    </PivotItem>

                    <!-- Orders Tab -->
                    <PivotItem Header="Orders">
                        <ListView ItemsSource="{x:Bind ViewModel.SelectedCustomerDetail.RecentOrders, Mode=OneWay}">
                            <ListView.ItemTemplate>
                                <DataTemplate>
                                    <Grid Padding="8" ColumnSpacing="12">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="Auto"/>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <TextBlock Text="{Binding OrderNumber}" FontWeight="SemiBold"/>
                                        <TextBlock Grid.Column="1" Text="{Binding OrderDate, Converter={StaticResource DateConverter}}"/>
                                        <TextBlock Grid.Column="2" Text="{Binding TotalAmount, Converter={StaticResource CurrencyConverter}}"/>
                                    </Grid>
                                </DataTemplate>
                            </ListView.ItemTemplate>
                        </ListView>
                    </PivotItem>

                    <!-- Invoices Tab -->
                    <PivotItem Header="Invoices">
                        <ListView ItemsSource="{x:Bind ViewModel.SelectedCustomerDetail.RecentInvoices, Mode=OneWay}">
                            <!-- Similar template -->
                        </ListView>
                    </PivotItem>

                    <!-- Contacts Tab -->
                    <PivotItem Header="Contacts">
                        <ListView ItemsSource="{x:Bind ViewModel.SelectedCustomerDetail.Contacts, Mode=OneWay}">
                            <!-- Contact list template -->
                        </ListView>
                    </PivotItem>
                </Pivot>
            </Grid>
        </Grid>

        <!-- Status Bar / Paging -->
        <Grid Grid.Row="3" Padding="16,8" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBlock
                Grid.Column="0"
                Text="{x:Bind ViewModel.TotalRecords, Mode=OneWay, Converter={StaticResource RecordCountConverter}}"
                VerticalAlignment="Center"/>

            <!-- Paging Controls -->
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4">
                <Button Content="◀◀" Command="{x:Bind ViewModel.FirstPageCommand}" Width="36"/>
                <Button Content="◀" Command="{x:Bind ViewModel.PreviousPageCommand}" Width="36"/>
                <TextBlock VerticalAlignment="Center" Margin="8,0">
                    <Run Text="Page "/>
                    <Run Text="{x:Bind ViewModel.CurrentPage, Mode=OneWay}"/>
                    <Run Text=" of "/>
                    <Run Text="{x:Bind ViewModel.TotalPages, Mode=OneWay}"/>
                </TextBlock>
                <Button Content="▶" Command="{x:Bind ViewModel.NextPageCommand}" Width="36"/>
                <Button Content="▶▶" Command="{x:Bind ViewModel.LastPageCommand}" Width="36"/>

                <ComboBox
                    Margin="16,0,0,0"
                    SelectedValue="{x:Bind ViewModel.PageSize, Mode=TwoWay}"
                    Width="80">
                    <x:Int32>25</x:Int32>
                    <x:Int32>50</x:Int32>
                    <x:Int32>100</x:Int32>
                    <x:Int32>200</x:Int32>
                </ComboBox>
                <TextBlock Text="/page" VerticalAlignment="Center"/>
            </StackPanel>
        </Grid>

        <!-- Loading Overlay -->
        <Grid
            Grid.RowSpan="4"
            Background="{ThemeResource LayerFillColorDefaultBrush}"
            Opacity="0.8"
            Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay}">
            <ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"/>
        </Grid>
    </Grid>
</UserControl>
```

---

### 5. Application Shell Integration

#### 5.1 Main Window Shell

```csharp
// ============================================
// MAIN WINDOW SHELL (主視窗框架)
// ============================================

public sealed partial class MainWindow : Window
{
    private readonly IFunctionTreeService _functionTreeService;
    private readonly IMdiService _mdiService;
    private readonly IAuthService _authService;
    private readonly ILogger<MainWindow> _logger;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        IFunctionTreeService functionTreeService,
        IMdiService mdiService,
        IAuthService authService,
        ILogger<MainWindow> logger)
    {
        _functionTreeService = functionTreeService;
        _mdiService = mdiService;
        _authService = authService;
        _logger = logger;

        ViewModel = new MainWindowViewModel(functionTreeService, mdiService, authService);

        InitializeComponent();

        // Set window title
        Title = $"Arcana ERP - {authService.CurrentUser?.CompanyName}";

        // Handle function tree selection
        FunctionTree.FunctionOpenRequested += OnFunctionOpenRequested;

        // Handle MDI events
        _mdiService.ActiveDocumentChanged += OnActiveDocumentChanged;
    }

    private async void OnFunctionOpenRequested(object? sender, FunctionOpenRequestedEventArgs e)
    {
        try
        {
            if (e.Function.ViewType == null) return;

            // Add to recent
            await _functionTreeService.AddToRecentAsync(
                _authService.CurrentUser!.Id,
                e.Function.Id);

            // Open document
            var document = await _mdiService.OpenOrActivateDocumentAsync(
                e.Function.ViewType,
                e.Function.Id);

            _logger.LogInformation("Opened function: {FunctionId} -> {ViewType}",
                e.Function.Id, e.Function.ViewType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open function: {FunctionId}", e.Function.Id);
            await ShowErrorAsync("Error", $"Failed to open: {ex.Message}");
        }
    }

    private void OnActiveDocumentChanged(object? sender, MdiDocumentEventArgs e)
    {
        // Update window title
        if (e.Document != null)
        {
            Title = $"Arcana ERP - {e.Document.Title} - {_authService.CurrentUser?.CompanyName}";
        }
        else
        {
            Title = $"Arcana ERP - {_authService.CurrentUser?.CompanyName}";
        }
    }

    private async void OnWindowClosing(object sender, WindowClosingEventArgs e)
    {
        // Check for unsaved documents
        var dirtyDocs = _mdiService.Documents.Where(d => d.IsDirty).ToList();

        if (dirtyDocs.Any())
        {
            e.Cancel = true;

            var result = await ShowSaveAllDialogAsync(dirtyDocs);

            if (result == SaveAllResult.SaveAll)
            {
                await _mdiService.SaveAllAsync();
                Close();
            }
            else if (result == SaveAllResult.DiscardAll)
            {
                Close();
            }
            // Cancel - do nothing
        }
    }
}
```

#### 5.2 Main Window XAML

```xml
<!-- MainWindow.xaml -->
<Window
    x:Class="Arcana.App.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls"
    xmlns:local="using:Arcana.App.Controls"
    Title="Arcana ERP"
    Width="1400"
    Height="900"
    MinWidth="1024"
    MinHeight="768">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Title Bar -->
            <RowDefinition Height="Auto"/>  <!-- Menu Bar -->
            <RowDefinition Height="Auto"/>  <!-- Toolbar -->
            <RowDefinition Height="*"/>     <!-- Content -->
            <RowDefinition Height="Auto"/>  <!-- Status Bar -->
        </Grid.RowDefinitions>

        <!-- Custom Title Bar -->
        <Grid Grid.Row="0" Height="32" Background="{ThemeResource SystemAccentColorDark2}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="Assets/logo.png" Height="20" Margin="8,0"/>
            <TextBlock
                Grid.Column="1"
                Text="{x:Bind Title, Mode=OneWay}"
                VerticalAlignment="Center"
                Foreground="White"/>
        </Grid>

        <!-- Menu Bar -->
        <MenuBar Grid.Row="1">
            <MenuBarItem Title="File">
                <MenuFlyoutItem Text="New" Click="OnNewClicked">
                    <MenuFlyoutItem.KeyboardAccelerators>
                        <KeyboardAccelerator Key="N" Modifiers="Control"/>
                    </MenuFlyoutItem.KeyboardAccelerators>
                </MenuFlyoutItem>
                <MenuFlyoutItem Text="Save" Command="{x:Bind ViewModel.SaveCommand}">
                    <MenuFlyoutItem.KeyboardAccelerators>
                        <KeyboardAccelerator Key="S" Modifiers="Control"/>
                    </MenuFlyoutItem.KeyboardAccelerators>
                </MenuFlyoutItem>
                <MenuFlyoutItem Text="Save All" Command="{x:Bind ViewModel.SaveAllCommand}">
                    <MenuFlyoutItem.KeyboardAccelerators>
                        <KeyboardAccelerator Key="S" Modifiers="Control,Shift"/>
                    </MenuFlyoutItem.KeyboardAccelerators>
                </MenuFlyoutItem>
                <MenuFlyoutSeparator/>
                <MenuFlyoutItem Text="Print" Click="OnPrintClicked"/>
                <MenuFlyoutItem Text="Export" Click="OnExportClicked"/>
                <MenuFlyoutSeparator/>
                <MenuFlyoutItem Text="Exit" Click="OnExitClicked"/>
            </MenuBarItem>

            <MenuBarItem Title="Edit">
                <MenuFlyoutItem Text="Undo" Icon="Undo"/>
                <MenuFlyoutItem Text="Redo" Icon="Redo"/>
                <MenuFlyoutSeparator/>
                <MenuFlyoutItem Text="Cut" Icon="Cut"/>
                <MenuFlyoutItem Text="Copy" Icon="Copy"/>
                <MenuFlyoutItem Text="Paste" Icon="Paste"/>
            </MenuBarItem>

            <MenuBarItem Title="View">
                <ToggleMenuFlyoutItem
                    Text="Function Tree"
                    IsChecked="{x:Bind ViewModel.IsFunctionTreeVisible, Mode=TwoWay}"/>
                <ToggleMenuFlyoutItem
                    Text="Status Bar"
                    IsChecked="{x:Bind ViewModel.IsStatusBarVisible, Mode=TwoWay}"/>
                <MenuFlyoutSeparator/>
                <MenuFlyoutItem Text="Refresh" Command="{x:Bind ViewModel.RefreshCommand}"/>
            </MenuBarItem>

            <MenuBarItem Title="Modules" x:Name="ModulesMenu">
                <!-- Dynamically populated from function tree -->
            </MenuBarItem>

            <MenuBarItem Title="Window">
                <MenuFlyoutItem Text="Close Tab" Command="{x:Bind ViewModel.CloseCurrentTabCommand}"/>
                <MenuFlyoutItem Text="Close All Tabs" Command="{x:Bind ViewModel.CloseAllTabsCommand}"/>
                <MenuFlyoutSeparator/>
                <MenuFlyoutItem Text="Next Tab">
                    <MenuFlyoutItem.KeyboardAccelerators>
                        <KeyboardAccelerator Key="Tab" Modifiers="Control"/>
                    </MenuFlyoutItem.KeyboardAccelerators>
                </MenuFlyoutItem>
                <MenuFlyoutItem Text="Previous Tab">
                    <MenuFlyoutItem.KeyboardAccelerators>
                        <KeyboardAccelerator Key="Tab" Modifiers="Control,Shift"/>
                    </MenuFlyoutItem.KeyboardAccelerators>
                </MenuFlyoutItem>
            </MenuBarItem>

            <MenuBarItem Title="Help">
                <MenuFlyoutItem Text="Documentation" Click="OnDocumentationClicked"/>
                <MenuFlyoutItem Text="Keyboard Shortcuts" Click="OnShortcutsClicked"/>
                <MenuFlyoutSeparator/>
                <MenuFlyoutItem Text="About" Click="OnAboutClicked"/>
            </MenuBarItem>
        </MenuBar>

        <!-- Main Toolbar -->
        <CommandBar Grid.Row="2" DefaultLabelPosition="Collapsed">
            <AppBarButton Icon="Save" Label="Save" Command="{x:Bind ViewModel.SaveCommand}"/>
            <AppBarButton Icon="Refresh" Label="Refresh" Command="{x:Bind ViewModel.RefreshCommand}"/>
            <AppBarSeparator/>
            <AppBarButton Icon="Add" Label="New" Command="{x:Bind ViewModel.NewCommand}"/>
            <AppBarButton Icon="Edit" Label="Edit" Command="{x:Bind ViewModel.EditCommand}"/>
            <AppBarButton Icon="Delete" Label="Delete" Command="{x:Bind ViewModel.DeleteCommand}"/>
            <AppBarSeparator/>
            <AppBarButton Icon="Print" Label="Print" Command="{x:Bind ViewModel.PrintCommand}"/>

            <CommandBar.SecondaryCommands>
                <AppBarButton Icon="Setting" Label="Settings" Click="OnSettingsClicked"/>
            </CommandBar.SecondaryCommands>
        </CommandBar>

        <!-- Main Content: Function Tree + MDI -->
        <Grid Grid.Row="3">
            <Grid.ColumnDefinitions>
                <ColumnDefinition
                    Width="{x:Bind ViewModel.FunctionTreeWidth, Mode=TwoWay}"
                    MinWidth="200"
                    MaxWidth="400"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Function Tree (左側功能樹) -->
            <local:FunctionTreeControl
                x:Name="FunctionTree"
                Grid.Column="0"
                Visibility="{x:Bind ViewModel.IsFunctionTreeVisible, Mode=OneWay}"/>

            <!-- Splitter -->
            <controls:GridSplitter
                Grid.Column="1"
                Width="4"
                ResizeBehavior="BasedOnAlignment"
                ResizeDirection="Columns"
                Visibility="{x:Bind ViewModel.IsFunctionTreeVisible, Mode=OneWay}"/>

            <!-- MDI Container (多文件區域) -->
            <local:MdiContainer
                Grid.Column="2"
                ViewModel="{x:Bind ViewModel.MdiViewModel}"/>
        </Grid>

        <!-- Status Bar -->
        <Grid
            Grid.Row="4"
            Padding="16,4"
            Background="{ThemeResource SystemAccentColorDark2}"
            Visibility="{x:Bind ViewModel.IsStatusBarVisible, Mode=OneWay}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBlock
                Grid.Column="0"
                Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
                Foreground="White"/>

            <TextBlock
                Grid.Column="2"
                Text="{x:Bind ViewModel.CurrentUser, Mode=OneWay}"
                Foreground="White"
                Margin="16,0"/>

            <StackPanel Grid.Column="3" Orientation="Horizontal" Spacing="4">
                <Ellipse
                    Width="8"
                    Height="8"
                    Fill="{x:Bind ViewModel.ConnectionStatus, Mode=OneWay, Converter={StaticResource ConnectionStatusToBrush}}"/>
                <TextBlock
                    Text="{x:Bind ViewModel.ConnectionStatusText, Mode=OneWay}"
                    Foreground="White"/>
            </StackPanel>

            <TextBlock
                Grid.Column="4"
                Text="{x:Bind ViewModel.CurrentDateTime, Mode=OneWay}"
                Foreground="White"
                Margin="16,0,0,0"/>
        </Grid>
    </Grid>
</Window>
```

---

### 6. Enterprise Patterns Summary

| Pattern | Purpose | Implementation |
|---------|---------|----------------|
| **Function Tree (功能樹)** | Hierarchical navigation | TreeView with permission filtering |
| **MDI (多文件介面)** | Multiple open documents | TabView with document management |
| **Master-Detail** | List + details view | DataGrid + Detail panel |
| **CRUD Operations** | Create, Read, Update, Delete | Command pattern with validation |
| **Paging** | Large dataset navigation | Server-side pagination |
| **Filtering** | Data filtering | Query builder pattern |
| **Export** | Data export | Excel, CSV, PDF providers |
| **Permissions** | Access control | Role-based + function-level |
| **Localization** | Multi-language support | Resource dictionaries + keys |
| **Offline Support** | Work without connection | Local DB + sync queue |

---

### 5. Dependency Injection Setup

```csharp
// In App.xaml.cs or Program.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArcanaServices(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IAnalyticsTracker, AnalyticsManager>();
        services.AddSingleton<INetworkMonitor, NetworkMonitor>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=arcana.db"));

        // Network
        services.AddHttpClient();
        services.AddRefitClient<IApiService>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));

        // Repository with decorator pattern
        services.AddSingleton<OfflineFirstDataRepository>();
        services.AddSingleton<ICacheEventBus, CacheEventBus>();
        services.AddSingleton<IDataRepository>(sp =>
        {
            var offlineFirst = sp.GetRequiredService<OfflineFirstDataRepository>();
            var cacheEventBus = sp.GetRequiredService<ICacheEventBus>();
            return new CachingDataRepository(offlineFirst, cacheEventBus);
        });

        // Register as Syncable
        services.AddSingleton<ISyncable>(sp =>
            sp.GetRequiredService<OfflineFirstDataRepository>());

        // Domain services
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IValidator<User>, UserValidator>();

        // Sync
        services.AddSingleton<ISynchronizer, SyncManager>();

        // ViewModels
        services.AddTransient<HomeViewModel>();
        services.AddTransient<UserListViewModel>();
        services.AddTransient<UserDetailViewModel>();

        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .WriteTo.File("logs/arcana-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger());
        });

        return services;
    }

    public static IServiceCollection AddPluginSupport(this IServiceCollection services, string pluginsPath)
    {
        services.AddSingleton<IPluginManager>(sp =>
            new PluginManager(
                sp.GetRequiredService<ILogger<PluginManager>>(),
                sp,
                pluginsPath));

        return services;
    }
}
```

### 6. Error Handling System

```csharp
// Error codes
public enum ErrorCode
{
    // Network errors (1000-1999)
    NetworkUnavailable = 1000,
    ConnectionTimeout = 1001,
    ServerUnreachable = 1002,

    // Validation errors (2000-2999)
    ValidationFailed = 2000,
    InvalidEmail = 2001,
    RequiredFieldMissing = 2002,

    // Server errors (3000-3999)
    ServerError = 3000,
    NotFound = 3001,
    Unauthorized = 3002,

    // Data errors (5000-5999)
    ConflictError = 5000,
    DataCorruption = 5001,

    // Database errors (6000-6999)
    DatabaseError = 6000,
    MigrationFailed = 6001,

    // Unknown (9000-9999)
    Unknown = 9000
}

// AppError hierarchy
public abstract record AppError(ErrorCode Code, string Message, Exception? InnerException = null)
{
    public record Network(ErrorCode Code, string Message, Exception? Inner = null)
        : AppError(Code, Message, Inner);

    public record Validation(ErrorCode Code, string Message, IReadOnlyList<string> Errors)
        : AppError(Code, Message);

    public record Server(ErrorCode Code, string Message, int? StatusCode = null)
        : AppError(Code, Message);

    public record Data(ErrorCode Code, string Message, Exception? Inner = null)
        : AppError(Code, Message, Inner);

    public record Unknown(string Message, Exception? Inner = null)
        : AppError(ErrorCode.Unknown, Message, Inner);
}

// Result pattern
public readonly struct Result<T>
{
    public T? Value { get; }
    public AppError? Error { get; }
    public bool IsSuccess => Error == null;
    public bool IsFailure => !IsSuccess;

    private Result(T? value, AppError? error)
    {
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(AppError error) => new(default, error);

    public TResult Match<TResult>(Func<T, TResult> success, Func<AppError, TResult> failure)
        => IsSuccess ? success(Value!) : failure(Error!);
}
```

### 7. Navigation Service

```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(object parameter) where TViewModel : ViewModelBase;
    void GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    private readonly Frame _frame;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAnalyticsTracker _analytics;
    private readonly Dictionary<Type, Type> _viewModelToPage = new();

    public NavigationService(Frame frame, IServiceProvider serviceProvider, IAnalyticsTracker analytics)
    {
        _frame = frame;
        _serviceProvider = serviceProvider;
        _analytics = analytics;

        // Register ViewModel to Page mappings
        RegisterMapping<HomeViewModel, HomePage>();
        RegisterMapping<UserListViewModel, UserListPage>();
        RegisterMapping<UserDetailViewModel, UserDetailPage>();
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var pageType = _viewModelToPage[typeof(TViewModel)];
        _frame.Navigate(pageType);

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        if (_frame.Content is FrameworkElement page)
        {
            page.DataContext = viewModel;
        }

        _analytics.TrackScreen(typeof(TViewModel).Name);
    }

    public void NavigateTo<TViewModel>(object parameter) where TViewModel : ViewModelBase
    {
        var pageType = _viewModelToPage[typeof(TViewModel)];
        _frame.Navigate(pageType, parameter);

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        if (viewModel is INavigationAware navAware)
        {
            navAware.OnNavigatedTo(parameter);
        }

        if (_frame.Content is FrameworkElement page)
        {
            page.DataContext = viewModel;
        }

        _analytics.TrackScreen(typeof(TViewModel).Name, new { parameter });
    }
}

public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom();
}
```

### 8. Background Sync

```csharp
public interface ISynchronizer
{
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
    SyncStatus Status { get; }
    event EventHandler<SyncStatus>? StatusChanged;
}

public enum SyncStatus
{
    Idle,
    Syncing,
    Success,
    Failed
}

public class SyncManager : ISynchronizer
{
    private readonly IEnumerable<ISyncable> _syncables;
    private readonly INetworkMonitor _networkMonitor;
    private readonly ILogger<SyncManager> _logger;

    public SyncStatus Status { get; private set; } = SyncStatus.Idle;
    public event EventHandler<SyncStatus>? StatusChanged;

    public SyncManager(
        IEnumerable<ISyncable> syncables,
        INetworkMonitor networkMonitor,
        ILogger<SyncManager> logger)
    {
        _syncables = syncables;
        _networkMonitor = networkMonitor;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _networkMonitor.IsOnlineAsync())
        {
            return SyncResult.Failure("No network connection");
        }

        UpdateStatus(SyncStatus.Syncing);

        try
        {
            foreach (var syncable in _syncables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await syncable.SyncAsync(cancellationToken);
            }

            UpdateStatus(SyncStatus.Success);
            return SyncResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            UpdateStatus(SyncStatus.Failed);
            return SyncResult.Failure(ex.Message);
        }
    }

    private void UpdateStatus(SyncStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }
}

// Windows Background Task
public sealed class BackgroundSyncTask : IBackgroundTask
{
    public async void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();

        try
        {
            // Get services from the app
            var synchronizer = App.Current.Services.GetRequiredService<ISynchronizer>();
            await synchronizer.SyncAsync();
        }
        finally
        {
            deferral.Complete();
        }
    }
}
```

---

## Key Implementation Guidelines

### 1. Local-First Strategy (本地優先策略)

**Core Principle: The local SQLite database is THE source of truth, not a cache.**

```csharp
// CORRECT: Local-First Repository Pattern
public class LocalFirstRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly AppDbContext _localDb;
    private readonly ISyncService _syncService;  // Optional, for background sync

    // All reads come from local database - instant response
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _localDb.Set<T>().FindAsync(id);
    }

    // All writes go to local database first - instant save
    public async Task<T> CreateAsync(T entity)
    {
        entity.IsPendingSync = true;  // Mark for background sync
        _localDb.Set<T>().Add(entity);
        await _localDb.SaveChangesAsync();

        // Fire-and-forget: Queue for background sync (if server is configured)
        _ = _syncService?.QueueForSyncAsync(entity);

        return entity;  // Return immediately, don't wait for server
    }

    public async Task<T> UpdateAsync(T entity)
    {
        entity.IsPendingSync = true;
        entity.ModifiedAt = DateTime.UtcNow;
        _localDb.Set<T>().Update(entity);
        await _localDb.SaveChangesAsync();

        _ = _syncService?.QueueForSyncAsync(entity);

        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _localDb.Set<T>().FindAsync(id);
        if (entity != null)
        {
            // Soft delete with sync flag
            entity.IsDeleted = true;
            entity.IsPendingSync = true;
            await _localDb.SaveChangesAsync();

            _ = _syncService?.QueueForSyncAsync(entity);
        }
    }
}
```

**Key Rules:**
- **NEVER** wait for server response for CRUD operations
- **NEVER** show loading spinner for local data operations
- **ALWAYS** write to local database first, then sync in background
- **ALWAYS** read from local database, never directly from server
- Server sync is **optional** and happens in background
- Application must work 100% without any server connectivity

**Sync Queue Pattern:**
```csharp
// Pending changes are stored locally and synced when online
public class SyncQueue
{
    // Local table to track pending changes
    public DbSet<PendingSyncItem> PendingSyncItems { get; set; }
}

public class PendingSyncItem
{
    public long Id { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public SyncOperation Operation { get; set; }  // Create, Update, Delete
    public string SerializedData { get; set; }
    public DateTime QueuedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
```

### 2. Plugin Isolation
- Each plugin runs in its own AssemblyLoadContext
- Plugins communicate only through defined contracts
- Plugin crashes don't affect main application
- Hot-reload support via collectible assemblies

### 3. Dependency Direction
```
App → Domain (allowed)
App → Data (allowed)
Domain → Core (allowed)
Data → Domain (allowed)
Data → Core (allowed)
Core → nothing (only primitives/interfaces)
Plugins → Contracts only
```

### 4. Testing Strategy
- Unit tests for Domain services and validators
- Integration tests for Repository layer
- UI tests with WinAppDriver
- Plugin contract verification tests

### 5. Error Handling
- Use Result<T> pattern for operations that can fail
- Centralized error codes for consistency
- Analytics tracking for all errors
- User-friendly error messages

---

## Sample Plugin: Excel Export

```csharp
// plugins/Arcana.Plugin.Excel/ExcelExportPlugin.cs
public class ExcelExportPlugin : PluginBase, IExportPlugin
{
    public override PluginMetadata Metadata => new(
        Id: "arcana.plugin.excel",
        Name: "Excel Export",
        Version: "1.0.0",
        Author: "Arcana Team",
        Description: "Export data to Excel format",
        Dependencies: Array.Empty<string>()
    );

    public string FileExtension => ".xlsx";
    public string MimeType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<byte[]> ExportAsync(IEnumerable<User> users)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Users");

        // Headers
        worksheet.Cells[1, 1].Value = "ID";
        worksheet.Cells[1, 2].Value = "Email";
        worksheet.Cells[1, 3].Value = "First Name";
        worksheet.Cells[1, 4].Value = "Last Name";

        // Data
        var row = 2;
        foreach (var user in users)
        {
            worksheet.Cells[row, 1].Value = user.Id;
            worksheet.Cells[row, 2].Value = user.Email;
            worksheet.Cells[row, 3].Value = user.FirstName;
            worksheet.Cells[row, 4].Value = user.LastName;
            row++;
        }

        return await package.GetAsByteArrayAsync();
    }
}
```

---

## NuGet Packages

```xml
<!-- Core packages -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.*" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.*" />

<!-- Database -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.*" />

<!-- Network -->
<PackageReference Include="Refit" Version="7.*" />
<PackageReference Include="Refit.HttpClientFactory" Version="7.*" />

<!-- Validation -->
<PackageReference Include="FluentValidation" Version="11.*" />

<!-- Logging -->
<PackageReference Include="Serilog" Version="3.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.*" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.*" />

<!-- Caching -->
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.*" />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
```

---

## Getting Started Commands

```bash
# Create solution
dotnet new sln -n Arcana.Windows

# Create projects
dotnet new classlib -n Arcana.Core -o src/Arcana.Core
dotnet new classlib -n Arcana.Domain -o src/Arcana.Domain
dotnet new classlib -n Arcana.Data -o src/Arcana.Data
dotnet new classlib -n Arcana.Sync -o src/Arcana.Sync
dotnet new classlib -n Arcana.Plugins -o src/Arcana.Plugins
dotnet new classlib -n Arcana.Infrastructure -o src/Arcana.Infrastructure
dotnet new winui3 -n Arcana.App -o src/Arcana.App

# Add projects to solution
dotnet sln add src/Arcana.Core
dotnet sln add src/Arcana.Domain
dotnet sln add src/Arcana.Data
dotnet sln add src/Arcana.Sync
dotnet sln add src/Arcana.Plugins
dotnet sln add src/Arcana.Infrastructure
dotnet sln add src/Arcana.App

# Add references
dotnet add src/Arcana.Domain reference src/Arcana.Core
dotnet add src/Arcana.Data reference src/Arcana.Domain src/Arcana.Core
dotnet add src/Arcana.Sync reference src/Arcana.Core
dotnet add src/Arcana.Plugins reference src/Arcana.Core
dotnet add src/Arcana.Infrastructure reference src/Arcana.Core src/Arcana.Domain src/Arcana.Data src/Arcana.Sync src/Arcana.Plugins
dotnet add src/Arcana.App reference src/Arcana.Infrastructure
```

---

## Multi-Faceted Application Foundation (多面向的 App 應用基礎)

The application supports diverse application scenarios through a flexible foundation that accommodates:
- **Enterprise ERP** (Master-Detail CRUD, Function Tree, MDI)
- **Workflow & Process Design** (BPMN 2.0 style visual workflow editor)
- **Dashboard & Analytics** (Modern dashboard with cards, charts, KPIs)
- **Data Visualization** (Interactive charts and graphs)

### 1. Application Facet Types

```csharp
// Core facet types supported by the application
public enum ApplicationFacet
{
    // Enterprise ERP
    MasterDetail,           // List + Detail CRUD
    FormEntry,              // Single form data entry
    ReportViewer,           // Report viewing and export

    // Workflow & Process
    WorkflowDesigner,       // BPMN 2.0 visual workflow designer
    WorkflowMonitor,        // Process instance monitoring
    TaskInbox,              // User task inbox

    // Dashboard & Analytics
    Dashboard,              // KPI cards and charts
    AnalyticsExplorer,      // Ad-hoc data exploration

    // Specialized
    DocumentEditor,         // Rich document editing
    CanvasDesigner,         // Visual design canvas
    TreeEditor,             // Hierarchical data editor
    CalendarView,           // Schedule and calendar
    KanbanBoard,            // Kanban task management
    MapView                 // Geographic visualization
}

// Facet registration for plugins
public interface IFacetProvider
{
    ApplicationFacet Facet { get; }
    string DisplayName { get; }
    string Icon { get; }
    Type ViewType { get; }
    Type ViewModelType { get; }
}
```

### 2. BPMN 2.0 Workflow Designer

Modern workflow designer inspired by professional BPMN tools:

```csharp
// BPMN Element Types
public enum BpmnElementType
{
    // Events
    StartEvent,
    EndEvent,
    IntermediateEvent,
    BoundaryEvent,

    // Activities
    Task,
    UserTask,
    ServiceTask,
    ScriptTask,
    BusinessRuleTask,
    SubProcess,
    CallActivity,

    // Gateways
    ExclusiveGateway,
    ParallelGateway,
    InclusiveGateway,
    EventBasedGateway,

    // Artifacts
    DataObject,
    DataStore,
    Annotation,
    Group,

    // Flows
    SequenceFlow,
    MessageFlow,
    Association
}

// BPMN Element Model
public record BpmnElement
{
    public required string Id { get; init; }
    public required BpmnElementType Type { get; init; }
    public required string Name { get; init; }
    public Point Position { get; init; }
    public Size Size { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    public string? Documentation { get; init; }
}

// BPMN Connection
public record BpmnConnection
{
    public required string Id { get; init; }
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public required BpmnElementType Type { get; init; }
    public string? ConditionExpression { get; init; }
    public List<Point> Waypoints { get; init; } = new();
}

// BPMN Process Definition
public record BpmnProcess
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Version { get; init; }
    public List<BpmnElement> Elements { get; init; } = new();
    public List<BpmnConnection> Connections { get; init; } = new();
    public Dictionary<string, BpmnLane> Lanes { get; init; } = new();
}

// Workflow Designer ViewModel
public partial class WorkflowDesignerViewModel : ViewModelBase, IMdiDocument
{
    private readonly IWorkflowService _workflowService;

    [ObservableProperty]
    private BpmnProcess? _process;

    [ObservableProperty]
    private BpmnElement? _selectedElement;

    [ObservableProperty]
    private BpmnElementType _currentTool = BpmnElementType.Task;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isGridVisible = true;

    [ObservableProperty]
    private bool _isSnapToGrid = true;

    public ObservableCollection<BpmnElement> Elements { get; } = new();
    public ObservableCollection<BpmnConnection> Connections { get; } = new();

    // Toolbar element categories
    public IReadOnlyList<ToolboxCategory> ToolboxCategories { get; } = new[]
    {
        new ToolboxCategory("Events", new[]
        {
            new ToolboxItem(BpmnElementType.StartEvent, "Start", "\uE768"),
            new ToolboxItem(BpmnElementType.EndEvent, "End", "\uE711"),
            new ToolboxItem(BpmnElementType.IntermediateEvent, "Intermediate", "\uE7C4"),
        }),
        new ToolboxCategory("Activities", new[]
        {
            new ToolboxItem(BpmnElementType.Task, "Task", "\uE8A5"),
            new ToolboxItem(BpmnElementType.UserTask, "User Task", "\uE77B"),
            new ToolboxItem(BpmnElementType.ServiceTask, "Service", "\uE713"),
            new ToolboxItem(BpmnElementType.ScriptTask, "Script", "\uE943"),
            new ToolboxItem(BpmnElementType.SubProcess, "Sub-Process", "\uE8C8"),
        }),
        new ToolboxCategory("Gateways", new[]
        {
            new ToolboxItem(BpmnElementType.ExclusiveGateway, "Exclusive", "\uE8B0"),
            new ToolboxItem(BpmnElementType.ParallelGateway, "Parallel", "\uE8B1"),
            new ToolboxItem(BpmnElementType.InclusiveGateway, "Inclusive", "\uE8B2"),
        }),
        new ToolboxCategory("Data", new[]
        {
            new ToolboxItem(BpmnElementType.DataObject, "Data Object", "\uE8A5"),
            new ToolboxItem(BpmnElementType.DataStore, "Data Store", "\uE8B7"),
        }),
    };

    [RelayCommand]
    private void AddElement(BpmnElementType type, Point position)
    {
        var element = new BpmnElement
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Name = GetDefaultName(type),
            Position = SnapToGrid(position),
            Size = GetDefaultSize(type)
        };

        Elements.Add(element);
        Process = Process with { Elements = Elements.ToList() };
        IsDirty = true;
    }

    [RelayCommand]
    private void ConnectElements(string sourceId, string targetId)
    {
        var connection = new BpmnConnection
        {
            Id = Guid.NewGuid().ToString(),
            SourceId = sourceId,
            TargetId = targetId,
            Type = BpmnElementType.SequenceFlow
        };

        Connections.Add(connection);
        IsDirty = true;
    }

    [RelayCommand]
    private async Task ValidateProcessAsync()
    {
        var errors = await _workflowService.ValidateAsync(Process!);
        // Show validation results
    }

    [RelayCommand]
    private async Task DeployProcessAsync()
    {
        if (IsDirty) await SaveAsync();
        await _workflowService.DeployAsync(Process!);
    }
}
```

### 3. Dashboard & Analytics View

Modern dashboard with cards, charts, and KPIs:

```csharp
// Dashboard Widget Types
public enum WidgetType
{
    KpiCard,                // Single metric with trend
    LineChart,              // Time series
    BarChart,               // Comparison
    PieChart,               // Distribution
    DonutChart,             // Distribution with center value
    AreaChart,              // Filled time series
    ScatterPlot,            // Correlation
    Gauge,                  // Progress indicator
    DataTable,              // Tabular data
    Heatmap,                // Grid with intensity
    Map,                    // Geographic
    TreeMap,                // Hierarchical
    Sparkline,              // Inline mini chart
    ProgressBar,            // Completion
    ActivityFeed,           // Recent activities
    QuickActions            // Action buttons
}

// Dashboard Layout
public record DashboardLayout
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Columns { get; init; } = 12;
    public List<DashboardWidget> Widgets { get; init; } = new();
    public string? RefreshInterval { get; init; } // e.g., "30s", "5m"
    public Dictionary<string, string> Parameters { get; init; } = new();
}

// Dashboard Widget
public record DashboardWidget
{
    public required string Id { get; init; }
    public required WidgetType Type { get; init; }
    public required string Title { get; init; }

    // Grid positioning (12-column grid)
    public int Column { get; init; }
    public int Row { get; init; }
    public int ColumnSpan { get; init; } = 3;
    public int RowSpan { get; init; } = 2;

    // Data binding
    public string? DataSource { get; init; }
    public string? Query { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();

    // Interactions
    public string? DrillDownAction { get; init; }
    public List<WidgetAction> Actions { get; init; } = new();
}

// KPI Card Data
public record KpiData
{
    public required string Title { get; init; }
    public required decimal Value { get; init; }
    public string? Unit { get; init; }
    public string? Format { get; init; }  // e.g., "C0", "P2", "N0"
    public decimal? PreviousValue { get; init; }
    public decimal? ChangePercent { get; init; }
    public TrendDirection? Trend { get; init; }
    public string? TrendColor { get; init; }
    public List<decimal>? SparklineData { get; init; }
}

public enum TrendDirection { Up, Down, Neutral }

// Dashboard ViewModel
public partial class DashboardViewModel : ViewModelBase, IMdiDocument
{
    private readonly IDashboardService _dashboardService;
    private readonly IDataSourceProvider _dataProvider;
    private DispatcherTimer? _refreshTimer;

    [ObservableProperty]
    private DashboardLayout? _layout;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<WidgetViewModel> Widgets { get; } = new();

    [RelayCommand]
    private async Task LoadDashboardAsync(string dashboardId)
    {
        IsLoading = true;
        try
        {
            Layout = await _dashboardService.GetLayoutAsync(dashboardId);
            await RefreshAllWidgetsAsync();
            SetupAutoRefresh();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAllWidgetsAsync()
    {
        var tasks = Widgets.Select(w => w.RefreshAsync());
        await Task.WhenAll(tasks);
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private void AddWidget(WidgetType type)
    {
        var widget = new DashboardWidget
        {
            Id = Guid.NewGuid().ToString(),
            Type = type,
            Title = $"New {type}",
            Column = 0,
            Row = GetNextAvailableRow()
        };

        Layout = Layout! with { Widgets = Layout.Widgets.Append(widget).ToList() };
        IsDirty = true;
    }

    private void SetupAutoRefresh()
    {
        if (Layout?.RefreshInterval is { } interval)
        {
            var duration = ParseInterval(interval);
            _refreshTimer = new DispatcherTimer { Interval = duration };
            _refreshTimer.Tick += async (_, _) => await RefreshAllWidgetsAsync();
            _refreshTimer.Start();
        }
    }
}
```

### 4. Dashboard XAML Layout

```xml
<Page
    x:Class="Arcana.App.Views.DashboardPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:CommunityToolkit.WinUI.Controls"
    xmlns:widgets="using:Arcana.App.Controls.Widgets">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Dashboard Header -->
        <Grid Padding="24,16" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <StackPanel>
                <TextBlock
                    Text="{x:Bind ViewModel.Layout.Name, Mode=OneWay}"
                    Style="{StaticResource TitleTextBlockStyle}"/>
                <TextBlock
                    Text="{x:Bind ViewModel.LastRefreshed, Mode=OneWay, Converter={StaticResource RelativeTimeConverter}}"
                    Style="{StaticResource CaptionTextBlockStyle}"
                    Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
            </StackPanel>

            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                <Button Content="Refresh" Command="{x:Bind ViewModel.RefreshAllWidgetsCommand}">
                    <Button.KeyboardAccelerators>
                        <KeyboardAccelerator Key="F5"/>
                    </Button.KeyboardAccelerators>
                </Button>
                <ToggleButton
                    Content="Edit"
                    IsChecked="{x:Bind ViewModel.IsEditing, Mode=TwoWay}"/>
                <Button Content="Export" Command="{x:Bind ViewModel.ExportCommand}"/>
            </StackPanel>
        </Grid>

        <!-- Dashboard Grid -->
        <ScrollViewer Grid.Row="1" Padding="24">
            <controls:UniformGrid
                Columns="12"
                RowSpacing="16"
                ColumnSpacing="16">

                <!-- Widget Items via ItemsRepeater -->
                <ItemsRepeater ItemsSource="{x:Bind ViewModel.Widgets}">
                    <ItemsRepeater.Layout>
                        <controls:UniformGridLayout/>
                    </ItemsRepeater.Layout>
                    <ItemsRepeater.ItemTemplate>
                        <DataTemplate x:DataType="widgets:WidgetViewModel">
                            <widgets:DashboardWidgetControl
                                ViewModel="{x:Bind}"
                                Grid.Column="{x:Bind Column}"
                                Grid.Row="{x:Bind Row}"
                                Grid.ColumnSpan="{x:Bind ColumnSpan}"
                                Grid.RowSpan="{x:Bind RowSpan}"/>
                        </DataTemplate>
                    </ItemsRepeater.ItemTemplate>
                </ItemsRepeater>
            </controls:UniformGrid>
        </ScrollViewer>

        <!-- Loading Overlay -->
        <Grid
            Grid.RowSpan="2"
            Background="{ThemeResource LayerOnMicaBaseAltFillColorDefaultBrush}"
            Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay}">
            <ProgressRing IsActive="True"/>
        </Grid>
    </Grid>
</Page>
```

### 5. KPI Card Widget

```xml
<!-- KPI Card Control -->
<UserControl x:Class="Arcana.App.Controls.Widgets.KpiCardWidget">
    <Border
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        CornerRadius="8"
        Padding="20"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1">

        <Grid RowSpacing="12">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Title -->
            <TextBlock
                Text="{x:Bind ViewModel.Title, Mode=OneWay}"
                Style="{StaticResource BodyTextBlockStyle}"
                Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>

            <!-- Value -->
            <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8">
                <TextBlock
                    Text="{x:Bind ViewModel.FormattedValue, Mode=OneWay}"
                    Style="{StaticResource TitleLargeTextBlockStyle}"
                    FontWeight="SemiBold"/>
                <TextBlock
                    Text="{x:Bind ViewModel.Unit, Mode=OneWay}"
                    Style="{StaticResource BodyTextBlockStyle}"
                    VerticalAlignment="Bottom"
                    Margin="0,0,0,4"/>
            </StackPanel>

            <!-- Trend -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="4">
                <FontIcon
                    Glyph="{x:Bind ViewModel.TrendIcon, Mode=OneWay}"
                    Foreground="{x:Bind ViewModel.TrendColor, Mode=OneWay}"
                    FontSize="12"/>
                <TextBlock
                    Text="{x:Bind ViewModel.ChangePercentFormatted, Mode=OneWay}"
                    Foreground="{x:Bind ViewModel.TrendColor, Mode=OneWay}"
                    Style="{StaticResource CaptionTextBlockStyle}"/>
                <TextBlock
                    Text="vs last period"
                    Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                    Style="{StaticResource CaptionTextBlockStyle}"/>
            </StackPanel>

            <!-- Sparkline -->
            <controls:Sparkline
                Grid.Row="3"
                Data="{x:Bind ViewModel.SparklineData, Mode=OneWay}"
                StrokeColor="{ThemeResource AccentFillColorDefaultBrush}"
                FillColor="{ThemeResource AccentFillColorSecondaryBrush}"
                Height="40"/>
        </Grid>
    </Border>
</UserControl>
```

---

## Theme System (主題系統)

Comprehensive theming support with built-in themes and custom theme definition capabilities.

### 1. Theme Architecture

```csharp
// Theme definition
public record Theme
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public ThemeType Type { get; init; } = ThemeType.Custom;
    public string? Author { get; init; }
    public string? Description { get; init; }
    public Uri? PreviewImage { get; init; }

    // Color palette
    public required ThemeColors Colors { get; init; }

    // Typography
    public ThemeTypography? Typography { get; init; }

    // Spacing and sizing
    public ThemeSpacing? Spacing { get; init; }

    // Component-specific overrides
    public Dictionary<string, ThemeComponentStyle> Components { get; init; } = new();
}

public enum ThemeType
{
    System,     // Light/Dark system themes
    BuiltIn,    // Pre-defined app themes
    Custom,     // User-defined themes
    Plugin      // Contributed by plugins
}

// Color palette definition
public record ThemeColors
{
    // Primary colors
    public required string Primary { get; init; }
    public required string PrimaryLight { get; init; }
    public required string PrimaryDark { get; init; }
    public required string OnPrimary { get; init; }

    // Secondary colors
    public required string Secondary { get; init; }
    public required string SecondaryLight { get; init; }
    public required string SecondaryDark { get; init; }
    public required string OnSecondary { get; init; }

    // Background colors
    public required string Background { get; init; }
    public required string Surface { get; init; }
    public required string SurfaceVariant { get; init; }
    public required string OnBackground { get; init; }
    public required string OnSurface { get; init; }

    // Status colors
    public required string Success { get; init; }
    public required string Warning { get; init; }
    public required string Error { get; init; }
    public required string Info { get; init; }

    // Additional colors
    public string? Accent { get; init; }
    public string? Border { get; init; }
    public string? Divider { get; init; }
    public string? Shadow { get; init; }

    // Semantic colors
    public Dictionary<string, string> Semantic { get; init; } = new();
}

// Typography settings
public record ThemeTypography
{
    public string FontFamily { get; init; } = "Segoe UI Variable";
    public string MonospaceFamily { get; init; } = "Cascadia Code";

    public double DisplayLarge { get; init; } = 57;
    public double DisplayMedium { get; init; } = 45;
    public double DisplaySmall { get; init; } = 36;

    public double HeadlineLarge { get; init; } = 32;
    public double HeadlineMedium { get; init; } = 28;
    public double HeadlineSmall { get; init; } = 24;

    public double TitleLarge { get; init; } = 22;
    public double TitleMedium { get; init; } = 16;
    public double TitleSmall { get; init; } = 14;

    public double BodyLarge { get; init; } = 16;
    public double BodyMedium { get; init; } = 14;
    public double BodySmall { get; init; } = 12;

    public double LabelLarge { get; init; } = 14;
    public double LabelMedium { get; init; } = 12;
    public double LabelSmall { get; init; } = 11;
}

// Spacing settings
public record ThemeSpacing
{
    public double XSmall { get; init; } = 4;
    public double Small { get; init; } = 8;
    public double Medium { get; init; } = 16;
    public double Large { get; init; } = 24;
    public double XLarge { get; init; } = 32;
    public double XXLarge { get; init; } = 48;

    public double CornerRadiusSmall { get; init; } = 4;
    public double CornerRadiusMedium { get; init; } = 8;
    public double CornerRadiusLarge { get; init; } = 12;
    public double CornerRadiusFull { get; init; } = 9999;
}

// Component-specific styling
public record ThemeComponentStyle
{
    public Dictionary<string, string> Properties { get; init; } = new();
}
```

### 2. Theme Service

```csharp
public interface IThemeService
{
    Theme CurrentTheme { get; }
    IReadOnlyList<Theme> AvailableThemes { get; }

    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    Task SetThemeAsync(string themeId);
    Task SetSystemThemeAsync(); // Follow system Light/Dark

    Task<Theme> CreateCustomThemeAsync(Theme baseTheme, string name);
    Task UpdateCustomThemeAsync(Theme theme);
    Task DeleteCustomThemeAsync(string themeId);
    Task<Theme> ImportThemeAsync(string filePath);
    Task ExportThemeAsync(Theme theme, string filePath);

    void RegisterTheme(Theme theme); // For plugins
    void UnregisterTheme(string themeId);
}

public class ThemeService : IThemeService
{
    private readonly ISettingsService _settings;
    private readonly ILogger<ThemeService> _logger;
    private readonly List<Theme> _themes = new();
    private Theme _currentTheme;

    public Theme CurrentTheme => _currentTheme;
    public IReadOnlyList<Theme> AvailableThemes => _themes.AsReadOnly();

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService(ISettingsService settings, ILogger<ThemeService> logger)
    {
        _settings = settings;
        _logger = logger;

        // Register built-in themes
        RegisterBuiltInThemes();

        // Load custom themes
        LoadCustomThemes();
    }

    private void RegisterBuiltInThemes()
    {
        _themes.AddRange(new[]
        {
            Themes.Light,
            Themes.Dark,
            Themes.HighContrast,
            Themes.Midnight,
            Themes.Ocean,
            Themes.Forest,
            Themes.Sunset,
            Themes.Professional
        });
    }

    public async Task SetThemeAsync(string themeId)
    {
        var theme = _themes.FirstOrDefault(t => t.Id == themeId)
            ?? throw new ArgumentException($"Theme '{themeId}' not found");

        _currentTheme = theme;
        await _settings.SetAsync("theme.current", themeId);

        ApplyTheme(theme);

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme));
    }

    private void ApplyTheme(Theme theme)
    {
        var resources = Application.Current.Resources;

        // Apply color resources
        ApplyColorResource(resources, "PrimaryBrush", theme.Colors.Primary);
        ApplyColorResource(resources, "PrimaryLightBrush", theme.Colors.PrimaryLight);
        ApplyColorResource(resources, "PrimaryDarkBrush", theme.Colors.PrimaryDark);
        ApplyColorResource(resources, "OnPrimaryBrush", theme.Colors.OnPrimary);

        ApplyColorResource(resources, "SecondaryBrush", theme.Colors.Secondary);
        ApplyColorResource(resources, "BackgroundBrush", theme.Colors.Background);
        ApplyColorResource(resources, "SurfaceBrush", theme.Colors.Surface);
        ApplyColorResource(resources, "OnBackgroundBrush", theme.Colors.OnBackground);
        ApplyColorResource(resources, "OnSurfaceBrush", theme.Colors.OnSurface);

        ApplyColorResource(resources, "SuccessBrush", theme.Colors.Success);
        ApplyColorResource(resources, "WarningBrush", theme.Colors.Warning);
        ApplyColorResource(resources, "ErrorBrush", theme.Colors.Error);
        ApplyColorResource(resources, "InfoBrush", theme.Colors.Info);

        // Apply semantic colors
        foreach (var (key, value) in theme.Colors.Semantic)
        {
            ApplyColorResource(resources, $"{key}Brush", value);
        }

        // Apply typography
        if (theme.Typography != null)
        {
            resources["DefaultFontFamily"] = new FontFamily(theme.Typography.FontFamily);
            resources["MonospaceFontFamily"] = new FontFamily(theme.Typography.MonospaceFamily);
        }

        // Apply spacing
        if (theme.Spacing != null)
        {
            resources["SpacingXSmall"] = theme.Spacing.XSmall;
            resources["SpacingSmall"] = theme.Spacing.Small;
            resources["SpacingMedium"] = theme.Spacing.Medium;
            resources["SpacingLarge"] = theme.Spacing.Large;
            resources["CornerRadiusSmall"] = new CornerRadius(theme.Spacing.CornerRadiusSmall);
            resources["CornerRadiusMedium"] = new CornerRadius(theme.Spacing.CornerRadiusMedium);
            resources["CornerRadiusLarge"] = new CornerRadius(theme.Spacing.CornerRadiusLarge);
        }

        // Force UI refresh
        if (App.MainWindow?.Content is FrameworkElement root)
        {
            var currentTheme = root.RequestedTheme;
            root.RequestedTheme = currentTheme == ElementTheme.Light
                ? ElementTheme.Dark
                : ElementTheme.Light;
            root.RequestedTheme = currentTheme;
        }
    }

    private void ApplyColorResource(ResourceDictionary resources, string key, string colorHex)
    {
        var color = ParseColor(colorHex);
        resources[key] = new SolidColorBrush(color);
        resources[$"{key}Color"] = color;
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        return hex.Length switch
        {
            6 => Color.FromArgb(255,
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16)),
            8 => Color.FromArgb(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                Convert.ToByte(hex[6..8], 16)),
            _ => throw new ArgumentException($"Invalid color format: {hex}")
        };
    }

    public async Task<Theme> CreateCustomThemeAsync(Theme baseTheme, string name)
    {
        var customTheme = baseTheme with
        {
            Id = $"custom.{Guid.NewGuid():N}",
            Name = name.ToLowerInvariant().Replace(" ", "-"),
            DisplayName = name,
            Type = ThemeType.Custom,
            Author = Environment.UserName
        };

        _themes.Add(customTheme);
        await SaveCustomThemesAsync();

        return customTheme;
    }

    public async Task<Theme> ImportThemeAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var theme = JsonSerializer.Deserialize<Theme>(json, JsonOptions.Default)!;

        // Ensure it's marked as custom
        theme = theme with { Type = ThemeType.Custom };

        _themes.Add(theme);
        await SaveCustomThemesAsync();

        return theme;
    }

    public async Task ExportThemeAsync(Theme theme, string filePath)
    {
        var json = JsonSerializer.Serialize(theme, JsonOptions.Indented);
        await File.WriteAllTextAsync(filePath, json);
    }
}
```

### 3. Built-in Themes

```csharp
public static class Themes
{
    public static Theme Light => new()
    {
        Id = "system.light",
        Name = "light",
        DisplayName = "Light",
        Type = ThemeType.System,
        Colors = new ThemeColors
        {
            Primary = "#0078D4",
            PrimaryLight = "#429CE3",
            PrimaryDark = "#005A9E",
            OnPrimary = "#FFFFFF",

            Secondary = "#6B6B6B",
            SecondaryLight = "#8A8A8A",
            SecondaryDark = "#4A4A4A",
            OnSecondary = "#FFFFFF",

            Background = "#F3F3F3",
            Surface = "#FFFFFF",
            SurfaceVariant = "#F9F9F9",
            OnBackground = "#1A1A1A",
            OnSurface = "#1A1A1A",

            Success = "#107C10",
            Warning = "#F7630C",
            Error = "#C42B1C",
            Info = "#0078D4",

            Border = "#E5E5E5",
            Divider = "#EBEBEB",
            Shadow = "#00000029"
        }
    };

    public static Theme Dark => new()
    {
        Id = "system.dark",
        Name = "dark",
        DisplayName = "Dark",
        Type = ThemeType.System,
        Colors = new ThemeColors
        {
            Primary = "#60CDFF",
            PrimaryLight = "#99EBFF",
            PrimaryDark = "#0093CC",
            OnPrimary = "#003544",

            Secondary = "#9E9E9E",
            SecondaryLight = "#BDBDBD",
            SecondaryDark = "#757575",
            OnSecondary = "#1E1E1E",

            Background = "#202020",
            Surface = "#2D2D2D",
            SurfaceVariant = "#383838",
            OnBackground = "#FFFFFF",
            OnSurface = "#FFFFFF",

            Success = "#6CCB5F",
            Warning = "#FCE100",
            Error = "#FF99A4",
            Info = "#60CDFF",

            Border = "#404040",
            Divider = "#353535",
            Shadow = "#00000066"
        }
    };

    public static Theme Midnight => new()
    {
        Id = "builtin.midnight",
        Name = "midnight",
        DisplayName = "Midnight",
        Type = ThemeType.BuiltIn,
        Description = "Deep blue theme for night work",
        Colors = new ThemeColors
        {
            Primary = "#7C4DFF",
            PrimaryLight = "#B47CFF",
            PrimaryDark = "#651FFF",
            OnPrimary = "#FFFFFF",

            Secondary = "#00BFA5",
            SecondaryLight = "#5DF2D6",
            SecondaryDark = "#008E76",
            OnSecondary = "#000000",

            Background = "#0A0E14",
            Surface = "#0F1419",
            SurfaceVariant = "#1A1F26",
            OnBackground = "#E6E6E6",
            OnSurface = "#E6E6E6",

            Success = "#00C853",
            Warning = "#FFB300",
            Error = "#FF5252",
            Info = "#448AFF",

            Accent = "#7C4DFF",
            Border = "#2B3037",
            Divider = "#1E2228",
            Shadow = "#00000080"
        }
    };

    public static Theme Professional => new()
    {
        Id = "builtin.professional",
        Name = "professional",
        DisplayName = "Professional",
        Type = ThemeType.BuiltIn,
        Description = "Clean, corporate-friendly theme",
        Colors = new ThemeColors
        {
            Primary = "#1E3A5F",
            PrimaryLight = "#4A6FA5",
            PrimaryDark = "#0D1F33",
            OnPrimary = "#FFFFFF",

            Secondary = "#5C6BC0",
            SecondaryLight = "#8E99C0",
            SecondaryDark = "#3F51B5",
            OnSecondary = "#FFFFFF",

            Background = "#FAFAFA",
            Surface = "#FFFFFF",
            SurfaceVariant = "#F5F5F5",
            OnBackground = "#212121",
            OnSurface = "#212121",

            Success = "#2E7D32",
            Warning = "#ED6C02",
            Error = "#D32F2F",
            Info = "#0288D1",

            Border = "#E0E0E0",
            Divider = "#EEEEEE",
            Shadow = "#00000014"
        }
    };
}
```

### 4. Theme JSON Definition (for custom/plugin themes)

```json
{
  "id": "custom.ocean-breeze",
  "name": "ocean-breeze",
  "displayName": "Ocean Breeze",
  "type": "Custom",
  "author": "User",
  "description": "Calm ocean-inspired theme with blue-green tones",
  "colors": {
    "primary": "#0891B2",
    "primaryLight": "#22D3EE",
    "primaryDark": "#0E7490",
    "onPrimary": "#FFFFFF",

    "secondary": "#14B8A6",
    "secondaryLight": "#5EEAD4",
    "secondaryDark": "#0D9488",
    "onSecondary": "#FFFFFF",

    "background": "#F0FDFA",
    "surface": "#FFFFFF",
    "surfaceVariant": "#CCFBF1",
    "onBackground": "#134E4A",
    "onSurface": "#134E4A",

    "success": "#059669",
    "warning": "#D97706",
    "error": "#DC2626",
    "info": "#0891B2",

    "accent": "#06B6D4",
    "border": "#99F6E4",
    "divider": "#A7F3D0",

    "semantic": {
      "revenue": "#059669",
      "expense": "#DC2626",
      "pending": "#D97706"
    }
  },
  "typography": {
    "fontFamily": "Segoe UI Variable",
    "monospaceFamily": "Cascadia Code"
  },
  "spacing": {
    "small": 8,
    "medium": 16,
    "large": 24,
    "cornerRadiusMedium": 12
  },
  "components": {
    "button": {
      "borderRadius": "8",
      "fontWeight": "600"
    },
    "card": {
      "borderRadius": "16",
      "elevation": "2"
    }
  }
}
```

### 5. Theme Picker UI

```xml
<ContentDialog x:Class="Arcana.App.Dialogs.ThemePickerDialog">
    <Grid Width="600" RowSpacing="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <TextBlock Text="Choose Theme" Style="{StaticResource SubtitleTextBlockStyle}"/>

        <!-- Theme Grid -->
        <GridView
            Grid.Row="1"
            ItemsSource="{x:Bind ViewModel.Themes}"
            SelectedItem="{x:Bind ViewModel.SelectedTheme, Mode=TwoWay}"
            SelectionMode="Single">
            <GridView.ItemTemplate>
                <DataTemplate x:DataType="models:Theme">
                    <Border
                        Width="160"
                        Height="120"
                        CornerRadius="8"
                        BorderThickness="2"
                        BorderBrush="{x:Bind IsSelected, Converter={StaticResource BoolToBorderBrush}}">

                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>

                            <!-- Color Preview -->
                            <Grid CornerRadius="6,6,0,0">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <Rectangle Fill="{x:Bind Colors.Primary, Converter={StaticResource HexToBrush}}"/>
                                <Rectangle Grid.Column="1" Fill="{x:Bind Colors.Secondary, Converter={StaticResource HexToBrush}}"/>
                                <Rectangle Height="20" VerticalAlignment="Bottom" Grid.ColumnSpan="2"
                                           Fill="{x:Bind Colors.Background, Converter={StaticResource HexToBrush}}"/>
                            </Grid>

                            <!-- Theme Name -->
                            <Border Grid.Row="1" Padding="8,4"
                                    Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                                <StackPanel>
                                    <TextBlock Text="{x:Bind DisplayName}" FontWeight="SemiBold"/>
                                    <TextBlock
                                        Text="{x:Bind Type}"
                                        Style="{StaticResource CaptionTextBlockStyle}"
                                        Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                                </StackPanel>
                            </Border>
                        </Grid>
                    </Border>
                </DataTemplate>
            </GridView.ItemTemplate>
        </GridView>

        <!-- Actions -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="8" HorizontalAlignment="Right">
            <Button Content="Import Theme" Command="{x:Bind ViewModel.ImportThemeCommand}"/>
            <Button Content="Create Custom" Command="{x:Bind ViewModel.CreateCustomCommand}"/>
        </StackPanel>
    </Grid>
</ContentDialog>
```

### 6. Theme Settings in App

```xml
<!-- In Settings Page -->
<StackPanel Spacing="16">
    <TextBlock Text="Appearance" Style="{StaticResource SubtitleTextBlockStyle}"/>

    <!-- Theme Mode -->
    <RadioButtons Header="Theme Mode">
        <RadioButton Content="Follow System" Tag="system"/>
        <RadioButton Content="Light" Tag="light"/>
        <RadioButton Content="Dark" Tag="dark"/>
    </RadioButtons>

    <!-- Accent Color -->
    <ComboBox Header="Accent Color" ItemsSource="{x:Bind ViewModel.AccentColors}">
        <ComboBox.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <Ellipse Width="16" Height="16" Fill="{Binding Color}"/>
                    <TextBlock Text="{Binding Name}"/>
                </StackPanel>
            </DataTemplate>
        </ComboBox.ItemTemplate>
    </ComboBox>

    <!-- Theme Selection -->
    <Button Content="Choose Theme..." Command="{x:Bind ViewModel.OpenThemePickerCommand}"/>

    <!-- Current Theme Preview -->
    <Border
        Padding="16"
        CornerRadius="8"
        Background="{ThemeResource SurfaceBrush}"
        BorderBrush="{ThemeResource BorderBrush}"
        BorderThickness="1">
        <Grid RowSpacing="8">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <TextBlock Text="Preview" FontWeight="SemiBold"/>

            <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8">
                <Button Content="Primary" Style="{StaticResource AccentButtonStyle}"/>
                <Button Content="Secondary"/>
                <Button Content="Success" Background="{ThemeResource SuccessBrush}"/>
                <Button Content="Warning" Background="{ThemeResource WarningBrush}"/>
                <Button Content="Error" Background="{ThemeResource ErrorBrush}"/>
            </StackPanel>
        </Grid>
    </Border>
</StackPanel>
```

### 7. Plugin Theme Contribution

Plugins can contribute themes via the extension point system:

```json
// In plugin.json
{
  "contributes": {
    "themes": [
      {
        "id": "plugin.corporate-blue",
        "name": "corporate-blue",
        "displayName": "Corporate Blue",
        "path": "./themes/corporate-blue.json"
      },
      {
        "id": "plugin.high-contrast-green",
        "name": "high-contrast-green",
        "displayName": "High Contrast Green",
        "path": "./themes/high-contrast-green.json"
      }
    ]
  }
}
```

```csharp
// Theme contribution point handler
public class ThemeContributionHandler : IContributionHandler<ThemeContribution>
{
    private readonly IThemeService _themeService;

    public async Task ProcessAsync(
        string extensionId,
        ThemeContribution contribution,
        CancellationToken ct)
    {
        var themePath = Path.Combine(
            GetExtensionPath(extensionId),
            contribution.Path);

        var theme = await LoadThemeFromFile(themePath);
        theme = theme with { Type = ThemeType.Plugin };

        _themeService.RegisterTheme(theme);
    }
}
```

---

## GUI Design References

The application GUI is inspired by modern design patterns from:

### 1. BPMN 2.0 Workflow Design

Reference: Modern workflow editors with clean, node-based interfaces

**Key Design Elements:**
- **Canvas-based editing** with zoom/pan controls
- **Toolbox sidebar** with categorized elements (Events, Activities, Gateways)
- **Property panel** on the right for selected element configuration
- **Mini-map** for navigation in large diagrams
- **Connection handles** with smart routing
- **Grid background** with snap-to-grid functionality
- **Contextual menus** on right-click
- **Validation indicators** (error badges on invalid elements)

**Visual Style:**
- Clean, minimal interface with focus on the canvas
- Subtle shadows and rounded corners on nodes
- Color-coded element types (green for start, red for end, blue for tasks)
- Animated connections and transitions
- Semi-transparent overlays for selection states

### 2. Dashboard Design

Reference: Modern analytics dashboards with card-based layouts

**Key Design Elements:**
- **12-column responsive grid** for widget placement
- **KPI cards** with large numbers, trends, and sparklines
- **Chart widgets** (line, bar, pie, donut) with hover interactions
- **Data tables** with sorting, filtering, and pagination
- **Activity feeds** with timeline visualization
- **Date range selectors** for time-based filtering
- **Export options** (PDF, Excel, Image)

**Visual Style:**
- Card-based layout with subtle shadows
- Generous white space and padding
- Accent colors for data visualization
- Micro-animations on data updates
- Skeleton loaders during data fetch
- Responsive breakpoints for different screen sizes

### 3. Enterprise Application Patterns

**Shell Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│ 🏠 App Title                            🔔 👤 User ⚙️      │
├──────────────┬──────────────────────────────────────────────┤
│              │ Tab 1 │ Tab 2 │ Tab 3 │                × │
│  Function    ├──────────────────────────────────────────────┤
│  Tree        │                                              │
│              │         Document Content Area                │
│  ├─ Module 1 │                                              │
│  │  ├─ Sub 1 │    ┌─────────────────────────────────┐      │
│  │  └─ Sub 2 │    │     Master-Detail View          │      │
│  ├─ Module 2 │    │  ┌─────────┬───────────────────┐│      │
│  └─ Module 3 │    │  │ List    │  Detail Form      ││      │
│              │    │  │         │                   ││      │
│              │    │  │ Item 1  │  Field 1: ___     ││      │
│  ★ Favorites │    │  │ Item 2  │  Field 2: ___     ││      │
│  🕐 Recent   │    │  │ Item 3► │  Field 3: ___     ││      │
│              │    │  │         │                   ││      │
│              │    │  └─────────┴───────────────────┘│      │
│              │    └─────────────────────────────────┘      │
├──────────────┴──────────────────────────────────────────────┤
│ Status: Ready                    │ User │ 🟢 Online │ Time │
└─────────────────────────────────────────────────────────────┘
```

**Design Principles:**
- Clear visual hierarchy with consistent spacing
- Keyboard navigation support throughout
- Contextual actions based on selection
- Persistent state across sessions
- Responsive layout that adapts to window size

---

## Local-First Data Architecture (本地優先資料架構)

### Complete Local Database Schema

```csharp
// ============================================
// LOCAL DATABASE CONTEXT (本地資料庫上下文)
// ============================================

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Business Entities (業務實體)
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    // Application Entities (應用程式實體)
    public DbSet<AppUser> Users { get; set; }
    public DbSet<FunctionNode> FunctionNodes { get; set; }
    public DbSet<UserFavorite> UserFavorites { get; set; }
    public DbSet<UserRecentItem> UserRecentItems { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    // Sync Infrastructure (同步基礎設施)
    public DbSet<SyncQueueItem> SyncQueue { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<ConflictRecord> ConflictRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }
}
```

### Sync Queue Implementation

```csharp
// ============================================
// SYNC QUEUE (同步佇列) - Tracks all pending changes
// ============================================

public enum SyncOperation
{
    Create,
    Update,
    Delete
}

public enum SyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Conflict
}

/// <summary>
/// Tracks changes that need to be synced to server.
/// 追蹤需要同步到伺服器的變更
/// </summary>
public class SyncQueueItem
{
    public long Id { get; set; }

    /// <summary>Type name of the entity</summary>
    public required string EntityType { get; set; }

    /// <summary>Local ID of the entity</summary>
    public required string LocalId { get; set; }

    /// <summary>Server ID of the entity (if known)</summary>
    public string? ServerId { get; set; }

    /// <summary>Type of operation</summary>
    public SyncOperation Operation { get; set; }

    /// <summary>Serialized entity data (JSON)</summary>
    public required string SerializedData { get; set; }

    /// <summary>When the change was queued</summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Current sync status</summary>
    public SyncStatus Status { get; set; } = SyncStatus.Pending;

    /// <summary>Number of sync attempts</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>Maximum retry attempts before marking as failed</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Last error message if failed</summary>
    public string? LastError { get; set; }

    /// <summary>When last sync attempt was made</summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>When sync completed successfully</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Priority (lower = higher priority)</summary>
    public int Priority { get; set; } = 100;
}

/// <summary>
/// Service for managing the sync queue.
/// 同步佇列管理服務
/// </summary>
public interface ISyncQueueService
{
    /// <summary>Queue an entity for sync</summary>
    Task EnqueueAsync<T>(T entity, SyncOperation operation) where T : class, IEntity;

    /// <summary>Get pending items to sync</summary>
    Task<IReadOnlyList<SyncQueueItem>> GetPendingItemsAsync(int batchSize = 50);

    /// <summary>Mark item as completed</summary>
    Task MarkCompletedAsync(long queueItemId, string? serverId = null);

    /// <summary>Mark item as failed</summary>
    Task MarkFailedAsync(long queueItemId, string error);

    /// <summary>Mark item as conflict</summary>
    Task MarkConflictAsync(long queueItemId, string serverData);

    /// <summary>Get pending count</summary>
    Task<int> GetPendingCountAsync();

    /// <summary>Clear completed items older than specified date</summary>
    Task CleanupCompletedAsync(DateTime olderThan);
}

public class SyncQueueService : ISyncQueueService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SyncQueueService> _logger;

    public async Task EnqueueAsync<T>(T entity, SyncOperation operation) where T : class, IEntity
    {
        var queueItem = new SyncQueueItem
        {
            EntityType = typeof(T).Name,
            LocalId = entity.Id.ToString(),
            Operation = operation,
            SerializedData = JsonSerializer.Serialize(entity),
            QueuedAt = DateTime.UtcNow
        };

        _db.SyncQueue.Add(queueItem);
        await _db.SaveChangesAsync();

        _logger.LogDebug("Queued {Operation} for {EntityType} #{Id}",
            operation, typeof(T).Name, entity.Id);
    }

    public async Task<IReadOnlyList<SyncQueueItem>> GetPendingItemsAsync(int batchSize = 50)
    {
        return await _db.SyncQueue
            .Where(x => x.Status == SyncStatus.Pending || x.Status == SyncStatus.Failed)
            .Where(x => x.RetryCount < x.MaxRetries)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.QueuedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    // ... other implementations
}
```

### Background Sync Service

```csharp
// ============================================
// BACKGROUND SYNC SERVICE (背景同步服務)
// ============================================

/// <summary>
/// Background service that syncs local changes to server when online.
/// This is OPTIONAL - the app works fully without it.
/// 背景同步服務 - 可選功能，應用程式可完全離線運作
/// </summary>
public class BackgroundSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INetworkMonitor _networkMonitor;
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly SyncSettings _settings;

    // Sync interval (default: every 5 minutes when online)
    private TimeSpan SyncInterval => TimeSpan.FromMinutes(_settings.SyncIntervalMinutes);

    public BackgroundSyncService(
        IServiceScopeFactory scopeFactory,
        INetworkMonitor networkMonitor,
        IOptions<SyncSettings> settings,
        ILogger<BackgroundSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _networkMonitor = networkMonitor;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Don't start if sync is disabled
        if (!_settings.IsEnabled)
        {
            _logger.LogInformation("Background sync is disabled");
            return;
        }

        _logger.LogInformation("Background sync service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Only sync when online
                if (await _networkMonitor.IsOnlineAsync())
                {
                    await SyncPendingChangesAsync(stoppingToken);
                }

                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background sync");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Background sync service stopped");
    }

    private async Task SyncPendingChangesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var syncQueue = scope.ServiceProvider.GetRequiredService<ISyncQueueService>();
        var apiClient = scope.ServiceProvider.GetService<IApiClient>();  // May be null if no server configured

        if (apiClient == null)
        {
            _logger.LogDebug("No API client configured, skipping sync");
            return;
        }

        var pendingItems = await syncQueue.GetPendingItemsAsync();

        if (pendingItems.Count == 0)
        {
            _logger.LogDebug("No pending items to sync");
            return;
        }

        _logger.LogInformation("Syncing {Count} pending items", pendingItems.Count);

        foreach (var item in pendingItems)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await SyncItemAsync(item, apiClient, syncQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync item {Id}", item.Id);
                await syncQueue.MarkFailedAsync(item.Id, ex.Message);
            }
        }
    }

    private async Task SyncItemAsync(
        SyncQueueItem item,
        IApiClient api,
        ISyncQueueService queue)
    {
        // Implementation varies by entity type and operation
        // This is just the sync mechanism - doesn't affect local data
    }
}

/// <summary>
/// Sync settings - can be configured to disable server sync entirely.
/// 同步設定 - 可配置為完全禁用伺服器同步
/// </summary>
public class SyncSettings
{
    /// <summary>Enable/disable background sync</summary>
    public bool IsEnabled { get; set; } = false;  // Disabled by default

    /// <summary>Server API base URL (optional)</summary>
    public string? ServerUrl { get; set; }

    /// <summary>Sync interval in minutes</summary>
    public int SyncIntervalMinutes { get; set; } = 5;

    /// <summary>Maximum items per sync batch</summary>
    public int BatchSize { get; set; } = 50;
}
```

### Conflict Resolution

```csharp
// ============================================
// CONFLICT RESOLUTION (衝突解決)
// ============================================

public enum ConflictResolutionStrategy
{
    /// <summary>Local changes always win</summary>
    LocalWins,

    /// <summary>Server changes always win</summary>
    ServerWins,

    /// <summary>Most recent change wins (by timestamp)</summary>
    LastWriteWins,

    /// <summary>Prompt user to resolve manually</summary>
    Manual
}

public class ConflictRecord
{
    public long Id { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public required string LocalData { get; set; }
    public required string ServerData { get; set; }
    public DateTime LocalModifiedAt { get; set; }
    public DateTime ServerModifiedAt { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public interface IConflictResolver
{
    Task<ConflictResolution> ResolveAsync(ConflictRecord conflict);
}

public class DefaultConflictResolver : IConflictResolver
{
    private readonly ConflictResolutionStrategy _defaultStrategy;

    public DefaultConflictResolver(IOptions<SyncSettings> settings)
    {
        _defaultStrategy = settings.Value.ConflictStrategy;
    }

    public Task<ConflictResolution> ResolveAsync(ConflictRecord conflict)
    {
        return _defaultStrategy switch
        {
            ConflictResolutionStrategy.LocalWins =>
                Task.FromResult(new ConflictResolution(conflict.LocalData, "Local wins")),

            ConflictResolutionStrategy.ServerWins =>
                Task.FromResult(new ConflictResolution(conflict.ServerData, "Server wins")),

            ConflictResolutionStrategy.LastWriteWins =>
                Task.FromResult(conflict.LocalModifiedAt > conflict.ServerModifiedAt
                    ? new ConflictResolution(conflict.LocalData, "Last write wins (local)")
                    : new ConflictResolution(conflict.ServerData, "Last write wins (server)")),

            ConflictResolutionStrategy.Manual =>
                Task.FromResult(new ConflictResolution(null, "Manual resolution required")),

            _ => Task.FromResult(new ConflictResolution(conflict.LocalData, "Default: local wins"))
        };
    }
}
```

### Local-First Repository Pattern

```csharp
// ============================================
// LOCAL-FIRST GENERIC REPOSITORY
// ============================================

/// <summary>
/// Generic repository that always operates on local database first.
/// Server sync happens asynchronously in background.
/// 本地優先通用儲存庫 - 所有操作優先在本地執行
/// </summary>
public class LocalFirstRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ISyncable
    where TKey : notnull
{
    protected readonly AppDbContext _db;
    protected readonly ISyncQueueService _syncQueue;
    protected readonly ILogger _logger;

    public LocalFirstRepository(
        AppDbContext db,
        ISyncQueueService syncQueue,
        ILogger<LocalFirstRepository<TEntity, TKey>> logger)
    {
        _db = db;
        _syncQueue = syncQueue;
        _logger = logger;
    }

    /// <summary>
    /// Get entity by ID - always from local database.
    /// 透過 ID 取得實體 - 永遠從本地資料庫
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _db.Set<TEntity>().FindAsync(id);
    }

    /// <summary>
    /// Get all entities with optional filtering - always from local database.
    /// 取得所有實體 - 永遠從本地資料庫
    /// </summary>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _db.Set<TEntity>().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Get paged results - always from local database.
    /// 取得分頁結果 - 永遠從本地資料庫
    /// </summary>
    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _db.Set<TEntity>().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TEntity>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Create entity - saves to local database immediately, queues for server sync.
    /// 建立實體 - 立即儲存到本地，排隊同步到伺服器
    /// </summary>
    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        // Set sync tracking fields
        entity.IsPendingSync = true;
        entity.LastSyncAt = null;

        // Save to local database
        _db.Set<TEntity>().Add(entity);
        await _db.SaveChangesAsync();

        // Queue for background sync (fire-and-forget)
        _ = _syncQueue.EnqueueAsync(entity, SyncOperation.Create);

        _logger.LogDebug("Created {EntityType} #{Id} locally",
            typeof(TEntity).Name, entity.Id);

        return entity;
    }

    /// <summary>
    /// Update entity - saves to local database immediately, queues for server sync.
    /// 更新實體 - 立即儲存到本地，排隊同步到伺服器
    /// </summary>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        // Set sync tracking fields
        entity.IsPendingSync = true;
        if (entity is IAuditableEntity auditable)
        {
            auditable.ModifiedAt = DateTime.UtcNow;
        }

        // Save to local database
        _db.Set<TEntity>().Update(entity);
        await _db.SaveChangesAsync();

        // Queue for background sync
        _ = _syncQueue.EnqueueAsync(entity, SyncOperation.Update);

        _logger.LogDebug("Updated {EntityType} #{Id} locally",
            typeof(TEntity).Name, entity.Id);

        return entity;
    }

    /// <summary>
    /// Delete entity - soft delete locally, queues for server sync.
    /// 刪除實體 - 本地軟刪除，排隊同步到伺服器
    /// </summary>
    public virtual async Task DeleteAsync(TKey id)
    {
        var entity = await _db.Set<TEntity>().FindAsync(id);
        if (entity == null) return;

        // Soft delete
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAt = DateTime.UtcNow;
        }

        entity.IsPendingSync = true;
        await _db.SaveChangesAsync();

        // Queue for background sync
        _ = _syncQueue.EnqueueAsync(entity, SyncOperation.Delete);

        _logger.LogDebug("Deleted {EntityType} #{Id} locally",
            typeof(TEntity).Name, id);
    }

    /// <summary>
    /// Check if entity exists - always from local database.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        return await _db.Set<TEntity>().AnyAsync(e => e.Id.Equals(id));
    }
}
```

### UI Status Indicator

```csharp
// ============================================
// SYNC STATUS FOR UI (同步狀態 UI 顯示)
// ============================================

public class SyncStatusViewModel : ViewModelBase
{
    private readonly ISyncQueueService _syncQueue;
    private readonly INetworkMonitor _networkMonitor;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private DateTime? _lastSyncAt;

    public string StatusText => (IsOnline, PendingCount, IsSyncing) switch
    {
        (false, _, _) => "Offline - Changes saved locally",
        (true, 0, false) => "All changes synced",
        (true, _, true) => $"Syncing {PendingCount} changes...",
        (true, var n, false) => $"{n} changes pending sync"
    };

    public string StatusIcon => IsOnline ? "\uE701" : "\uE8CD";  // Connected / Disconnected

    public SyncStatusViewModel(ISyncQueueService syncQueue, INetworkMonitor networkMonitor)
    {
        _syncQueue = syncQueue;
        _networkMonitor = networkMonitor;

        // Monitor network status
        _networkMonitor.ConnectivityChanged += (_, online) =>
        {
            IsOnline = online;
            OnPropertyChanged(nameof(StatusText));
        };

        // Refresh status periodically
        _ = RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        while (true)
        {
            PendingCount = await _syncQueue.GetPendingCountAsync();
            IsOnline = await _networkMonitor.IsOnlineAsync();
            OnPropertyChanged(nameof(StatusText));

            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }
}
```

---

## Plugin System Architecture (插件系統架構)

### 1. Plugin Shared Context (插件共享上下文)

All plugins share a common context that provides access to application services, shared state, and inter-plugin communication.

```csharp
// ============================================
// PLUGIN CONTEXT (插件上下文)
// ============================================

/// <summary>
/// Shared context available to all plugins.
/// 所有插件可用的共享上下文
/// </summary>
public interface IPluginContext
{
    // ========== Application Info ==========
    /// <summary>Application version</summary>
    Version AppVersion { get; }

    /// <summary>Current user info</summary>
    ICurrentUser CurrentUser { get; }

    /// <summary>Application settings</summary>
    ISettingsService Settings { get; }

    // ========== Core Services ==========
    /// <summary>Service provider for DI</summary>
    IServiceProvider Services { get; }

    /// <summary>Local database context factory</summary>
    IDbContextFactory<AppDbContext> DbFactory { get; }

    /// <summary>Logger factory</summary>
    ILoggerFactory LoggerFactory { get; }

    // ========== UI Services ==========
    /// <summary>Navigation service</summary>
    INavigationService Navigation { get; }

    /// <summary>Dialog service</summary>
    IDialogService Dialogs { get; }

    /// <summary>Notification service</summary>
    INotificationService Notifications { get; }

    /// <summary>Theme service</summary>
    IThemeService Theme { get; }

    // ========== Plugin Communication ==========
    /// <summary>Message bus for plugin-to-plugin messaging</summary>
    IMessageBus MessageBus { get; }

    /// <summary>Event aggregator for application events</summary>
    IEventAggregator Events { get; }

    /// <summary>Shared state store</summary>
    ISharedStateStore SharedState { get; }

    // ========== Plugin Management ==========
    /// <summary>Access to other plugins</summary>
    IPluginRegistry PluginRegistry { get; }

    /// <summary>Contribution registry for extension points</summary>
    IContributionRegistry Contributions { get; }

    // ========== MDI & Documents ==========
    /// <summary>MDI document service</summary>
    IMdiService Mdi { get; }

    /// <summary>Function tree service</summary>
    IFunctionTreeService FunctionTree { get; }
}

/// <summary>
/// Implementation of plugin context.
/// </summary>
public class PluginContext : IPluginContext
{
    private readonly IServiceProvider _services;

    public PluginContext(IServiceProvider services)
    {
        _services = services;
    }

    public Version AppVersion => typeof(App).Assembly.GetName().Version!;
    public ICurrentUser CurrentUser => _services.GetRequiredService<ICurrentUser>();
    public ISettingsService Settings => _services.GetRequiredService<ISettingsService>();
    public IServiceProvider Services => _services;
    public IDbContextFactory<AppDbContext> DbFactory => _services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    public ILoggerFactory LoggerFactory => _services.GetRequiredService<ILoggerFactory>();
    public INavigationService Navigation => _services.GetRequiredService<INavigationService>();
    public IDialogService Dialogs => _services.GetRequiredService<IDialogService>();
    public INotificationService Notifications => _services.GetRequiredService<INotificationService>();
    public IThemeService Theme => _services.GetRequiredService<IThemeService>();
    public IMessageBus MessageBus => _services.GetRequiredService<IMessageBus>();
    public IEventAggregator Events => _services.GetRequiredService<IEventAggregator>();
    public ISharedStateStore SharedState => _services.GetRequiredService<ISharedStateStore>();
    public IPluginRegistry PluginRegistry => _services.GetRequiredService<IPluginRegistry>();
    public IContributionRegistry Contributions => _services.GetRequiredService<IContributionRegistry>();
    public IMdiService Mdi => _services.GetRequiredService<IMdiService>();
    public IFunctionTreeService FunctionTree => _services.GetRequiredService<IFunctionTreeService>();
}
```

### 2. Plugin-to-Plugin Communication (插件間通訊)

```csharp
// ============================================
// MESSAGE BUS (訊息匯流排)
// Plugin-to-plugin direct messaging
// ============================================

/// <summary>
/// Message bus for direct plugin-to-plugin communication.
/// 插件間直接通訊的訊息匯流排
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Send a message to a specific plugin.
    /// 發送訊息給特定插件
    /// </summary>
    Task<TResponse?> SendAsync<TRequest, TResponse>(
        string targetPluginId,
        TRequest message,
        CancellationToken ct = default);

    /// <summary>
    /// Send a message without expecting response.
    /// 發送訊息，不期待回應
    /// </summary>
    Task SendAsync<TMessage>(string targetPluginId, TMessage message);

    /// <summary>
    /// Register a message handler.
    /// 註冊訊息處理器
    /// </summary>
    void RegisterHandler<TRequest, TResponse>(
        string pluginId,
        Func<TRequest, CancellationToken, Task<TResponse>> handler);

    /// <summary>
    /// Broadcast a message to all plugins.
    /// 廣播訊息給所有插件
    /// </summary>
    Task BroadcastAsync<TMessage>(TMessage message);

    /// <summary>
    /// Subscribe to broadcast messages.
    /// 訂閱廣播訊息
    /// </summary>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler);
}

public class MessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, Delegate>> _handlers = new();
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<MessageBus> _logger;

    public async Task<TResponse?> SendAsync<TRequest, TResponse>(
        string targetPluginId,
        TRequest message,
        CancellationToken ct = default)
    {
        if (_handlers.TryGetValue(targetPluginId, out var pluginHandlers) &&
            pluginHandlers.TryGetValue(typeof(TRequest), out var handler))
        {
            var typedHandler = (Func<TRequest, CancellationToken, Task<TResponse>>)handler;
            return await typedHandler(message, ct);
        }

        _logger.LogWarning("No handler found for {MessageType} in plugin {PluginId}",
            typeof(TRequest).Name, targetPluginId);
        return default;
    }

    public async Task BroadcastAsync<TMessage>(TMessage message)
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    ((Action<TMessage>)handler)(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting {MessageType}", typeof(TMessage).Name);
                }
            }
        }
    }

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
    {
        var handlers = _subscribers.GetOrAdd(typeof(TMessage), _ => new List<Delegate>());
        handlers.Add(handler);

        return new Subscription(() => handlers.Remove(handler));
    }

    // ... other implementations
}

// ============================================
// EVENT AGGREGATOR (事件聚合器)
// Application-wide events
// ============================================

/// <summary>
/// Event aggregator for application-wide events.
/// 應用程式範圍的事件聚合器
/// </summary>
public interface IEventAggregator
{
    /// <summary>Publish an event</summary>
    void Publish<TEvent>(TEvent eventData) where TEvent : IApplicationEvent;

    /// <summary>Subscribe to an event type</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent;

    /// <summary>Subscribe with async handler</summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IApplicationEvent;
}

/// <summary>
/// Base interface for all application events.
/// </summary>
public interface IApplicationEvent
{
    DateTime Timestamp { get; }
    string SourcePluginId { get; }
}

// ========== Common Application Events ==========

public record EntityCreatedEvent<T>(T Entity, string SourcePluginId) : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record EntityUpdatedEvent<T>(T Entity, string SourcePluginId) : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record EntityDeletedEvent<T>(object EntityId, string SourcePluginId) : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record NavigationRequestedEvent(string TargetView, object? Parameter, string SourcePluginId) : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record UserLoggedInEvent(ICurrentUser User, string SourcePluginId) : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record ThemeChangedEvent(string ThemeId, string SourcePluginId) : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
```

### 3. Shared State Store (共享狀態存儲)

```csharp
// ============================================
// SHARED STATE STORE (共享狀態存儲)
// Cross-plugin shared state
// ============================================

/// <summary>
/// Shared state store for cross-plugin state sharing.
/// 跨插件共享狀態存儲
/// </summary>
public interface ISharedStateStore
{
    /// <summary>Get state value</summary>
    T? Get<T>(string key);

    /// <summary>Set state value</summary>
    void Set<T>(string key, T value);

    /// <summary>Remove state value</summary>
    void Remove(string key);

    /// <summary>Check if key exists</summary>
    bool ContainsKey(string key);

    /// <summary>Subscribe to state changes</summary>
    IDisposable Subscribe<T>(string key, Action<T?> onChange);

    /// <summary>Get or create state value</summary>
    T GetOrCreate<T>(string key, Func<T> factory);
}

public class SharedStateStore : ISharedStateStore
{
    private readonly ConcurrentDictionary<string, object?> _state = new();
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers = new();

    public T? Get<T>(string key)
    {
        return _state.TryGetValue(key, out var value) ? (T?)value : default;
    }

    public void Set<T>(string key, T value)
    {
        _state[key] = value;
        NotifySubscribers(key, value);
    }

    public IDisposable Subscribe<T>(string key, Action<T?> onChange)
    {
        var handlers = _subscribers.GetOrAdd(key, _ => new List<Delegate>());
        handlers.Add(onChange);

        // Immediately notify with current value
        if (_state.TryGetValue(key, out var currentValue))
        {
            onChange((T?)currentValue);
        }

        return new Subscription(() => handlers.Remove(onChange));
    }

    private void NotifySubscribers<T>(string key, T? value)
    {
        if (_subscribers.TryGetValue(key, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    ((Action<T?>)handler)(value);
                }
                catch { /* Log error */ }
            }
        }
    }

    // ... other implementations
}

// ========== Common Shared State Keys ==========

public static class SharedStateKeys
{
    public const string CurrentCustomer = "current.customer";
    public const string CurrentOrder = "current.order";
    public const string SelectedItems = "selected.items";
    public const string FilterCriteria = "filter.criteria";
    public const string ClipboardData = "clipboard.data";
    public const string DragDropData = "dragdrop.data";
}
```

### 4. Plugin Registry & Discovery (插件註冊與發現)

```csharp
// ============================================
// PLUGIN REGISTRY (插件註冊表)
// ============================================

/// <summary>
/// Registry for discovering and accessing plugins.
/// 用於發現和存取插件的註冊表
/// </summary>
public interface IPluginRegistry
{
    /// <summary>Get all loaded plugins</summary>
    IReadOnlyList<IPlugin> GetAllPlugins();

    /// <summary>Get plugin by ID</summary>
    IPlugin? GetPlugin(string pluginId);

    /// <summary>Get plugins of specific type</summary>
    IReadOnlyList<T> GetPlugins<T>() where T : class, IPlugin;

    /// <summary>Check if plugin is loaded</summary>
    bool IsLoaded(string pluginId);

    /// <summary>Check if plugin is enabled</summary>
    bool IsEnabled(string pluginId);

    /// <summary>Get plugin service</summary>
    T? GetPluginService<T>(string pluginId) where T : class;

    /// <summary>Query plugins that provide a capability</summary>
    IReadOnlyList<IPlugin> GetPluginsWithCapability(string capability);
}

/// <summary>
/// Plugin capabilities for discovery.
/// </summary>
public static class PluginCapabilities
{
    public const string ExportData = "export.data";
    public const string ImportData = "import.data";
    public const string PrintDocument = "print.document";
    public const string SendNotification = "send.notification";
    public const string ProcessWorkflow = "process.workflow";
    public const string GenerateReport = "generate.report";
    public const string EditCustomer = "edit.customer";
    public const string EditOrder = "edit.order";
}
```

### 5. Contribution Points (貢獻點)

Plugins contribute functionality through well-defined contribution points:

```csharp
// ============================================
// CONTRIBUTION REGISTRY (貢獻註冊表)
// ============================================

/// <summary>
/// Registry for plugin contributions to extension points.
/// 插件貢獻到擴展點的註冊表
/// </summary>
public interface IContributionRegistry
{
    // ========== Menu Contributions ==========
    void RegisterMenuItem(MenuItemContribution item);
    void RegisterContextMenuItem(string contextId, MenuItemContribution item);
    void RegisterToolbarItem(ToolbarItemContribution item);
    IReadOnlyList<MenuItemContribution> GetMenuItems(string menuId);
    IReadOnlyList<MenuItemContribution> GetContextMenuItems(string contextId);
    IReadOnlyList<ToolbarItemContribution> GetToolbarItems(string toolbarId);

    // ========== Function Tree Contributions ==========
    void RegisterFunctionNode(FunctionNodeContribution node);
    IReadOnlyList<FunctionNodeContribution> GetFunctionNodes(string? parentId = null);

    // ========== View Contributions ==========
    void RegisterView(ViewContribution view);
    ViewContribution? GetView(string viewId);
    IReadOnlyList<ViewContribution> GetViewsByCategory(string category);

    // ========== Entity Extensions ==========
    void RegisterEntityExtension(EntityExtensionContribution extension);
    IReadOnlyList<EntityExtensionContribution> GetEntityExtensions(string entityType);

    // ========== Export/Import Handlers ==========
    void RegisterExportHandler(ExportHandlerContribution handler);
    void RegisterImportHandler(ImportHandlerContribution handler);
    IReadOnlyList<ExportHandlerContribution> GetExportHandlers(string entityType);
    IReadOnlyList<ImportHandlerContribution> GetImportHandlers(string entityType);

    // ========== Settings Pages ==========
    void RegisterSettingsPage(SettingsPageContribution page);
    IReadOnlyList<SettingsPageContribution> GetSettingsPages();

    // ========== Dashboard Widgets ==========
    void RegisterDashboardWidget(DashboardWidgetContribution widget);
    IReadOnlyList<DashboardWidgetContribution> GetDashboardWidgets();
}

// ========== Contribution Types ==========

public record MenuItemContribution(
    string PluginId,
    string Id,
    string Label,
    string? Icon,
    string? ParentMenuId,
    int Order,
    ICommand? Command,
    string? CommandParameter,
    Func<bool>? CanExecute,
    IReadOnlyList<MenuItemContribution>? SubItems = null
);

public record FunctionNodeContribution(
    string PluginId,
    string Id,
    string Label,
    string? Icon,
    string? ParentId,
    int Order,
    string? ViewId,
    string? ViewParameter,
    string? RequiredPermission,
    IReadOnlyList<string>? Tags = null
);

public record ViewContribution(
    string PluginId,
    string Id,
    string Title,
    Type ViewType,
    Type ViewModelType,
    string? Category,
    string? Icon,
    bool SupportsMultipleInstances = false
);

public record EntityExtensionContribution(
    string PluginId,
    string EntityType,
    Type ExtensionType,
    string? TabLabel,
    int TabOrder
);

public record ExportHandlerContribution(
    string PluginId,
    string EntityType,
    string Format,
    string Label,
    string? Icon,
    Type HandlerType
);
```

### 6. Plugin Base Class & Implementation Example

```csharp
// ============================================
// PLUGIN BASE CLASS (插件基類)
// ============================================

/// <summary>
/// Base class for all plugins.
/// 所有插件的基類
/// </summary>
public abstract class PluginBase : IPlugin
{
    protected IPluginContext Context { get; private set; } = null!;
    protected ILogger Logger { get; private set; } = null!;

    public abstract PluginMetadata Metadata { get; }

    /// <summary>
    /// Called when plugin is being initialized.
    /// </summary>
    public virtual Task InitializeAsync(IPluginContext context)
    {
        Context = context;
        Logger = context.LoggerFactory.CreateLogger(GetType());

        Logger.LogInformation("Initializing plugin: {PluginId}", Metadata.Id);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to register contributions.
    /// Override this to register menus, views, etc.
    /// </summary>
    public virtual void RegisterContributions(IContributionRegistry registry)
    {
        // Override in derived class
    }

    /// <summary>
    /// Called to register message handlers.
    /// </summary>
    public virtual void RegisterMessageHandlers(IMessageBus messageBus)
    {
        // Override in derived class
    }

    /// <summary>
    /// Called to subscribe to events.
    /// </summary>
    public virtual void SubscribeToEvents(IEventAggregator events)
    {
        // Override in derived class
    }

    /// <summary>
    /// Called when plugin is being activated (enabled).
    /// </summary>
    public virtual Task ActivateAsync()
    {
        Logger.LogInformation("Activating plugin: {PluginId}", Metadata.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when plugin is being deactivated (disabled).
    /// </summary>
    public virtual Task DeactivateAsync()
    {
        Logger.LogInformation("Deactivating plugin: {PluginId}", Metadata.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when plugin is being unloaded.
    /// </summary>
    public virtual Task ShutdownAsync()
    {
        Logger.LogInformation("Shutting down plugin: {PluginId}", Metadata.Id);
        return Task.CompletedTask;
    }
}

// ============================================
// EXAMPLE: CUSTOMER MODULE PLUGIN
// ============================================

public class CustomerModulePlugin : PluginBase
{
    public override PluginMetadata Metadata => new(
        Id: "arcana.module.customer",
        Name: "Customer Module",
        Version: "1.0.0",
        Author: "Arcana Team",
        Description: "Customer management module with CRUD operations",
        Dependencies: new[] { "arcana.core.menu", "arcana.core.mdi" },
        Capabilities: new[] {
            PluginCapabilities.EditCustomer,
            PluginCapabilities.ExportData,
            PluginCapabilities.PrintDocument
        }
    );

    public override Task InitializeAsync(IPluginContext context)
    {
        base.InitializeAsync(context);

        // Register entity with local database
        // The entity is defined in this plugin
        return Task.CompletedTask;
    }

    public override void RegisterContributions(IContributionRegistry registry)
    {
        // Register function tree nodes
        registry.RegisterFunctionNode(new FunctionNodeContribution(
            PluginId: Metadata.Id,
            Id: "customer.root",
            Label: "Customers",
            Icon: "\uE77B",
            ParentId: null,
            Order: 100,
            ViewId: null,
            ViewParameter: null,
            RequiredPermission: "customer.view"
        ));

        registry.RegisterFunctionNode(new FunctionNodeContribution(
            PluginId: Metadata.Id,
            Id: "customer.list",
            Label: "Customer List",
            Icon: "\uE8FD",
            ParentId: "customer.root",
            Order: 1,
            ViewId: "customer.list.view",
            ViewParameter: null,
            RequiredPermission: "customer.view"
        ));

        // Register views
        registry.RegisterView(new ViewContribution(
            PluginId: Metadata.Id,
            Id: "customer.list.view",
            Title: "Customers",
            ViewType: typeof(CustomerListView),
            ViewModelType: typeof(CustomerListViewModel),
            Category: "Master Data",
            Icon: "\uE77B",
            SupportsMultipleInstances: false
        ));

        registry.RegisterView(new ViewContribution(
            PluginId: Metadata.Id,
            Id: "customer.detail.view",
            Title: "Customer Detail",
            ViewType: typeof(CustomerDetailView),
            ViewModelType: typeof(CustomerDetailViewModel),
            Category: "Master Data",
            Icon: "\uE77B",
            SupportsMultipleInstances: true
        ));

        // Register context menu items
        registry.RegisterContextMenuItem("customer.list", new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "customer.export.excel",
            Label: "Export to Excel",
            Icon: "\uE9F9",
            ParentMenuId: null,
            Order: 100,
            Command: null,
            CommandParameter: "excel",
            CanExecute: () => true
        ));

        // Register export handlers
        registry.RegisterExportHandler(new ExportHandlerContribution(
            PluginId: Metadata.Id,
            EntityType: "Customer",
            Format: "xlsx",
            Label: "Excel Workbook",
            Icon: "\uE9F9",
            HandlerType: typeof(CustomerExcelExportHandler)
        ));
    }

    public override void RegisterMessageHandlers(IMessageBus messageBus)
    {
        // Handle requests from other plugins
        messageBus.RegisterHandler<GetCustomerRequest, CustomerDto>(
            Metadata.Id,
            async (request, ct) =>
            {
                using var db = Context.DbFactory.CreateDbContext();
                var customer = await db.Customers.FindAsync(request.CustomerId);
                return customer?.ToDto();
            }
        );

        messageBus.RegisterHandler<GetCustomerOrdersRequest, IReadOnlyList<OrderSummaryDto>>(
            Metadata.Id,
            async (request, ct) =>
            {
                using var db = Context.DbFactory.CreateDbContext();
                var orders = await db.Orders
                    .Where(o => o.CustomerId == request.CustomerId)
                    .Select(o => o.ToSummaryDto())
                    .ToListAsync(ct);
                return orders;
            }
        );
    }

    public override void SubscribeToEvents(IEventAggregator events)
    {
        // React to order created events
        events.Subscribe<EntityCreatedEvent<Order>>(async evt =>
        {
            // Update customer statistics when order is created
            Logger.LogInformation("Order created for customer, updating stats");
            await UpdateCustomerStatisticsAsync(evt.Entity.CustomerId);
        });
    }

    private async Task UpdateCustomerStatisticsAsync(int customerId)
    {
        // Update customer order count, total spent, etc.
    }
}

// Message types for plugin communication
public record GetCustomerRequest(int CustomerId);
public record GetCustomerOrdersRequest(int CustomerId);
```

### 7. Plugin Communication Example (插件通訊範例)

```csharp
// ============================================
// PLUGIN COMMUNICATION EXAMPLE
// Order Plugin requesting data from Customer Plugin
// ============================================

public class OrderModulePlugin : PluginBase
{
    public override PluginMetadata Metadata => new(
        Id: "arcana.module.order",
        Name: "Order Module",
        Version: "1.0.0",
        Dependencies: new[] { "arcana.module.customer" }  // Depends on Customer plugin
    );

    private async Task CreateOrderAsync(CreateOrderDto dto)
    {
        // 1. Request customer data from Customer plugin
        var customer = await Context.MessageBus.SendAsync<GetCustomerRequest, CustomerDto>(
            targetPluginId: "arcana.module.customer",
            message: new GetCustomerRequest(dto.CustomerId)
        );

        if (customer == null)
        {
            Context.Notifications.ShowError("Customer not found");
            return;
        }

        // 2. Create the order
        using var db = Context.DbFactory.CreateDbContext();
        var order = new Order
        {
            CustomerId = dto.CustomerId,
            CustomerName = customer.Name,  // Denormalized for display
            Items = dto.Items.Select(i => new OrderItem { /* ... */ }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // 3. Publish event for other plugins to react
        Context.Events.Publish(new EntityCreatedEvent<Order>(order, Metadata.Id));

        // 4. Update shared state
        Context.SharedState.Set(SharedStateKeys.CurrentOrder, order);

        // 5. Navigate to order detail
        await Context.Mdi.OpenDocumentAsync("order.detail.view", order.Id);
    }

    // React to customer updates
    public override void SubscribeToEvents(IEventAggregator events)
    {
        events.Subscribe<EntityUpdatedEvent<Customer>>(async evt =>
        {
            // Update denormalized customer name in orders
            using var db = Context.DbFactory.CreateDbContext();
            var orders = await db.Orders
                .Where(o => o.CustomerId == evt.Entity.Id)
                .ToListAsync();

            foreach (var order in orders)
            {
                order.CustomerName = evt.Entity.Name;
            }

            await db.SaveChangesAsync();
        });
    }
}
```

### 8. Menu Plugin Example (菜單插件範例)

```csharp
// ============================================
// CORE MENU PLUGIN
// Provides the main application menu structure
// ============================================

public class CoreMenuPlugin : PluginBase
{
    public override PluginMetadata Metadata => new(
        Id: "arcana.core.menu",
        Name: "Core Menu",
        Version: "1.0.0",
        Description: "Provides main menu structure"
    );

    public override void RegisterContributions(IContributionRegistry registry)
    {
        // File Menu
        registry.RegisterMenuItem(new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "menu.file",
            Label: "_File",
            Icon: null,
            ParentMenuId: null,
            Order: 0,
            Command: null,
            CommandParameter: null,
            CanExecute: null,
            SubItems: new[]
            {
                new MenuItemContribution(Metadata.Id, "menu.file.new", "_New", "\uE710", "menu.file", 0, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.save", "_Save", "\uE74E", "menu.file", 10, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.saveall", "Save _All", null, "menu.file", 11, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.sep1", "-", null, "menu.file", 20, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.print", "_Print", "\uE749", "menu.file", 30, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.export", "_Export", "\uEDE1", "menu.file", 31, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.sep2", "-", null, "menu.file", 40, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.file.exit", "E_xit", null, "menu.file", 100, null, null, null)
            }
        ));

        // Edit Menu
        registry.RegisterMenuItem(new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "menu.edit",
            Label: "_Edit",
            Icon: null,
            ParentMenuId: null,
            Order: 10,
            Command: null,
            CommandParameter: null,
            CanExecute: null,
            SubItems: new[]
            {
                new MenuItemContribution(Metadata.Id, "menu.edit.undo", "_Undo", "\uE7A7", "menu.edit", 0, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.edit.redo", "_Redo", "\uE7A6", "menu.edit", 1, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.edit.sep1", "-", null, "menu.edit", 10, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.edit.cut", "Cu_t", "\uE8C6", "menu.edit", 20, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.edit.copy", "_Copy", "\uE8C8", "menu.edit", 21, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.edit.paste", "_Paste", "\uE77F", "menu.edit", 22, null, null, null)
            }
        ));

        // View Menu
        registry.RegisterMenuItem(new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "menu.view",
            Label: "_View",
            Icon: null,
            ParentMenuId: null,
            Order: 20,
            Command: null,
            CommandParameter: null,
            CanExecute: null
        ));

        // Tools Menu
        registry.RegisterMenuItem(new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "menu.tools",
            Label: "_Tools",
            Icon: null,
            ParentMenuId: null,
            Order: 30,
            Command: null,
            CommandParameter: null,
            CanExecute: null
        ));

        // Window Menu
        registry.RegisterMenuItem(new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "menu.window",
            Label: "_Window",
            Icon: null,
            ParentMenuId: null,
            Order: 40,
            Command: null,
            CommandParameter: null,
            CanExecute: null
        ));

        // Help Menu
        registry.RegisterMenuItem(new MenuItemContribution(
            PluginId: Metadata.Id,
            Id: "menu.help",
            Label: "_Help",
            Icon: null,
            ParentMenuId: null,
            Order: 100,
            Command: null,
            CommandParameter: null,
            CanExecute: null
        ));

        // Main Toolbar
        registry.RegisterToolbarItem(new ToolbarItemContribution(
            PluginId: Metadata.Id,
            Id: "toolbar.save",
            Label: "Save",
            Icon: "\uE74E",
            ToolbarId: "main",
            Order: 0,
            Command: null,
            CanExecute: null
        ));

        registry.RegisterToolbarItem(new ToolbarItemContribution(
            PluginId: Metadata.Id,
            Id: "toolbar.refresh",
            Label: "Refresh",
            Icon: "\uE72C",
            ToolbarId: "main",
            Order: 10,
            Command: null,
            CanExecute: null
        ));
    }
}
```

### 9. Plugin Manifest (plugin.json)

```json
{
  "id": "arcana.module.customer",
  "name": "Customer Module",
  "version": "1.0.0",
  "author": "Arcana Team",
  "description": "Customer management module",
  "main": "Arcana.Module.Customer.dll",
  "pluginClass": "Arcana.Module.Customer.CustomerModulePlugin",

  "dependencies": [
    { "id": "arcana.core.menu", "version": ">=1.0.0" },
    { "id": "arcana.core.mdi", "version": ">=1.0.0" }
  ],

  "capabilities": [
    "edit.customer",
    "export.data",
    "print.document"
  ],

  "contributes": {
    "functionTree": [
      {
        "id": "customer.root",
        "label": "Customers",
        "icon": "\\uE77B",
        "order": 100
      },
      {
        "id": "customer.list",
        "label": "Customer List",
        "parentId": "customer.root",
        "viewId": "customer.list.view"
      }
    ],

    "views": [
      {
        "id": "customer.list.view",
        "title": "Customers",
        "viewType": "CustomerListView",
        "viewModelType": "CustomerListViewModel"
      }
    ],

    "menus": [
      {
        "id": "customer.menu",
        "label": "Customers",
        "parentId": "menu.view",
        "order": 10
      }
    ],

    "settings": [
      {
        "id": "customer.settings",
        "title": "Customer Settings",
        "icon": "\\uE77B"
      }
    ]
  },

  "configuration": {
    "type": "object",
    "properties": {
      "defaultPageSize": {
        "type": "integer",
        "default": 50,
        "description": "Default page size for customer list"
      },
      "enableAutoSave": {
        "type": "boolean",
        "default": true,
        "description": "Auto-save customer changes"
      }
    }
  }
}
```

---

## UI Component Library (UI 元件庫)

The application shell includes a comprehensive set of reusable UI components following WinUI 3 and Fluent Design principles.

### 1. Component Overview (元件概覽)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           UI COMPONENT LIBRARY                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │   Navigation    │  │   Data Display  │  │   Data Entry    │              │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤              │
│  │ • MenuBar       │  │ • DataGrid      │  │ • TextBox       │              │
│  │ • NavigationView│  │ • TreeView      │  │ • ComboBox      │              │
│  │ • TabView (MDI) │  │ • ListView      │  │ • DatePicker    │              │
│  │ • BreadcrumbBar │  │ • MasterDetail  │  │ • NumberBox     │              │
│  │ • CommandBar    │  │ • PropertyGrid  │  │ • AutoComplete  │              │
│  │ • ToolBar       │  │ • CardView      │  │ • RichEditBox   │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │    Dialogs      │  │    Feedback     │  │    Layout       │              │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤              │
│  │ • ContentDialog │  │ • InfoBar       │  │ • SplitView     │              │
│  │ • MessageDialog │  │ • ProgressRing  │  │ • Expander      │              │
│  │ • FilePicker    │  │ • ProgressBar   │  │ • SettingsCard  │              │
│  │ • FolderPicker  │  │ • TeachingTip   │  │ • HeaderedContent│             │
│  │ • PrintDialog   │  │ • Notification  │  │ • ResponsiveGrid│              │
│  │ • InputDialog   │  │ • StatusBar     │  │ • WrapPanel     │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. MenuBar Component (選單列元件)

```xml
<!-- MenuBar Control -->
<UserControl x:Class="Arcana.App.Controls.ArcanaMenuBar">
    <MenuBar>
        <MenuBarItem x:Name="FileMenu" Title="File">
            <MenuFlyoutItem Text="New" Icon="{ui:FontIcon Glyph=&#xE710;}"
                           Command="{x:Bind ViewModel.NewCommand}">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="N" Modifiers="Control"/>
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutItem Text="Open" Icon="{ui:FontIcon Glyph=&#xE8E5;}"
                           Command="{x:Bind ViewModel.OpenCommand}">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="O" Modifiers="Control"/>
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="Save" Icon="{ui:FontIcon Glyph=&#xE74E;}"
                           Command="{x:Bind ViewModel.SaveCommand}">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="S" Modifiers="Control"/>
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutItem Text="Save All"
                           Command="{x:Bind ViewModel.SaveAllCommand}">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="S" Modifiers="Control,Shift"/>
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutSeparator/>
            <MenuFlyoutSubItem Text="Export">
                <MenuFlyoutItem Text="Export to Excel"
                               Command="{x:Bind ViewModel.ExportCommand}"
                               CommandParameter="xlsx"/>
                <MenuFlyoutItem Text="Export to PDF"
                               Command="{x:Bind ViewModel.ExportCommand}"
                               CommandParameter="pdf"/>
                <MenuFlyoutItem Text="Export to CSV"
                               Command="{x:Bind ViewModel.ExportCommand}"
                               CommandParameter="csv"/>
            </MenuFlyoutSubItem>
            <MenuFlyoutItem Text="Print" Icon="{ui:FontIcon Glyph=&#xE749;}"
                           Command="{x:Bind ViewModel.PrintCommand}">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="P" Modifiers="Control"/>
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="Exit" Command="{x:Bind ViewModel.ExitCommand}">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="F4" Modifiers="Menu"/>
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
        </MenuBarItem>

        <MenuBarItem Title="Edit">
            <MenuFlyoutItem Text="Undo" Icon="{ui:FontIcon Glyph=&#xE7A7;}"
                           Command="{x:Bind ViewModel.UndoCommand}"/>
            <MenuFlyoutItem Text="Redo" Icon="{ui:FontIcon Glyph=&#xE7A6;}"
                           Command="{x:Bind ViewModel.RedoCommand}"/>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="Cut" Icon="{ui:FontIcon Glyph=&#xE8C6;}"
                           Command="{x:Bind ViewModel.CutCommand}"/>
            <MenuFlyoutItem Text="Copy" Icon="{ui:FontIcon Glyph=&#xE8C8;}"
                           Command="{x:Bind ViewModel.CopyCommand}"/>
            <MenuFlyoutItem Text="Paste" Icon="{ui:FontIcon Glyph=&#xE77F;}"
                           Command="{x:Bind ViewModel.PasteCommand}"/>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="Select All"
                           Command="{x:Bind ViewModel.SelectAllCommand}"/>
        </MenuBarItem>

        <MenuBarItem Title="View">
            <ToggleMenuFlyoutItem Text="Function Tree"
                                  IsChecked="{x:Bind ViewModel.IsFunctionTreeVisible, Mode=TwoWay}"/>
            <ToggleMenuFlyoutItem Text="Status Bar"
                                  IsChecked="{x:Bind ViewModel.IsStatusBarVisible, Mode=TwoWay}"/>
            <MenuFlyoutSeparator/>
            <MenuFlyoutSubItem Text="Theme">
                <RadioMenuFlyoutItem Text="Light" GroupName="Theme"
                                    Command="{x:Bind ViewModel.SetThemeCommand}"
                                    CommandParameter="Light"/>
                <RadioMenuFlyoutItem Text="Dark" GroupName="Theme"
                                    Command="{x:Bind ViewModel.SetThemeCommand}"
                                    CommandParameter="Dark"/>
                <RadioMenuFlyoutItem Text="System" GroupName="Theme" IsChecked="True"
                                    Command="{x:Bind ViewModel.SetThemeCommand}"
                                    CommandParameter="System"/>
            </MenuFlyoutSubItem>
        </MenuBarItem>

        <!-- Dynamic menu items from plugins -->
        <ItemsRepeater ItemsSource="{x:Bind ViewModel.PluginMenuItems}">
            <ItemsRepeater.ItemTemplate>
                <DataTemplate x:DataType="models:MenuItemContribution">
                    <MenuBarItem Title="{x:Bind Label}">
                        <!-- Sub items populated dynamically -->
                    </MenuBarItem>
                </DataTemplate>
            </ItemsRepeater.ItemTemplate>
        </ItemsRepeater>

        <MenuBarItem Title="Help">
            <MenuFlyoutItem Text="Documentation" Icon="{ui:FontIcon Glyph=&#xE7BE;}"
                           Command="{x:Bind ViewModel.OpenDocsCommand}"/>
            <MenuFlyoutItem Text="Check for Updates"
                           Command="{x:Bind ViewModel.CheckUpdatesCommand}"/>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="About" Command="{x:Bind ViewModel.ShowAboutCommand}"/>
        </MenuBarItem>
    </MenuBar>
</UserControl>
```

### 3. TabView (MDI) Component (分頁元件)

```xml
<!-- MDI TabView Control -->
<UserControl x:Class="Arcana.App.Controls.MdiTabView">
    <TabView
        x:Name="DocumentTabs"
        TabItemsSource="{x:Bind ViewModel.OpenDocuments, Mode=OneWay}"
        SelectedItem="{x:Bind ViewModel.ActiveDocument, Mode=TwoWay}"
        IsAddTabButtonVisible="False"
        TabCloseRequested="OnTabCloseRequested"
        TabDroppedOutside="OnTabDroppedOutside"
        CanDragTabs="True"
        CanReorderTabs="True"
        AllowDropTabs="True">

        <TabView.TabItemTemplate>
            <DataTemplate x:DataType="vm:DocumentViewModel">
                <TabViewItem Header="{x:Bind Title, Mode=OneWay}"
                            IconSource="{x:Bind Icon, Mode=OneWay}"
                            IsClosable="{x:Bind CanClose}">
                    <TabViewItem.HeaderTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="8">
                                <FontIcon Glyph="{x:Bind Icon}" FontSize="14"/>
                                <TextBlock Text="{x:Bind Title}"/>
                                <Ellipse Width="8" Height="8"
                                        Fill="{ThemeResource SystemAccentColor}"
                                        Visibility="{x:Bind IsDirty, Converter={StaticResource BoolToVisibility}}"/>
                            </StackPanel>
                        </DataTemplate>
                    </TabViewItem.HeaderTemplate>

                    <!-- Document content -->
                    <ContentControl Content="{x:Bind View}"
                                   HorizontalContentAlignment="Stretch"
                                   VerticalContentAlignment="Stretch"/>
                </TabViewItem>
            </DataTemplate>
        </TabView.TabItemTemplate>

        <TabView.TabStripHeader>
            <StackPanel Orientation="Horizontal" Padding="8,0">
                <Button Content="&#xE710;" FontFamily="Segoe MDL2 Assets"
                       ToolTipService.ToolTip="New Document"
                       Command="{x:Bind ViewModel.NewDocumentCommand}"/>
            </StackPanel>
        </TabView.TabStripHeader>

        <TabView.TabStripFooter>
            <StackPanel Orientation="Horizontal" Padding="8,0" Spacing="4">
                <TextBlock Text="{x:Bind ViewModel.OpenDocuments.Count, Mode=OneWay}"
                          VerticalAlignment="Center"/>
                <TextBlock Text="documents open" VerticalAlignment="Center"
                          Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
            </StackPanel>
        </TabView.TabStripFooter>
    </TabView>
</UserControl>
```

```csharp
// MDI Service
public interface IMdiService
{
    IReadOnlyObservableCollection<DocumentViewModel> OpenDocuments { get; }
    DocumentViewModel? ActiveDocument { get; set; }

    event EventHandler<DocumentViewModel>? DocumentOpened;
    event EventHandler<DocumentViewModel>? DocumentClosed;
    event EventHandler<DocumentViewModel>? ActiveDocumentChanged;

    Task<DocumentViewModel> OpenDocumentAsync(string viewId, object? parameter = null);
    Task<DocumentViewModel> OpenDocumentAsync<TView, TViewModel>(object? parameter = null);
    Task<bool> CloseDocumentAsync(DocumentViewModel document);
    Task<bool> CloseAllDocumentsAsync();
    Task<bool> SaveDocumentAsync(DocumentViewModel document);
    Task<bool> SaveAllDocumentsAsync();

    bool CanCloseDocument(DocumentViewModel document);
    DocumentViewModel? FindDocument(string viewId, object? key);
}

public class MdiService : IMdiService
{
    private readonly ObservableCollection<DocumentViewModel> _documents = new();
    private readonly IContributionRegistry _contributions;
    private readonly IServiceProvider _services;
    private DocumentViewModel? _activeDocument;

    public IReadOnlyObservableCollection<DocumentViewModel> OpenDocuments =>
        new ReadOnlyObservableCollection<DocumentViewModel>(_documents);

    public DocumentViewModel? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (_activeDocument != value)
            {
                _activeDocument = value;
                ActiveDocumentChanged?.Invoke(this, value!);
            }
        }
    }

    public async Task<DocumentViewModel> OpenDocumentAsync(string viewId, object? parameter = null)
    {
        // Check if document already open (for single-instance views)
        var viewContribution = _contributions.GetView(viewId);
        if (viewContribution != null && !viewContribution.SupportsMultipleInstances)
        {
            var existing = _documents.FirstOrDefault(d => d.ViewId == viewId);
            if (existing != null)
            {
                ActiveDocument = existing;
                return existing;
            }
        }

        // Create new document
        var viewModel = (ViewModelBase)_services.GetRequiredService(viewContribution!.ViewModelType);
        var view = (FrameworkElement)_services.GetRequiredService(viewContribution.ViewType);
        view.DataContext = viewModel;

        var document = new DocumentViewModel
        {
            ViewId = viewId,
            Title = viewContribution.Title,
            Icon = viewContribution.Icon ?? "\uE8A5",
            View = view,
            ViewModel = viewModel,
            Parameter = parameter
        };

        // Initialize the view model
        if (viewModel is INavigationAware navigationAware)
        {
            await navigationAware.OnNavigatedToAsync(parameter);
        }

        _documents.Add(document);
        ActiveDocument = document;
        DocumentOpened?.Invoke(this, document);

        return document;
    }

    public async Task<bool> CloseDocumentAsync(DocumentViewModel document)
    {
        if (document.IsDirty)
        {
            var result = await ShowSaveConfirmationAsync(document);
            if (result == SaveConfirmationResult.Cancel)
                return false;
            if (result == SaveConfirmationResult.Save)
                await SaveDocumentAsync(document);
        }

        _documents.Remove(document);

        if (document.ViewModel is IDisposable disposable)
            disposable.Dispose();

        DocumentClosed?.Invoke(this, document);
        return true;
    }
}
```

### 4. DataGrid Component (資料表格元件)

```xml
<!-- Advanced DataGrid Control -->
<UserControl x:Class="Arcana.App.Controls.ArcanaDataGrid">
    <Grid RowSpacing="8">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- Search Box -->
            <AutoSuggestBox
                x:Name="SearchBox"
                PlaceholderText="Search..."
                QueryIcon="Find"
                Width="300"
                HorizontalAlignment="Left"
                TextChanged="OnSearchTextChanged"
                QuerySubmitted="OnSearchSubmitted"/>

            <!-- Actions -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4">
                <Button ToolTipService.ToolTip="Refresh"
                       Command="{x:Bind ViewModel.RefreshCommand}">
                    <FontIcon Glyph="&#xE72C;" FontSize="14"/>
                </Button>
                <AppBarSeparator/>
                <Button ToolTipService.ToolTip="Add New"
                       Command="{x:Bind ViewModel.AddCommand}">
                    <FontIcon Glyph="&#xE710;" FontSize="14"/>
                </Button>
                <Button ToolTipService.ToolTip="Edit"
                       Command="{x:Bind ViewModel.EditCommand}"
                       IsEnabled="{x:Bind ViewModel.HasSelection, Mode=OneWay}">
                    <FontIcon Glyph="&#xE70F;" FontSize="14"/>
                </Button>
                <Button ToolTipService.ToolTip="Delete"
                       Command="{x:Bind ViewModel.DeleteCommand}"
                       IsEnabled="{x:Bind ViewModel.HasSelection, Mode=OneWay}">
                    <FontIcon Glyph="&#xE74D;" FontSize="14"/>
                </Button>
                <AppBarSeparator/>
                <DropDownButton ToolTipService.ToolTip="Export">
                    <FontIcon Glyph="&#xEDE1;" FontSize="14"/>
                    <DropDownButton.Flyout>
                        <MenuFlyout>
                            <MenuFlyoutItem Text="Export to Excel"
                                           Command="{x:Bind ViewModel.ExportCommand}"
                                           CommandParameter="xlsx"/>
                            <MenuFlyoutItem Text="Export to CSV"
                                           Command="{x:Bind ViewModel.ExportCommand}"
                                           CommandParameter="csv"/>
                            <MenuFlyoutItem Text="Export to PDF"
                                           Command="{x:Bind ViewModel.ExportCommand}"
                                           CommandParameter="pdf"/>
                        </MenuFlyout>
                    </DropDownButton.Flyout>
                </DropDownButton>
            </StackPanel>
        </Grid>

        <!-- DataGrid -->
        <toolkit:DataGrid
            Grid.Row="1"
            x:Name="MainDataGrid"
            ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
            SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}"
            SelectionMode="Extended"
            AutoGenerateColumns="False"
            CanUserReorderColumns="True"
            CanUserResizeColumns="True"
            CanUserSortColumns="True"
            GridLinesVisibility="Horizontal"
            AlternatingRowBackground="{ThemeResource CardBackgroundFillColorSecondaryBrush}"
            IsReadOnly="True"
            DoubleTapped="OnRowDoubleTapped"
            Sorting="OnSorting"
            LoadingRow="OnLoadingRow">

            <!-- Column Definitions -->
            <toolkit:DataGrid.Columns>
                <!-- Selection Column -->
                <toolkit:DataGridCheckBoxColumn
                    Binding="{Binding IsSelected, Mode=TwoWay}"
                    Width="40"/>

                <!-- ID Column -->
                <toolkit:DataGridTextColumn
                    Header="ID"
                    Binding="{Binding Id}"
                    Width="80"
                    Tag="Id"/>

                <!-- Status Column with Badge -->
                <toolkit:DataGridTemplateColumn Header="Status" Width="120" Tag="Status">
                    <toolkit:DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Border Background="{Binding Status, Converter={StaticResource StatusToBrush}}"
                                   CornerRadius="4" Padding="8,4" HorizontalAlignment="Left">
                                <TextBlock Text="{Binding Status}"
                                          Foreground="White" FontSize="12"/>
                            </Border>
                        </DataTemplate>
                    </toolkit:DataGridTemplateColumn.CellTemplate>
                </toolkit:DataGridTemplateColumn>

                <!-- Date Column -->
                <toolkit:DataGridTextColumn
                    Header="Date"
                    Binding="{Binding CreatedAt, Converter={StaticResource DateTimeConverter}}"
                    Width="120"
                    Tag="CreatedAt"/>

                <!-- Amount Column with Currency -->
                <toolkit:DataGridTextColumn
                    Header="Amount"
                    Binding="{Binding TotalAmount, Converter={StaticResource CurrencyConverter}}"
                    Width="120"
                    Tag="TotalAmount">
                    <toolkit:DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="HorizontalAlignment" Value="Right"/>
                            <Setter Property="FontFamily" Value="Consolas"/>
                        </Style>
                    </toolkit:DataGridTextColumn.ElementStyle>
                </toolkit:DataGridTextColumn>

                <!-- Actions Column -->
                <toolkit:DataGridTemplateColumn Header="" Width="100">
                    <toolkit:DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="4">
                                <Button Style="{StaticResource SubtleButtonStyle}"
                                       Command="{Binding DataContext.ViewCommand, ElementName=MainDataGrid}"
                                       CommandParameter="{Binding}">
                                    <FontIcon Glyph="&#xE7B3;" FontSize="12"/>
                                </Button>
                                <Button Style="{StaticResource SubtleButtonStyle}"
                                       Command="{Binding DataContext.EditCommand, ElementName=MainDataGrid}"
                                       CommandParameter="{Binding}">
                                    <FontIcon Glyph="&#xE70F;" FontSize="12"/>
                                </Button>
                            </StackPanel>
                        </DataTemplate>
                    </toolkit:DataGridTemplateColumn.CellTemplate>
                </toolkit:DataGridTemplateColumn>
            </toolkit:DataGrid.Columns>

            <!-- Context Menu -->
            <toolkit:DataGrid.ContextFlyout>
                <MenuFlyout>
                    <MenuFlyoutItem Text="View Details" Icon="{ui:FontIcon Glyph=&#xE7B3;}"
                                   Command="{x:Bind ViewModel.ViewCommand}"/>
                    <MenuFlyoutItem Text="Edit" Icon="{ui:FontIcon Glyph=&#xE70F;}"
                                   Command="{x:Bind ViewModel.EditCommand}"/>
                    <MenuFlyoutSeparator/>
                    <MenuFlyoutItem Text="Duplicate" Icon="{ui:FontIcon Glyph=&#xE8C8;}"
                                   Command="{x:Bind ViewModel.DuplicateCommand}"/>
                    <MenuFlyoutSeparator/>
                    <MenuFlyoutItem Text="Delete" Icon="{ui:FontIcon Glyph=&#xE74D;}"
                                   Command="{x:Bind ViewModel.DeleteCommand}"/>
                </MenuFlyout>
            </toolkit:DataGrid.ContextFlyout>
        </toolkit:DataGrid>

        <!-- Pagination -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- Item Count -->
            <TextBlock VerticalAlignment="Center">
                <Run Text="Showing"/>
                <Run Text="{x:Bind ViewModel.StartIndex, Mode=OneWay}"/>
                <Run Text="-"/>
                <Run Text="{x:Bind ViewModel.EndIndex, Mode=OneWay}"/>
                <Run Text="of"/>
                <Run Text="{x:Bind ViewModel.TotalCount, Mode=OneWay}" FontWeight="SemiBold"/>
                <Run Text="items"/>
            </TextBlock>

            <!-- Page Size -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                <TextBlock Text="Show:" VerticalAlignment="Center"/>
                <ComboBox SelectedItem="{x:Bind ViewModel.PageSize, Mode=TwoWay}" Width="80">
                    <x:Int32>25</x:Int32>
                    <x:Int32>50</x:Int32>
                    <x:Int32>100</x:Int32>
                    <x:Int32>200</x:Int32>
                </ComboBox>
            </StackPanel>

            <!-- Page Navigation -->
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4" Margin="16,0,0,0">
                <Button Content="&#xE892;" FontFamily="Segoe MDL2 Assets"
                       Command="{x:Bind ViewModel.FirstPageCommand}"
                       IsEnabled="{x:Bind ViewModel.CanGoBack, Mode=OneWay}"/>
                <Button Content="&#xE76B;" FontFamily="Segoe MDL2 Assets"
                       Command="{x:Bind ViewModel.PreviousPageCommand}"
                       IsEnabled="{x:Bind ViewModel.CanGoBack, Mode=OneWay}"/>
                <TextBlock VerticalAlignment="Center" Margin="8,0">
                    <Run Text="Page"/>
                    <Run Text="{x:Bind ViewModel.CurrentPage, Mode=OneWay}" FontWeight="SemiBold"/>
                    <Run Text="of"/>
                    <Run Text="{x:Bind ViewModel.TotalPages, Mode=OneWay}"/>
                </TextBlock>
                <Button Content="&#xE76C;" FontFamily="Segoe MDL2 Assets"
                       Command="{x:Bind ViewModel.NextPageCommand}"
                       IsEnabled="{x:Bind ViewModel.CanGoForward, Mode=OneWay}"/>
                <Button Content="&#xE893;" FontFamily="Segoe MDL2 Assets"
                       Command="{x:Bind ViewModel.LastPageCommand}"
                       IsEnabled="{x:Bind ViewModel.CanGoForward, Mode=OneWay}"/>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### 5. Master-Detail Component (主從元件)

```xml
<!-- Master-Detail Layout Control -->
<UserControl x:Class="Arcana.App.Controls.MasterDetailLayout">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="{x:Bind MasterWidth, Mode=OneWay}" MinWidth="200"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*" MinWidth="400"/>
        </Grid.ColumnDefinitions>

        <!-- Master Panel (List) -->
        <Grid x:Name="MasterPanel" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Master Header -->
            <Grid Padding="16,12" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Text="{x:Bind MasterTitle, Mode=OneWay}"
                          Style="{StaticResource SubtitleTextBlockStyle}"/>
                <Button Grid.Column="1" Content="&#xE710;" FontFamily="Segoe MDL2 Assets"
                       Command="{x:Bind AddCommand}"/>
            </Grid>

            <!-- Master List -->
            <ListView Grid.Row="1"
                     ItemsSource="{x:Bind Items, Mode=OneWay}"
                     SelectedItem="{x:Bind SelectedItem, Mode=TwoWay}"
                     SelectionMode="Single">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <Grid Padding="12,8" ColumnSpacing="12">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>

                            <!-- Icon -->
                            <Border Background="{ThemeResource AccentFillColorDefaultBrush}"
                                   CornerRadius="4" Width="40" Height="40">
                                <TextBlock Text="{Binding Initials}"
                                          HorizontalAlignment="Center"
                                          VerticalAlignment="Center"
                                          Foreground="White"/>
                            </Border>

                            <!-- Content -->
                            <StackPanel Grid.Column="1" VerticalAlignment="Center">
                                <TextBlock Text="{Binding Title}"
                                          Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                <TextBlock Text="{Binding Subtitle}"
                                          Style="{StaticResource CaptionTextBlockStyle}"
                                          Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                            </StackPanel>

                            <!-- Status -->
                            <Border Grid.Column="2"
                                   Background="{Binding Status, Converter={StaticResource StatusToBrush}}"
                                   CornerRadius="8" Padding="8,2" VerticalAlignment="Center">
                                <TextBlock Text="{Binding StatusText}" FontSize="10" Foreground="White"/>
                            </Border>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Grid>

        <!-- Splitter -->
        <toolkit:GridSplitter Grid.Column="1" Width="8"
                             ResizeBehavior="BasedOnAlignment"
                             ResizeDirection="Columns"/>

        <!-- Detail Panel -->
        <Grid Grid.Column="2" x:Name="DetailPanel">
            <!-- Detail Content (injected) -->
            <ContentPresenter Content="{x:Bind DetailContent, Mode=OneWay}"/>

            <!-- Empty State -->
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center"
                       Visibility="{x:Bind HasSelection, Mode=OneWay, Converter={StaticResource BoolToVisibilityInverse}}">
                <FontIcon Glyph="&#xE8A5;" FontSize="48"
                         Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                <TextBlock Text="Select an item to view details"
                          Style="{StaticResource BodyTextBlockStyle}"
                          Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                          Margin="0,12,0,0"/>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### 6. Form Controls (表單控制項)

```xml
<!-- Form Field Components -->

<!-- Text Input Field -->
<UserControl x:Class="Arcana.App.Controls.FormTextField">
    <StackPanel Spacing="4">
        <StackPanel Orientation="Horizontal" Spacing="4">
            <TextBlock Text="{x:Bind Label}" Style="{StaticResource BodyStrongTextBlockStyle}"/>
            <TextBlock Text="*" Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                      Visibility="{x:Bind IsRequired, Converter={StaticResource BoolToVisibility}}"/>
        </StackPanel>
        <TextBox Text="{x:Bind Value, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                PlaceholderText="{x:Bind Placeholder}"
                MaxLength="{x:Bind MaxLength}"
                IsReadOnly="{x:Bind IsReadOnly}"/>
        <TextBlock Text="{x:Bind ErrorMessage, Mode=OneWay}"
                  Style="{StaticResource CaptionTextBlockStyle}"
                  Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                  Visibility="{x:Bind HasError, Mode=OneWay, Converter={StaticResource BoolToVisibility}}"/>
        <TextBlock Text="{x:Bind HelpText}"
                  Style="{StaticResource CaptionTextBlockStyle}"
                  Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                  Visibility="{x:Bind HasHelpText, Converter={StaticResource BoolToVisibility}}"/>
    </StackPanel>
</UserControl>

<!-- Lookup/ComboBox Field -->
<UserControl x:Class="Arcana.App.Controls.FormLookupField">
    <StackPanel Spacing="4">
        <StackPanel Orientation="Horizontal" Spacing="4">
            <TextBlock Text="{x:Bind Label}" Style="{StaticResource BodyStrongTextBlockStyle}"/>
            <TextBlock Text="*" Foreground="{ThemeResource SystemFillColorCriticalBrush}"
                      Visibility="{x:Bind IsRequired, Converter={StaticResource BoolToVisibility}}"/>
        </StackPanel>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <ComboBox ItemsSource="{x:Bind Items, Mode=OneWay}"
                     SelectedItem="{x:Bind SelectedItem, Mode=TwoWay}"
                     DisplayMemberPath="{x:Bind DisplayMember}"
                     HorizontalAlignment="Stretch"
                     IsEditable="{x:Bind IsSearchable}"/>
            <Button Grid.Column="1" Content="..." Margin="4,0,0,0"
                   Command="{x:Bind BrowseCommand}"
                   Visibility="{x:Bind ShowBrowseButton, Converter={StaticResource BoolToVisibility}}"/>
        </Grid>
    </StackPanel>
</UserControl>

<!-- Date Field -->
<UserControl x:Class="Arcana.App.Controls.FormDateField">
    <StackPanel Spacing="4">
        <TextBlock Text="{x:Bind Label}" Style="{StaticResource BodyStrongTextBlockStyle}"/>
        <CalendarDatePicker Date="{x:Bind Value, Mode=TwoWay}"
                           PlaceholderText="{x:Bind Placeholder}"
                           MinDate="{x:Bind MinDate}"
                           MaxDate="{x:Bind MaxDate}"
                           DateFormat="{}{year.full}-{month.integer(2)}-{day.integer(2)}"
                           HorizontalAlignment="Stretch"/>
    </StackPanel>
</UserControl>

<!-- Number Field -->
<UserControl x:Class="Arcana.App.Controls.FormNumberField">
    <StackPanel Spacing="4">
        <TextBlock Text="{x:Bind Label}" Style="{StaticResource BodyStrongTextBlockStyle}"/>
        <NumberBox Value="{x:Bind Value, Mode=TwoWay}"
                  Minimum="{x:Bind Minimum}"
                  Maximum="{x:Bind Maximum}"
                  SmallChange="{x:Bind Step}"
                  SpinButtonPlacementMode="Inline"
                  NumberFormatter="{x:Bind Formatter}"
                  HorizontalAlignment="Stretch"/>
    </StackPanel>
</UserControl>

<!-- Currency Field -->
<UserControl x:Class="Arcana.App.Controls.FormCurrencyField">
    <StackPanel Spacing="4">
        <TextBlock Text="{x:Bind Label}" Style="{StaticResource BodyStrongTextBlockStyle}"/>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="{x:Bind CurrencySymbol}"
                      VerticalAlignment="Center" Margin="8,0"/>
            <NumberBox Grid.Column="1"
                      Value="{x:Bind Value, Mode=TwoWay}"
                      Minimum="0"
                      SpinButtonPlacementMode="Inline">
                <NumberBox.NumberFormatter>
                    <DecimalFormatter FractionDigits="2"/>
                </NumberBox.NumberFormatter>
            </NumberBox>
        </Grid>
    </StackPanel>
</UserControl>
```

### 7. Dialog Components (對話框元件)

```xml
<!-- Confirmation Dialog -->
<ContentDialog x:Class="Arcana.App.Dialogs.ConfirmationDialog"
              Title="{x:Bind Title}"
              PrimaryButtonText="Yes"
              SecondaryButtonText="No"
              CloseButtonText="Cancel"
              DefaultButton="Primary">
    <StackPanel Spacing="16">
        <FontIcon Glyph="{x:Bind IconGlyph}" FontSize="48"
                 Foreground="{x:Bind IconBrush}"/>
        <TextBlock Text="{x:Bind Message}" TextWrapping="Wrap"/>
    </StackPanel>
</ContentDialog>

<!-- Input Dialog -->
<ContentDialog x:Class="Arcana.App.Dialogs.InputDialog"
              Title="{x:Bind Title}"
              PrimaryButtonText="OK"
              CloseButtonText="Cancel"
              DefaultButton="Primary"
              IsPrimaryButtonEnabled="{x:Bind IsValid, Mode=OneWay}">
    <StackPanel Spacing="16" MinWidth="400">
        <TextBlock Text="{x:Bind Message}" TextWrapping="Wrap"/>
        <TextBox Text="{x:Bind InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                PlaceholderText="{x:Bind Placeholder}"/>
    </StackPanel>
</ContentDialog>

<!-- Progress Dialog -->
<ContentDialog x:Class="Arcana.App.Dialogs.ProgressDialog"
              Title="{x:Bind Title}"
              CloseButtonText="Cancel"
              Closing="OnClosing">
    <StackPanel Spacing="16" MinWidth="400">
        <ProgressBar Value="{x:Bind Progress, Mode=OneWay}"
                    IsIndeterminate="{x:Bind IsIndeterminate, Mode=OneWay}"/>
        <TextBlock Text="{x:Bind StatusMessage, Mode=OneWay}" TextWrapping="Wrap"/>
    </StackPanel>
</ContentDialog>
```

### 8. StatusBar Component (狀態列元件)

```xml
<!-- Status Bar Control -->
<UserControl x:Class="Arcana.App.Controls.StatusBar">
    <Grid Height="24"
         Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
         BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
         BorderThickness="0,1,0,0"
         Padding="8,0">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Status Message -->
        <TextBlock Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
                  VerticalAlignment="Center"
                  Style="{StaticResource CaptionTextBlockStyle}"/>

        <!-- Sync Status -->
        <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4" Margin="16,0">
            <FontIcon Glyph="{x:Bind ViewModel.SyncIcon, Mode=OneWay}" FontSize="12"/>
            <TextBlock Text="{x:Bind ViewModel.SyncStatus, Mode=OneWay}"
                      Style="{StaticResource CaptionTextBlockStyle}"/>
        </StackPanel>

        <!-- User -->
        <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4" Margin="16,0">
            <FontIcon Glyph="&#xE77B;" FontSize="12"/>
            <TextBlock Text="{x:Bind ViewModel.CurrentUser, Mode=OneWay}"
                      Style="{StaticResource CaptionTextBlockStyle}"/>
        </StackPanel>

        <!-- Connection Status -->
        <Border Grid.Column="3" Margin="16,0" Padding="8,2" CornerRadius="4"
               Background="{x:Bind ViewModel.IsOnline, Mode=OneWay, Converter={StaticResource OnlineToBrush}}">
            <TextBlock Text="{x:Bind ViewModel.ConnectionStatus, Mode=OneWay}"
                      Style="{StaticResource CaptionTextBlockStyle}"
                      Foreground="White"/>
        </Border>

        <!-- Time -->
        <TextBlock Grid.Column="4"
                  Text="{x:Bind ViewModel.CurrentTime, Mode=OneWay}"
                  Style="{StaticResource CaptionTextBlockStyle}"
                  Margin="16,0,0,0"/>
    </Grid>
</UserControl>
```

---

## Complete Order Master-Detail CRUD Example (完整訂單 Master-Detail CRUD 範例)

This section demonstrates a complete Order management module with all UI components.

### 1. Order Entities (訂單實體)

```csharp
// ============================================
// ORDER ENTITIES (訂單實體)
// ============================================

public class Order : EntityBase<int>
{
    public required string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? RequiredDate { get; set; }
    public DateTime? ShippedDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    // Customer (外鍵)
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }  // Denormalized for display
    public string? CustomerCode { get; set; }

    // Shipping Info
    public string? ShipToAddress { get; set; }
    public string? ShipToCity { get; set; }
    public string? ShipToPostalCode { get; set; }
    public string? ShipToCountry { get; set; }

    // Payment
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // Amounts
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; } = 0.05m;
    public decimal TaxAmount => SubTotal * TaxRate;
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount => SubTotal + TaxAmount + ShippingFee - DiscountAmount;

    // Notes
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }

    // Line Items
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem : EntityBase<int>
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int LineNumber { get; set; }

    // Product
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }

    // Quantity & Pricing
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount => Quantity * UnitPrice * (DiscountPercent / 100);
    public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;

    public string? Notes { get; set; }
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    OnHold
}

public enum PaymentMethod
{
    Cash,
    CreditCard,
    BankTransfer,
    Check,
    COD
}

public enum PaymentStatus
{
    Pending,
    Partial,
    Paid,
    Refunded
}
```

### 2. Order Module Plugin (訂單模組插件)

```csharp
// ============================================
// ORDER MODULE PLUGIN (訂單模組插件)
// ============================================

public class OrderModulePlugin : PluginBase
{
    public override PluginMetadata Metadata => new(
        Id: "arcana.module.order",
        Name: "Order Management",
        Version: "1.0.0",
        Author: "Arcana Team",
        Description: "Order management with Master-Detail CRUD",
        Dependencies: new[] { "arcana.core.menu", "arcana.core.mdi", "arcana.module.customer", "arcana.module.product" },
        Capabilities: new[] { "edit.order", "export.data", "print.document" }
    );

    public override void RegisterContributions(IContributionRegistry registry)
    {
        // Function Tree
        registry.RegisterFunctionNode(new(Metadata.Id, "order.root", "Orders", "\uE719", null, 200, null, null, "order.view"));
        registry.RegisterFunctionNode(new(Metadata.Id, "order.list", "Order List", "\uE8FD", "order.root", 1, "order.list.view", null, "order.view"));
        registry.RegisterFunctionNode(new(Metadata.Id, "order.new", "New Order", "\uE710", "order.root", 2, "order.detail.view", "new", "order.create"));
        registry.RegisterFunctionNode(new(Metadata.Id, "order.reports", "Order Reports", "\uE9F9", "order.root", 10, "order.reports.view", null, "order.report"));

        // Views
        registry.RegisterView(new(Metadata.Id, "order.list.view", "Orders", typeof(OrderListView), typeof(OrderListViewModel), "Sales", "\uE719", false));
        registry.RegisterView(new(Metadata.Id, "order.detail.view", "Order", typeof(OrderDetailView), typeof(OrderDetailViewModel), "Sales", "\uE719", true));

        // Menus
        registry.RegisterMenuItem(new(Metadata.Id, "menu.order", "_Orders", null, "menu.view", 20, null, null, null,
            new[] {
                new MenuItemContribution(Metadata.Id, "menu.order.list", "Order List", "\uE8FD", "menu.order", 1, null, null, null),
                new MenuItemContribution(Metadata.Id, "menu.order.new", "New Order", "\uE710", "menu.order", 2, null, null, null)
            }));

        // Export Handlers
        registry.RegisterExportHandler(new(Metadata.Id, "Order", "xlsx", "Excel Workbook", "\uE9F9", typeof(OrderExcelExporter)));
        registry.RegisterExportHandler(new(Metadata.Id, "Order", "pdf", "PDF Document", "\uEA90", typeof(OrderPdfExporter)));
    }
}
```

### 3. Order List View (訂單列表視圖)

```xml
<!-- OrderListView.xaml -->
<Page
    x:Class="Arcana.Module.Order.Views.OrderListView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Arcana.App.Controls"
    xmlns:toolkit="using:CommunityToolkit.WinUI.UI.Controls">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Page Header -->
        <Grid Padding="24,16" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <StackPanel>
                <TextBlock Text="Orders" Style="{StaticResource TitleTextBlockStyle}"/>
                <TextBlock Style="{StaticResource CaptionTextBlockStyle}"
                          Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                    <Run Text="{x:Bind ViewModel.TotalCount, Mode=OneWay}"/>
                    <Run Text="orders found"/>
                </TextBlock>
            </StackPanel>

            <!-- Action Buttons -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                <Button Style="{StaticResource AccentButtonStyle}"
                       Command="{x:Bind ViewModel.NewOrderCommand}">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <FontIcon Glyph="&#xE710;" FontSize="14"/>
                        <TextBlock Text="New Order"/>
                    </StackPanel>
                </Button>
                <Button Command="{x:Bind ViewModel.RefreshCommand}">
                    <FontIcon Glyph="&#xE72C;" FontSize="14"/>
                </Button>
            </StackPanel>
        </Grid>

        <!-- Main Content -->
        <Grid Grid.Row="1" Padding="24,16">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Filters -->
            <Grid Margin="0,0,0,16">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- Search & Filters -->
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <AutoSuggestBox
                        Width="300"
                        PlaceholderText="Search orders..."
                        QueryIcon="Find"
                        Text="{x:Bind ViewModel.SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

                    <ComboBox Header="Status" Width="150"
                             ItemsSource="{x:Bind ViewModel.StatusFilters}"
                             SelectedItem="{x:Bind ViewModel.SelectedStatus, Mode=TwoWay}"/>

                    <CalendarDatePicker Header="From"
                                       Date="{x:Bind ViewModel.DateFrom, Mode=TwoWay}"/>
                    <CalendarDatePicker Header="To"
                                       Date="{x:Bind ViewModel.DateTo, Mode=TwoWay}"/>

                    <Button Content="Apply" Command="{x:Bind ViewModel.ApplyFilterCommand}"/>
                    <Button Content="Clear" Command="{x:Bind ViewModel.ClearFilterCommand}"/>
                </StackPanel>

                <!-- View Options -->
                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4">
                    <RadioButtons Orientation="Horizontal" SelectedIndex="{x:Bind ViewModel.ViewModeIndex, Mode=TwoWay}">
                        <RadioButton ToolTipService.ToolTip="Table View">
                            <FontIcon Glyph="&#xE80A;" FontSize="14"/>
                        </RadioButton>
                        <RadioButton ToolTipService.ToolTip="Card View">
                            <FontIcon Glyph="&#xF0E2;" FontSize="14"/>
                        </RadioButton>
                    </RadioButtons>
                </StackPanel>
            </Grid>

            <!-- Data Grid -->
            <toolkit:DataGrid
                Grid.Row="1"
                ItemsSource="{x:Bind ViewModel.Orders, Mode=OneWay}"
                SelectedItem="{x:Bind ViewModel.SelectedOrder, Mode=TwoWay}"
                SelectionMode="Extended"
                AutoGenerateColumns="False"
                CanUserReorderColumns="True"
                CanUserResizeColumns="True"
                CanUserSortColumns="True"
                GridLinesVisibility="Horizontal"
                AlternatingRowBackground="{ThemeResource CardBackgroundFillColorSecondaryBrush}"
                IsReadOnly="True"
                DoubleTapped="OnOrderDoubleTapped">

                <toolkit:DataGrid.Columns>
                    <!-- Order Number -->
                    <toolkit:DataGridTextColumn
                        Header="Order #"
                        Binding="{Binding OrderNumber}"
                        Width="120"
                        FontWeight="SemiBold"/>

                    <!-- Date -->
                    <toolkit:DataGridTextColumn
                        Header="Date"
                        Binding="{Binding OrderDate, Converter={StaticResource DateConverter}}"
                        Width="110"/>

                    <!-- Customer -->
                    <toolkit:DataGridTemplateColumn Header="Customer" Width="200">
                        <toolkit:DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="8" Padding="0,4">
                                    <PersonPicture Width="32" Height="32" DisplayName="{Binding CustomerName}"/>
                                    <StackPanel VerticalAlignment="Center">
                                        <TextBlock Text="{Binding CustomerName}" FontWeight="SemiBold"/>
                                        <TextBlock Text="{Binding CustomerCode}"
                                                  Style="{StaticResource CaptionTextBlockStyle}"
                                                  Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                                    </StackPanel>
                                </StackPanel>
                            </DataTemplate>
                        </toolkit:DataGridTemplateColumn.CellTemplate>
                    </toolkit:DataGridTemplateColumn>

                    <!-- Status -->
                    <toolkit:DataGridTemplateColumn Header="Status" Width="120">
                        <toolkit:DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Border Background="{Binding Status, Converter={StaticResource OrderStatusToBrush}}"
                                       CornerRadius="4" Padding="8,4" HorizontalAlignment="Left">
                                    <TextBlock Text="{Binding Status}" Foreground="White" FontSize="12"/>
                                </Border>
                            </DataTemplate>
                        </toolkit:DataGridTemplateColumn.CellTemplate>
                    </toolkit:DataGridTemplateColumn>

                    <!-- Items Count -->
                    <toolkit:DataGridTextColumn
                        Header="Items"
                        Binding="{Binding Items.Count}"
                        Width="70">
                        <toolkit:DataGridTextColumn.ElementStyle>
                            <Style TargetType="TextBlock">
                                <Setter Property="HorizontalAlignment" Value="Center"/>
                            </Style>
                        </toolkit:DataGridTextColumn.ElementStyle>
                    </toolkit:DataGridTextColumn>

                    <!-- Total Amount -->
                    <toolkit:DataGridTextColumn
                        Header="Total"
                        Binding="{Binding TotalAmount, Converter={StaticResource CurrencyConverter}}"
                        Width="120">
                        <toolkit:DataGridTextColumn.ElementStyle>
                            <Style TargetType="TextBlock">
                                <Setter Property="HorizontalAlignment" Value="Right"/>
                                <Setter Property="FontWeight" Value="SemiBold"/>
                                <Setter Property="FontFamily" Value="Consolas"/>
                            </Style>
                        </toolkit:DataGridTextColumn.ElementStyle>
                    </toolkit:DataGridTextColumn>

                    <!-- Payment Status -->
                    <toolkit:DataGridTemplateColumn Header="Payment" Width="100">
                        <toolkit:DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Border Background="{Binding PaymentStatus, Converter={StaticResource PaymentStatusToBrush}}"
                                       CornerRadius="4" Padding="6,2" HorizontalAlignment="Left">
                                    <TextBlock Text="{Binding PaymentStatus}" Foreground="White" FontSize="11"/>
                                </Border>
                            </DataTemplate>
                        </toolkit:DataGridTemplateColumn.CellTemplate>
                    </toolkit:DataGridTemplateColumn>

                    <!-- Actions -->
                    <toolkit:DataGridTemplateColumn Header="" Width="120">
                        <toolkit:DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal" Spacing="4">
                                    <Button Style="{StaticResource SubtleButtonStyle}" ToolTipService.ToolTip="View"
                                           Command="{Binding DataContext.ViewOrderCommand, ElementName=PageRoot}"
                                           CommandParameter="{Binding}">
                                        <FontIcon Glyph="&#xE7B3;" FontSize="12"/>
                                    </Button>
                                    <Button Style="{StaticResource SubtleButtonStyle}" ToolTipService.ToolTip="Edit"
                                           Command="{Binding DataContext.EditOrderCommand, ElementName=PageRoot}"
                                           CommandParameter="{Binding}">
                                        <FontIcon Glyph="&#xE70F;" FontSize="12"/>
                                    </Button>
                                    <Button Style="{StaticResource SubtleButtonStyle}" ToolTipService.ToolTip="Print"
                                           Command="{Binding DataContext.PrintOrderCommand, ElementName=PageRoot}"
                                           CommandParameter="{Binding}">
                                        <FontIcon Glyph="&#xE749;" FontSize="12"/>
                                    </Button>
                                </StackPanel>
                            </DataTemplate>
                        </toolkit:DataGridTemplateColumn.CellTemplate>
                    </toolkit:DataGridTemplateColumn>
                </toolkit:DataGrid.Columns>

                <!-- Context Menu -->
                <toolkit:DataGrid.ContextFlyout>
                    <MenuFlyout>
                        <MenuFlyoutItem Text="View" Icon="{ui:FontIcon Glyph=&#xE7B3;}"
                                       Command="{x:Bind ViewModel.ViewOrderCommand}"/>
                        <MenuFlyoutItem Text="Edit" Icon="{ui:FontIcon Glyph=&#xE70F;}"
                                       Command="{x:Bind ViewModel.EditOrderCommand}"/>
                        <MenuFlyoutSeparator/>
                        <MenuFlyoutItem Text="Duplicate" Icon="{ui:FontIcon Glyph=&#xE8C8;}"
                                       Command="{x:Bind ViewModel.DuplicateOrderCommand}"/>
                        <MenuFlyoutSeparator/>
                        <MenuFlyoutSubItem Text="Change Status">
                            <MenuFlyoutItem Text="Confirm" Command="{x:Bind ViewModel.ChangeStatusCommand}" CommandParameter="Confirmed"/>
                            <MenuFlyoutItem Text="Process" Command="{x:Bind ViewModel.ChangeStatusCommand}" CommandParameter="Processing"/>
                            <MenuFlyoutItem Text="Ship" Command="{x:Bind ViewModel.ChangeStatusCommand}" CommandParameter="Shipped"/>
                            <MenuFlyoutItem Text="Cancel" Command="{x:Bind ViewModel.ChangeStatusCommand}" CommandParameter="Cancelled"/>
                        </MenuFlyoutSubItem>
                        <MenuFlyoutSeparator/>
                        <MenuFlyoutItem Text="Print" Icon="{ui:FontIcon Glyph=&#xE749;}"
                                       Command="{x:Bind ViewModel.PrintOrderCommand}"/>
                        <MenuFlyoutSubItem Text="Export">
                            <MenuFlyoutItem Text="Export to Excel" Command="{x:Bind ViewModel.ExportCommand}" CommandParameter="xlsx"/>
                            <MenuFlyoutItem Text="Export to PDF" Command="{x:Bind ViewModel.ExportCommand}" CommandParameter="pdf"/>
                        </MenuFlyoutSubItem>
                        <MenuFlyoutSeparator/>
                        <MenuFlyoutItem Text="Delete" Icon="{ui:FontIcon Glyph=&#xE74D;}"
                                       Command="{x:Bind ViewModel.DeleteOrderCommand}"/>
                    </MenuFlyout>
                </toolkit:DataGrid.ContextFlyout>
            </toolkit:DataGrid>
        </Grid>

        <!-- Pagination -->
        <Grid Grid.Row="2" Padding="24,8" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBlock VerticalAlignment="Center">
                <Run Text="Showing"/>
                <Run Text="{x:Bind ViewModel.StartIndex, Mode=OneWay}"/>
                <Run Text="-"/>
                <Run Text="{x:Bind ViewModel.EndIndex, Mode=OneWay}"/>
                <Run Text="of"/>
                <Run Text="{x:Bind ViewModel.TotalCount, Mode=OneWay}" FontWeight="SemiBold"/>
            </TextBlock>

            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                <TextBlock Text="Show:" VerticalAlignment="Center"/>
                <ComboBox SelectedItem="{x:Bind ViewModel.PageSize, Mode=TwoWay}" Width="80">
                    <x:Int32>25</x:Int32>
                    <x:Int32>50</x:Int32>
                    <x:Int32>100</x:Int32>
                </ComboBox>
            </StackPanel>

            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4" Margin="16,0,0,0">
                <Button Content="&#xE892;" FontFamily="Segoe MDL2 Assets" Command="{x:Bind ViewModel.FirstPageCommand}"/>
                <Button Content="&#xE76B;" FontFamily="Segoe MDL2 Assets" Command="{x:Bind ViewModel.PreviousPageCommand}"/>
                <TextBlock VerticalAlignment="Center" Margin="8,0">
                    <Run Text="Page"/>
                    <Run Text="{x:Bind ViewModel.CurrentPage, Mode=OneWay}" FontWeight="SemiBold"/>
                    <Run Text="of"/>
                    <Run Text="{x:Bind ViewModel.TotalPages, Mode=OneWay}"/>
                </TextBlock>
                <Button Content="&#xE76C;" FontFamily="Segoe MDL2 Assets" Command="{x:Bind ViewModel.NextPageCommand}"/>
                <Button Content="&#xE893;" FontFamily="Segoe MDL2 Assets" Command="{x:Bind ViewModel.LastPageCommand}"/>
            </StackPanel>
        </Grid>
    </Grid>
</Page>
```

### 4. Order Detail View - Master (訂單明細視圖 - 主表)

```xml
<!-- OrderDetailView.xaml -->
<Page
    x:Class="Arcana.Module.Order.Views.OrderDetailView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Arcana.App.Controls"
    xmlns:toolkit="using:CommunityToolkit.WinUI.UI.Controls">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header with Actions -->
        <Grid Padding="24,16" Background="{ThemeResource LayerFillColorDefaultBrush}"
             BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="0,0,0,1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <StackPanel Orientation="Horizontal" Spacing="16">
                <!-- Back Button -->
                <Button Style="{StaticResource SubtleButtonStyle}"
                       Command="{x:Bind ViewModel.GoBackCommand}">
                    <FontIcon Glyph="&#xE72B;" FontSize="16"/>
                </Button>

                <!-- Title -->
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <TextBlock Text="{x:Bind ViewModel.PageTitle, Mode=OneWay}"
                                  Style="{StaticResource TitleTextBlockStyle}"/>
                        <Border Background="{x:Bind ViewModel.Order.Status, Mode=OneWay, Converter={StaticResource OrderStatusToBrush}}"
                               CornerRadius="4" Padding="8,2" VerticalAlignment="Center">
                            <TextBlock Text="{x:Bind ViewModel.Order.Status, Mode=OneWay}"
                                      Foreground="White" FontSize="12"/>
                        </Border>
                    </StackPanel>
                    <TextBlock Style="{StaticResource CaptionTextBlockStyle}"
                              Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                        <Run Text="Created"/>
                        <Run Text="{x:Bind ViewModel.Order.CreatedAt, Mode=OneWay, Converter={StaticResource RelativeTimeConverter}}"/>
                        <Run Text="• Last modified"/>
                        <Run Text="{x:Bind ViewModel.Order.ModifiedAt, Mode=OneWay, Converter={StaticResource RelativeTimeConverter}}"/>
                    </TextBlock>
                </StackPanel>
            </StackPanel>

            <!-- Action Buttons -->
            <CommandBar Grid.Column="1" DefaultLabelPosition="Right" Background="Transparent">
                <AppBarButton Icon="Save" Label="Save" Command="{x:Bind ViewModel.SaveCommand}">
                    <AppBarButton.KeyboardAccelerators>
                        <KeyboardAccelerator Key="S" Modifiers="Control"/>
                    </AppBarButton.KeyboardAccelerators>
                </AppBarButton>
                <AppBarButton Icon="Refresh" Label="Refresh" Command="{x:Bind ViewModel.RefreshCommand}"/>
                <AppBarSeparator/>
                <AppBarButton Icon="Print" Label="Print" Command="{x:Bind ViewModel.PrintCommand}"/>
                <AppBarButton Label="Export">
                    <AppBarButton.Icon>
                        <FontIcon Glyph="&#xEDE1;"/>
                    </AppBarButton.Icon>
                    <AppBarButton.Flyout>
                        <MenuFlyout>
                            <MenuFlyoutItem Text="Export to Excel" Command="{x:Bind ViewModel.ExportCommand}" CommandParameter="xlsx"/>
                            <MenuFlyoutItem Text="Export to PDF" Command="{x:Bind ViewModel.ExportCommand}" CommandParameter="pdf"/>
                        </MenuFlyout>
                    </AppBarButton.Flyout>
                </AppBarButton>
                <AppBarSeparator/>
                <AppBarButton Icon="Delete" Label="Delete" Command="{x:Bind ViewModel.DeleteCommand}"/>
            </CommandBar>
        </Grid>

        <!-- Main Content -->
        <ScrollViewer Grid.Row="1" Padding="24">
            <StackPanel Spacing="24" MaxWidth="1200">

                <!-- Order Info Card -->
                <controls:SettingsCard Header="Order Information" Description="Basic order details">
                    <controls:SettingsCard.HeaderIcon>
                        <FontIcon Glyph="&#xE719;"/>
                    </controls:SettingsCard.HeaderIcon>
                    <controls:SettingsCard.Content>
                        <Grid ColumnSpacing="24" RowSpacing="16" Padding="0,16,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>

                            <!-- Order Number -->
                            <controls:FormTextField
                                Label="Order Number"
                                Value="{x:Bind ViewModel.Order.OrderNumber, Mode=TwoWay}"
                                IsReadOnly="{x:Bind ViewModel.IsExisting, Mode=OneWay}"/>

                            <!-- Order Date -->
                            <controls:FormDateField
                                Grid.Column="1"
                                Label="Order Date"
                                Value="{x:Bind ViewModel.Order.OrderDate, Mode=TwoWay}"/>

                            <!-- Required Date -->
                            <controls:FormDateField
                                Grid.Column="2"
                                Label="Required Date"
                                Value="{x:Bind ViewModel.Order.RequiredDate, Mode=TwoWay}"/>

                            <!-- Status -->
                            <StackPanel Grid.Column="3" Spacing="4">
                                <TextBlock Text="Status" Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                <ComboBox ItemsSource="{x:Bind ViewModel.OrderStatuses}"
                                         SelectedItem="{x:Bind ViewModel.Order.Status, Mode=TwoWay}"
                                         HorizontalAlignment="Stretch"/>
                            </StackPanel>

                            <!-- Payment Method -->
                            <StackPanel Grid.Row="1" Spacing="4">
                                <TextBlock Text="Payment Method" Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                <ComboBox ItemsSource="{x:Bind ViewModel.PaymentMethods}"
                                         SelectedItem="{x:Bind ViewModel.Order.PaymentMethod, Mode=TwoWay}"
                                         HorizontalAlignment="Stretch"/>
                            </StackPanel>

                            <!-- Payment Status -->
                            <StackPanel Grid.Row="1" Grid.Column="1" Spacing="4">
                                <TextBlock Text="Payment Status" Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                <ComboBox ItemsSource="{x:Bind ViewModel.PaymentStatuses}"
                                         SelectedItem="{x:Bind ViewModel.Order.PaymentStatus, Mode=TwoWay}"
                                         HorizontalAlignment="Stretch"/>
                            </StackPanel>
                        </Grid>
                    </controls:SettingsCard.Content>
                </controls:SettingsCard>

                <!-- Customer Card -->
                <controls:SettingsCard Header="Customer" Description="Customer information">
                    <controls:SettingsCard.HeaderIcon>
                        <FontIcon Glyph="&#xE77B;"/>
                    </controls:SettingsCard.HeaderIcon>
                    <controls:SettingsCard.Content>
                        <Grid ColumnSpacing="16" Padding="0,16,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>

                            <!-- Customer Lookup -->
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>

                                <AutoSuggestBox
                                    PlaceholderText="Search customer..."
                                    Text="{x:Bind ViewModel.CustomerSearchText, Mode=TwoWay}"
                                    ItemsSource="{x:Bind ViewModel.CustomerSuggestions, Mode=OneWay}"
                                    TextChanged="OnCustomerSearchTextChanged"
                                    SuggestionChosen="OnCustomerChosen">
                                    <AutoSuggestBox.ItemTemplate>
                                        <DataTemplate>
                                            <StackPanel Orientation="Horizontal" Spacing="8" Padding="4">
                                                <PersonPicture Width="32" Height="32" DisplayName="{Binding Name}"/>
                                                <StackPanel>
                                                    <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                                                    <TextBlock Text="{Binding Code}" Style="{StaticResource CaptionTextBlockStyle}"/>
                                                </StackPanel>
                                            </StackPanel>
                                        </DataTemplate>
                                    </AutoSuggestBox.ItemTemplate>
                                </AutoSuggestBox>

                                <Button Grid.Column="1" Content="..." Margin="8,0,0,0"
                                       Command="{x:Bind ViewModel.BrowseCustomerCommand}"/>
                            </Grid>

                            <!-- Selected Customer Display -->
                            <Border Grid.Column="1"
                                   Background="{ThemeResource CardBackgroundFillColorSecondaryBrush}"
                                   CornerRadius="8" Padding="16"
                                   Visibility="{x:Bind ViewModel.HasCustomer, Mode=OneWay, Converter={StaticResource BoolToVisibility}}">
                                <StackPanel Orientation="Horizontal" Spacing="12">
                                    <PersonPicture Width="48" Height="48"
                                                  DisplayName="{x:Bind ViewModel.Order.CustomerName, Mode=OneWay}"/>
                                    <StackPanel VerticalAlignment="Center">
                                        <TextBlock Text="{x:Bind ViewModel.Order.CustomerName, Mode=OneWay}"
                                                  Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                        <TextBlock Text="{x:Bind ViewModel.Order.CustomerCode, Mode=OneWay}"
                                                  Style="{StaticResource CaptionTextBlockStyle}"/>
                                    </StackPanel>
                                    <Button Style="{StaticResource SubtleButtonStyle}"
                                           Command="{x:Bind ViewModel.ClearCustomerCommand}">
                                        <FontIcon Glyph="&#xE711;" FontSize="12"/>
                                    </Button>
                                </StackPanel>
                            </Border>
                        </Grid>
                    </controls:SettingsCard.Content>
                </controls:SettingsCard>

                <!-- Shipping Address Card -->
                <controls:SettingsCard Header="Shipping Address" Description="Delivery address">
                    <controls:SettingsCard.HeaderIcon>
                        <FontIcon Glyph="&#xE81D;"/>
                    </controls:SettingsCard.HeaderIcon>
                    <controls:SettingsCard.Content>
                        <Grid ColumnSpacing="16" RowSpacing="12" Padding="0,16,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="2*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>

                            <controls:FormTextField
                                Label="Address"
                                Value="{x:Bind ViewModel.Order.ShipToAddress, Mode=TwoWay}"/>
                            <controls:FormTextField
                                Grid.Column="1"
                                Label="City"
                                Value="{x:Bind ViewModel.Order.ShipToCity, Mode=TwoWay}"/>
                            <controls:FormTextField
                                Grid.Column="2"
                                Label="Postal Code"
                                Value="{x:Bind ViewModel.Order.ShipToPostalCode, Mode=TwoWay}"/>
                            <controls:FormTextField
                                Grid.Column="3"
                                Label="Country"
                                Value="{x:Bind ViewModel.Order.ShipToCountry, Mode=TwoWay}"/>
                        </Grid>
                    </controls:SettingsCard.Content>
                </controls:SettingsCard>

                <!-- Order Items Card (Detail Grid) -->
                <controls:SettingsCard Header="Order Items" Description="Products in this order">
                    <controls:SettingsCard.HeaderIcon>
                        <FontIcon Glyph="&#xE7B8;"/>
                    </controls:SettingsCard.HeaderIcon>
                    <controls:SettingsCard.HeaderAction>
                        <Button Style="{StaticResource AccentButtonStyle}"
                               Command="{x:Bind ViewModel.AddItemCommand}">
                            <StackPanel Orientation="Horizontal" Spacing="8">
                                <FontIcon Glyph="&#xE710;" FontSize="12"/>
                                <TextBlock Text="Add Item"/>
                            </StackPanel>
                        </Button>
                    </controls:SettingsCard.HeaderAction>
                    <controls:SettingsCard.Content>
                        <Grid Padding="0,16,0,0">
                            <toolkit:DataGrid
                                ItemsSource="{x:Bind ViewModel.Order.Items, Mode=OneWay}"
                                SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}"
                                AutoGenerateColumns="False"
                                CanUserReorderColumns="False"
                                CanUserResizeColumns="True"
                                GridLinesVisibility="Horizontal"
                                MinHeight="200">

                                <toolkit:DataGrid.Columns>
                                    <!-- Line # -->
                                    <toolkit:DataGridTextColumn
                                        Header="#"
                                        Binding="{Binding LineNumber}"
                                        Width="50"
                                        IsReadOnly="True"/>

                                    <!-- Product (Lookup) -->
                                    <toolkit:DataGridTemplateColumn Header="Product" Width="250">
                                        <toolkit:DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <StackPanel Orientation="Horizontal" Spacing="8" Padding="4">
                                                    <Image Source="{Binding ProductImage}" Width="32" Height="32"/>
                                                    <StackPanel VerticalAlignment="Center">
                                                        <TextBlock Text="{Binding ProductName}" FontWeight="SemiBold"/>
                                                        <TextBlock Text="{Binding ProductCode}"
                                                                  Style="{StaticResource CaptionTextBlockStyle}"/>
                                                    </StackPanel>
                                                </StackPanel>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellTemplate>
                                        <toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                            <DataTemplate>
                                                <AutoSuggestBox
                                                    Text="{Binding ProductName, Mode=TwoWay}"
                                                    PlaceholderText="Search product..."/>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                    </toolkit:DataGridTemplateColumn>

                                    <!-- Quantity -->
                                    <toolkit:DataGridTemplateColumn Header="Qty" Width="100">
                                        <toolkit:DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <TextBlock Text="{Binding Quantity}" HorizontalAlignment="Right"/>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellTemplate>
                                        <toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                            <DataTemplate>
                                                <NumberBox Value="{Binding Quantity, Mode=TwoWay}"
                                                          Minimum="0.01" SpinButtonPlacementMode="Compact"/>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                    </toolkit:DataGridTemplateColumn>

                                    <!-- Unit -->
                                    <toolkit:DataGridTextColumn
                                        Header="Unit"
                                        Binding="{Binding Unit}"
                                        Width="60"/>

                                    <!-- Unit Price -->
                                    <toolkit:DataGridTemplateColumn Header="Unit Price" Width="120">
                                        <toolkit:DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <TextBlock Text="{Binding UnitPrice, Converter={StaticResource CurrencyConverter}}"
                                                          HorizontalAlignment="Right" FontFamily="Consolas"/>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellTemplate>
                                        <toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                            <DataTemplate>
                                                <NumberBox Value="{Binding UnitPrice, Mode=TwoWay}"
                                                          Minimum="0" SpinButtonPlacementMode="Compact">
                                                    <NumberBox.NumberFormatter>
                                                        <DecimalFormatter FractionDigits="2"/>
                                                    </NumberBox.NumberFormatter>
                                                </NumberBox>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                    </toolkit:DataGridTemplateColumn>

                                    <!-- Discount % -->
                                    <toolkit:DataGridTemplateColumn Header="Disc %" Width="90">
                                        <toolkit:DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <TextBlock Text="{Binding DiscountPercent, StringFormat='{}{0:N1}%'}"
                                                          HorizontalAlignment="Right"/>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellTemplate>
                                        <toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                            <DataTemplate>
                                                <NumberBox Value="{Binding DiscountPercent, Mode=TwoWay}"
                                                          Minimum="0" Maximum="100" SpinButtonPlacementMode="Compact"/>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellEditingTemplate>
                                    </toolkit:DataGridTemplateColumn>

                                    <!-- Line Total -->
                                    <toolkit:DataGridTextColumn
                                        Header="Total"
                                        Binding="{Binding LineTotal, Converter={StaticResource CurrencyConverter}}"
                                        Width="120"
                                        IsReadOnly="True">
                                        <toolkit:DataGridTextColumn.ElementStyle>
                                            <Style TargetType="TextBlock">
                                                <Setter Property="HorizontalAlignment" Value="Right"/>
                                                <Setter Property="FontWeight" Value="SemiBold"/>
                                                <Setter Property="FontFamily" Value="Consolas"/>
                                            </Style>
                                        </toolkit:DataGridTextColumn.ElementStyle>
                                    </toolkit:DataGridTextColumn>

                                    <!-- Actions -->
                                    <toolkit:DataGridTemplateColumn Header="" Width="80">
                                        <toolkit:DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <StackPanel Orientation="Horizontal">
                                                    <Button Style="{StaticResource SubtleButtonStyle}"
                                                           Command="{Binding DataContext.EditItemCommand, ElementName=PageRoot}"
                                                           CommandParameter="{Binding}">
                                                        <FontIcon Glyph="&#xE70F;" FontSize="12"/>
                                                    </Button>
                                                    <Button Style="{StaticResource SubtleButtonStyle}"
                                                           Command="{Binding DataContext.DeleteItemCommand, ElementName=PageRoot}"
                                                           CommandParameter="{Binding}">
                                                        <FontIcon Glyph="&#xE74D;" FontSize="12"/>
                                                    </Button>
                                                </StackPanel>
                                            </DataTemplate>
                                        </toolkit:DataGridTemplateColumn.CellTemplate>
                                    </toolkit:DataGridTemplateColumn>
                                </toolkit:DataGrid.Columns>
                            </toolkit:DataGrid>
                        </Grid>
                    </controls:SettingsCard.Content>
                </controls:SettingsCard>

                <!-- Order Summary -->
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="400"/>
                    </Grid.ColumnDefinitions>

                    <!-- Notes -->
                    <controls:SettingsCard Header="Notes" Description="Additional information">
                        <controls:SettingsCard.Content>
                            <StackPanel Spacing="12" Padding="0,16,0,0">
                                <StackPanel Spacing="4">
                                    <TextBlock Text="Internal Notes" Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                    <TextBox Text="{x:Bind ViewModel.Order.InternalNotes, Mode=TwoWay}"
                                            AcceptsReturn="True" TextWrapping="Wrap" MinHeight="80"/>
                                </StackPanel>
                                <StackPanel Spacing="4">
                                    <TextBlock Text="Customer Notes" Style="{StaticResource BodyStrongTextBlockStyle}"/>
                                    <TextBox Text="{x:Bind ViewModel.Order.CustomerNotes, Mode=TwoWay}"
                                            AcceptsReturn="True" TextWrapping="Wrap" MinHeight="80"/>
                                </StackPanel>
                            </StackPanel>
                        </controls:SettingsCard.Content>
                    </controls:SettingsCard>

                    <!-- Summary Totals -->
                    <Border Grid.Column="1"
                           Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
                           CornerRadius="8" Padding="24" Margin="24,0,0,0">
                        <StackPanel Spacing="12">
                            <TextBlock Text="Order Summary" Style="{StaticResource SubtitleTextBlockStyle}"/>

                            <Grid RowSpacing="8">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>

                                <TextBlock Text="Subtotal"/>
                                <TextBlock Grid.Column="1"
                                          Text="{x:Bind ViewModel.Order.SubTotal, Mode=OneWay, Converter={StaticResource CurrencyConverter}}"
                                          HorizontalAlignment="Right" FontFamily="Consolas"/>

                                <TextBlock Grid.Row="1" Text="Tax (5%)"/>
                                <TextBlock Grid.Row="1" Grid.Column="1"
                                          Text="{x:Bind ViewModel.Order.TaxAmount, Mode=OneWay, Converter={StaticResource CurrencyConverter}}"
                                          HorizontalAlignment="Right" FontFamily="Consolas"/>

                                <TextBlock Grid.Row="2" Text="Shipping"/>
                                <NumberBox Grid.Row="2" Grid.Column="1"
                                          Value="{x:Bind ViewModel.Order.ShippingFee, Mode=TwoWay}"
                                          HorizontalAlignment="Right" Width="120">
                                    <NumberBox.NumberFormatter>
                                        <DecimalFormatter FractionDigits="2"/>
                                    </NumberBox.NumberFormatter>
                                </NumberBox>

                                <TextBlock Grid.Row="3" Text="Discount"/>
                                <NumberBox Grid.Row="3" Grid.Column="1"
                                          Value="{x:Bind ViewModel.Order.DiscountAmount, Mode=TwoWay}"
                                          HorizontalAlignment="Right" Width="120">
                                    <NumberBox.NumberFormatter>
                                        <DecimalFormatter FractionDigits="2"/>
                                    </NumberBox.NumberFormatter>
                                </NumberBox>

                                <Border Grid.Row="4" Grid.ColumnSpan="2"
                                       BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}"
                                       BorderThickness="0,1,0,0" Margin="0,8"/>

                                <TextBlock Grid.Row="5" Text="Total"
                                          Style="{StaticResource SubtitleTextBlockStyle}"/>
                                <TextBlock Grid.Row="5" Grid.Column="1"
                                          Text="{x:Bind ViewModel.Order.TotalAmount, Mode=OneWay, Converter={StaticResource CurrencyConverter}}"
                                          Style="{StaticResource TitleTextBlockStyle}"
                                          Foreground="{ThemeResource AccentFillColorDefaultBrush}"
                                          HorizontalAlignment="Right" FontFamily="Consolas"/>
                            </Grid>
                        </StackPanel>
                    </Border>
                </Grid>
            </StackPanel>
        </ScrollViewer>

        <!-- Footer Actions -->
        <Grid Grid.Row="2" Padding="24,12"
             Background="{ThemeResource LayerFillColorDefaultBrush}"
             BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="0,1,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- Left: Info -->
            <StackPanel Orientation="Horizontal" Spacing="16">
                <InfoBar IsOpen="{x:Bind ViewModel.IsDirty, Mode=OneWay}"
                        Severity="Warning"
                        Title="Unsaved Changes"
                        Message="You have unsaved changes."
                        IsClosable="False"/>
            </StackPanel>

            <!-- Right: Buttons -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                <Button Content="Cancel" Command="{x:Bind ViewModel.CancelCommand}"/>
                <Button Content="Save as Draft" Command="{x:Bind ViewModel.SaveDraftCommand}"
                       Visibility="{x:Bind ViewModel.CanSaveDraft, Mode=OneWay}"/>
                <Button Content="Save" Style="{StaticResource AccentButtonStyle}"
                       Command="{x:Bind ViewModel.SaveCommand}"/>
                <Button Content="Save &amp; Close" Style="{StaticResource AccentButtonStyle}"
                       Command="{x:Bind ViewModel.SaveAndCloseCommand}"/>
            </StackPanel>
        </Grid>
    </Grid>
</Page>
```

### 5. Order ViewModel (訂單 ViewModel)

```csharp
// ============================================
// ORDER LIST VIEW MODEL
// ============================================

public partial class OrderListViewModel : ViewModelBase
{
    private readonly IPluginContext _context;
    private readonly IOrderRepository _orderRepository;

    [ObservableProperty] private ObservableCollection<Order> _orders = new();
    [ObservableProperty] private Order? _selectedOrder;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private OrderStatus? _selectedStatus;
    [ObservableProperty] private DateTimeOffset? _dateFrom;
    [ObservableProperty] private DateTimeOffset? _dateTo;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _isLoading;

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public int StartIndex => (CurrentPage - 1) * PageSize + 1;
    public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);
    public bool CanGoBack => CurrentPage > 1;
    public bool CanGoForward => CurrentPage < TotalPages;
    public bool HasSelection => SelectedOrder != null;

    public IReadOnlyList<OrderStatus?> StatusFilters { get; } =
        new OrderStatus?[] { null }.Concat(Enum.GetValues<OrderStatus>().Cast<OrderStatus?>()).ToList();

    public OrderListViewModel(IPluginContext context, IOrderRepository orderRepository)
    {
        _context = context;
        _orderRepository = orderRepository;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var filter = new OrderFilter
            {
                SearchText = SearchText,
                Status = SelectedStatus,
                DateFrom = DateFrom?.DateTime,
                DateTo = DateTo?.DateTime
            };

            var result = await _orderRepository.GetPagedAsync(CurrentPage, PageSize, filter);

            Orders.Clear();
            foreach (var order in result.Items)
                Orders.Add(order);

            TotalCount = result.TotalCount;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewOrderAsync()
    {
        await _context.Mdi.OpenDocumentAsync("order.detail.view", "new");
    }

    [RelayCommand]
    private async Task ViewOrderAsync(Order? order)
    {
        if (order == null) return;
        await _context.Mdi.OpenDocumentAsync("order.detail.view", order.Id);
    }

    [RelayCommand]
    private async Task EditOrderAsync(Order? order)
    {
        if (order == null) order = SelectedOrder;
        if (order == null) return;
        await _context.Mdi.OpenDocumentAsync("order.detail.view", order.Id);
    }

    [RelayCommand]
    private async Task DeleteOrderAsync(Order? order)
    {
        if (order == null) order = SelectedOrder;
        if (order == null) return;

        var result = await _context.Dialogs.ShowConfirmationAsync(
            "Delete Order",
            $"Are you sure you want to delete order {order.OrderNumber}?",
            "Delete", "Cancel");

        if (result == ContentDialogResult.Primary)
        {
            await _orderRepository.DeleteAsync(order.Id);
            Orders.Remove(order);
            TotalCount--;

            _context.Events.Publish(new EntityDeletedEvent<Order>(order.Id, "arcana.module.order"));
            _context.Notifications.ShowSuccess($"Order {order.OrderNumber} deleted.");
        }
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(string status)
    {
        if (SelectedOrder == null) return;

        var newStatus = Enum.Parse<OrderStatus>(status);
        SelectedOrder.Status = newStatus;
        await _orderRepository.UpdateAsync(SelectedOrder);

        _context.Events.Publish(new EntityUpdatedEvent<Order>(SelectedOrder, "arcana.module.order"));
        _context.Notifications.ShowSuccess($"Order status changed to {status}.");
    }

    [RelayCommand]
    private async Task ExportAsync(string format)
    {
        var exporter = _context.Contributions.GetExportHandlers("Order")
            .FirstOrDefault(h => h.Format == format);

        if (exporter == null) return;

        var handler = (IExportHandler)_context.Services.GetRequiredService(exporter.HandlerType);
        await handler.ExportAsync(Orders, format);
    }

    // Pagination commands
    [RelayCommand] private async Task FirstPageAsync() { CurrentPage = 1; await LoadAsync(); }
    [RelayCommand] private async Task PreviousPageAsync() { if (CanGoBack) { CurrentPage--; await LoadAsync(); } }
    [RelayCommand] private async Task NextPageAsync() { if (CanGoForward) { CurrentPage++; await LoadAsync(); } }
    [RelayCommand] private async Task LastPageAsync() { CurrentPage = TotalPages; await LoadAsync(); }
}

// ============================================
// ORDER DETAIL VIEW MODEL
// ============================================

public partial class OrderDetailViewModel : ViewModelBase, INavigationAware
{
    private readonly IPluginContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;

    [ObservableProperty] private Order _order = new();
    [ObservableProperty] private OrderItem? _selectedItem;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isExisting;
    [ObservableProperty] private string _customerSearchText = "";
    [ObservableProperty] private ObservableCollection<Customer> _customerSuggestions = new();

    public string PageTitle => IsExisting ? $"Order #{Order.OrderNumber}" : "New Order";
    public bool HasCustomer => Order.CustomerId > 0;
    public bool CanSaveDraft => Order.Status == OrderStatus.Draft;

    public IReadOnlyList<OrderStatus> OrderStatuses { get; } = Enum.GetValues<OrderStatus>().ToList();
    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>().ToList();
    public IReadOnlyList<PaymentStatus> PaymentStatuses { get; } = Enum.GetValues<PaymentStatus>().ToList();

    public OrderDetailViewModel(
        IPluginContext context,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository)
    {
        _context = context;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
    }

    public async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;
        try
        {
            if (parameter is "new" or null)
            {
                // New order
                Order = new Order
                {
                    OrderNumber = await GenerateOrderNumberAsync(),
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Draft
                };
                IsExisting = false;
            }
            else if (parameter is int orderId)
            {
                // Edit existing
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    _context.Notifications.ShowError("Order not found");
                    await _context.Navigation.GoBackAsync();
                    return;
                }
                Order = order;
                IsExisting = true;
                CustomerSearchText = order.CustomerName ?? "";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        IsLoading = true;
        try
        {
            // Calculate totals
            Order.SubTotal = Order.Items.Sum(i => i.LineTotal);

            if (IsExisting)
            {
                await _orderRepository.UpdateAsync(Order);
                _context.Events.Publish(new EntityUpdatedEvent<Order>(Order, "arcana.module.order"));
            }
            else
            {
                await _orderRepository.CreateAsync(Order);
                IsExisting = true;
                _context.Events.Publish(new EntityCreatedEvent<Order>(Order, "arcana.module.order"));
            }

            IsDirty = false;
            _context.Notifications.ShowSuccess("Order saved successfully.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        await SaveAsync();
        if (!IsDirty)
        {
            await _context.Navigation.GoBackAsync();
        }
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var dialog = new OrderItemDialog();
        var result = await _context.Dialogs.ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary && dialog.Item != null)
        {
            dialog.Item.LineNumber = Order.Items.Count + 1;
            Order.Items.Add(dialog.Item);
            IsDirty = true;
            UpdateTotals();
        }
    }

    [RelayCommand]
    private async Task EditItemAsync(OrderItem item)
    {
        var dialog = new OrderItemDialog(item);
        var result = await _context.Dialogs.ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary)
        {
            IsDirty = true;
            UpdateTotals();
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(OrderItem item)
    {
        var result = await _context.Dialogs.ShowConfirmationAsync(
            "Delete Item",
            "Are you sure you want to remove this item?",
            "Delete", "Cancel");

        if (result == ContentDialogResult.Primary)
        {
            Order.Items.Remove(item);
            RenumberItems();
            IsDirty = true;
            UpdateTotals();
        }
    }

    [RelayCommand]
    private async Task BrowseCustomerAsync()
    {
        var dialog = new CustomerLookupDialog();
        var result = await _context.Dialogs.ShowDialogAsync(dialog);

        if (result == ContentDialogResult.Primary && dialog.SelectedCustomer != null)
        {
            SetCustomer(dialog.SelectedCustomer);
        }
    }

    [RelayCommand]
    private void ClearCustomer()
    {
        Order.CustomerId = 0;
        Order.CustomerName = null;
        Order.CustomerCode = null;
        CustomerSearchText = "";
        IsDirty = true;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsExisting) return;

        var result = await _context.Dialogs.ShowConfirmationAsync(
            "Delete Order",
            $"Are you sure you want to delete order {Order.OrderNumber}? This action cannot be undone.",
            "Delete", "Cancel");

        if (result == ContentDialogResult.Primary)
        {
            await _orderRepository.DeleteAsync(Order.Id);
            _context.Events.Publish(new EntityDeletedEvent<Order>(Order.Id, "arcana.module.order"));
            _context.Notifications.ShowSuccess("Order deleted.");
            await _context.Navigation.GoBackAsync();
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsDirty)
        {
            var result = await _context.Dialogs.ShowConfirmationAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to cancel?",
                "Discard", "Keep Editing");

            if (result != ContentDialogResult.Primary) return;
        }

        await _context.Navigation.GoBackAsync();
    }

    private void SetCustomer(Customer customer)
    {
        Order.CustomerId = customer.Id;
        Order.CustomerName = customer.Name;
        Order.CustomerCode = customer.Code;
        Order.ShipToAddress = customer.Address;
        Order.ShipToCity = customer.City;
        Order.ShipToPostalCode = customer.PostalCode;
        Order.ShipToCountry = customer.Country;
        CustomerSearchText = customer.Name;
        IsDirty = true;
    }

    private void UpdateTotals()
    {
        Order.SubTotal = Order.Items.Sum(i => i.LineTotal);
        OnPropertyChanged(nameof(Order));
    }

    private void RenumberItems()
    {
        int lineNumber = 1;
        foreach (var item in Order.Items)
        {
            item.LineNumber = lineNumber++;
        }
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var date = DateTime.Now;
        var prefix = $"ORD-{date:yyyyMMdd}-";
        var lastOrder = await _orderRepository.GetLastOrderNumberAsync(prefix);
        var sequence = 1;

        if (lastOrder != null)
        {
            var lastSeq = lastOrder.OrderNumber.Replace(prefix, "");
            if (int.TryParse(lastSeq, out var num))
                sequence = num + 1;
        }

        return $"{prefix}{sequence:D4}";
    }

    private async Task<bool> ValidateAsync()
    {
        var errors = new List<string>();

        if (Order.CustomerId == 0)
            errors.Add("Please select a customer.");

        if (!Order.Items.Any())
            errors.Add("Order must have at least one item.");

        if (errors.Any())
        {
            await _context.Dialogs.ShowErrorAsync("Validation Error", string.Join("\n", errors));
            return false;
        }

        return true;
    }
}
```

---

This prompt provides a complete architectural blueprint for building a **Local-First, Plugin-Everything** Windows desktop application with C# and WinUI3. The application:

1. **Local-First**: Treats the local SQLite database as the primary data store
2. **Plugin-Everything**: All functionality beyond the minimal shell is implemented as plugins
3. **Shared Context**: Plugins share a common context for services and state
4. **Inter-Plugin Communication**: Message bus and event aggregator for plugin-to-plugin messaging
5. **Extensible**: Infinite extensibility through contribution points
6. **Complete UI Library**: Full set of reusable components (Menu, Tab, DataGrid, Master-Detail, Forms, Dialogs)
7. **Working Example**: Complete Order Master-Detail CRUD demonstrating all components

This architecture ensures instant performance, full offline capability, maximum extensibility, and clean separation of concerns.
