using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Extension methods for JObject to convert to Unity types
    /// </summary>
    public static class JObjectExtensions
    {
        /// <summary>
        /// Convert a JObject to a Vector2
        /// </summary>
        public static Vector2 ToVector2(this JObject jObject)
        {
            float x = jObject["x"]?.ToObject<float>() ?? 0;
            float y = jObject["y"]?.ToObject<float>() ?? 0;
            
            return new Vector2(x, y);
        }
        
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