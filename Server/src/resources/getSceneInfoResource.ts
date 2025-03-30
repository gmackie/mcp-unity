import { McpUnity } from "../unity/mcpUnity.js";
import { Logger } from "../utils/logger.js";
import { ResourceDefinition } from "./resourceRegistry.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { ReadResourceResult } from "@modelcontextprotocol/sdk/types.js";

export function createGetSceneInfoResource(
  mcpUnity: McpUnity,
  logger: Logger
): ResourceDefinition {
  const resourceName = "get_scene_info";
  const resourceUri = `unity://${resourceName}`;
  const resourceMimeType = "application/json";

  return {
    name: resourceName,
    uri: resourceUri,
    metadata: {
      description: "Detailed information about the current scene",
      mimeType: resourceMimeType,
    },
    handler: async (params: {
      includeGameObjects?: boolean;
      includeComponents?: boolean;
      includeSettings?: boolean;
    }): Promise<ReadResourceResult> => {
      logger.debug("Fetching scene info with params:", params);

      const response = await mcpUnity.sendRequest({
        method: resourceName,
        params,
      });

      if (!response.success) {
        throw new McpUnityError(
          ErrorType.RESOURCE_FETCH,
          response.message || "Failed to fetch scene info from Unity"
        );
      }

      return {
        contents: [
          {
            uri: resourceUri,
            mimeType: resourceMimeType,
            // Convert the scene info to a formatted JSON string
            text: JSON.stringify(response, null, 2),
          },
        ],
      };
    },
  };
}
