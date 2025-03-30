using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for saving scenes in Unity Editor
    /// </summary>
    public class SaveSceneTool : McpToolBase
    {
        public SaveSceneTool()
        {
            Name = "save_scene";
            Description = "Saves changes to scenes";
        }

        /// <summary>
        /// Saves the current scene with the provided parameters
        /// </summary>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
        {
            try
            {
                string scenePath = parameters.TryGetValue("scenePath", out JToken scenePathToken) ? scenePathToken.ToString() : null;
                bool saveAsCopy = parameters.TryGetValue("saveAsCopy", out JToken saveAsCopyToken) && saveAsCopyToken.Type == JTokenType.Boolean && (bool)saveAsCopyToken;
                
                // Get the active scene
                Scene activeScene = EditorSceneManager.GetActiveScene();
                
                // Check if there is an active scene
                if (!activeScene.IsValid())
                {
                    return JObject.FromObject(new
                    {
                        error = "No active scene to save"
                    });
                }
                
                bool savedSuccessfully;
                string finalPath = scenePath;
                
                // If no path is provided, use the current scene path if available
                if (string.IsNullOrEmpty(scenePath))
                {
                    if (string.IsNullOrEmpty(activeScene.path))
                    {
                        return JObject.FromObject(new
                        {
                            error = "No path specified and current scene has no path. Use scenePath parameter to specify where to save."
                        });
                    }
                    
                    finalPath = activeScene.path;
                }
                else if (!scenePath.EndsWith(".unity"))
                {
                    // Make sure path ends with .unity
                    finalPath = scenePath + ".unity";
                }
                
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(finalPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Save the scene
                if (saveAsCopy)
                {
                    savedSuccessfully = EditorSceneManager.SaveScene(activeScene, finalPath, true);
                }
                else
                {
                    savedSuccessfully = EditorSceneManager.SaveScene(activeScene, finalPath);
                }
                
                if (!savedSuccessfully)
                {
                    return JObject.FromObject(new
                    {
                        error = $"Failed to save scene to: {finalPath}"
                    });
                }
                
                // Get scene info for response
                return JObject.FromObject(new
                {
                    success = true,
                    message = $"Scene '{activeScene.name}' saved successfully to {finalPath}",
                    scene = new
                    {
                        name = activeScene.name,
                        path = finalPath,
                        isLoaded = activeScene.isLoaded,
                        isDirty = activeScene.isDirty,
                        rootCount = activeScene.rootCount,
                        savedAsCopy = saveAsCopy
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving scene: {ex.Message}");
                return JObject.FromObject(new
                {
                    error = ex.Message
                });
            }
        }
    }
} 