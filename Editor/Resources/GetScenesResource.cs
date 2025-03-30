using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for retrieving information about scenes in the project
    /// </summary>
    public class GetScenesResource : McpResourceBase
    {
        public GetScenesResource()
        {
            Name = "get_scenes";
            Description = "Returns a list of all scenes in the project with optional filtering";
        }

        public override JObject Fetch(JObject parameters)
        {
            try
            {
                // Extract parameters
                string filter = parameters?["filter"]?.ToString();
                bool includeBuildSettings = parameters?["includeBuildSettings"]?.ToObject<bool>() ?? false;
                bool includeMetadata = parameters?["includeMetadata"]?.ToObject<bool>() ?? false;
                
                // Find all .unity files
                string[] guids = AssetDatabase.FindAssets("t:Scene");
                List<JObject> scenesList = new List<JObject>();
                
                foreach (string guid in guids)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    
                    // Apply filter if specified
                    if (!string.IsNullOrEmpty(filter) && !scenePath.Contains(filter))
                    {
                        continue;
                    }
                    
                    // Get basic scene info
                    JObject sceneInfo = new JObject
                    {
                        ["path"] = scenePath,
                        ["name"] = Path.GetFileNameWithoutExtension(scenePath)
                    };
                    
                    // Add build settings info if requested
                    if (includeBuildSettings)
                    {
                        var buildScene = Array.Find(EditorBuildSettings.scenes, s => s.path == scenePath);
                        bool isInBuildSettings = buildScene != null;
                        int buildIndex = -1;
                        
                        if (isInBuildSettings)
                        {
                            buildIndex = Array.IndexOf(EditorBuildSettings.scenes, buildScene);
                        }
                        
                        sceneInfo["buildSettings"] = new JObject
                        {
                            ["isInBuildSettings"] = isInBuildSettings,
                            ["buildIndex"] = buildIndex,
                            ["enabled"] = isInBuildSettings && buildScene.enabled
                        };
                    }
                    
                    // Add additional metadata if requested
                    if (includeMetadata)
                    {
                        // Get last modified time
                        FileInfo fileInfo = new FileInfo(scenePath);
                        
                        // Check if scene is currently loaded
                        bool isLoaded = false;
                        bool isActive = false;
                        
                        for (int i = 0; i < SceneManager.sceneCount; i++)
                        {
                            Scene loadedScene = SceneManager.GetSceneAt(i);
                            if (loadedScene.path == scenePath)
                            {
                                isLoaded = true;
                                isActive = loadedScene == SceneManager.GetActiveScene();
                                break;
                            }
                        }
                        
                        sceneInfo["metadata"] = new JObject
                        {
                            ["fileSize"] = fileInfo.Length,
                            ["lastModified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            ["isLoaded"] = isLoaded,
                            ["isActive"] = isActive
                        };
                    }
                    
                    scenesList.Add(sceneInfo);
                }
                
                // Sort scenes by name
                scenesList = scenesList.OrderBy(s => s["name"]?.ToString()).ToList();
                
                // Create the response
                return new JObject
                {
                    ["success"] = true,
                    ["count"] = scenesList.Count,
                    ["scenes"] = new JArray(scenesList)
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching scenes: {ex.Message}");
                return new JObject
                {
                    ["error"] = ex.Message
                };
            }
        }
    }
} 