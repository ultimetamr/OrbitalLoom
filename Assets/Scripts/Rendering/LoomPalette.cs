using UnityEngine;

namespace OrbitalLoom.Rendering
{
    public static class LoomPalette
    {
        public static readonly Color Indigo = Hex("1A2348"), Gold = Hex("FFD27A"), Text = Hex("EEF3F1"),
            Muted = Hex("A8BEBD"), Error = Hex("FF5A5A"), Gravity = Hex("FF9A5A"),
            Repulsion = Hex("7AD7FF"), Splitter = Hex("B07AFF"), Mirror = Hex("E0E8FF"), Delay = Hex("D4AF37");
        public static readonly Color[] SkinEnergy = { Gold, Hex("7AD7FF"), Hex("F0B3C7"), Hex("B8E4BC") };
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        public static Color Mechanic(string type)
        {
            switch (type)
            {
                case "gravity": return Gravity; case "repulsion": return Repulsion;
                case "splitter": return Splitter; case "mirror": return Mirror;
                case "delay": return Delay; default: return Text;
            }
        }
        public static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
