import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

export function createSaveSceneTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "save_scene";
  return {
    name: toolName,
    description: "Saves changes to the current scene",
    paramsSchema: z.object({
      scenePath: z
        .string()
        .optional()
        .describe(
          "Optional path where to save the scene (uses current path if not specified)"
        ),
      saveAsCopy: z
        .boolean()
        .optional()
        .describe("Whether to save as a copy (default: false)"),
    }),
    handler: async (params: {
      scenePath?: string;
      saveAsCopy?: boolean;
    }): Promise<CallToolResult> => {
      logger.debug("Saving scene:", params);

      const response = await mcpUnity.sendRequest({
        method: toolName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message || "Failed to save scene"
        );
      }

      return {
        success: true,
        message: response.message || "Scene saved successfully",
        content: [
          {
            type: "text",
            text: response.message || "Scene saved successfully",
          },
        ],
      };
    },
  };
}
