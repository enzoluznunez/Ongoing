using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AccountingImmersiveAnalytics.App
{
    public static class GraphicsWarm
    {
        private struct Draw
        {
            public Mesh mesh;
            public int submesh;
            public RenderParams parameters;
            public float size;
        }

        private static readonly List<Draw> Kinds = new List<Draw>();
        private static readonly List<Draw> Meshes = new List<Draw>();
        private static readonly HashSet<(EntityId, MeshTopology, int, ShadowCastingMode, bool, int)> SeenKinds =
            new HashSet<(EntityId, MeshTopology, int, ShadowCastingMode, bool, int)>();
        private static readonly HashSet<EntityId> SeenMeshes = new HashSet<EntityId>();
        private static readonly List<Material> Materials = new List<Material>();
        private static readonly List<VertexAttributeDescriptor> Attributes = new List<VertexAttributeDescriptor>();

        private static MeshRenderer[] _renderers;
        private static int _scanned;
        private static int _kindFrames;
        private static int _drawn;
        private static int _batch;
        private static int _settleFrames;
        private static int _frames;
        private static float _started;
        private static bool _done;

        private const float Ahead = 1.25f;
        private const float Width = 0.001f;

        private const int ScanPerFrame = 2000;
        private const int KindFrames = 4;
        private const int FirstBatch = 64;
        private const int LeastBatch = 16;
        private const int MostBatch = 1024;
        private const float SlowMs = 25f;
        private const int SettleFrames = 4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Kinds.Clear();
            Meshes.Clear();
            SeenKinds.Clear();
            SeenMeshes.Clear();
            _renderers = null;
            _scanned = 0;
            _kindFrames = 0;
            _drawn = 0;
            _batch = FirstBatch;
            _settleFrames = 0;
            _frames = 0;
            _done = false;
        }

        public static bool Step()
        {
            if (_done) return true;
            Transform cam = CameraRig.MainTransform;
            if (cam == null) return false;

            if (_renderers == null)
            {
                _started = Time.unscaledTime;
                _batch = FirstBatch;
                _renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
            }
            _frames++;

            if (_scanned < _renderers.Length)
            {
                int end = Mathf.Min(_scanned + ScanPerFrame, _renderers.Length);
                for (; _scanned < end; _scanned++) Collect(_renderers[_scanned]);
                if (_scanned == _renderers.Length)
                    Debug.Log($"[GraphicsWarm] Drawing {Kinds.Count} kinds of thing and {Meshes.Count} meshes once, " +
                              $"from {_renderers.Length} renderers.");
                return false;
            }

            Vector3 at = cam.position + cam.forward * Ahead;
            Quaternion facing = cam.rotation;

            if (_kindFrames < KindFrames)
            {
                for (int i = 0; i < Kinds.Count; i++) Render(Kinds[i], at, facing);
                _kindFrames++;
                return false;
            }

            if (_drawn < Meshes.Count)
            {
                if (_drawn > 0)
                {
                    bool slow = Time.unscaledDeltaTime * 1000f > SlowMs;
                    _batch = slow ? Mathf.Max(LeastBatch, _batch / 2) : Mathf.Min(MostBatch, _batch + _batch / 2);
                }
                int end = Mathf.Min(_drawn + _batch, Meshes.Count);
                for (; _drawn < end; _drawn++) Render(Meshes[_drawn], at, facing);
                return false;
            }

            if (_settleFrames++ < SettleFrames) return false;

            _done = true;
            Debug.Log($"[GraphicsWarm] Drew {Meshes.Count} meshes in {_frames} frames, " +
                      $"{Time.unscaledTime - _started:F1} s.");
            _renderers = null;
            Kinds.Clear();
            Meshes.Clear();
            SeenKinds.Clear();
            SeenMeshes.Clear();
            return true;
        }

        private static void Render(Draw d, Vector3 at, Quaternion facing)
        {
            if (d.mesh == null || d.parameters.material == null) return;
            Graphics.RenderMesh(d.parameters, d.mesh, d.submesh, Matrix4x4.TRS(at, facing, Vector3.one * d.size));
        }

        private static void Collect(MeshRenderer r)
        {
            if (r == null) return;
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || mesh.vertexCount == 0) return;

            r.GetSharedMaterials(Materials);
            int submeshes = Mathf.Min(Materials.Count, mesh.subMeshCount);
            if (submeshes == 0) return;

            mesh.GetVertexAttributes(Attributes);
            int layout = 17;
            for (int i = 0; i < Attributes.Count; i++)
            {
                VertexAttributeDescriptor a = Attributes[i];
                layout = layout * 31 + ((int)a.attribute << 16 | (int)a.format << 8 | a.dimension << 4 | a.stream);
            }

            float size = Width / Mathf.Max(mesh.bounds.size.magnitude, 1e-4f);
            bool newMesh = SeenMeshes.Add(mesh.GetEntityId());
            for (int s = 0; s < submeshes; s++)
            {
                Material material = Materials[s];
                if (material == null) continue;

                bool newKind = SeenKinds.Add((material.GetEntityId(), mesh.GetTopology(s), r.gameObject.layer,
                                              r.shadowCastingMode, r.receiveShadows, layout));
                if (!newKind && !newMesh) continue;

                var draw = new Draw
                {
                    mesh = mesh,
                    submesh = s,
                    size = size,
                    parameters = new RenderParams(material)
                    {
                        layer = r.gameObject.layer,
                        shadowCastingMode = r.shadowCastingMode,
                        receiveShadows = r.receiveShadows,
                        renderingLayerMask = r.renderingLayerMask
                    }
                };
                if (newKind) Kinds.Add(draw);
                if (newMesh)
                {
                    Meshes.Add(draw);
                    newMesh = false;
                }
            }
        }
    }
}
