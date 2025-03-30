import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ToolDefinition } from "./toolRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

export function createUpdatePrefabTool(
  mcpUnity: McpUnity,
  logger: Logger
): ToolDefinition {
  const toolName = "update_prefab";

  // Component update schema
  const ComponentUpdateSchema = z.object({
    componentPath: z
      .string()
      .optional()
      .describe(
        "Path to the component, including GameObject path and component name"
      ),
    componentType: z
      .string()
      .optional()
      .describe(
        "Type name of the component, alternatively can specify full path"
      ),
    properties: z
      .record(z.any())
      .describe("Object containing property names and their values"),
  });

  // Create the schema as a ZodObject to match the interface requirements
  const ParamsSchema = z.object({
    prefabPath: z.string().describe("Path to the prefab to update"),
    componentUpdates: z
      .array(ComponentUpdateSchema)
      .describe("Array of component property updates to apply"),
    applyToAllInstances: z
      .boolean()
      .optional()
      .default(true)
      .describe("Whether to apply changes to all instances in the scene"),
    instancePath: z
      .string()
      .optional()
      .describe(
        "Path to a specific prefab instance to update (if not updating the prefab asset)"
      ),
    instanceId: z
      .number()
      .optional()
      .describe(
        "Instance ID of a specific prefab instance to update (if not updating the prefab asset)"
      ),
  });

  return {
    name: toolName,
    description: "Modifies prefab properties and applies changes",
    paramsSchema: ParamsSchema,
    handler: async (params): Promise<CallToolResult> => {
      // Validate that the required parameters are provided
      if (!params.prefabPath) {
        throw new McpUnityError(
          ErrorType.VALIDATION,
          "Required parameter 'prefabPath' not provided"
        );
      }

      if (!params.componentUpdates || params.componentUpdates.length === 0) {
        throw new McpUnityError(
          ErrorType.VALIDATION,
          "Required parameter 'componentUpdates' not provided or empty"
        );
      }

      // Validate each component update
      for (const update of params.componentUpdates) {
        if (
          (!update.componentPath && !update.componentType) ||
          !update.properties
        ) {
          throw new McpUnityError(
            ErrorType.VALIDATION,
            "Each component update must have either componentPath or componentType, and properties"
          );
        }
      }

      const response = await mcpUnity.sendRequest({
        method: toolName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          response.message || `Failed to update prefab`
        );
      }

      // If we have metadata, include it in the response
      const content: any[] = [
        {
          type: response.type || "prefab_updated",
          text: response.message || `Successfully updated prefab`,
        },
      ];

      if (response.metadata) {
        content.push({
          type: "prefab_update_metadata",
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
