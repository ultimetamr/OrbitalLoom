using UnityEngine;
namespace OrbitalLoom.Data
{
    public static class Point3
    {
        public static bool IsFinite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
