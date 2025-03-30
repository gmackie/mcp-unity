using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for retrieving detailed information about the current scene
    /// </summary>
    public class GetSceneInfoResource : McpResourceBase
    {
        public GetSceneInfoResource()
        {
            Name = "get_scene_info";
            Description = "Returns detailed information about the current scene";
        }

        public override JObject Fetch(JObject parameters)
        {
            try
            {
                // Extract parameters
                bool includeGameObjects = parameters?["includeGameObjects"]?.ToObject<bool>() ?? false;
                bool includeComponents = parameters?["includeComponents"]?.ToObject<bool>() ?? false;
                bool includeSettings = parameters?["includeSettings"]?.ToObject<bool>() ?? false;
                
                // Get the active scene
                Scene activeScene = SceneManager.GetActiveScene();
                
                // Check if scene is valid
                if (!activeScene.IsValid())
                {
                    return new JObject
                    {
                        ["error"] = "No active scene found"
                    };
                }
                
                // Create the response with basic scene information
                JObject response = new JObject
                {
                    ["success"] = true,
                    ["name"] = activeScene.name,
                    ["path"] = activeScene.path,
                    ["isDirty"] = activeScene.isDirty,
                    ["isLoaded"] = activeScene.isLoaded,
                    ["rootCount"] = activeScene.rootCount
                };
                
                // Add GameObject hierarchy if requested
                if (includeGameObjects)
                {
                    GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
                    JArray gameObjectsArray = new JArray();
                    
                    foreach (GameObject rootObject in rootGameObjects)
                    {
                        gameObjectsArray.Add(SerializeGameObject(rootObject, includeComponents));
                    }
                    
                    response["gameObjects"] = gameObjectsArray;
                }
                
                // Add scene settings if requested
                if (includeSettings)
                {
                    JObject settings = new JObject();
                    
                    // Lighting settings
                    // RenderSettings is a static class, no instantiation is needed
                    settings["lighting"] = new JObject
                    {
                        ["ambientSkyColor"] = SerializeColor(RenderSettings.ambientSkyColor),
                        ["ambientEquatorColor"] = SerializeColor(RenderSettings.ambientEquatorColor),
                        ["ambientGroundColor"] = SerializeColor(RenderSettings.ambientGroundColor),
                        ["ambientIntensity"] = RenderSettings.ambientIntensity,
                        ["ambientMode"] = Enum.GetName(typeof(AmbientMode), RenderSettings.ambientMode),
                        ["fogEnabled"] = RenderSettings.fog,
                        ["fogColor"] = SerializeColor(RenderSettings.fogColor),
                        ["fogDensity"] = RenderSettings.fogDensity
                    };
                    
                    // Physics settings
                    settings["physics"] = new JObject
                    {
                        ["gravity"] = SerializeVector3(Physics.gravity),
                        ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                        ["bounceThreshold"] = Physics.bounceThreshold,
                        ["sleepThreshold"] = Physics.sleepThreshold
                    };
                    
                    // Navigation settings
                    settings["navigation"] = new JObject
                    {
                        ["avoidancePredictionTime"] = UnityEngine.AI.NavMesh.avoidancePredictionTime,
                        // Removed invalid property 'obstacleAvoidanceType' as it does not exist in UnityEngine.AI.NavMesh
                        ["pathfindingIterationsPerFrame"] = UnityEngine.AI.NavMesh.pathfindingIterationsPerFrame
                    };
                    
                    // Audio settings
                    AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
                    settings["audio"] = new JObject
                    {
                        ["bufferLength"] = bufferLength,
                        ["numBuffers"] = numBuffers,
                        ["speakerMode"] = Enum.GetName(typeof(AudioSpeakerMode), AudioSettings.speakerMode),
                        ["outputSampleRate"] = AudioSettings.outputSampleRate
                    };
                    
                    response["settings"] = settings;
                }
                
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching scene info: {ex.Message}");
                return new JObject
                {
                    ["error"] = ex.Message
                };
            }
        }

        /// <summary>
        /// Recursively serializes a GameObject and its children
        /// </summary>
        private JObject SerializeGameObject(GameObject gameObject, bool includeComponents)
        {
            JObject result = new JObject
            {
                ["name"] = gameObject.name,
                ["id"] = gameObject.GetInstanceID(),
                ["active"] = gameObject.activeSelf,
                ["layer"] = gameObject.layer,
                ["layerName"] = LayerMask.LayerToName(gameObject.layer),
                ["tag"] = gameObject.tag,
                ["transform"] = new JObject
                {
                    ["position"] = SerializeVector3(gameObject.transform.localPosition),
                    ["rotation"] = SerializeVector3(gameObject.transform.localEulerAngles),
                    ["scale"] = SerializeVector3(gameObject.transform.localScale)
                }
            };
            
            // Add components if requested
            if (includeComponents)
            {
                Component[] components = gameObject.GetComponents<Component>();
                JArray componentsArray = new JArray();
                
                foreach (Component component in components)
                {
                    if (component == null) continue;
                    
                    JObject componentData = new JObject
                    {
                        ["type"] = component.GetType().Name,
                        ["enabled"] = component is Behaviour behaviour ? behaviour.enabled : true
                    };
                    
                    // Add special handling for common component types
                    if (component is MeshRenderer meshRenderer)
                    {
                        componentData["material"] = meshRenderer.sharedMaterial != null ? meshRenderer.sharedMaterial.name : "None";
                        componentData["lightmapIndex"] = meshRenderer.lightmapIndex;
                        componentData["receivesShadows"] = meshRenderer.receiveShadows;
                    }
                    else if (component is Light light)
                    {
                        componentData["type"] = Enum.GetName(typeof(LightType), light.type);
                        componentData["color"] = SerializeColor(light.color);
                        componentData["intensity"] = light.intensity;
                        componentData["range"] = light.range;
                    }
                    else if (component is Camera camera)
                    {
                        componentData["clearFlags"] = Enum.GetName(typeof(CameraClearFlags), camera.clearFlags);
                        componentData["backgroundColor"] = SerializeColor(camera.backgroundColor);
                        componentData["fieldOfView"] = camera.fieldOfView;
                        componentData["nearClipPlane"] = camera.nearClipPlane;
                        componentData["farClipPlane"] = camera.farClipPlane;
                    }
                    
                    componentsArray.Add(componentData);
                }
                
                result["components"] = componentsArray;
            }
            
            // Add children
            if (gameObject.transform.childCount > 0)
            {
                JArray childrenArray = new JArray();
                
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    Transform childTransform = gameObject.transform.GetChild(i);
                    childrenArray.Add(SerializeGameObject(childTransform.gameObject, includeComponents));
                }
                
                result["children"] = childrenArray;
            }
            
            return result;
        }

        /// <summary>
        /// Serializes a Vector3 to a JSON object
        /// </summary>
        private JObject SerializeVector3(Vector3 vector)
        {
            return new JObject
            {
                ["x"] = vector.x,
                ["y"] = vector.y,
                ["z"] = vector.z
            };
        }

        /// <summary>
        /// Serializes a Color to a JSON object
        /// </summary>
        private JObject SerializeColor(Color color)
        {
            return new JObject
            {
                ["r"] = color.r,
                ["g"] = color.g,
                ["b"] = color.b,
                ["a"] = color.a
            };
        }
    }
} 