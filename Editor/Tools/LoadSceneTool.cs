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
    /// Tool for loading scenes in Unity Editor
    /// </summary>
    public class LoadSceneTool : McpToolBase
    {
        public LoadSceneTool()
        {
            Name = "load_scene";
            Description = "Loads an existing scene in the editor";
        }

        /// <summary>
        /// Loads an existing scene with the provided parameters
        /// </summary>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
        {
            try
            {
                // Validate parameters
                if (!parameters.TryGetValue("scenePath", out JToken scenePathToken) || string.IsNullOrEmpty(scenePathToken.ToString()))
                {
                    return JObject.FromObject(new
                    {
                        error = "scenePath is required"
                    });
                }

                string scenePath = scenePathToken.ToString();
                bool additive = parameters.TryGetValue("additive", out JToken additiveToken) && additiveToken.Type == JTokenType.Boolean && (bool)additiveToken;
                bool saveCurrentSceneIfDirty = parameters.TryGetValue("saveCurrentSceneIfDirty", out JToken saveToken) 
                    ? (saveToken.Type == JTokenType.Boolean && (bool)saveToken)
                    : true; // Default to true
                
                // Check if scene file exists
                if (!File.Exists(scenePath))
                {
                    return JObject.FromObject(new
                    {
                        error = $"Scene file does not exist: {scenePath}"
                    });
                }
                
                // Check if we need to save current scene before loading the new one
                if (saveCurrentSceneIfDirty && EditorSceneManager.GetActiveScene().isDirty)
                {
                    bool saveOk = await Task.Run(() => 
                    {
                        bool userAction = EditorUtility.DisplayDialog(
                            "Unsaved Changes",
                            "The current scene has unsaved changes. Save before loading the new scene?",
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
                
                // Load the scene
                OpenSceneMode sceneMode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
                Scene loadedScene = EditorSceneManager.OpenScene(scenePath, sceneMode);
                
                if (!loadedScene.IsValid())
                {
                    return JObject.FromObject(new
                    {
                        error = $"Failed to load scene: {scenePath}"
                    });
                }
                
                // Get scene info for response
                return JObject.FromObject(new
                {
                    success = true,
                    message = $"Scene '{loadedScene.name}' loaded successfully",
                    scene = new
                    {
                        name = loadedScene.name,
                        path = loadedScene.path,
                        isLoaded = loadedScene.isLoaded,
                        isDirty = loadedScene.isDirty,
                        rootCount = loadedScene.rootCount,
                        loadMode = additive ? "Additive" : "Single"
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading scene: {ex.Message}");
                return JObject.FromObject(new
                {
                    error = ex.Message
                });
            }
        }
    }
} 