# FlowChart Plugin for Arcana

A flowchart drawing and editing plugin with Draw.io compatibility.

## Features

- **14 Shape Types**: Rectangle, Diamond, Ellipse, Parallelogram, Hexagon, Cylinder, Document, Cloud, and more
- **Draw.io Compatible**: Save/load `.drawio` files compatible with draw.io application
- **Multiple Formats**: Supports `.afc` (native), `.drawio` (XML), `.json`
- **Node Editing**: Text, colors, size, font, Z-ordering
- **Edge Editing**: Labels, line styles (solid/dashed/dotted), routing (direct/orthogonal/curved), arrows
- **Undo/Redo**: 50-level undo history
- **Grid Support**: Show grid, snap to grid, configurable grid size
- **Zoom**: Zoom in/out/reset (10%-300%)
- **i18n Ready**: English, Traditional Chinese, Japanese

## Building

### Prerequisites

- Windows 10/11
- .NET 10.0 SDK
- Visual Studio 2022 or later (optional)

### Build Steps

1. Open PowerShell in this directory
2. Run the build script:

```powershell
# Release build
.\build.ps1

# Debug build
.\build.ps1 -Configuration Debug

# Clean and rebuild
.\build.ps1 -Clean
```

The script will create a zip package: `FlowChartPlugin-v1.0.0-x64.zip`

## Installation

1. Build the plugin using the build script
2. Extract the zip contents to: `%LOCALAPPDATA%\Arcana\plugins\`
3. Restart Arcana application
4. The FlowChart plugin will appear under **Tools** menu

## Usage

### Creating a New FlowChart

1. Go to **Tools** → **FlowChart** → **New FlowChart**
2. Or use the Function Tree: click **FlowChart**
3. Or use Quick Access: **New FlowChart**

### Adding Shapes

1. Click on a shape in the left **Shapes** panel
2. The shape will be added to the canvas
3. Drag to reposition

### Connecting Shapes

1. Click the **Connect** toggle button in the toolbar
2. Click on a connection point (blue dot) on a source shape
3. Drag to a connection point on the target shape
4. Release to create the connection

### Editing Properties

- Select a node or edge to see its properties in the right panel
- Change text, colors, size, line style, etc.

### Saving/Loading

- **Save**: Ctrl+S or toolbar Save button
- **Save As**: Choose format (.afc, .drawio, .json)
- **Open**: Ctrl+O or toolbar Open button

## File Formats

| Format | Extension | Description |
|--------|-----------|-------------|
| Arcana FlowChart | `.afc` | Native JSON format |
| Draw.io | `.drawio` | XML format compatible with draw.io |
| JSON | `.json` | Standard JSON format |

## Localization

The plugin supports multiple languages:

- English (en-US) - Default
- Traditional Chinese (zh-TW)
- Japanese (ja-JP)

Localization files are in the `locales/` directory.

## License

This plugin is part of the Arcana project.
