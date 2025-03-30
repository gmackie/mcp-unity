/**
 * GetScenesResource for MCP Unity
 * Retrieves a list of all scenes in the project with optional filtering
 */

import { createResource, validateParams } from '../utils.js';

/**
 * Resource class for retrieving scene information
 */
export class GetScenesResource {
  constructor() {
    this.name = 'get_scenes';
    this.description = 'Returns a list of all scenes in the project';
  }

  /**
   * Fetches scenes information based on provided parameters
   * @param {Object} params - Query parameters
   * @param {string} [params.filter] - Optional filter pattern for scene paths
   * @param {boolean} [params.includeBuildSettings=false] - Whether to include build settings information
   * @param {boolean} [params.includeMetadata=false] - Whether to include additional metadata
   * @returns {Promise<Object>} - Scene information
   */
  async fetch(params, ctx) {
    // We don't have required params, but validate any optional ones
    
    try {
      return await ctx.mcpUnity.fetch('get_scenes', params);
    } catch (error) {
      ctx.logger.error('Error fetching scenes:', error);
      return { error: error.message };
    }
  }
} 