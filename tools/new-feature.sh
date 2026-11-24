#!/usr/bin/env bash
# Add one vertical slice (entity + list page) in this template's layered architecture.
#
#   tools/new-feature.sh Invoice
#
# Generates, exactly like the Customer/Order features (AGENTS.md §2), with <P> = the solution
# prefix (Arcana, or the product name after tools/new-app.sh):
#   src/<P>.Domain/Entities/<X>.cs                         entity (BaseEntity: audit, soft delete, sync)
#   src/<P>.Domain/Validation/<X>Validator.cs              FluentValidation rules
#   src/<P>.Domain/Services/<X>Service.cs                  service interface (Result<T>)
#   src/<P>.Data/Dao/<X>Dao.cs + Dao/impl/<X>DaoImpl.cs    EF Core queries (only place with LINQ-to-EF)
#   src/<P>.Data/Repository/<X>Repository.cs + Repository/impl/<X>RepositoryImpl.cs
#   src/<P>.Infrastructure/Services/impl/<X>ServiceImpl.cs  business rules, injects the repository interface
#   src/<P>.App/ViewModels/<Xs>/<X>ListViewModel.cs        ReactiveViewModelBase with Input/Output/Effect
#   src/<P>.App/Views/<X>ListPage.xaml(.cs)                view: talks to its ViewModel only
#   edits: AppDbContext DbSet + model config, DI (ServiceCollectionExtensions.cs, App.xaml.cs),
#          NavGraph route + To<X>List(), MainWindow page/title/icon maps, en-US/zh-TW/ja-JP strings
#   tests: Domain.Tests/Validation/<X>ValidatorTests.cs, Data.Tests/Repository/<X>RepositoryTests.cs,
#          Infrastructure.Tests/Services/<X>ServiceTests.cs
# Fails loudly (exit 1) when an anchor in the existing files is missing or a file exists.
set -euo pipefail

[ $# -eq 1 ] || { echo "usage: $0 <PascalCaseName>   e.g. $0 Invoice" >&2; exit 1; }
NAME="$1"
[[ "$NAME" =~ ^[A-Z][a-zA-Z0-9]*$ ]] || { echo "name must be PascalCase (e.g. Invoice, PurchaseOrder)" >&2; exit 1; }
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
die() { echo "new-feature: $*" >&2; exit 1; }
command -v perl >/dev/null || die "perl is required"

DOMAIN_DIR="$(ls -d src/*.Domain 2>/dev/null | head -1)"
[ -n "$DOMAIN_DIR" ] || die "no src/<P>.Domain project found — run from a repo created from arcana-windows"
P="${DOMAIN_DIR#src/}"; P="${P%.Domain}"
LOWER="$(printf '%s' "${NAME:0:1}" | tr 'A-Z' 'a-z')${NAME:1}"
case "$NAME" in *s) PLURAL="${NAME}es" ;; *y) PLURAL="${NAME%y}ies" ;; *) PLURAL="${NAME}s" ;; esac
LPLURAL="$(printf '%s' "${PLURAL:0:1}" | tr 'A-Z' 'a-z')${PLURAL:1}"
KEY="$(printf '%s' "$LOWER" | tr 'A-Z' 'a-z')"     # localization key prefix, e.g. purchaseorder

DBCTX="src/$P.Data/Local/AppDbContext.cs"
DI="src/$P.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs"
APPXAML="src/$P.App/App.xaml.cs"
NAV="src/$P.App/Navigation/NavGraph.cs"
MAINWIN="src/$P.App/MainWindow.xaml.cs"
STRINGS="src/$P.App/Resources/Strings"

# Anchors in existing files. If the template changed, fix this script — do not hand-wire.
need() { [ -f "$1" ] || die "missing file $1 — template changed; update tools/new-feature.sh"
         grep -qF -- "$2" "$1" || die "anchor not found in $1: '$2' — template changed; update tools/new-feature.sh"; }
need "$DBCTX"   "public DbSet<Customer> Customers => Set<Customer>();"
need "$DBCTX"   "        // Customer"
need "$DI"      "services.AddScoped<CustomerDao, CustomerDaoImpl>();"
need "$DI"      "services.AddScoped<CustomerRepository, CustomerRepositoryImpl>();"
need "$DI"      "services.AddScoped<CustomerService, CustomerServiceImpl>();"
need "$APPXAML" "using $P.App.ViewModels.Orders;"
need "$APPXAML" "services.AddTransient<OrderListViewModel>();"
need "$NAV"     "public const string CustomerDetail = \"CustomerDetailPage\";"
need "$NAV"     "    #region Customers"
need "$MAINWIN" "\"CustomerListPage\" => typeof(CustomerListPage),"
need "$MAINWIN" "\"CustomerListPage\" => _localization.Get(\"customer.list\"),"
need "$MAINWIN" "\"CustomerListPage\" => \"\\uE716\",   // Contact"
for l in en-US zh-TW ja-JP; do need "$STRINGS/$l.json" '  "customer.list": '; done
grep -qF "DbSet<$NAME> " "$DBCTX" && die "$NAME already exists (DbSet<$NAME> in AppDbContext)"

FILES=(
  "src/$P.Domain/Entities/$NAME.cs"
  "src/$P.Domain/Validation/${NAME}Validator.cs"
  "src/$P.Domain/Services/${NAME}Service.cs"
  "src/$P.Data/Dao/${NAME}Dao.cs" "src/$P.Data/Dao/impl/${NAME}DaoImpl.cs"
  "src/$P.Data/Repository/${NAME}Repository.cs" "src/$P.Data/Repository/impl/${NAME}RepositoryImpl.cs"
  "src/$P.Infrastructure/Services/impl/${NAME}ServiceImpl.cs"
  "src/$P.App/ViewModels/$PLURAL/${NAME}ListViewModel.cs"
  "src/$P.App/Views/${NAME}ListPage.xaml" "src/$P.App/Views/${NAME}ListPage.xaml.cs"
  "tests/$P.Domain.Tests/Validation/${NAME}ValidatorTests.cs"
  "tests/$P.Data.Tests/Repository/${NAME}RepositoryTests.cs"
  "tests/$P.Infrastructure.Tests/Services/${NAME}ServiceTests.cs"
)
for f in "${FILES[@]}"; do [ ! -e "$f" ] || die "$f already exists"; done
for d in "tests/$P.Domain.Tests" "tests/$P.Data.Tests" "tests/$P.Infrastructure.Tests"; do [ -d "$d" ] || die "missing test project $d"; done

sub() { perl -pe "s/__P__/$P/g; s/__Name__/$NAME/g; s/__name__/$LOWER/g; s/__Names__/$PLURAL/g; s/__names__/$LPLURAL/g; s/__key__/$KEY/g"; }
emit() { mkdir -p "$(dirname "$1")"; sub > "$1"; }

# ───────────────────────── Domain ─────────────────────────
emit "src/$P.Domain/Entities/$NAME.cs" <<'EOT'
namespace __P__.Domain.Entities;

/// <summary>
/// __Name__ entity. Audit, soft-delete and sync fields come from <see cref="BaseEntity"/>.
/// </summary>
public class __Name__ : BaseEntity
{
    /// <summary>Unique business code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the __name__ is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Notes.</summary>
    public string? Notes { get; set; }
}
EOT

emit "src/$P.Domain/Validation/${NAME}Validator.cs" <<'EOT'
using __P__.Domain.Entities;
using FluentValidation;

namespace __P__.Domain.Validation;

/// <summary>
/// __Name__ entity validator.
/// </summary>
public class __Name__Validator : AbstractValidator<__Name__>
{
    public __Name__Validator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("__Name__ code cannot be empty")
            .MaximumLength(20).WithMessage("__Name__ code cannot exceed 20 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("__Name__ name cannot be empty")
            .MaximumLength(100).WithMessage("__Name__ name cannot exceed 100 characters");
    }
}
EOT

emit "src/$P.Domain/Services/${NAME}Service.cs" <<'EOT'
using __P__.Core.Common;
using __P__.Domain.Entities;

namespace __P__.Domain.Services;

/// <summary>
/// __Name__ service interface. Implementation: Infrastructure/Services/impl/__Name__ServiceImpl.cs.
/// </summary>
public interface __Name__Service  // NOSONAR
{
    Task<Result<PagedResult<__Name__>>> Get__Names__Async(PageRequest request, CancellationToken cancellationToken = default);
    Task<Result<__Name__>> Get__Name__ByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<__Name__>> Create__Name__Async(__Name__ __name__, CancellationToken cancellationToken = default);
    Task<Result<__Name__>> Update__Name__Async(__Name__ __name__, CancellationToken cancellationToken = default);
    Task<Result> Delete__Name__Async(int id, CancellationToken cancellationToken = default);
}
EOT

# ───────────────────────── Data: DAO + Repository ─────────────────────────
emit "src/$P.Data/Dao/${NAME}Dao.cs" <<'EOT'
using __P__.Domain.Entities;

namespace __P__.Data.Dao;

/// <summary>
/// Data Access Object interface for __Name__. Raw database access used by the repository.
/// </summary>
public interface __Name__Dao  // NOSONAR
{
    Task<__Name__?> FindByCode(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExists(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
EOT

emit "src/$P.Data/Dao/impl/${NAME}DaoImpl.cs" <<'EOT'
using __P__.Data.Local;
using __P__.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace __P__.Data.Dao.Impl;

/// <summary>
/// EF Core implementation of __Name__Dao. All direct AppDbContext / LINQ-to-EF calls live here.
/// </summary>
public class __Name__DaoImpl : __Name__Dao
{
    private readonly AppDbContext _context;

    public __Name__DaoImpl(AppDbContext context)
    {
        _context = context;
    }

    public async Task<__Name__?> FindByCode(string code, CancellationToken cancellationToken = default)
    {
        return await _context.__Names__.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<bool> CodeExists(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.__Names__.Where(x => x.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }
}
EOT

emit "src/$P.Data/Repository/${NAME}Repository.cs" <<'EOT'
using __P__.Core.Common;
using __P__.Domain.Entities;

namespace __P__.Data.Repository;

/// <summary>
/// __Name__ repository interface; generic CRUD comes from Repository&lt;T&gt;.
/// </summary>
public interface __Name__Repository : Repository<__Name__>  // NOSONAR
{
    Task<__Name__?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
EOT

emit "src/$P.Data/Repository/impl/${NAME}RepositoryImpl.cs" <<'EOT'
using __P__.Data.Dao;
using __P__.Data.Local;
using __P__.Domain.Entities;

namespace __P__.Data.Repository.Impl;

/// <summary>
/// __Name__ repository implementation. Generic CRUD from RepositoryImpl; specific queries via __Name__Dao.
/// </summary>
public class __Name__RepositoryImpl : RepositoryImpl<__Name__>, __Name__Repository
{
    private readonly __Name__Dao _dao;

    public __Name__RepositoryImpl(AppDbContext context, __Name__Dao dao) : base(context)
    {
        _dao = dao;
    }

    public Task<__Name__?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _dao.FindByCode(code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
        => _dao.CodeExists(code, excludeId, cancellationToken);
}
EOT

# ───────────────────────── Service impl (Infrastructure) ─────────────────────────
emit "src/$P.Infrastructure/Services/impl/${NAME}ServiceImpl.cs" <<'EOT'
using __P__.Core.Common;
using __P__.Data.Repository;
using __P__.Domain.Entities;
using __P__.Domain.Services;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace __P__.Infrastructure.Services.Impl;

/// <summary>
/// __Name__ service implementation. Registered in DependencyInjection/ServiceCollectionExtensions.cs.
/// </summary>
public class __Name__ServiceImpl : __Name__Service
{
    private readonly __Name__Repository _repository;
    private readonly IValidator<__Name__> _validator;
    private readonly ILogger<__Name__ServiceImpl> _logger;

    public __Name__ServiceImpl(
        __Name__Repository repository,
        IValidator<__Name__> validator,
        ILogger<__Name__ServiceImpl> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<__Name__>>> Get__Names__Async(PageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _repository.GetPagedAsync(request, cancellationToken);
            return Result<PagedResult<__Name__>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting __names__");
            return Result<PagedResult<__Name__>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get __names__", ex));
        }
    }

    public async Task<Result<__Name__>> Get__Name__ByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity == null
                ? Result<__Name__>.Failure(new AppError.Data(ErrorCode.NotFound, $"__Name__ with ID {id} not found"))
                : Result<__Name__>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting __name__ {Id}", id);
            return Result<__Name__>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get __name__", ex));
        }
    }

    public async Task<Result<__Name__>> Create__Name__Async(__Name__ __name__, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _repository.CodeExistsAsync(__name__.Code, null, cancellationToken))
            {
                return Result<__Name__>.Failure(new AppError.Validation(ErrorCode.DuplicateEntry, "__Name__ code already exists", new[] { $"__Name__ code {__name__.Code} already exists" }));
            }

            var validation = await _validator.ValidateAsync(__name__, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<__Name__>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "__Name__ validation failed", errors));
            }

            var created = await _repository.AddAsync(__name__, cancellationToken);
            _logger.LogInformation("Created __name__ {Code}", __name__.Code);
            return Result<__Name__>.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating __name__");
            return Result<__Name__>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to create __name__", ex));
        }
    }

    public async Task<Result<__Name__>> Update__Name__Async(__Name__ __name__, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _repository.GetByIdAsync(__name__.Id, cancellationToken) == null)
            {
                return Result<__Name__>.Failure(new AppError.Data(ErrorCode.NotFound, $"__Name__ with ID {__name__.Id} not found"));
            }

            if (await _repository.CodeExistsAsync(__name__.Code, __name__.Id, cancellationToken))
            {
                return Result<__Name__>.Failure(new AppError.Validation(ErrorCode.DuplicateEntry, "__Name__ code already exists", new[] { $"__Name__ code {__name__.Code} already exists" }));
            }

            var validation = await _validator.ValidateAsync(__name__, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<__Name__>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "__Name__ validation failed", errors));
            }

            await _repository.UpdateAsync(__name__, cancellationToken);
            return Result<__Name__>.Success(__name__);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating __name__ {Id}", __name__.Id);
            return Result<__Name__>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to update __name__", ex));
        }
    }

    public async Task<Result> Delete__Name__Async(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                return Result.Failure(new AppError.Data(ErrorCode.NotFound, $"__Name__ with ID {id} not found"));
            }

            await _repository.DeleteAsync(entity, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting __name__ {Id}", id);
            return Result.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to delete __name__", ex));
        }
    }
}
EOT

# ───────────────────────── Presentation: ViewModel + View ─────────────────────────
emit "src/$P.App/ViewModels/$PLURAL/${NAME}ListViewModel.cs" <<'EOT'
using System.Collections.ObjectModel;
using __P__.Core.Common;
using __P__.Domain.Entities;
using __P__.Domain.Services;
using __P__.Plugins.Contracts.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace __P__.App.ViewModels.__Names__;

/// <summary>
/// ViewModel for the __name__ list page (UDF): View → Input → state → Output → View;
/// one-time side effects go out through Effect. Same shape as Orders/OrderListViewModel.
/// </summary>
public partial class __Name__ListViewModel : ReactiveViewModelBase
{
    private readonly __Name__Service ___name__Service;

    [ObservableProperty]
    private ObservableCollection<__Name__> ___names__ = new();

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    private Input? _input;
    private Output? _output;
    private Effect? _effect;
    private ReadOnlyObservableCollection<__Name__>? ___names__ReadOnly;

    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    public __Name__ListViewModel(__Name__Service __name__Service)
    {
        ___name__Service = __name__Service;
    }

    public override Task InitializeAsync() => Load__Names__Async();

    private async Task Load__Names__Async()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var result = await ___name__Service.Get__Names__Async(new PageRequest(CurrentPage, 20));
            if (result.IsSuccess)
            {
                __Names__.Clear();
                foreach (var item in result.Value!.Items)
                {
                    __Names__.Add(item);
                }
                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages;
            }
            else
            {
                ErrorMessage = result.Error!.Message;
                Fx.ShowError.Emit(result.Error!);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task Delete__Name__Async(__Name__ item)
    {
        var result = await ___name__Service.Delete__Name__Async(item.Id);
        if (result.IsSuccess)
        {
            __Names__.Remove(item);
            TotalCount--;
            Fx.ShowSuccess.Emit($"{item.Code} deleted");
        }
        else
        {
            Fx.ShowError.Emit(result.Error!);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _effect?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Input actions — the only entry point for state changes.</summary>
    public sealed class Input : IViewModelInput
    {
        private readonly __Name__ListViewModel _vm;
        internal Input(__Name__ListViewModel vm) => _vm = vm;

        public Task Initialize() => _vm.InitializeAsync();
        public Task Refresh() => _vm.Load__Names__Async();
        public Task Delete(__Name__ item) => _vm.Delete__Name__Async(item);
    }

    /// <summary>Output — read-only state the View binds to.</summary>
    public sealed class Output : IViewModelOutput
    {
        private readonly __Name__ListViewModel _vm;
        internal Output(__Name__ListViewModel vm) => _vm = vm;

        public ReadOnlyObservableCollection<__Name__> Items =>
            _vm.___names__ReadOnly ??= new ReadOnlyObservableCollection<__Name__>(_vm.__Names__);
        public int CurrentPage => _vm.CurrentPage;
        public int TotalPages => _vm.TotalPages;
        public int TotalCount => _vm.TotalCount;
        public bool IsLoading => _vm.IsLoading;
        public string? ErrorMessage => _vm.ErrorMessage;
    }

    /// <summary>Effect — one-time events (notifications). Navigation goes through NavGraph.</summary>
    public sealed class Effect : IViewModelEffect, IDisposable
    {
        private bool _disposed;
        public EffectSubject<AppError> ShowError { get; } = new();
        public EffectSubject<string> ShowSuccess { get; } = new();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ShowError.Dispose();
            ShowSuccess.Dispose();
        }
    }
}
EOT

emit "src/$P.App/Views/${NAME}ListPage.xaml" <<'EOT'
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="__P__.App.Views.__Name__ListPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid Padding="24" RowSpacing="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock x:Name="PageTitle" Style="{StaticResource TitleTextBlockStyle}"/>

        <ListView x:Name="ItemsList" Grid.Row="1" SelectionMode="Single">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock Text="{Binding Code}" FontWeight="SemiBold"/>
                        <TextBlock Text="{Binding Name}"/>
                    </StackPanel>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</Page>
EOT

emit "src/$P.App/Views/${NAME}ListPage.xaml.cs" <<'EOT'
using __P__.App.ViewModels.__Names__;
using __P__.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace __P__.App.Views;

/// <summary>
/// __Name__ list page. Talks to its ViewModel only (In / Out / Fx), never to a service.
/// </summary>
public sealed partial class __Name__ListPage : Page
{
    private __Name__ListViewModel ViewModel { get; }
    private readonly LocalizationService _localization;

    public __Name__ListPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<__Name__ListViewModel>();
        _localization = App.Services.GetRequiredService<LocalizationService>();
        DataContext = ViewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = _localization.Get("__key__.list");
        ItemsList.ItemsSource = ViewModel.Out.Items;
        await ViewModel.In.Initialize();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => ViewModel.Dispose();
}
EOT

# ───────────────────────── Tests ─────────────────────────
emit "tests/$P.Domain.Tests/Validation/${NAME}ValidatorTests.cs" <<'EOT'
using __P__.Domain.Entities;
using __P__.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace __P__.Domain.Tests.Validation;

public class __Name__ValidatorTests
{
    private readonly __Name__Validator _validator = new();

    [Fact]
    public void Validate_Valid__Name___ShouldPass()
    {
        var result = _validator.Validate(new __Name__ { Code = "C001", Name = "Valid" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCode_ShouldFail()
    {
        var result = _validator.Validate(new __Name__ { Code = "", Name = "Valid" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(__Name__.Code));
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        var result = _validator.Validate(new __Name__ { Code = "C001", Name = new string('x', 101) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(__Name__.Name));
    }
}
EOT

emit "tests/$P.Data.Tests/Repository/${NAME}RepositoryTests.cs" <<'EOT'
using __P__.Data.Dao.Impl;
using __P__.Data.Local;
using __P__.Data.Repository.Impl;
using __P__.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace __P__.Data.Tests.Repository;

public class __Name__RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly __Name__RepositoryImpl _repository;

    public __Name__RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new __Name__RepositoryImpl(_context, new __Name__DaoImpl(_context));

        _context.__Names__.Add(new __Name__ { Id = 1, Code = "C001", Name = "First" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByCodeAsync_Existing_ShouldReturnEntity()
    {
        var entity = await _repository.GetByCodeAsync("C001");
        entity.Should().NotBeNull();
        entity!.Name.Should().Be("First");
    }

    [Fact]
    public async Task CodeExistsAsync_ExcludingItself_ShouldBeFalse()
    {
        (await _repository.CodeExistsAsync("C001")).Should().BeTrue();
        (await _repository.CodeExistsAsync("C001", excludeId: 1)).Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
EOT

emit "tests/$P.Infrastructure.Tests/Services/${NAME}ServiceTests.cs" <<'EOT'
using __P__.Core.Common;
using __P__.Data.Repository;
using __P__.Domain.Entities;
using __P__.Infrastructure.Services.Impl;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace __P__.Infrastructure.Tests.Services;

public class __Name__ServiceTests
{
    private readonly Mock<__Name__Repository> _repoMock = new();
    private readonly Mock<IValidator<__Name__>> _validatorMock = new();
    private readonly __Name__ServiceImpl _service;

    public __Name__ServiceTests()
    {
        _service = new __Name__ServiceImpl(_repoMock.Object, _validatorMock.Object, new Mock<ILogger<__Name__ServiceImpl>>().Object);
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<__Name__>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    [Fact]
    public async Task Get__Name__ByIdAsync_Existing_ShouldReturnSuccess()
    {
        var entity = new __Name__ { Id = 1, Code = "C001", Name = "First" };
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _service.Get__Name__ByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(entity);
    }

    [Fact]
    public async Task Get__Name__ByIdAsync_NotFound_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((__Name__?)null);

        var result = await _service.Get__Name__ByIdAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create__Name__Async_DuplicateCode_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.CodeExistsAsync("C001", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.Create__Name__Async(new __Name__ { Code = "C001", Name = "Dup" });

        result.IsSuccess.Should().BeFalse();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<__Name__>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create__Name__Async_Valid_ShouldAdd()
    {
        var entity = new __Name__ { Code = "C002", Name = "New" };
        _repoMock.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _service.Create__Name__Async(entity);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete__Name__Async_RepositoryThrows_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db error"));

        var result = await _service.Delete__Name__Async(1);

        result.IsSuccess.Should().BeFalse();
    }
}
EOT

# ───────────────────────── Wiring into existing files ─────────────────────────
# Anchor/text go through the environment, not the perl source: "\uE716" in perl code would be
# read as the \u case-modifier escape and silently change the anchor.
ins_after()  { A="$2" T="$3" perl -0pi -e '$a=$ENV{A}; $t=$ENV{T}; s/\Q$a\E/$a\n$t/ or die "no anchor\n"' "$1" || die "could not edit $1 (anchor: $2)"; }
ins_before() { A="$2" T="$3" perl -0pi -e '$a=$ENV{A}; $t=$ENV{T}; s/\Q$a\E/$t\n$a/ or die "no anchor\n"' "$1" || die "could not edit $1 (anchor: $2)"; }

ins_after  "$DBCTX" "public DbSet<Customer> Customers => Set<Customer>();" "    public DbSet<$NAME> $PLURAL => Set<$NAME>();"
ins_before "$DBCTX" "        // Customer" "        // $NAME
        modelBuilder.Entity<$NAME>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });
"
ins_after "$DI" "services.AddScoped<CustomerDao, CustomerDaoImpl>();"               "        services.AddScoped<${NAME}Dao, ${NAME}DaoImpl>();"
ins_after "$DI" "services.AddScoped<CustomerRepository, CustomerRepositoryImpl>();" "        services.AddScoped<${NAME}Repository, ${NAME}RepositoryImpl>();"
ins_after "$DI" "services.AddScoped<CustomerService, CustomerServiceImpl>();"       "        services.AddScoped<${NAME}Service, ${NAME}ServiceImpl>();"
ins_after "$APPXAML" "using $P.App.ViewModels.Orders;" "using $P.App.ViewModels.$PLURAL;"
ins_after "$APPXAML" "services.AddTransient<OrderListViewModel>();" "                services.AddTransient<${NAME}ListViewModel>();"
ins_after "$NAV" "public const string CustomerDetail = \"CustomerDetailPage\";" "
        // $PLURAL Module
        public const string ${NAME}List = \"${NAME}ListPage\";"
ins_before "$NAV" "    #region Customers" "    #region $PLURAL

    /// <summary>
    /// Navigate to the $LOWER list.
    /// </summary>
    public Task<bool> To${NAME}List()
        => _navigationService.NavigateToNewTabAsync(Routes.${NAME}List);

    #endregion
"
ins_after "$MAINWIN" "\"CustomerListPage\" => typeof(CustomerListPage)," "            \"${NAME}ListPage\" => typeof(${NAME}ListPage),"
ins_after "$MAINWIN" "\"CustomerListPage\" => _localization.Get(\"customer.list\")," "            \"${NAME}ListPage\" => _localization.Get(\"${KEY}.list\"),"
ins_after "$MAINWIN" "\"CustomerListPage\" => \"\\uE716\",   // Contact" "            \"${NAME}ListPage\" => \"\\uE8A5\",    // Document"
for l in en-US zh-TW ja-JP; do
    ins_before "$STRINGS/$l.json" '  "customer.list": ' "  \"${KEY}.list\": \"${PLURAL}\","
done

# Verify every edit landed (a silent no-op would leave the slice unreachable).
grep -qF "DbSet<$NAME> $PLURAL" "$DBCTX" || die "DbSet not added"
grep -qF "AddScoped<${NAME}Service, ${NAME}ServiceImpl>" "$DI" || die "service DI not added"
grep -qF "AddTransient<${NAME}ListViewModel>" "$APPXAML" || die "ViewModel DI not added"
grep -qF "To${NAME}List()" "$NAV" || die "NavGraph method not added"
grep -qF "\"${NAME}ListPage\" => typeof(${NAME}ListPage)" "$MAINWIN" || die "MainWindow page map not added"

echo "new-feature: $NAME slice created (${#FILES[@]} files) and wired (AppDbContext, DI, App.xaml.cs, NavGraph, MainWindow, strings)."
echo "next: translate \"${KEY}.list\" in $STRINGS/{zh-TW,ja-JP}.json, add a menu entry via a plugin if needed (see Plugins/CustomerModulePlugin.cs), then tools/arch-check.sh"
