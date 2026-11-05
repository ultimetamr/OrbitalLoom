using System;
using UnityEngine;

namespace OrbitalLoom.Gameplay
{
    /// <summary>Optional observation evidence, independent of the energy solver.
    /// Text access exposes the same clue; sight and hearing are never prerequisites.</summary>
    [Serializable] public sealed class RelicClue
    {
        public string kind, title, instruction, discovery;
        public float angle;
        // JsonUtility materializes null nested Serializable classes as empty
        // objects. Treat that legacy round-trip representation as no exhibit.
        public bool IsEmpty=>string.IsNullOrEmpty(kind)&&string.IsNullOrEmpty(title)&&
            string.IsNullOrEmpty(instruction)&&string.IsNullOrEmpty(discovery)&&angle==0;
        public void Validate()
        {
            if((kind!="parallax"&&kind!="mirror"&&kind!="resonance")||
               string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(instruction)||
               string.IsNullOrWhiteSpace(discovery)||!Data.Point3.IsFinite(angle)||Mathf.Abs(angle)>180)
                throw new FormatException("遗迹观察线索无效");
        }
    }
    public sealed class RelicObservation
    {
        public readonly RelicClue Clue;
        public float Angle { get; private set; }
        public bool Found { get; private set; }
        public bool UsedTextAccess { get; private set; }
        public bool Aligned=>Mathf.Abs(Mathf.DeltaAngle(Angle,Clue.angle))<=7.5f;
        public RelicObservation(RelicClue clue){Clue=clue;clue.Validate();}
        public void Rotate(int direction){Angle=Mathf.DeltaAngle(0,Angle+Math.Sign(direction)*15);}
        public bool Record(bool textAccess=false)
        {
            if(!textAccess&&!Aligned)return false;
            Found=true;UsedTextAccess|=textAccess;return true;
        }
        public string Status=>Found?"已记录 · 可随时重读":Aligned?"刻纹成形 · 可以记录":"转动遗迹，寻找刻纹完整的一面";
    }
}
