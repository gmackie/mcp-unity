import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

export function createInstantiatePrefabTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "instantiate_prefab";

  // Create schema for Vector3 and Quaternion
  const Vector3Schema = z
    .object({
      x: z.number().optional(),
      y: z.number().optional(),
      z: z.number().optional(),
    })
    .optional();

  const QuaternionSchema = z
    .object({
      x: z.number().optional(),
      y: z.number().optional(),
      z: z.number().optional(),
      w: z.number().optional(),
    })
    .optional();

  // Create the schema as a ZodObject to match the interface requirements
  const ParamsSchema = z.object({
    prefabPath: z.string().describe("Path to the prefab asset to instantiate"),
    position: Vector3Schema.describe(
      "(Optional) World position for the instance"
    ),
    rotation: QuaternionSchema.describe("(Optional) Rotation for the instance"),
    parent: z
      .string()
      .optional()
      .describe("(Optional) Path to parent GameObject"),
    name: z
      .string()
      .optional()
      .describe("(Optional) Custom name for the instance"),
  });

  return {
    name: toolName,
    description: "Instantiates a prefab into the scene",
    paramsSchema: ParamsSchema,
    handler: async (params): Promise<CallToolResult> => {
      // Validate that the required parameters are provided
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
          response.message || `Failed to instantiate prefab`
        );
      }

      // If we have metadata, include it in the response
      const content: any[] = [
        {
          type: response.type || "prefab_instantiated",
          text: response.message || `Successfully instantiated prefab`,
        },
      ];

      if (response.metadata) {
        content.push({
          type: "prefab_instance_metadata",
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
