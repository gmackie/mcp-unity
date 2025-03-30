# Scene Management Implementation

This document summarizes the scene management features implemented in the MCP Unity server.

## Completed Features

### Tools

1. **create_scene**
   - Creates a new scene from scratch or based on a template scene
   - Parameters:
     - `sceneName` (required): Name for the new scene
     - `scenePath` (optional): Path where to save the scene
     - `templateScene` (optional): Path to a template scene to use as a base
   - Returns metadata about the created scene

2. **load_scene**
   - Loads an existing scene in the editor
   - Parameters:
     - `scenePath` (required): Path to the scene to load
     - `additive` (optional): Whether to load the scene additively
     - `saveCurrentSceneIfDirty` (optional): Whether to save current scene if dirty
   - Returns metadata about the loaded scene

3. **save_scene**
   - Saves the current scene, either at its current path or a specified path
   - Parameters:
     - `scenePath` (optional): Path where to save the scene (uses current if not specified)
     - `saveAsCopy` (optional): Whether to save as a copy
   - Returns metadata about the saved scene

4. **switch_build_scenes**
   - Modifies the build settings for scenes
   - Parameters:
     - `operation` (required): Operation type: "add", "remove", "reorder", or "set"
     - `scenePaths` (required): Array of scene paths to operate on
   - Returns the updated build settings

### Resources

1. **get_scenes**
   - Returns a list of all scenes in the project with optional filtering
   - Parameters:
     - `filter` (optional): Filter pattern for scene paths
     - `includeBuildSettings` (optional): Whether to include build settings information
     - `includeMetadata` (optional): Whether to include additional metadata
   - Returns a list of scenes with requested information

2. **get_scene_info**
   - Returns detailed information about the current scene
   - Parameters:
     - `includeGameObjects` (optional): Whether to include GameObject hierarchy
     - `includeComponents` (optional): Whether to include component details
     - `includeSettings` (optional): Whether to include scene settings
   - Returns detailed information about the current scene

## Implementation Details

### Unity-side Implementation

1. **SceneManagementTool.cs**
   - Implements all four tool methods using a method dispatcher pattern
   - Handles scene creation, loading, saving, and build settings manipulation
   - Provides detailed error handling and validation
   - Returns consistent JSON responses with metadata

2. **GetScenesResource.cs**
   - Implements the resource for listing scenes in the project
   - Uses Unity's AssetDatabase to find all scenes
   - Provides filtering and optional inclusion of build settings and metadata

3. **GetSceneInfoResource.cs**
   - Implements the resource for getting detailed information about the current scene
   - Can include GameObject hierarchy with components
   - Provides detailed scene settings (lighting, physics, navigation, audio)

### Node.js-side Implementation

1. **scene-management.js**
   - Implements the Node.js side of all four tools
   - Provides parameter validation and error handling
   - Follows the existing pattern of using `createTool` from utils.js

2. **get-scenes.js** and **get-scene-info.js**
   - Implement the Node.js side of both resources
   - Follow the existing pattern of resource implementation
   - Provide error handling and metadata formatting

### Registration

All tools and resources are registered in both:
- Unity-side: Updated McpUnityServer.cs to register all new tools and resources
- Node.js-side: Updated index.ts to register all new tools and resources (commented out for now until all dependencies are implemented)

## Usage Examples

### Creating a New Scene

```javascript
const result = await mcpServer.execute('create_scene', {
  sceneName: 'NewLevel',
  scenePath: 'Assets/Scenes/Level1.unity'
});
```

### Loading a Scene

```javascript
const result = await mcpServer.execute('load_scene', {
  scenePath: 'Assets/Scenes/Level1.unity',
  additive: false
});
```

### Saving a Scene

```javascript
const result = await mcpServer.execute('save_scene', {
  scenePath: 'Assets/Scenes/Level1_Updated.unity',
  saveAsCopy: true
});
```

### Modifying Build Settings

```javascript
const result = await mcpServer.execute('switch_build_scenes', {
  operation: 'add',
  scenePaths: ['Assets/Scenes/Level1.unity', 'Assets/Scenes/Level2.unity']
});
```

### Getting Scene List

```javascript
const scenes = await mcpServer.fetch('get_scenes', {
  includeBuildSettings: true,
  includeMetadata: true
});
```

### Getting Scene Info

```javascript
const sceneInfo = await mcpServer.fetch('get_scene_info', {
  includeGameObjects: true,
  includeComponents: true,
  includeSettings: true
});
``` 