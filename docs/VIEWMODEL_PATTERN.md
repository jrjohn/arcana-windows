# ViewModel Input/Output/Effect Pattern (UDF)

This document describes the UDF (Unidirectional Data Flow) ViewModel pattern implemented in this project.

## Overview

The pattern separates ViewModel concerns into three nested classes:

```
┌─────────────────────────────────────────────────────────────┐
│                         VIEW                                │
│  ┌─────────────────┐              ┌─────────────────┐      │
│  │  User Actions   │              │  UI Rendering   │      │
│  └────────┬────────┘              └────────▲────────┘      │
└───────────┼────────────────────────────────┼───────────────┘
            │                                │
            ▼                                │
┌───────────────────┐              ┌─────────────────────┐
│    vm.In          │              │      vm.Out         │
│  (Input Actions)  │              │  (Readonly State)   │
└─────────┬─────────┘              └──────────▲──────────┘
          │                                   │
          ▼                                   │
┌─────────────────────────────────────────────────────────────┐
│                    VIEWMODEL STATE                          │
│                  (Private Properties)                       │
└─────────────────────────────────────────────────────────────┘
          │
          ▼
┌───────────────────┐
│      vm.Fx        │
│  (Side Effects)   │
└───────────────────┘
```

## Three Nested Classes

### 1. Input (In) - Actions

**Purpose**: The ONLY entry point for state changes.

```csharp
public sealed class Input : IViewModelInput
{
    public Task LoadOrders() => _vm.LoadOrdersAsync();
    public Task Search() => _vm.SearchAsync();
    public void UpdateSearchText(string? text) => _vm.SearchText = text;
}
```

### 2. Output (Out) - State

**Purpose**: Read-only reactive state exposed to the View.

```csharp
public sealed class Output : IViewModelOutput
{
    public ReadOnlyObservableCollection<Order> Orders => ...;
    public bool IsLoading => _vm.IsLoading;
    public bool CanGoPrevious => CurrentPage > 1;
}
```

### 3. Effect (Fx) - Side Effects

**Purpose**: One-time events for side effects.

```csharp
public sealed class Effect : IViewModelEffect, IDisposable
{
    public EffectSubject<NavigationRequest> NavigateToOrder { get; } = new();
    public EffectSubject<AppError> ShowError { get; } = new();
    public EffectSubject<string> ShowSuccess { get; } = new();
}
```

## Implementation

```csharp
public partial class OrderListViewModel : ReactiveViewModelBase
{
    // Private state
    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();

    // Expose nested classes
    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    // Private action methods
    private async Task LoadOrdersAsync() { ... }

    // Nested classes
    public sealed class Input : IViewModelInput { ... }
    public sealed class Output : IViewModelOutput { ... }
    public sealed class Effect : IViewModelEffect { ... }

    // Effect DTOs
    public record NavigationRequest(int OrderId, bool IsReadOnly = false);
}
```

## View Usage

```csharp
// Initialize
await _vm.In.Initialize();

// Read state (XAML binding or code)
var orders = _vm.Out.Orders;
var isLoading = _vm.Out.IsLoading;

// Trigger actions
await _vm.In.Search();
_vm.In.UpdateSearchText("query");

// Subscribe to effects
_vm.Fx.ShowError.Subscribe(error => ShowErrorDialog(error.Message));
_vm.Fx.ConfirmDelete.Subscribe(req => {
    if (ShowConfirmDialog()) req.OnConfirm();
});
```

## XAML Binding

```xml
<!-- Bind to Out properties (read-only) -->
<ListView ItemsSource="{x:Bind _vm.Out.Orders, Mode=OneWay}" />
<TextBlock Text="{x:Bind _vm.Out.StatusMessage, Mode=OneWay}" />
<ProgressRing IsActive="{x:Bind _vm.Out.IsLoading, Mode=OneWay}" />
<Button IsEnabled="{x:Bind _vm.Out.CanGoPrevious, Mode=OneWay}" />
```

## File Structure

```
ViewModels/
├── Core/
│   ├── IViewModelInput.cs
│   ├── IViewModelOutput.cs
│   ├── IViewModelEffect.cs
│   ├── ReactiveViewModelBase.cs
│   ├── EffectSubject.cs
│   └── CommonEffects.cs
├── Orders/
│   └── OrderListViewModel.cs    ← Contains Input, Output, Effect as nested classes
└── ...
```

## Benefits

1. **Single File**: All related code in one place
2. **Clear Separation**: View knows where to read (Out) and write (In)
3. **Predictable Flow**: State changes flow in one direction
4. **Testability**: Each layer can be tested independently
5. **Type Safety**: Nested classes provide compile-time safety

## Naming Convention

| Property | Type | Purpose |
|----------|------|---------|
| `In` | `Input` | Actions to trigger state changes |
| `Out` | `Output` | Read-only state for View |
| `Fx` | `Effect` | Side effect streams (dialogs, notifications) |

## NavGraph - Centralized Navigation

All navigation is centralized in `NavGraph` for type-safe routing:

```csharp
public sealed class NavGraph
{
    // Routes
    public static class Routes
    {
        public const string OrderList = "OrderListPage";
        public const string OrderDetail = "OrderDetailPage";
    }

    // Navigation actions
    public Task<bool> ToOrderList() => ...;
    public Task<bool> ToOrderDetail(int orderId, bool readOnly = false) => ...;
    public Task<bool> ToNewOrder() => ...;
    public Task<bool> Back() => ...;
}
```

### Usage in ViewModel

```csharp
public class OrderListViewModel
{
    private readonly NavGraph _nav;

    private Task CreateOrderAsync() => _nav.ToNewOrder();
    private Task EditOrderAsync(Order order) => _nav.ToOrderDetail(order.Id);
    private Task ViewOrderAsync(Order order) => _nav.ToOrderDetail(order.Id, readOnly: true);
}
```

### Benefits

1. **Type-safe navigation** - compile-time route checking
2. **Centralized routes** - all destinations in one place
3. **Readable code** - `_nav.ToOrderDetail()` vs `NavigateToNewTabAsync("OrderDetailPage")`
4. **Easy refactoring** - change route in one place
