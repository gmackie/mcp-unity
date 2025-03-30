import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

/**
 * Create a tool that creates a new scene in Unity
 */
export function createCreateSceneTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "create_scene";
  return {
    name: toolName,
    description: "Creates a new scene in Unity",
    paramsSchema: z.object({
      sceneName: z.string().describe("Name for the new scene"),
      scenePath: z
        .string()
        .optional()
        .describe("Optional path where to save the scene"),
      templateScene: z
        .string()
        .optional()
        .describe("Optional template scene to use as a base"),
    }),
    handler: async (params: {
      sceneName: string;
      scenePath?: string;
      templateScene?: string;
    }): Promise<CallToolResult> => {
      logger.debug("Creating scene:", params);

      const response = await mcpUnity.sendRequest({
        method: toolName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message || `Failed to create scene: ${params.sceneName}`
        );
      }

      return {
        success: true,
        message:
          response.message || `Scene created successfully: ${params.sceneName}`,
        content: [
          {
            type: "text",
            text:
              response.message ||
              `Scene ${params.sceneName} created successfully`,
          },
        ],
      };
    },
  };
}
