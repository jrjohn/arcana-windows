using System.Text.RegularExpressions;

namespace Arcana.Plugins.Contracts.Validation;

/// <summary>
/// Validator for menu item definitions.
/// </summary>
public static partial class MenuItemValidator
{
    // Valid command ID format: alphanumeric with dots and underscores
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9._-]*$")]
    private static partial Regex CommandIdRegex();

    public static ContributionValidationResult Validate(MenuItemDefinition item)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required: Id
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            errors.Add("Menu item Id is required");
        }
        else if (!CommandIdRegex().IsMatch(item.Id))
        {
            errors.Add($"Menu item Id '{item.Id}' has invalid format. Must start with a letter and contain only letters, numbers, dots, underscores, or hyphens.");
        }

        // Required: Title (unless separator)
        if (!item.IsSeparator && string.IsNullOrWhiteSpace(item.Title))
        {
            errors.Add($"Menu item '{item.Id}' requires a Title (unless it's a separator)");
        }

        // Warning: Command format
        if (!string.IsNullOrWhiteSpace(item.Command) && !CommandIdRegex().IsMatch(item.Command))
        {
            warnings.Add($"Menu item '{item.Id}' has command '{item.Command}' with non-standard format");
        }

        // Warning: Negative order
        if (item.Order < 0)
        {
            warnings.Add($"Menu item '{item.Id}' has negative order ({item.Order}), consider using positive values");
        }

        // Warning: ModuleId required for ModuleQuickAccess
        if (item.Location == MenuLocation.ModuleQuickAccess && string.IsNullOrWhiteSpace(item.ModuleId))
        {
            warnings.Add($"Menu item '{item.Id}' has ModuleQuickAccess location but no ModuleId set");
        }

        if (errors.Count > 0)
        {
            return new ContributionValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }

        if (warnings.Count > 0)
        {
            return new ContributionValidationResult
            {
                IsValid = true,
                Warnings = warnings
            };
        }

        return ContributionValidationResult.Success();
    }
}

/// <summary>
/// Validator for view definitions.
/// </summary>
public static partial class ViewValidator
{
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*$")]
    private static partial Regex ViewIdRegex();

    public static ContributionValidationResult Validate(ViewDefinition view)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required: Id
        if (string.IsNullOrWhiteSpace(view.Id))
        {
            errors.Add("View Id is required");
        }
        else if (!ViewIdRegex().IsMatch(view.Id))
        {
            errors.Add($"View Id '{view.Id}' has invalid format. Must start with a letter and contain only letters, numbers, or underscores.");
        }

        // Required: Title
        if (string.IsNullOrWhiteSpace(view.Title))
        {
            errors.Add($"View '{view.Id}' requires a Title");
        }

        // Warning: ViewClass recommended
        if (view.ViewClass == null)
        {
            warnings.Add($"View '{view.Id}' has no ViewClass set. Views without ViewClass can only be created via factory.");
        }

        // Warning: ModuleId required for module tabs
        if (view.IsModuleDefaultTab && string.IsNullOrWhiteSpace(view.ModuleId))
        {
            warnings.Add($"View '{view.Id}' is marked as IsModuleDefaultTab but has no ModuleId set");
        }

        // Warning: TitleKey recommended for better localization
        if (string.IsNullOrWhiteSpace(view.TitleKey))
        {
            warnings.Add($"View '{view.Id}' has no TitleKey set. Consider setting TitleKey for dynamic localization.");
        }

        if (errors.Count > 0)
        {
            return new ContributionValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }

        if (warnings.Count > 0)
        {
            return new ContributionValidationResult
            {
                IsValid = true,
                Warnings = warnings
            };
        }

        return ContributionValidationResult.Success();
    }
}

/// <summary>
/// Validator for command registrations.
/// </summary>
public static partial class CommandValidator
{
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9._-]*$")]
    private static partial Regex CommandIdRegex();

    public static ContributionValidationResult ValidateCommandId(string commandId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(commandId))
        {
            errors.Add("Command Id is required");
        }
        else if (!CommandIdRegex().IsMatch(commandId))
        {
            errors.Add($"Command Id '{commandId}' has invalid format. Must start with a letter and contain only letters, numbers, dots, underscores, or hyphens.");
        }

        if (errors.Count > 0)
        {
            return ContributionValidationResult.Failure(errors.ToArray());
        }

        return ContributionValidationResult.Success();
    }
}
