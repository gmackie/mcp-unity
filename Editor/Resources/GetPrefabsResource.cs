using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for retrieving prefab information from the Unity project
    /// </summary>
    public class GetPrefabsResource : McpResourceBase
    {
        public GetPrefabsResource()
        {
            Name = "get_prefabs";
            Description = "Retrieves a list of all prefabs in the project with their properties";
        }
        
        /// <summary>
        /// Fetch prefab information based on the provided parameters
        /// </summary>
        /// <param name="parameters">Parameters for filtering and configuring the prefab retrieval</param>
        /// <returns>JSON object containing prefab information</returns>
        public override JObject Fetch(JObject parameters)
        {
            try
            {
                // Extract parameters
                string filter = parameters["filter"]?.ToObject<string>();
                bool includeComponents = parameters["includeComponents"]?.ToObject<bool>() ?? false;
                int maxDepth = parameters["maxDepth"]?.ToObject<int>() ?? -1;
                
                // Find all prefab assets in the project
                string[] guids = AssetDatabase.FindAssets("t:prefab", null);
                
                // Create a list to hold prefab data
                var prefabsList = new List<JObject>();
                
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    
                    // Skip if not a valid prefab
                    if (prefabAsset == null)
                        continue;
                    
                    // Skip if filter is provided and doesn't match
                    if (!string.IsNullOrEmpty(filter) && 
                        !prefabAsset.name.Contains(filter) && 
                        !assetPath.Contains(filter))
                        continue;
                    
                    // Create basic prefab data
                    var prefabData = new JObject
                    {
                        ["path"] = assetPath,
                        ["name"] = prefabAsset.name,
                        ["instanceID"] = prefabAsset.GetInstanceID()
                    };
                    
                    // Add dependency information
                    string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
                    prefabData["dependencies"] = new JArray(dependencies);
                    
                    // Add size information
                    prefabData["fileSize"] = new System.IO.FileInfo(assetPath).Length;
                    
                    // Get prefab type information
                    PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabAsset);
                    prefabData["prefabType"] = prefabType.ToString();
                    
                    // If requested, include component information
                    if (includeComponents)
                    {
                        var componentsArray = new JArray();
                        AddComponents(prefabAsset, componentsArray, 0, maxDepth);
                        prefabData["components"] = componentsArray;
                    }
                    
                    prefabsList.Add(prefabData);
                }
                
                // Return the results
                return new JObject
                {
                    ["prefabs"] = new JArray(prefabsList),
                    ["count"] = prefabsList.Count
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Unity] Error fetching prefabs: {ex.Message}");
                return new JObject
                {
                    ["error"] = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Recursively add components to the components array
        /// </summary>
        private void AddComponents(GameObject gameObject, JArray componentsArray, int currentDepth, int maxDepth)
        {
            // Stop recursion if max depth reached (except when maxDepth is -1, which means no limit)
            if (maxDepth != -1 && currentDepth > maxDepth)
                return;
            
            // Add components on this GameObject
            Component[] components = gameObject.GetComponents<Component>();
            
            foreach (Component component in components)
            {
                if (component == null) continue;
                
                var componentData = new JObject
                {
                    ["type"] = component.GetType().Name,
                    ["enabled"] = component is Behaviour behaviour ? behaviour.enabled : true
                };
                
                componentsArray.Add(componentData);
            }
            
            // Add child GameObjects
            foreach (Transform child in gameObject.transform)
            {
                var childData = new JObject
                {
                    ["name"] = child.gameObject.name,
                    ["components"] = new JArray()
                };
                
                AddComponents(child.gameObject, (JArray)childData["components"], currentDepth + 1, maxDepth);
                
                if (((JArray)childData["components"]).Count > 0)
                {
                    componentsArray.Add(new JObject
                    {
                        ["child"] = childData
                    });
                }
            }
        }
    }
} 