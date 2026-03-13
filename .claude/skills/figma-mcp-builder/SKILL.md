---
name: figma-mcp-builder
description: Guide for building a custom Figma MCP server to automate wireframe generation, component creation, and design system management. Use when building Figma integrations, automating design workflows, or creating design-to-code pipelines.
triggers:
  - build figma mcp
  - figma integration
  - automate figma
  - figma api
---

# Figma MCP Server Builder

Build a Model Context Protocol (MCP) server for Figma automation to generate wireframes, components, and design systems programmatically.

---

## Overview

An MCP server provides Claude with tools to interact with Figma's REST API. This enables:
- **Wireframe generation** from text descriptions
- **Component creation** with auto-layout and constraints
- **Design token management** (colors, typography, spacing)
- **File/page/frame creation** and organization
- **Export automation** (PNG, SVG, PDF)

## Architecture

```
Claude Code → MCP Server → Figma REST API
              ↓
         [Python/TS]
         - Authentication
         - API Client
         - Wireframe Generator
         - Component Builder
```

## Setup Options

### Option 1: FastMCP (Python - Recommended)

**Install:**
```bash
pip install fastmcp anthropic-mcp-sdk requests
```

**File: `figma_mcp_server.py`**
```python
from fastmcp import FastMCP
import requests
import os

# Initialize MCP server
mcp = FastMCP("Figma Wireframe Generator")

# Figma API configuration
FIGMA_TOKEN = os.getenv("FIGMA_ACCESS_TOKEN")
BASE_URL = "https://api.figma.com/v1"

headers = {
    "X-Figma-Token": FIGMA_TOKEN,
    "Content-Type": "application/json"
}

@mcp.tool()
def create_wireframe_frame(
    file_key: str,
    page_name: str,
    frame_name: str,
    width: int = 1440,
    height: int = 1024
) -> dict:
    """
    Create a new frame in a Figma file for wireframing.

    Args:
        file_key: Figma file key from URL
        page_name: Name of the page to add frame to
        frame_name: Name for the new frame
        width: Frame width in pixels
        height: Frame height in pixels
    """
    # Get file structure
    file_url = f"{BASE_URL}/files/{file_key}"
    response = requests.get(file_url, headers=headers)
    data = response.json()

    # Find page ID
    page_id = None
    for page in data['document']['children']:
        if page['name'] == page_name:
            page_id = page['id']
            break

    if not page_id:
        return {"error": f"Page '{page_name}' not found"}

    # Create frame using Figma's node creation
    # Note: Figma API doesn't support direct creation via REST
    # Use Variables API or Plugins API for advanced automation

    return {
        "message": "Frame structure prepared",
        "file_key": file_key,
        "page_id": page_id,
        "frame_spec": {
            "name": frame_name,
            "width": width,
            "height": height,
            "type": "FRAME",
            "background": [{"type": "SOLID", "color": {"r": 1, "g": 1, "b": 1}}]
        }
    }

@mcp.tool()
def add_rectangle_component(
    file_key: str,
    node_id: str,
    x: int,
    y: int,
    width: int,
    height: int,
    fill_color: str = "#FFFFFF",
    label: str = "Component"
) -> dict:
    """
    Add a rectangle component to a Figma frame.

    Args:
        file_key: Figma file key
        node_id: Parent node ID (frame/group)
        x, y: Position coordinates
        width, height: Rectangle dimensions
        fill_color: Hex color (e.g., "#FF5733")
        label: Component label/name
    """
    # Convert hex to RGB
    hex_color = fill_color.lstrip('#')
    r, g, b = tuple(int(hex_color[i:i+2], 16) / 255 for i in (0, 2, 4))

    component_spec = {
        "name": label,
        "type": "RECTANGLE",
        "x": x,
        "y": y,
        "width": width,
        "height": height,
        "fills": [{
            "type": "SOLID",
            "color": {"r": r, "g": g, "b": b}
        }],
        "constraints": {
            "vertical": "TOP",
            "horizontal": "LEFT"
        }
    }

    return {
        "status": "Component spec generated",
        "component": component_spec,
        "note": "Use Figma Plugin API or Variables API to apply"
    }

@mcp.tool()
def create_text_element(
    file_key: str,
    node_id: str,
    text: str,
    x: int,
    y: int,
    font_size: int = 16,
    font_family: str = "Inter"
) -> dict:
    """
    Create a text element in Figma.

    Args:
        file_key: Figma file key
        node_id: Parent node ID
        text: Text content
        x, y: Position
        font_size: Font size in pixels
        font_family: Font family name
    """
    text_spec = {
        "type": "TEXT",
        "name": text[:50],  # Truncate for name
        "characters": text,
        "x": x,
        "y": y,
        "fontSize": font_size,
        "fontName": {
            "family": font_family,
            "style": "Regular"
        },
        "fills": [{
            "type": "SOLID",
            "color": {"r": 0, "g": 0, "b": 0}
        }]
    }

    return {
        "status": "Text element spec generated",
        "element": text_spec
    }

@mcp.tool()
def export_frame_as_png(
    file_key: str,
    node_ids: str,
    scale: float = 2.0
) -> dict:
    """
    Export Figma frames/nodes as PNG images.

    Args:
        file_key: Figma file key
        node_ids: Comma-separated node IDs (e.g., "1:2,3:4")
        scale: Export scale (1.0 = 1x, 2.0 = 2x)
    """
    export_url = f"{BASE_URL}/images/{file_key}"
    params = {
        "ids": node_ids,
        "format": "png",
        "scale": scale
    }

    response = requests.get(export_url, headers=headers, params=params)
    data = response.json()

    return {
        "status": "Export complete",
        "images": data.get("images", {}),
        "note": "Download URLs are temporary (valid ~5 minutes)"
    }

@mcp.tool()
def get_design_tokens(file_key: str) -> dict:
    """
    Extract design tokens (colors, typography) from a Figma file.

    Args:
        file_key: Figma file key
    """
    # Get file styles
    styles_url = f"{BASE_URL}/files/{file_key}/styles"
    response = requests.get(styles_url, headers=headers)
    styles = response.json()

    # Get file variables (design tokens)
    variables_url = f"{BASE_URL}/files/{file_key}/variables/local"
    var_response = requests.get(variables_url, headers=headers)
    variables = var_response.json()

    return {
        "styles": styles.get("meta", {}).get("styles", []),
        "variables": variables.get("meta", {}).get("variables", {}),
        "collections": variables.get("meta", {}).get("variable_collections", {})
    }

if __name__ == "__main__":
    mcp.run()
```

### Option 2: TypeScript MCP SDK

**Install:**
```bash
npm install @modelcontextprotocol/sdk axios
```

**File: `figma-mcp-server.ts`**
```typescript
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import axios from 'axios';

const FIGMA_TOKEN = process.env.FIGMA_ACCESS_TOKEN;
const BASE_URL = 'https://api.figma.com/v1';

const server = new Server(
  {
    name: 'figma-wireframe-server',
    version: '1.0.0',
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Tool: Create wireframe frame
server.setRequestHandler('tools/call', async (request) => {
  const { name, arguments: args } = request.params;

  if (name === 'create_wireframe_frame') {
    const { fileKey, pageName, frameName, width = 1440, height = 1024 } = args;

    const response = await axios.get(`${BASE_URL}/files/${fileKey}`, {
      headers: { 'X-Figma-Token': FIGMA_TOKEN }
    });

    const page = response.data.document.children.find(
      (p: any) => p.name === pageName
    );

    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            pageId: page?.id,
            frameSpec: { name: frameName, width, height }
          }, null, 2)
        }
      ]
    };
  }

  // Add other tools...
});

// Start server
const transport = new StdioServerTransport();
await server.connect(transport);
```

## Figma API Key Setup

1. **Get Personal Access Token:**
   - Go to Figma → Settings → Account → Personal access tokens
   - Click "Generate new token"
   - Copy token and save securely

2. **Set environment variable:**
   ```bash
   export FIGMA_ACCESS_TOKEN="figd_your_token_here"
   ```

3. **Add to Claude Code MCP config:**
   ```json
   {
     "mcpServers": {
       "figma-wireframe": {
         "command": "python",
         "args": ["figma_mcp_server.py"],
         "env": {
           "FIGMA_ACCESS_TOKEN": "figd_your_token_here"
         }
       }
     }
   }
   ```

## Wireframe Generation Workflow

### 1. Create Wireframe from Description

```python
@mcp.tool()
def generate_wireframe_from_description(
    file_key: str,
    page_name: str,
    description: str
) -> dict:
    """
    Generate a wireframe from a text description.
    Example: "Login page with email field, password field, and submit button"
    """
    # Parse description (use LLM or simple keyword matching)
    components = parse_wireframe_description(description)

    # Create frame
    frame_name = f"Wireframe - {description[:30]}"

    # Add components (headers, inputs, buttons, etc.)
    layout = auto_layout_components(components)

    return {
        "frame": frame_name,
        "components": layout,
        "instructions": "Apply these specs in Figma using Plugin API"
    }

def parse_wireframe_description(desc: str) -> list:
    """Extract components from description"""
    components = []

    if "email" in desc.lower():
        components.append({
            "type": "input",
            "label": "Email",
            "placeholder": "Enter your email"
        })

    if "password" in desc.lower():
        components.append({
            "type": "input",
            "label": "Password",
            "inputType": "password"
        })

    if "button" in desc.lower() or "submit" in desc.lower():
        components.append({
            "type": "button",
            "label": "Submit",
            "variant": "primary"
        })

    return components
```

## Limitations & Workarounds

### Figma REST API Limitations:
1. **Cannot create nodes directly** - Only read operations
2. **Cannot modify file structure** - Read-only access
3. **No real-time updates** - Polling required

### Workarounds:
1. **Use Figma Plugin API** - Write a companion plugin
2. **Use Variables API** - Create design tokens programmatically
3. **Generate specs** - Create JSON specs for manual application
4. **Use Rube MCP** - Leverage Composio's Figma toolkit (installed via `figma-automation` skill)

## Advanced: Figma Plugin Companion

For full automation, pair the MCP server with a Figma plugin:

**plugin.ts**
```typescript
figma.ui.onmessage = async (msg) => {
  if (msg.type === 'create-wireframe') {
    const frame = figma.createFrame();
    frame.name = msg.frameName;
    frame.resize(msg.width, msg.height);

    // Add components from MCP specs
    for (const spec of msg.components) {
      if (spec.type === 'RECTANGLE') {
        const rect = figma.createRectangle();
        rect.x = spec.x;
        rect.y = spec.y;
        rect.resize(spec.width, spec.height);
        rect.fills = spec.fills;
        frame.appendChild(rect);
      }
    }

    figma.viewport.scrollAndZoomIntoView([frame]);
  }
};
```

## Next Steps

1. **Start with Rube MCP** (via `figma-automation` skill)
2. **Build custom MCP** for specific wireframe needs
3. **Add Figma plugin** for full automation
4. **Integrate with design system** via `ui-design-system` skill

## Resources

- Figma API: https://www.figma.com/developers/api
- Figma Plugin API: https://www.figma.com/plugin-docs/
- FastMCP: https://github.com/jlowin/fastmcp
- MCP SDK: https://github.com/anthropics/mcp
