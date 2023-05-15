using UnityEngine;

namespace DefaultNamespace
{
    public static class ExtensionMethods
    {
        public static Vector3 Invert(this Vector3 vec)
        {
            return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
        }
        
        public static bool HasComponent <T>(this GameObject obj) where T:Component
        {
            return obj.GetComponent<T>() != null;
        }
    }
}