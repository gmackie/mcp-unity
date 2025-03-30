using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.IO;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating prefabs from existing GameObjects in the scene
    /// </summary>
    public class CreatePrefabTool : McpToolBase
    {
        public CreatePrefabTool()
        {
            Name = "create_prefab";
            Description = "Creates a prefab from an existing GameObject in the scene";
        }
        
        /// <summary>
        /// Execute the CreatePrefab tool with the provided parameters asynchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
        {
            // Extract parameters
            string gameObjectPath = parameters["gameObjectPath"]?.ToObject<string>();
            string prefabPath = parameters["prefabPath"]?.ToObject<string>();
            bool replaceOriginal = parameters["replaceOriginal"]?.ToObject<bool>() ?? false;
            
            // Validate parameters
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'gameObjectPath' not provided", 
                    "validation_error"
                );
            }
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'prefabPath' not provided", 
                    "validation_error"
                );
            }
            
            // Find the GameObject
            GameObject sourceObject = GameObject.Find(gameObjectPath);
            if (sourceObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found at path: {gameObjectPath}", 
                    "not_found_error"
                );
            }
            
            try
            {
                // Ensure the target directory exists
                string directory = Path.GetDirectoryName(prefabPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists($"Assets/{directory}"))
                {
                    Directory.CreateDirectory($"Assets/{directory}");
                    AssetDatabase.Refresh();
                }
                
                // Create the full asset path
                string assetPath = $"Assets/{prefabPath}.prefab";
                
                // Check if the prefab already exists
                bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
                
                // Create the prefab
                GameObject prefabAsset;
                
                if (prefabExists)
                {
                    // If the prefab exists, we use SaveAsPrefabAssetAndConnect for existing prefabs
                    prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(sourceObject, assetPath, InteractionMode.UserAction);
                }
                else
                {
                    // If the prefab doesn't exist, create a new one
                    prefabAsset = PrefabUtility.SaveAsPrefabAsset(sourceObject, assetPath);
                }
                
                // If we don't want to replace the original, we need to break the prefab connection
                if (!replaceOriginal && prefabAsset != null)
                {
                    PrefabUtility.UnpackPrefabInstance(sourceObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                }
                
                // Get prefab metadata
                var metadata = new JObject
                {
                    ["path"] = assetPath,
                    ["name"] = prefabAsset.name,
                    ["instanceID"] = prefabAsset.GetInstanceID()
                };
                
                // Create the response
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"Successfully created prefab at: {assetPath}",
                    ["type"] = "prefab_created",
                    ["metadata"] = metadata
                };
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error creating prefab: {ex.Message}", 
                    "execution_error"
                );
            }
        }
        
        /// <summary>
        /// Get the full path to a GameObject in the hierarchy
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            
            return path;
        }
    }
} 