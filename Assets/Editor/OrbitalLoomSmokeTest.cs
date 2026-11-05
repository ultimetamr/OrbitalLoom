using System.IO;
using UnityEngine;
namespace OrbitalLoom.Editor
{
    public static class OrbitalLoomSmokeTest
    {
        public static void Capture(string path,int width=1600,int height=1200)
        {
            var camera=Object.FindObjectOfType<OrbitalLoom.Interaction.SpatialInputRouter>().editorCamera;
            var texture=new RenderTexture(width,height,24);var previous=camera.targetTexture;var active=RenderTexture.active;
            camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;
            Object.DestroyImmediate(image);Object.DestroyImmediate(texture);
        }
    }
}
