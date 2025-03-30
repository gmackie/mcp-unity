import { Logger, LogLevel } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ResourceRegistry } from "./resourceRegistry.js";
import { createGetScenesResource } from "./getScenesResource.js";
import { createGetSceneInfoResource } from "./getSceneInfoResource.js";

/**
 * Register all scene management resources with the resource registry
 */
export function registerSceneManagementResources(
  resourceRegistry: ResourceRegistry,
  mcpUnity: McpUnity,
  logger: Logger
) {
  const resourceLogger = new Logger("SceneManagementResources", LogLevel.INFO);

  // Create and register each resource
  resourceRegistry.add(createGetScenesResource(mcpUnity, resourceLogger));
  resourceRegistry.add(createGetSceneInfoResource(mcpUnity, resourceLogger));
}
