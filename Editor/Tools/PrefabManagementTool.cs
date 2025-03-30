using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for managing prefabs in the Unity Editor
    /// </summary>
    public class PrefabManagementTool : McpToolBase
    {
        // Dictionary to map method names to handler methods
        private readonly Dictionary<string, Func<JObject, Task<JObject>>> _methodHandlers;
        
        public PrefabManagementTool()
        {
            Name = "create_prefab";
            Description = "Creates a prefab from an existing GameObject in the scene";
            
            // Initialize method handlers
            _methodHandlers = new Dictionary<string, Func<JObject, Task<JObject>>>
            {
                { "create_prefab", CreatePrefabAsync },
                { "instantiate_prefab", InstantiatePrefabAsync }
            };
        }
        
        /// <summary>
        /// Execute the PrefabManagement tool with the provided parameters asynchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override Task<JObject> ExecuteAsync(JObject parameters)
        {
            // Get the method name from the Name property
            string methodName = Name;
            
            // Check if we have a handler for this method
            if (_methodHandlers.TryGetValue(methodName, out var handler))
            {
                return handler(parameters);
            }
            
            // Return an error if method is not supported
            return Task.FromResult(McpUnitySocketHandler.CreateErrorResponse(
                $"Method '{methodName}' is not supported by PrefabManagementTool", 
                "method_not_supported"
            ));
        }
        
        /// <summary>
        /// Creates a prefab from an existing GameObject
        /// </summary>
        private async Task<JObject> CreatePrefabAsync(JObject parameters)
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
        /// Instantiates a prefab in the scene
        /// </summary>
        private async Task<JObject> InstantiatePrefabAsync(JObject parameters)
        {
            // Extract parameters
            string prefabPath = parameters["prefabPath"]?.ToObject<string>();
            Vector3? position = parameters["position"]?.ToObject<JObject>()?.ToVector3();
            Quaternion? rotation = parameters["rotation"]?.ToObject<JObject>()?.ToQuaternion();
            string parentPath = parameters["parent"]?.ToObject<string>();
            string instanceName = parameters["name"]?.ToObject<string>();
            
            // Validate parameters
            if (string.IsNullOrEmpty(prefabPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'prefabPath' not provided", 
                    "validation_error"
                );
            }
            
            try
            {
                // Ensure the prefab path has proper format
                string assetPath = prefabPath;
                if (!prefabPath.StartsWith("Assets/"))
                {
                    assetPath = $"Assets/{prefabPath}";
                }
                
                // If it doesn't end with .prefab, add it
                if (!assetPath.EndsWith(".prefab"))
                {
                    assetPath = $"{assetPath}.prefab";
                }
                
                // Load the prefab asset
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefabAsset == null)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Prefab not found at path: {assetPath}", 
                        "not_found_error"
                    );
                }
                
                // Find parent if specified
                Transform parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    GameObject parentObject = GameObject.Find(parentPath);
                    if (parentObject == null)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"Parent GameObject not found at path: {parentPath}", 
                            "not_found_error"
                        );
                    }
                    parent = parentObject.transform;
                }
                
                // Instantiate the prefab
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (instance == null)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Failed to instantiate prefab: {assetPath}", 
                        "instantiation_error"
                    );
                }
                
                // Set position if provided
                if (position.HasValue)
                {
                    instance.transform.position = position.Value;
                }
                
                // Set rotation if provided
                if (rotation.HasValue)
                {
                    instance.transform.rotation = rotation.Value;
                }
                
                // Set parent if provided
                if (parent != null)
                {
                    instance.transform.SetParent(parent, true);
                }
                
                // Set name if provided
                if (!string.IsNullOrEmpty(instanceName))
                {
                    instance.name = instanceName;
                }
                
                // Get instance metadata
                var metadata = new JObject
                {
                    ["path"] = GetGameObjectPath(instance),
                    ["name"] = instance.name,
                    ["instanceID"] = instance.GetInstanceID(),
                    ["prefabPath"] = assetPath
                };
                
                // Create the response
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"Successfully instantiated prefab: {assetPath}",
                    ["type"] = "prefab_instantiated",
                    ["metadata"] = metadata
                };
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error instantiating prefab: {ex.Message}", 
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
    
    /// <summary>
    /// Extension methods for JObject to convert to Unity types
    /// </summary>
    public static class JObjectExtensions
    {
        /// <summary>
        /// Convert a JObject to a Vector3
        /// </summary>
        public static Vector3 ToVector3(this JObject jObject)
        {
            float x = jObject["x"]?.ToObject<float>() ?? 0;
            float y = jObject["y"]?.ToObject<float>() ?? 0;
            float z = jObject["z"]?.ToObject<float>() ?? 0;
            
            return new Vector3(x, y, z);
        }
        
        /// <summary>
        /// Convert a JObject to a Quaternion
        /// </summary>
        public static Quaternion ToQuaternion(this JObject jObject)
        {
            float x = jObject["x"]?.ToObject<float>() ?? 0;
            float y = jObject["y"]?.ToObject<float>() ?? 0;
            float z = jObject["z"]?.ToObject<float>() ?? 0;
            float w = jObject["w"]?.ToObject<float>() ?? 1;
            
            return new Quaternion(x, y, z, w);
        }
    }
} 