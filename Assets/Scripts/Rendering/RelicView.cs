using System.Linq;
using System.Collections.Generic;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Rendering
{
    /// <summary>All exhibits and restoration pieces are persistent exported meshes.
    /// Only their transforms and colors change. No collider can intercept a ray.</summary>
    public sealed class RelicView : MonoBehaviour
    {
        public Transform rotor, restorationRoot, heart;
        public SpatialMechanismView mechanism;
        public Transform[] exhibits;
        public MeshRenderer[] restoration;
        private EnergyBoxSession session;
        private bool displaying;
        private float solvedAge;
        public bool Displaying=>displaying;
        public float RestorationAge=>solvedAge;
        public void Bind(EnergyBoxSession current,bool observe,bool workspace)
        {
            if(session!=current){session=current;solvedAge=0;}
            displaying=observe&&current?.Observation!=null;
            rotor.gameObject.SetActive(displaying&&current?.Level.spatial==null);
            mechanism?.Bind(current,displaying);
            for(int i=0;i<exhibits.Length;i++)exhibits[i].gameObject.SetActive(displaying&&i==Kind(current?.Level.relic?.kind));
            restorationRoot.gameObject.SetActive(workspace&&current!=null);
            if(displaying)rotor.localRotation=Quaternion.Euler(0,current.Observation.Angle,0);
        }
        private static int Kind(string kind)=>kind=="parallax"?0:kind=="mirror"?1:kind=="resonance"?2:-1;
        private void Update()
        {
            if(session==null||!restorationRoot.gameObject.activeInHierarchy)return;
            var game=GameManager.Instance;if(game==null)return;
            bool animate=game.Settings.particles&&!game.Paused&&!game.UiOverlayOpen;
            if(!session.Solved)solvedAge=0;else if(animate)solvedAge+=Time.unscaledDeltaTime;
            var goals=session.Level.nodes.Where(n=>n.kind=="goal").ToArray();
            for(int i=0;i<restoration.Length;i++)
            {
                var goal=goals[i/2%goals.Length];var result=session.Result;
                bool repaired=result!=null&&result.arrivals.TryGetValue(goal.id,out float at)&&session.Elapsed>=at&&
                    result.received.GetValueOrDefault(goal.id)==goal.units&&
                    (goal.code.Length==0||result.signatures.GetValueOrDefault(goal.id)==goal.code)&&!result.failures.ContainsKey(goal.id);
                LoomPalette.SetColor(restoration[i],repaired?LoomPalette.Gold:LoomPalette.Hex("3C565B"));
            }
            heart.gameObject.SetActive(session.Solved);
            if(session.Solved)
            {
                float t=animate?Mathf.SmoothStep(0,1,Mathf.Clamp01(solvedAge/2.4f)):1;
                heart.localPosition=new Vector3(0,Mathf.Lerp(-.35f,-.16f,t),.34f);
                heart.localRotation=Quaternion.Euler(8,25+35*t,8);
                heart.localScale=Vector3.one*Mathf.Lerp(.065f,.105f,t);
            }
        }
    }
}
