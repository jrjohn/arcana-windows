namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// Application-level permission for fine-grained access control.
/// </summary>
public class AppPermission : BaseEntity
{
    /// <summary>
    /// Unique permission code (e.g., "orders.create", "customers.delete").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Permission description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping permissions (e.g., "Orders", "Customers").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a system permission that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Roles that have this permission.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Users that have this permission directly.
    /// </summary>
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

/// <summary>
/// Predefined system permissions.
/// </summary>
public static class SystemPermissions
{
    // Order permissions
    public const string OrdersView = "orders.view";
    public const string OrdersCreate = "orders.create";
    public const string OrdersEdit = "orders.edit";
    public const string OrdersDelete = "orders.delete";
    public const string OrdersApprove = "orders.approve";
    public const string OrdersCancel = "orders.cancel";

    // Customer permissions
    public const string CustomersView = "customers.view";
    public const string CustomersCreate = "customers.create";
    public const string CustomersEdit = "customers.edit";
    public const string CustomersDelete = "customers.delete";

    // Product permissions
    public const string ProductsView = "products.view";
    public const string ProductsCreate = "products.create";
    public const string ProductsEdit = "products.edit";
    public const string ProductsDelete = "products.delete";
    public const string ProductsManageStock = "products.manage_stock";

    // User management permissions
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string UsersManageRoles = "users.manage_roles";

    // Role management permissions
    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";

    // Plugin permissions
    public const string PluginsView = "plugins.view";
    public const string PluginsInstall = "plugins.install";
    public const string PluginsUninstall = "plugins.uninstall";
    public const string PluginsManage = "plugins.manage";

    // System permissions
    public const string SettingsView = "settings.view";
    public const string SettingsEdit = "settings.edit";
    public const string SystemBackup = "system.backup";
    public const string SystemRestore = "system.restore";
    public const string AuditLogsView = "audit.view";
}
