using System;
using OrbitalLoom.Data;
using UnityEngine;

namespace OrbitalLoom.Core
{
    [Serializable]
    public sealed class AppSettings
    {
        public int version = 1;
        public float masterVolume = .7f, effectsVolume = .75f, ambienceVolume = .35f;
        public float rhythmVolume = .65f;
        public int quality = 1;
        public float contentScale = 1, snapRadius = .045f;
        public bool particles = true, comfortMode = true, introSeen;
        public int skin;
        public string[] seenTutorials = Array.Empty<string>();

        public void Sanitize()
        {
            masterVolume = Safe(masterVolume, .7f, 0, 1);
            effectsVolume = Safe(effectsVolume, .75f, 0, 1);
            ambienceVolume = Safe(ambienceVolume, .35f, 0, 1);
            rhythmVolume = Safe(rhythmVolume, .65f, 0, 1);
            contentScale = Safe(contentScale, 1, .75f, 1.1f);
            snapRadius = Safe(snapRadius, .045f, .025f, .08f);
            quality = Mathf.Clamp(quality, 0, 2); skin = Mathf.Clamp(skin, 0, 3);
            if (seenTutorials == null) seenTutorials = Array.Empty<string>();
        }
        private static float Safe(float value, float fallback, float min, float max) =>
            Point3.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
    }
}
