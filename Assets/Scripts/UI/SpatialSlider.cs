using System;
using ByteDance.PICO.SpatialAdapter.Component;
using OrbitalLoom.Interaction;
using UnityEngine;

namespace OrbitalLoom.UI
{
    public sealed class SpatialSlider : MonoBehaviour
    {
        public LoomTarget target;
        public Transform rail, knob;
        public SpatialAdapterNativeText label;
        private Action<float> changed;
        private string title;
        private float width;
        private void Awake() { target.Dragged += UpdateValue; }
        public void Bind(string name, Vector3 position, float value, Action<float> onChange)
        {
            title = name; transform.localPosition = position; width = .42f; label.TextSize = 44;
            label.Justification = ByteDance.PICO.SpatialAdapter.Component.TextJustification.Center;
            label.TextColor = SpatialUITheme.Ink;
            rail.localScale = new Vector3(width, .006f, .006f);
            changed = onChange; Display(value);
        }
        private void UpdateValue(Vector3 world)
        {
            float value = Mathf.Clamp01(transform.InverseTransformPoint(world).x / width + .5f);
            Display(value); changed?.Invoke(value);
        }
        private void Display(float value)
        { knob.localPosition = new Vector3((value - .5f) * width, 0, -.01f); label.Text = title + "  " + Mathf.RoundToInt(value * 100) + "%"; }
    }
}
