using Arcana.Plugin.FlowChart.Navigation;
using Arcana.Plugin.FlowChart.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;

namespace Arcana.Plugin.FlowChart;

/// <summary>
/// FlowChart plugin - provides flowchart drawing and editing functionality.
/// Supports Draw.io compatible file format.
/// </summary>
public class FlowChartPlugin : PluginBase
{
    private FlowChartNavGraph? _nav;

    /// <summary>
    /// Type-safe navigation for this plugin.
    /// </summary>
    public FlowChartNavGraph Nav => _nav ?? throw new InvalidOperationException("Plugin not activated");

    public override PluginMetadata Metadata => new()
    {
        Id = "arcana.plugin.flowchart",
        Name = "FlowChart",
        Version = new Version(1, 0, 0),
        Description = "Flowchart drawing and editing plugin with Draw.io compatibility",
        Type = PluginType.Module,
        Author = "Arcana Team"
    };

    protected override async Task OnActivateAsync(IPluginContext context)
    {
        // Initialize plugin's type-safe NavGraph
        _nav = new FlowChartNavGraph(context.NavGraph);

        // Load localization from external JSON files
        var localesPath = Path.Combine(context.PluginPath, "locales");
        await LoadExternalLocalizationAsync(localesPath);
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "FlowChartEditorPage",
            Title = L("flowchart.editor"),
            TitleKey = "flowchart.editor",
            Icon = "\uE8FD",
            Type = ViewType.Page,
            ViewClass = typeof(FlowChartEditorPage),
            Category = L("menu.tools")
        });

        // Register main menu items under Tools menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.tools.flowchart",
                Title = L("flowchart.title"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.tools",
                Icon = "\uE8FD",
                Order = 10
            },
            new MenuItemDefinition
            {
                Id = "menu.tools.flowchart.new",
                Title = L("flowchart.new"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.tools.flowchart",
                Icon = "\uE710",
                Order = 1,
                Command = "flowchart.new"
            },
            new MenuItemDefinition
            {
                Id = "menu.tools.flowchart.open",
                Title = L("flowchart.open"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.tools.flowchart",
                Icon = "\uE8E5",
                Order = 2,
                Command = "flowchart.open"
            },
            new MenuItemDefinition
            {
                Id = "menu.tools.flowchart.sample",
                Title = L("flowchart.sample"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.tools.flowchart",
                Icon = "\uE8F1",
                Order = 3,
                Command = "flowchart.sample"
            }
        );

        // Register function tree items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "tree.flowchart",
                Title = L("flowchart.title"),
                Location = MenuLocation.FunctionTree,
                Icon = "\uE8FD",
                Order = 50,
                Command = "flowchart.new"
            }
        );

        // Quick Access - New FlowChart
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "quick.newFlowChart",
                Title = L("flowchart.new"),
                Location = MenuLocation.QuickAccess,
                Icon = "\uE8FD",
                Order = 10,
                Group = "tools",
                Command = "flowchart.new"
            }
        );

        // Register commands using type-safe NavGraph
        RegisterCommand("flowchart.new", async () =>
        {
            await Nav.ToNewEditor();
        });

        RegisterCommand("flowchart.open", async () =>
        {
            await Nav.ToEditorWithOpenDialog();
        });

        RegisterCommand("flowchart.sample", async () =>
        {
            await Nav.ToSampleEditor();
        });

        LogInfo("FlowChart plugin activated");
    }
}
