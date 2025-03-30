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
    /// Tool for managing prefabs in the Unity Editor
    /// </summary>
    public class PrefabManagementTool : McpToolBase
    {
        public PrefabManagementTool()
        {
            Name = "create_prefab";
            Description = "Creates a prefab from an existing GameObject in the scene";
        }
        
        /// <summary>
        /// Execute the CreatePrefab tool with the provided parameters asynchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override Task<JObject> ExecuteAsync(JObject parameters)
        {
            // Extract parameters
            string gameObjectPath = parameters["gameObjectPath"]?.ToObject<string>();
            string prefabPath = parameters["prefabPath"]?.ToObject<string>();
            bool replaceOriginal = parameters["replaceOriginal"]?.ToObject<bool>() ?? false;
            
            // Validate parameters
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return Task.FromResult(McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'gameObjectPath' not provided", 
                    "validation_error"
                ));
            }
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                return Task.FromResult(McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'prefabPath' not provided", 
                    "validation_error"
                ));
            }
            
            // Find the GameObject
            GameObject sourceObject = GameObject.Find(gameObjectPath);
            if (sourceObject == null)
            {
                return Task.FromResult(McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found at path: {gameObjectPath}", 
                    "not_found_error"
                ));
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
                return Task.FromResult(new JObject
                {
                    ["success"] = true,
                    ["message"] = $"Successfully created prefab at: {assetPath}",
                    ["type"] = "prefab_created",
                    ["metadata"] = metadata
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(McpUnitySocketHandler.CreateErrorResponse(
                    $"Error creating prefab: {ex.Message}", 
                    "execution_error"
                ));
            }
        }
    }
} 