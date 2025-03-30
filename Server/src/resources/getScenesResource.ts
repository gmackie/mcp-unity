import { McpUnity } from "../unity/mcpUnity.js";
import { Logger } from "../utils/logger.js";
import { ResourceDefinition } from "./resourceRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { ReadResourceResult } from "@modelcontextprotocol/sdk/types.js";

export function createGetScenesResource(
  mcpUnity: McpUnity,
  logger: Logger
): ResourceDefinition {
  const resourceName = "get_scenes";
  const resourceUri = `unity://${resourceName}`;
  const resourceMimeType = "application/json";

  return {
    name: resourceName,
    uri: resourceUri,
    metadata: {
      description: "List of all scenes in the project with optional filtering",
      mimeType: resourceMimeType,
    },
    handler: async (params: {
      filter?: string;
      includeBuildSettings?: boolean;
      includeMetadata?: boolean;
    }): Promise<ReadResourceResult> => {
      logger.debug("Fetching scenes with params:", params);

      const response = await mcpUnity.sendRequest({
        method: resourceName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.RESOURCE_FETCH,
          response.message || "Failed to fetch scenes from Unity"
        );
      }

      return {
        contents: [
          {
            uri: resourceUri,
            mimeType: resourceMimeType,
            // Convert the scenes array to a formatted JSON string
            text: JSON.stringify(
              {
                count: response.count,
                scenes: response.scenes,
              },
              null,
              2
            ),
          },
        ],
      };
    },
  };
}
