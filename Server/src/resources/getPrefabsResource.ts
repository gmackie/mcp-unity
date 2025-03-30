import { z } from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ResourceDefinition } from "./resourceRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";

export function createGetPrefabsResource(
  mcpUnity: McpUnity,
  logger: Logger
): ResourceDefinition {
  const resourceName = "get_prefabs";

  // Create the schema as a ZodObject to match the interface requirements
  const ParamsSchema = z.object({
    filter: z.string().optional().describe("Filter to apply (by name or path)"),
    includeComponents: z
      .boolean()
      .optional()
      .default(false)
      .describe("Whether to include component data"),
    maxDepth: z
      .number()
      .optional()
      .describe("How deep to traverse the prefab hierarchy, -1 means no limit"),
  });

  return {
    name: resourceName,
    description:
      "Retrieves a list of all prefabs in the project with their properties",
    paramsSchema: ParamsSchema,
    fetch: async (params) => {
      try {
        logger.info(`Fetching prefabs with filter: ${params.filter || "none"}`);

        const response = await mcpUnity.sendRequest({
          method: resourceName,
          params,
        });

        if (response.error) {
          throw new McpUnityError(
            ErrorType.RESOURCE_FETCH,
            response.error || "Failed to fetch prefabs"
          );
        }

        logger.info(`Fetched ${response.count || 0} prefabs`);

        return {
          prefabs: response.prefabs || [],
          count: response.count || 0,
        };
      } catch (error: any) {
        logger.error(`Error fetching prefabs: ${error.message}`, error);
        throw new McpUnityError(
          ErrorType.RESOURCE_FETCH,
          `Failed to fetch prefabs: ${error.message}`
        );
      }
    },
  };
}
