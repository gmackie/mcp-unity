using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for updating prefabs with component modifications
    /// </summary>
    public class UpdatePrefabTool : McpToolBase
    {
        public UpdatePrefabTool()
        {
            Name = "update_prefab";
            Description = "Modifies prefab properties and applies changes";
        }
        
        /// <summary>
        /// Execute the UpdatePrefab tool with the provided parameters asynchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override async Task<JObject> ExecuteAsync(JObject parameters)
        {
            // Extract parameters
            string prefabPath = parameters["prefabPath"]?.ToObject<string>();
            JArray componentUpdates = parameters["componentUpdates"] as JArray;
            bool applyToAllInstances = parameters["applyToAllInstances"]?.ToObject<bool>() ?? true;
            string instancePath = parameters["instancePath"]?.ToObject<string>();
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            
            // Validate parameters
            if (string.IsNullOrEmpty(prefabPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'prefabPath' not provided", 
                    "validation_error"
                );
            }
            
            if (componentUpdates == null || componentUpdates.Count == 0)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'componentUpdates' not provided or empty", 
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
                
                // Find specific instance if instancePath is provided
                GameObject instance = null;
                if (!string.IsNullOrEmpty(instancePath))
                {
                    instance = GameObject.Find(instancePath);
                    if (instance == null)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"Prefab instance not found at path: {instancePath}", 
                            "not_found_error"
                        );
                    }
                    
                    // Verify it's an instance of our prefab
                    if (PrefabUtility.GetPrefabAssetType(instance) == PrefabAssetType.NotAPrefab ||
                        PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefabAsset)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"GameObject at {instancePath} is not an instance of the specified prefab", 
                            "validation_error"
                        );
                    }
                }
                else if (instanceId.HasValue)
                {
                    // Try to find by instance ID
                    instance = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    if (instance == null)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"Prefab instance not found with ID: {instanceId.Value}", 
                            "not_found_error"
                        );
                    }
                    
                    // Verify it's an instance of our prefab
                    if (PrefabUtility.GetPrefabAssetType(instance) == PrefabAssetType.NotAPrefab ||
                        PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefabAsset)
                    {
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"GameObject with ID {instanceId.Value} is not an instance of the specified prefab", 
                            "validation_error"
                        );
                    }
                }
                
                // Create lists to track what was updated
                var updatedComponents = new List<string>();
                var updatedProperties = new List<string>();
                var errors = new List<string>();
                
                // Open prefab for editing if we're updating the prefab asset directly
                GameObject prefabRoot = null;
                if (instance == null)
                {
                    // Open the prefab for editing in prefab mode
                    prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                }
                else
                {
                    prefabRoot = instance;
                }
                
                // Process component updates
                foreach (JObject componentUpdate in componentUpdates)
                {
                    string componentPath = componentUpdate["componentPath"]?.ToObject<string>();
                    string componentType = componentUpdate["componentType"]?.ToObject<string>();
                    JObject properties = componentUpdate["properties"] as JObject;
                    
                    if (properties == null || (string.IsNullOrEmpty(componentPath) && string.IsNullOrEmpty(componentType)))
                    {
                        errors.Add("Component update missing required fields (componentPath/componentType or properties)");
                        continue;
                    }
                    
                    // Find the component
                    Component component = null;
                    
                    if (!string.IsNullOrEmpty(componentPath))
                    {
                        // Split path by '/' to handle nested GameObjects
                        string[] pathParts = componentPath.Split('/');
                        string componentName = pathParts[pathParts.Length - 1];
                        
                        // First part is the component name, rest is the path to the GameObject
                        GameObject targetObject = prefabRoot;
                        
                        // Navigate to the target GameObject if path has multiple parts
                        if (pathParts.Length > 1)
                        {
                            string gameObjectPath = string.Join("/", pathParts, 0, pathParts.Length - 1);
                            Transform child = prefabRoot.transform.Find(gameObjectPath);
                            if (child != null)
                            {
                                targetObject = child.gameObject;
                            }
                            else
                            {
                                errors.Add($"Could not find GameObject at path: {gameObjectPath}");
                                continue;
                            }
                        }
                        
                        // Find all components of the target GameObject
                        Component[] components = targetObject.GetComponents<Component>();
                        
                        // Try to find the component by name
                        foreach (Component comp in components)
                        {
                            if (comp != null && comp.GetType().Name == componentName)
                            {
                                component = comp;
                                break;
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(componentType))
                    {
                        // Find component by type
                        Type type = null;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = assembly.GetType(componentType);
                            if (type != null)
                                break;
                        }
                        
                        if (type != null)
                        {
                            component = prefabRoot.GetComponent(type);
                        }
                        else
                        {
                            errors.Add($"Could not find component type: {componentType}");
                            continue;
                        }
                    }
                    
                    if (component == null)
                    {
                        errors.Add($"Component not found: {componentPath ?? componentType}");
                        continue;
                    }
                    
                    // Update properties
                    foreach (var property in properties.Properties())
                    {
                        try
                        {
                            // Get property name and value
                            string propertyName = property.Name;
                            JToken propertyValue = property.Value;
                            
                            // Use SerializedObject to modify properties safely
                            SerializedObject serializedObject = new SerializedObject(component);
                            SerializedProperty serializedProperty = serializedObject.FindProperty(propertyName);
                            
                            if (serializedProperty != null)
                            {
                                // Set property value based on type
                                if (propertyValue.Type == JTokenType.Boolean)
                                {
                                    serializedProperty.boolValue = propertyValue.ToObject<bool>();
                                }
                                else if (propertyValue.Type == JTokenType.Integer)
                                {
                                    serializedProperty.intValue = propertyValue.ToObject<int>();
                                }
                                else if (propertyValue.Type == JTokenType.Float)
                                {
                                    serializedProperty.floatValue = propertyValue.ToObject<float>();
                                }
                                else if (propertyValue.Type == JTokenType.String)
                                {
                                    serializedProperty.stringValue = propertyValue.ToObject<string>();
                                }
                                else if (propertyValue is JObject objectValue)
                                {
                                    // Handle Vector3, Quaternion, Color, etc.
                                    if (serializedProperty.propertyType == SerializedPropertyType.Vector2)
                                    {
                                        Vector2 vector = objectValue.ToVector2();
                                        serializedProperty.vector2Value = vector;
                                    }
                                    else if (serializedProperty.propertyType == SerializedPropertyType.Vector3)
                                    {
                                        Vector3 vector = objectValue.ToVector3();
                                        serializedProperty.vector3Value = vector;
                                    }
                                    else if (serializedProperty.propertyType == SerializedPropertyType.Quaternion)
                                    {
                                        Quaternion quaternion = objectValue.ToQuaternion();
                                        serializedProperty.quaternionValue = quaternion;
                                    }
                                    else if (serializedProperty.propertyType == SerializedPropertyType.Color)
                                    {
                                        Color color = new Color(
                                            objectValue["r"]?.ToObject<float>() ?? 0,
                                            objectValue["g"]?.ToObject<float>() ?? 0,
                                            objectValue["b"]?.ToObject<float>() ?? 0,
                                            objectValue["a"]?.ToObject<float>() ?? 1
                                        );
                                        serializedProperty.colorValue = color;
                                    }
                                }
                                
                                // Apply the change
                                serializedObject.ApplyModifiedProperties();
                                updatedProperties.Add($"{component.GetType().Name}.{propertyName}");
                            }
                            else
                            {
                                errors.Add($"Property not found: {propertyName} on {component.GetType().Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error updating property {property.Name}: {ex.Message}");
                        }
                    }
                    
                    updatedComponents.Add(component.GetType().Name);
                }
                
                // If we're editing the prefab asset directly, save changes
                if (instance == null)
                {
                    // Save the prefab changes
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                    
                    // Unload the prefab contents
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    
                    // If we should apply to all instances, find and update them
                    if (applyToAllInstances)
                    {
                        // Find all prefab instances in the scene
                        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                        foreach (GameObject obj in allObjects)
                        {
                            // Check if it's an instance of our prefab
                            if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab &&
                                PrefabUtility.GetCorrespondingObjectFromSource(obj) == prefabAsset)
                            {
                                // Apply all overrides
                                PrefabUtility.RevertPrefabInstance(obj, InteractionMode.AutomatedAction);
                            }
                        }
                    }
                }
                else
                {
                    // Apply instance modifications back to the prefab
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                }
                
                // Create metadata about the update
                var metadata = new JObject
                {
                    ["prefabPath"] = assetPath,
                    ["updatedComponents"] = new JArray(updatedComponents),
                    ["updatedProperties"] = new JArray(updatedProperties),
                    ["errors"] = new JArray(errors)
                };
                
                // Create the response
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"Successfully updated prefab: {assetPath}",
                    ["type"] = "prefab_updated",
                    ["metadata"] = metadata
                };
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error updating prefab: {ex.Message}", 
                    "execution_error"
                );
            }
        }
    }
} 