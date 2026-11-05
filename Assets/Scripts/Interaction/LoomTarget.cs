using System;
using UnityEngine;

namespace OrbitalLoom.Interaction
{
    public enum TargetKind { Node, ControlPoint, Mechanic, Obstacle, Button, Slider, Workspace, RingHandle, RingPort, RingBase, RingBridge, BoxSocket, BoxClue }
    public sealed class LoomTarget : MonoBehaviour
    {
        public TargetKind kind;
        public string id;
        public int controlIndex;
        public event Action Clicked;
        public event Action<Vector3> Dragged;
        public event Action<bool> Hovered;
        public void Click() { if (isActiveAndEnabled) Clicked?.Invoke(); }
        public void Drag(Vector3 point) { if (isActiveAndEnabled) Dragged?.Invoke(point); }
        public void Hover(bool active) { Hovered?.Invoke(active); }
    }
}
