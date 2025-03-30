import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

export function createSwitchBuildScenesTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "switch_build_scenes";
  return {
    name: toolName,
    description: "Add/remove scenes from the build settings and reorder them",
    paramsSchema: z.object({
      operation: z
        .enum(["add", "remove", "reorder", "set"])
        .describe("Operation type: add, remove, reorder, or set"),
      scenePaths: z
        .array(z.string())
        .describe("Array of scene paths to operate on"),
    }),
    handler: async (params: {
      operation: "add" | "remove" | "reorder" | "set";
      scenePaths: string[];
    }): Promise<CallToolResult> => {
      logger.debug("Modifying build settings:", params);

      const response = await mcpUnity.sendRequest({
        method: toolName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message ||
            `Failed to ${params.operation} scenes in build settings`
        );
      }

      return {
        success: true,
        message:
          response.message ||
          `Build settings updated successfully using operation: ${params.operation}`,
        content: [
          {
            type: "text",
            text:
              response.message ||
              `Build settings updated successfully using operation: ${params.operation}`,
          },
        ],
      };
    },
  };
}
