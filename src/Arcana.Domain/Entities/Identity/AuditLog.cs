namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// Audit log for security-related events.
/// 安全相關事件的審計日誌
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Type of audit event.
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// User ID who triggered the event (null for system events).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Username at the time of the event.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// IP address or client identifier.
    /// </summary>
    public string? ClientInfo { get; set; }

    /// <summary>
    /// Resource being accessed (e.g., entity type and ID).
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Additional details in JSON format.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of audit events.
/// </summary>
public enum AuditEventType
{
    // Authentication events
    LoginSuccess,
    LoginFailed,
    Logout,
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    AccountLocked,
    AccountUnlocked,

    // Authorization events
    PermissionGranted,
    PermissionRevoked,
    RoleAssigned,
    RoleRevoked,
    AccessDenied,

    // User management events
    UserCreated,
    UserUpdated,
    UserDeleted,
    UserActivated,
    UserDeactivated,

    // Role management events
    RoleCreated,
    RoleUpdated,
    RoleDeleted,

    // Session events
    SessionCreated,
    SessionExpired,
    SessionTerminated,
    TokenRefreshed,

    // System events
    SystemStartup,
    SystemShutdown,
    ConfigurationChanged,
    PluginInstalled,
    PluginUninstalled
}
