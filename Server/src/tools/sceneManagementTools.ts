import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { Logger, LogLevel } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { createCreateSceneTool } from "./createSceneTool.js";
import { createLoadSceneTool } from "./loadSceneTool.js";
import { createSaveSceneTool } from "./saveSceneTool.js";
import { createSwitchBuildScenesTool } from "./switchBuildScenesTool.js";
import { ToolRegistry } from "./toolRegistry.js";

/**
 * Register all scene management tools with the tool registry
 */
export function registerSceneManagementTools(
  toolRegistry: ToolRegistry,
  mcpUnity: McpUnity,
  logger: Logger
) {
  const toolLogger = new Logger("SceneManagementTools", LogLevel.INFO);

  // Create and register each tool
  toolRegistry.add(createCreateSceneTool(mcpUnity, toolLogger));
  toolRegistry.add(createLoadSceneTool(mcpUnity, toolLogger));
  toolRegistry.add(createSaveSceneTool(mcpUnity, toolLogger));
  toolRegistry.add(createSwitchBuildScenesTool(mcpUnity, toolLogger));
}
