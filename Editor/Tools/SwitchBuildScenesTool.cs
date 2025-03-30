using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for manipulating build settings for scenes in Unity Editor
    /// </summary>
    public class SwitchBuildScenesTool : McpToolBase
    {
        public SwitchBuildScenesTool()
        {
            Name = "switch_build_scenes";
            Description = "Add/remove scenes from the build settings and reorder them";
        }

        /// <summary>
        /// Manipulates build settings with the provided parameters
        /// </summary>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
        {
            try
            {
                // Validate parameters
                if (!parameters.TryGetValue("operation", out JToken operationToken) || string.IsNullOrEmpty(operationToken.ToString()))
                {
                    return JObject.FromObject(new
                    {
                        error = "operation is required and must be one of: add, remove, reorder, set"
                    });
                }
                
                string operation = operationToken.ToString().ToLower();
                if (!new[] { "add", "remove", "reorder", "set" }.Contains(operation))
                {
                    return JObject.FromObject(new
                    {
                        error = "operation must be one of: add, remove, reorder, set"
                    });
                }
                
                if (!parameters.TryGetValue("scenePaths", out JToken scenePathsToken) || scenePathsToken.Type != JTokenType.Array)
                {
                    return JObject.FromObject(new
                    {
                        error = "scenePaths is required and must be an array"
                    });
                }
                
                // Convert JToken array to string array
                string[] scenePaths = scenePathsToken.ToObject<string[]>();
                
                // Check if all paths exist for operations that require it
                if (operation != "remove")
                {
                    foreach (string path in scenePaths)
                    {
                        if (!File.Exists(path))
                        {
                            return JObject.FromObject(new
                            {
                                error = $"Scene file does not exist: {path}"
                            });
                        }
                    }
                }
                
                // Get current build scenes
                EditorBuildSettingsScene[] currentBuildScenes = EditorBuildSettings.scenes;
                List<EditorBuildSettingsScene> newBuildScenes = new List<EditorBuildSettingsScene>();
                
                switch (operation)
                {
                    case "add":
                        // Add new scenes to the build settings
                        newBuildScenes.AddRange(currentBuildScenes);
                        foreach (string path in scenePaths)
                        {
                            // Skip if already in build settings
                            if (newBuildScenes.Any(s => s.path == path))
                            {
                                continue;
                            }
                            
                            newBuildScenes.Add(new EditorBuildSettingsScene(path, true));
                        }
                        break;
                        
                    case "remove":
                        // Remove specified scenes from build settings
                        newBuildScenes.AddRange(currentBuildScenes.Where(s => !scenePaths.Contains(s.path)));
                        break;
                        
                    case "reorder":
                        // Create a new order based on the provided array
                        // First add all scenes from the provided paths (preserving their order)
                        foreach (string path in scenePaths)
                        {
                            EditorBuildSettingsScene existingScene = currentBuildScenes.FirstOrDefault(s => s.path == path);
                            if (existingScene != null)
                            {
                                newBuildScenes.Add(existingScene);
                            }
                        }
                        
                        // Then add any remaining scenes that weren't in the provided paths
                        newBuildScenes.AddRange(currentBuildScenes.Where(s => !scenePaths.Contains(s.path)));
                        break;
                        
                    case "set":
                        // Replace all build settings with the provided scenes
                        foreach (string path in scenePaths)
                        {
                            newBuildScenes.Add(new EditorBuildSettingsScene(path, true));
                        }
                        break;
                }
                
                // Apply the new build settings
                EditorBuildSettings.scenes = newBuildScenes.ToArray();
                
                // Create a response with the updated build settings
                return JObject.FromObject(new
                {
                    success = true,
                    message = $"Build settings updated successfully using operation: {operation}",
                    buildSettings = new
                    {
                        sceneCount = EditorBuildSettings.scenes.Length,
                        scenes = EditorBuildSettings.scenes.Select(s => new
                        {
                            path = s.path,
                            enabled = s.enabled,
                            name = Path.GetFileNameWithoutExtension(s.path)
                        }).ToArray()
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error modifying build settings: {ex.Message}");
                return JObject.FromObject(new
                {
                    error = ex.Message
                });
            }
        }
    }
} 