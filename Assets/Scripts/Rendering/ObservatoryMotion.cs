using OrbitalLoom.Core;
using UnityEngine;
namespace OrbitalLoom.Rendering
{
    /// <summary>Only menu ornaments move. Puzzle coordinates and hit targets stay fixed.</summary>
    public sealed class ObservatoryMotion:MonoBehaviour
    {
        public Transform[] rings;
        private void Update()
        {
            var game=GameManager.Instance;if(game==null||game.Paused||!game.Settings.particles)return;
            for(int i=0;i<rings.Length;i++)rings[i].Rotate(i%2==0?Vector3.up:Vector3.forward,Time.unscaledDeltaTime*(i%2==0?5:-7),Space.Self);
        }
    }
}
