import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

export function createCreatePrefabTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "create_prefab";

  // Create the schema as a ZodObject to match the interface requirements
  const ParamsSchema = z.object({
    gameObjectPath: z
      .string()
      .describe(
        "The path or name of the GameObject in the scene to convert to a prefab"
      ),
    prefabPath: z
      .string()
      .describe(
        "Asset path where the prefab should be saved (relative to Assets/)"
      ),
    replaceOriginal: z
      .boolean()
      .optional()
      .default(false)
      .describe(
        "Whether to replace the original GameObject with an instance of the new prefab"
      ),
  });

  return {
    name: toolName,
    description: "Creates a prefab from an existing GameObject in the scene",
    paramsSchema: ParamsSchema,
    handler: async (params): Promise<CallToolResult> => {
      // Validate that the required parameters are provided
      if (!params.gameObjectPath) {
        throw new McpUnityError(
          ErrorType.VALIDATION,
          "Required parameter 'gameObjectPath' not provided"
        );
      }

      if (!params.prefabPath) {
        throw new McpUnityError(
          ErrorType.VALIDATION,
          "Required parameter 'prefabPath' not provided"
        );
      }

      const response = await mcpUnity.sendRequest({
        method: toolName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message || `Failed to create prefab`
        );
      }

      // If we have metadata, include it in the response
      const content: any[] = [
        {
          type: response.type || "prefab_created",
          text: response.message || `Successfully created prefab`,
        },
      ];

      if (response.metadata) {
        content.push({
          type: "prefab_metadata",
          metadata: response.metadata,
        });
      }

      return {
        success: true,
        message: response.message,
        content,
      };
    },
  };
}
