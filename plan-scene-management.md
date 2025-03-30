# Scene Management Implementation Plan

## 1. Features to Implement

Based on the roadmap, we'll implement the following scene management components:

### Tools
- **create_scene**: Create new scenes
- **load_scene**: Load existing scenes in the editor
- **save_scene**: Save changes to scenes
- **switch_build_scenes**: Add/remove scenes from the build settings and reorder them

### Resources
- **get_scene_info**: Retrieve detailed information about the current scene
- **get_scenes**: List all scenes in the project with metadata

## 2. Implementation Approach

### 2.1 Unity-side Implementation

#### Scene Management Tools
Create a new `SceneManagementTool.cs` in the `Editor/Tools` directory:
- Implement a method dispatcher pattern similar to `PrefabManagementTool.cs`
- Handle create, load, save and switch build scenes operations
- Ensure proper error handling and validation

#### Scene Information Resources
Create a new `GetScenesResource.cs` and `GetSceneInfoResource.cs` in the `Editor/Resources` directory:
- Implement scene listing with metadata
- Implement current scene information retrieval
- Include relevant metadata like scene objects, lighting settings, etc.

### 2.2 Node.js-side Implementation

Create corresponding TypeScript files in the appropriate directories:
- `Server/src/tools/createSceneTool.ts`
- `Server/src/tools/loadSceneTool.ts`
- `Server/src/tools/saveSceneTool.ts`
- `Server/src/tools/switchBuildScenesTool.ts`
- `Server/src/resources/getScenesResource.ts`
- `Server/src/resources/getSceneInfoResource.ts`

Each implementation should:
- Follow the existing architectural patterns
- Define appropriate parameter schemas with Zod
- Implement proper validation and error handling
- Forward requests to Unity and format responses

### 2.3 Registration

Register all tools and resources:
- Update `McpUnityServer.cs` to register all scene management tools and resources
- Update `index.ts` to register all scene management tools and resources

## 3. Technical Details

### 3.1 create_scene Tool

**Parameters:**
- `sceneName`: Name for the new scene
- `scenePath`: (Optional) Asset path where the scene should be saved (relative to Assets/)
- `template`: (Optional) Template to use (Empty or Default)

**Implementation Notes:**
- Use `EditorSceneManager.NewScene()` for creation
- Support different scene templates
- Return metadata about the created scene

### 3.2 load_scene Tool

**Parameters:**
- `scenePath`: Path to the scene to load
- `additiveMode`: (Optional) Whether to load in additive mode, default is false
- `saveCurrentBeforeLoad`: (Optional) Whether to save the current scene before loading, default is true

**Implementation Notes:**
- Use `EditorSceneManager.OpenScene()` for loading
- Support both single and additive loading modes
- Handle saving current scene if needed
- Return metadata about the loaded scene

### 3.3 save_scene Tool

**Parameters:**
- `scenePath`: (Optional) Path where the scene should be saved
- `saveAsCopy`: (Optional) Whether to save as a copy, default is false

**Implementation Notes:**
- Use `EditorSceneManager.SaveScene()` or `EditorSceneManager.SaveSceneAs()`
- Handle save as copy
- Return metadata about the saved scene

### 3.4 switch_build_scenes Tool

**Parameters:**
- `operation`: Type of operation ("add", "remove", "reorder", "set")
- `scenePaths`: Array of scene paths to operate on
- `indices`: (Optional) Array of indices for reordering scenes

**Implementation Notes:**
- Use `EditorBuildSettings.scenes` for manipulating build settings
- Support different operations on build settings
- Return updated build settings

### 3.5 get_scene_info Resource

**Parameters:**
- `includeGameObjects`: (Optional) Whether to include GameObject hierarchy
- `includeComponents`: (Optional) Whether to include component details on GameObjects
- `includeSettings`: (Optional) Whether to include scene settings like lighting, physics, etc.

**Implementation Notes:**
- Use `SceneManager.GetActiveScene()` to get current scene
- Extract relevant metadata based on parameters
- Return detailed information about the scene

### 3.6 get_scenes Resource

**Parameters:**
- `filter`: (Optional) Filter to apply to scene names
- `includeBuildSettings`: (Optional) Whether to include build settings information
- `includeMetadata`: (Optional) Whether to include additional metadata like dependencies

**Implementation Notes:**
- Use `AssetDatabase.FindAssets("t:Scene")` to list scenes
- Include build settings information if requested
- Return list of scenes with metadata

## 4. Implementation Order

1. Implement Unity-side `SceneManagementTool.cs` with the four tool methods
2. Implement Unity-side resources `GetScenesResource.cs` and `GetSceneInfoResource.cs`
3. Implement Node.js-side tools and register them
4. Implement Node.js-side resources and register them
5. Update documentation to include new scene management features

## 5. Testing Plan

- Create new scenes with different templates
- Load existing scenes in both single and additive modes
- Save scenes with different paths
- Manipulate build settings by adding/removing/reordering scenes
- Retrieve scene information with different parameter combinations
- List all project scenes with different filtering options 