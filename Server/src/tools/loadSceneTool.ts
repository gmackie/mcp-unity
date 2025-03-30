import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

export function createLoadSceneTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "load_scene";
  return {
    name: toolName,
    description: "Loads an existing scene in the Unity editor",
    paramsSchema: z.object({
      scenePath: z.string().describe("Path to the scene to load"),
      additive: z
        .boolean()
        .optional()
        .describe("Whether to load the scene additively (default: false)"),
      saveCurrentSceneIfDirty: z
        .boolean()
        .optional()
        .describe(
          "Whether to save the current scene if it has unsaved changes (default: true)"
        ),
    }),
    handler: async (params: {
      scenePath: string;
      additive?: boolean;
      saveCurrentSceneIfDirty?: boolean;
    }): Promise<CallToolResult> => {
      logger.debug("Loading scene:", params);

      const response = await mcpUnity.sendRequest({
        method: toolName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message || `Failed to load scene: ${params.scenePath}`
        );
      }

      return {
        success: true,
        message:
          response.message || `Scene loaded successfully: ${params.scenePath}`,
        content: [
          {
            type: "text",
            text:
              response.message ||
              `Scene ${params.scenePath} loaded successfully`,
          },
        ],
      };
    },
  };
}
