using UnityEngine;
using OrbitalLoom.Rendering;

namespace OrbitalLoom.UI
{
    /// <summary>Every interaction state retains opaque backing and light ink.
    /// Native text owns its backing so exported mesh tint cannot wash out type.</summary>
    public static class SpatialUITheme
    {
        public static readonly Color Panel=LoomPalette.Hex("101B30"), Surface=LoomPalette.Hex("1B2C43"),
            Hover=LoomPalette.Hex("29465E"), Pressed=LoomPalette.Hex("354E68"),
            Selected=LoomPalette.Hex("254354"), Disabled=LoomPalette.Hex("152136"),
            Ink=LoomPalette.Hex("F2F5FC"), Secondary=LoomPalette.Hex("B9CDD9"),
            DisabledInk=LoomPalette.Hex("B9C9C9"), Accent=LoomPalette.Hex("E8CF94"), Edge=LoomPalette.Hex("56767B"), ButtonAccent=LoomPalette.Hex("E8CA89"), Error=LoomPalette.Hex("FFA7A2");
        public static Color Fill(bool enabled,bool primary,bool selected,bool hover,bool pressed)
            =>!enabled?Disabled:pressed?Pressed:selected||primary?Selected:hover?Hover:Surface;
        public static Color Foreground(bool enabled)=>enabled?LoomPalette.Hex("F7F8F4"):DisabledInk;
        public static float Contrast(Color ink,Color backing)
        {float a=Luminance(ink),b=Luminance(backing);return (Mathf.Max(a,b)+.05f)/(Mathf.Min(a,b)+.05f);}
        private static float Luminance(Color c)=>.2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);
        private static float Linear(float v)=>v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);
    }
}
