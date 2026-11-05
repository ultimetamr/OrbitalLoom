using ByteDance.PICO.SpatialAdapter;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Rendering
{
    public sealed class TrackView : MonoBehaviour
    {
        public MeshFilter filter;
        public MeshRenderer tube;
        public Transform pulse;
        public string TrackId { get; private set; }
        private Mesh mesh;
        private Vector3[] points;
        private float length, startTime;
        private bool dirty, energized, invalid;
        private const int Sides = 6;
        private readonly Vector3[] vertices = new Vector3[(TrackGeometry.Samples + 1) * Sides];
        private readonly Vector2[] uv = new Vector2[(TrackGeometry.Samples + 1) * Sides];
        private readonly int[] triangles = new int[TrackGeometry.Samples * Sides * 6];

        public void Bind(string id, Vector3[] path, bool isInvalid, bool isEnergized, float emissionTime, bool dashed = false,float radius=.003f)
        {
            TrackId = id; points = path; length = PathLength(path);
            invalid = isInvalid; energized = isEnergized; startTime = emissionTime;
            if (mesh == null) { mesh = Instantiate(filter.sharedMesh); mesh.name = "Loom tube runtime"; mesh.MarkDynamic(); filter.sharedMesh = mesh; }
            for (int i = 0; i <= TrackGeometry.Samples; i++)
            {
                var tangent = i == TrackGeometry.Samples ? path[i] - path[i - 1] : path[i + 1] - path[i];
                if (tangent.sqrMagnitude < .000001f) tangent = Vector3.right;
                var normal = Vector3.Cross(tangent.normalized, Mathf.Abs(tangent.normalized.y) > .9f ? Vector3.forward : Vector3.up).normalized;
                var binormal = Vector3.Cross(tangent.normalized, normal).normalized;
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * Mathf.PI * 2 / Sides;
                    vertices[i * Sides + side] = path[i] + (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius;
                    uv[i * Sides + side] = new Vector2(i / (float)TrackGeometry.Samples, side / (float)Sides);
                }
            }
            for (int i = 0; i < TrackGeometry.Samples; i++) for (int side = 0; side < Sides; side++)
            {
                int k = (i * Sides + side) * 6, a = i * Sides + side, b = i * Sides + (side + 1) % Sides;
                if (dashed && i % 4 >= 2) { for (int j = 0; j < 6; j++) triangles[k + j] = a; continue; }
                triangles[k] = a; triangles[k + 1] = a + Sides; triangles[k + 2] = b;
                triangles[k + 3] = b; triangles[k + 4] = a + Sides; triangles[k + 5] = b + Sides;
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); dirty = true;
            var color = invalid ? LoomPalette.Error : energized ? LoomPalette.SkinEnergy[GameManager.Instance.Settings.skin] : LoomPalette.Repulsion;
            color.a = dashed ? .36f : .9f; LoomPalette.SetColor(tube, color);
        }
        public void SetTint(Color color) { LoomPalette.SetColor(tube, color); }
        private void LateUpdate()
        {
            if (dirty && SpatialAdapterRuntime.Instance != null && SpatialAdapterRuntime.IsSessionInitialized() == 0)
            {
                var tracker = filter.GetComponent<SpatialAdapterGameObjectTracker>();
                if (tracker != null && tracker.isValid && tracker.uid >= 0)
                {
                    SpatialAdapterRuntime.Instance.SyncMesh(filter, VertexAttributeFlags.POSITION | VertexAttributeFlags.NORMAL | VertexAttributeFlags.UV0);
                    dirty = false;
                }
            }
            if (pulse == null || points == null) return;
            var game = GameManager.Instance;
            bool show = energized && !invalid && game.Settings.particles && game.EnergyElapsed >= startTime;
            if (pulse.gameObject.activeSelf != show) pulse.gameObject.SetActive(show);
            if (show)
            {
                float distance = Mathf.Repeat((game.EnergyElapsed - startTime) * .38f, Mathf.Max(.001f, length));
                pulse.localPosition = AtDistance(points, distance);
            }
        }
        private static float PathLength(Vector3[] path){float length=0;for(int i=1;i<path.Length;i++)length+=Vector3.Distance(path[i-1],path[i]);return length;}
        private static Vector3 AtDistance(Vector3[] path,float distance){for(int i=1;i<path.Length;i++){float length=Vector3.Distance(path[i-1],path[i]);if(distance<=length)return Vector3.Lerp(path[i-1],path[i],distance/Mathf.Max(.0001f,length));distance-=length;}return path[path.Length-1];}
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
