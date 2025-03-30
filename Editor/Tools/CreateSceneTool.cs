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
    /// Tool for creating new scenes in Unity Editor
    /// </summary>
    public class CreateSceneTool : McpToolBase
    {
        public CreateSceneTool()
        {
            Name = "create_scene";
            Description = "Creates a new scene";
        }

        /// <summary>
        /// Creates a new scene with the provided parameters
        /// </summary>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
        {
            try
            {
                // Validate parameters
                if (!parameters.TryGetValue("sceneName", out JToken sceneNameToken) || string.IsNullOrEmpty(sceneNameToken.ToString()))
                {
                    return JObject.FromObject(new
                    {
                        error = "sceneName is required"
                    });
                }

                string sceneName = sceneNameToken.ToString();
                string scenePath = parameters.TryGetValue("scenePath", out JToken scenePathToken) ? scenePathToken.ToString() : null;
                string templateScene = parameters.TryGetValue("templateScene", out JToken templateSceneToken) ? templateSceneToken.ToString() : null;

                // Check if we need to save current scene before creating a new one
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    bool saveOk = await Task.Run(() => 
                    {
                        bool userAction = EditorUtility.DisplayDialog(
                            "Unsaved Changes",
                            "The current scene has unsaved changes. Save before creating a new scene?",
                            "Save",
                            "Don't Save"
                        );
                        
                        if (userAction)
                        {
                            return EditorSceneManager.SaveOpenScenes();
                        }
                        return true; // If user chooses not to save, we still proceed
                    });
                    
                    if (!saveOk)
                    {
                        return JObject.FromObject(new
                        {
                            error = "Failed to save current scene"
                        });
                    }
                }

                // Create a new scene
                Scene newScene;
                
                if (!string.IsNullOrEmpty(templateScene) && File.Exists(templateScene))
                {
                    // Create from template
                    bool loadResult = EditorSceneManager.OpenScene(templateScene, OpenSceneMode.Single) != null;
                    if (!loadResult)
                    {
                        return JObject.FromObject(new
                        {
                            error = $"Failed to load template scene: {templateScene}"
                        });
                    }
                    
                    newScene = EditorSceneManager.GetActiveScene();
                }
                else
                {
                    // Create empty scene
                    newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                }
                
                // Save the scene if path is provided
                bool savedSuccessfully = true;
                string finalPath = scenePath;
                
                if (!string.IsNullOrEmpty(scenePath))
                {
                    // Ensure the directory exists
                    string directory = Path.GetDirectoryName(scenePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Make sure path ends with .unity
                    if (!scenePath.EndsWith(".unity"))
                    {
                        finalPath = scenePath + ".unity";
                    }
                    
                    savedSuccessfully = EditorSceneManager.SaveScene(newScene, finalPath);
                }
                
                // Get scene info for response
                return JObject.FromObject(new
                {
                    success = true,
                    message = $"Scene '{sceneName}' created successfully",
                    saved = savedSuccessfully,
                    scene = new
                    {
                        name = newScene.name,
                        path = finalPath ?? "",
                        isLoaded = newScene.isLoaded,
                        isDirty = newScene.isDirty,
                        rootCount = newScene.rootCount
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating scene: {ex.Message}");
                return JObject.FromObject(new
                {
                    error = ex.Message
                });
            }
        }
    }
} 