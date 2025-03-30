using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for instantiating prefabs into the scene
    /// </summary>
    public class InstantiatePrefabTool : McpToolBase
    {
        public InstantiatePrefabTool()
        {
            Name = "instantiate_prefab";
            Description = "Instantiates prefabs into the scene";
        }
        
        /// <summary>
        /// Execute the InstantiatePrefab tool with the provided parameters asynchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
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
} 