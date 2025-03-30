/**
 * Scene Management Tools for MCP Unity
 * Handles operations related to Unity scenes
 */

import { createTool, validateParams } from '../utils.js';

/**
 * Creates a new scene with specified parameters
 * @param {object} params - Parameters for creating a scene
 * @param {string} params.sceneName - Name for the new scene
 * @param {string} [params.scenePath] - Optional path where to save the scene
 * @param {string} [params.templateScene] - Optional template scene to use as a base
 * @returns {Promise<object>} - Result with scene data
 */
const createScene = createTool({
  name: 'create_scene',
  description: 'Creates a new scene in Unity',
  handler: async (params, ctx) => {
    validateParams(params, ['sceneName']);
    
    return ctx.mcpUnity.execute('create_scene', params);
  }
});

/**
 * Loads an existing scene in the editor
 * @param {object} params - Parameters for loading a scene
 * @param {string} params.scenePath - Path to the scene to load
 * @param {boolean} [params.additive=false] - Whether to load the scene additively
 * @param {boolean} [params.saveCurrentSceneIfDirty=true] - Whether to save the current scene if it has unsaved changes
 * @returns {Promise<object>} - Result with scene data
 */
const loadScene = createTool({
  name: 'load_scene',
  description: 'Loads an existing scene in the Unity editor',
  handler: async (params, ctx) => {
    validateParams(params, ['scenePath']);
    
    return ctx.mcpUnity.execute('load_scene', params);
  }
});

/**
 * Saves the current scene
 * @param {object} params - Parameters for saving a scene
 * @param {string} [params.scenePath] - Optional path where to save the scene (uses current path if not specified)
 * @param {boolean} [params.saveAsCopy=false] - Whether to save as a copy
 * @returns {Promise<object>} - Result with scene data
 */
const saveScene = createTool({
  name: 'save_scene',
  description: 'Saves changes to the current scene',
  handler: async (params, ctx) => {
    return ctx.mcpUnity.execute('save_scene', params);
  }
});

/**
 * Modifies the build settings for scenes
 * @param {object} params - Parameters for switching build scenes
 * @param {string} params.operation - Operation type: "add", "remove", "reorder", or "set"
 * @param {string[]} params.scenePaths - Array of scene paths to operate on
 * @returns {Promise<object>} - Result with updated build settings
 */
const switchBuildScenes = createTool({
  name: 'switch_build_scenes',
  description: 'Add/remove scenes from the build settings and reorder them',
  handler: async (params, ctx) => {
    validateParams(params, ['operation', 'scenePaths']);
    
    // Validate operation type
    const validOperations = ['add', 'remove', 'reorder', 'set'];
    if (!validOperations.includes(params.operation)) {
      throw new Error(`Invalid operation: ${params.operation}. Must be one of: ${validOperations.join(', ')}`);
    }
    
    return ctx.mcpUnity.execute('switch_build_scenes', params);
  }
});

/**
 * Register all scene management tools with the server
 * @param {object} server - MCP Server instance
 */
export function registerSceneManagementTools(server) {
  server.registerTool(createScene);
  server.registerTool(loadScene);
  server.registerTool(saveScene);
  server.registerTool(switchBuildScenes);
} 