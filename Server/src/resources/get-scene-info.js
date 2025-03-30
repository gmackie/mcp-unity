/**
 * GetSceneInfoResource for MCP Unity
 * Retrieves detailed information about the current scene
 */

import { createResource, validateParams } from '../utils.js';

/**
 * Resource class for retrieving detailed scene information
 */
export class GetSceneInfoResource {
  constructor() {
    this.name = 'get_scene_info';
    this.description = 'Returns detailed information about the current scene';
  }

  /**
   * Fetches detailed information about the current scene
   * @param {Object} params - Query parameters
   * @param {boolean} [params.includeGameObjects=false] - Whether to include GameObject hierarchy
   * @param {boolean} [params.includeComponents=false] - Whether to include component details on GameObjects
   * @param {boolean} [params.includeSettings=false] - Whether to include scene settings
   * @returns {Promise<Object>} - Scene information
   */
  async fetch(params, ctx) {
    try {
      return await ctx.mcpUnity.fetch('get_scene_info', params);
    } catch (error) {
      ctx.logger.error('Error fetching scene info:', error);
      return { error: error.message };
    }
  }
} 