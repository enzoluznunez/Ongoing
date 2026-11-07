#if VIZ_EXPERIMENTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class ExperimentRunner : MonoBehaviour
{
    private const float SettleSeconds = 2f;
    private const float MeasureSeconds = 6f;
    private const float AheadMetres = 1.2f;
    private const float FieldMetres = 0.8f;
    private const float VolumeMetres = 0.6f;
    private const int UpdateMarks = 100000;
    private const string Tag = "[Experiment]";

    private static readonly int MarksId = Shader.PropertyToID("_Marks");
    private static readonly int VolumeId = Shader.PropertyToID("_Volume");
    private static readonly int StepsId = Shader.PropertyToID("_Steps");
    private static readonly Bounds Everywhere = new Bounds(Vector3.zero, Vector3.one * 100f);

    [StructLayout(LayoutKind.Sequential)]
    private struct Mark
    {
        public float4 centerAndSize;
        public float4 color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Corner
    {
        public float3 position;
        public float id;
    }

    [BurstCompile]
    private struct FillMarks : IJobParallelFor
    {
        public NativeArray<Mark> marks;
        public float3 origin;
        public float field;
        public int side;
        public float time;

        public void Execute(int index)
        {
            int x = index % side;
            int y = index / side % side;
            int z = index / (side * side);
            float3 cell = new float3(x, y, z) / math.max(side - 1, 1);
            float wave = time > 0f ? math.sin(time * 3f + index * 0.01f) * 0.01f : 0f;
            float3 center = origin + (cell - 0.5f) * field;
            center.y += wave;
            marks[index] = new Mark
            {
                centerAndSize = new float4(center, field / side * 0.6f),
                color = new float4(cell, 1f)
            };
        }
    }

    [BurstCompile]
    private struct FillBigMesh : IJob
    {
        [WriteOnly] public NativeArray<Corner> corners;
        [WriteOnly] public NativeArray<uint> indices;
        [ReadOnly] public NativeArray<float3> unitCorners;
        [ReadOnly] public NativeArray<uint> unitIndices;
        public int count;

        public void Execute()
        {
            int cornerCount = unitCorners.Length;
            int indexCount = unitIndices.Length;
            for (int mark = 0; mark < count; mark++)
            {
                for (int c = 0; c < cornerCount; c++)
                    corners[mark * cornerCount + c] = new Corner { position = unitCorners[c], id = mark };
                for (int i = 0; i < indexCount; i++)
                    indices[mark * indexCount + i] = (uint)(mark * cornerCount) + unitIndices[i];
            }
        }
    }

    [BurstCompile]
    private struct FillVolume : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<byte> voxels;
        public int side;

        public void Execute(int z)
        {
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float3 p = (new float3(x, y, z) + 0.5f) / side - 0.5f;
                    float ripple = math.sin(p.x * 14f) * math.sin(p.y * 11f) * math.sin(p.z * 9f);
                    float shell = 1f - math.saturate(math.length(p) * 2.1f);
                    voxels[x + y * side + z * side * side] = (byte)(math.saturate(shell * (0.6f + 0.6f * ripple)) * 255f);
                }
        }
    }

    private static ExperimentRunner _instance;

    private readonly StringBuilder _results = new StringBuilder();
    private readonly FrameTiming[] _timing = new FrameTiming[1];
    private readonly List<float> _frameMs = new List<float>(1024);
    private readonly List<float> _gpuMs = new List<float>(1024);
    private readonly List<float> _workMs = new List<float>(1024);

    private TextMeshPro _label;
    private Vector3 _anchor;
    private Mesh _unitCube;
    private Shader _markShader;
    private Shader _volumeShader;
    private NativeArray<float3> _unitCorners;
    private NativeArray<uint> _unitIndices;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create() => Spawn.Once(ref _instance);

    private IEnumerator Start()
    {
        _markShader = Resources.Load<Shader>("Experiments/MarkBuffer");
        _volumeShader = Resources.Load<Shader>("Experiments/VolumeMarch");
        if (_markShader == null || _volumeShader == null)
        {
            Debug.LogError($"{Tag} The experiment shaders were not found.");
            yield break;
        }

        while (!LoadingPhase.Done || CameraRig.MainTransform == null) yield return null;
        yield return new WaitForSeconds(3f);

        Transform head = CameraRig.MainTransform;
        _anchor = head.position + CameraRig.FlatForward * AheadMetres;
        BuildUnitCube();
        BuildLabel();

        Note($"device={SystemInfo.deviceModel} gpu={SystemInfo.graphicsDeviceName} api={SystemInfo.graphicsDeviceType} " +
             $"stereo={UnityEngine.XR.XRSettings.stereoRenderingMode} refresh={RefreshRate():F0}Hz compute={SystemInfo.supportsComputeShaders}");

        foreach (int count in new[] { 5000, 25000, 75000 })
            yield return RunIndirect(count);
        foreach (int count in new[] { 5000, 25000, 75000 })
            yield return RunBigMesh(count);

        yield return RunUpdates(false);
        yield return RunUpdates(true);

        foreach (int side in new[] { 64, 128 })
            foreach (int steps in new[] { 16, 32, 48, 64 })
                yield return RunVolume(side, steps);

        string path = Path.Combine(Application.persistentDataPath, "experiments.txt");
        File.WriteAllText(path, _results.ToString());
        Debug.Log($"{Tag} Finished. Results are in {path}");
        _label.text = "Experiments finished.\nTake the headset off and report what you saw.";

        _unitCorners.Dispose();
        _unitIndices.Dispose();
    }

    private IEnumerator RunIndirect(int count)
    {
        int side = Mathf.CeilToInt(Mathf.Pow(count, 1f / 3f));
        var clock = Stopwatch.StartNew();
        var marks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<Mark>());
        var data = new NativeArray<Mark>(count, Allocator.Persistent);
        new FillMarks { marks = data, origin = _anchor, field = FieldMetres, side = side }.Schedule(count, 1024).Complete();
        marks.SetData(data);
        data.Dispose();
        var commands = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commands.SetData(new[]
        {
            new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = _unitCube.GetIndexCount(0), instanceCount = (uint)count }
        });
        double createMs = clock.Elapsed.TotalMilliseconds;

        var material = new Material(_markShader);
        var parameters = new RenderParams(material)
        {
            worldBounds = Everywhere,
            matProps = new MaterialPropertyBlock(),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
        parameters.matProps.SetBuffer(MarksId, marks);

        yield return Measure($"indirect {count} boxes", "Stereo check: is this one solid block of boxes at the right depth in both eyes?",
            () => Graphics.RenderMeshIndirect(in parameters, _unitCube, commands), $"create={createMs:F2}ms");

        Destroy(material);
        marks.Dispose();
        commands.Dispose();
    }

    private IEnumerator RunBigMesh(int count)
    {
        int side = Mathf.CeilToInt(Mathf.Pow(count, 1f / 3f));
        var clock = Stopwatch.StartNew();
        var marks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<Mark>());
        var data = new NativeArray<Mark>(count, Allocator.Persistent);
        new FillMarks { marks = data, origin = _anchor, field = FieldMetres, side = side }.Schedule(count, 1024).Complete();
        marks.SetData(data);
        data.Dispose();
        Mesh mesh = BuildBigMesh(count);
        double createMs = clock.Elapsed.TotalMilliseconds;

        var material = new Material(_markShader);
        material.EnableKeyword("MARK_ID_IN_UV");
        var parameters = new RenderParams(material)
        {
            worldBounds = Everywhere,
            matProps = new MaterialPropertyBlock(),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
        parameters.matProps.SetBuffer(MarksId, marks);

        yield return Measure($"bigmesh {count} boxes", "Fallback check: is this one solid block of boxes at the right depth in both eyes?",
            () => Graphics.RenderMesh(in parameters, mesh, 0, Matrix4x4.identity), $"create={createMs:F2}ms");

        Destroy(mesh);
        Destroy(material);
        marks.Dispose();
    }

    private Mesh BuildBigMesh(int count)
    {
        var mesh = new Mesh { name = "ExperimentBigMesh", indexFormat = IndexFormat.UInt32 };
        Mesh.MeshDataArray writable = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = writable[0];
        int vertices = _unitCorners.Length * count;
        int indices = _unitIndices.Length * count;
        meshData.SetVertexBufferParams(vertices,
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 1));
        meshData.SetIndexBufferParams(indices, IndexFormat.UInt32);
        new FillBigMesh
        {
            corners = meshData.GetVertexData<Corner>(),
            indices = meshData.GetIndexData<uint>(),
            unitCorners = _unitCorners,
            unitIndices = _unitIndices,
            count = count
        }.Schedule().Complete();
        meshData.subMeshCount = 1;
        const MeshUpdateFlags Quick = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
                                      MeshUpdateFlags.DontNotifyMeshUsers;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, indices) { bounds = Everywhere, vertexCount = vertices }, Quick);
        Mesh.ApplyAndDisposeWritableMeshData(writable, mesh, Quick);
        mesh.bounds = Everywhere;
        return mesh;
    }

    private IEnumerator RunUpdates(bool locked)
    {
        int side = Mathf.CeilToInt(Mathf.Pow(UpdateMarks, 1f / 3f));
        GraphicsBuffer.UsageFlags usage = locked ? GraphicsBuffer.UsageFlags.LockBufferForWrite : GraphicsBuffer.UsageFlags.None;
        var marks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, usage, UpdateMarks, Marshal.SizeOf<Mark>());
        var staging = new NativeArray<Mark>(UpdateMarks, Allocator.Persistent);
        var commands = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commands.SetData(new[]
        {
            new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = _unitCube.GetIndexCount(0), instanceCount = UpdateMarks }
        });

        var material = new Material(_markShader);
        var parameters = new RenderParams(material)
        {
            worldBounds = Everywhere,
            matProps = new MaterialPropertyBlock(),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
        parameters.matProps.SetBuffer(MarksId, marks);

        string name = locked ? "update locked" : "update setdata";
        var clock = new Stopwatch();
        yield return Measure($"{name} {UpdateMarks} boxes", "Buffer check: the block should ripple smoothly.", () =>
        {
            clock.Restart();
            var fill = new FillMarks { origin = _anchor, field = FieldMetres, side = side, time = Time.time };
            if (locked)
            {
                fill.marks = marks.LockBufferForWrite<Mark>(0, UpdateMarks);
                fill.Schedule(UpdateMarks, 1024).Complete();
                marks.UnlockBufferAfterWrite<Mark>(UpdateMarks);
            }
            else
            {
                fill.marks = staging;
                fill.Schedule(UpdateMarks, 1024).Complete();
                marks.SetData(staging);
            }
            _workMs.Add((float)clock.Elapsed.TotalMilliseconds);
            Graphics.RenderMeshIndirect(in parameters, _unitCube, commands);
        }, "");

        Destroy(material);
        staging.Dispose();
        marks.Dispose();
        commands.Dispose();
    }

    private IEnumerator RunVolume(int side, int steps)
    {
        var clock = Stopwatch.StartNew();
        var volume = new Texture3D(side, side, side, TextureFormat.R8, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var voxels = new NativeArray<byte>(side * side * side, Allocator.Persistent);
        new FillVolume { voxels = voxels, side = side }.Schedule(side, 1).Complete();
        double fillMs = clock.Elapsed.TotalMilliseconds;
        volume.SetPixelData(voxels, 0);
        volume.Apply(false, true);
        double uploadMs = clock.Elapsed.TotalMilliseconds - fillMs;
        voxels.Dispose();

        var material = new Material(_volumeShader);
        var parameters = new RenderParams(material)
        {
            worldBounds = Everywhere,
            matProps = new MaterialPropertyBlock(),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
        parameters.matProps.SetTexture(VolumeId, volume);
        parameters.matProps.SetFloat(StepsId, steps);
        Matrix4x4 pose = Matrix4x4.TRS(_anchor, Quaternion.identity, Vector3.one * VolumeMetres);

        yield return Measure($"volume {side} cube {steps} steps", "Volume check: stand still and look at the cloud. Is it smooth?",
            () => Graphics.RenderMesh(in parameters, _unitCube, 0, pose), $"fill={fillMs:F2}ms upload={uploadMs:F2}ms");

        Destroy(material);
        Destroy(volume);
    }

    private IEnumerator Measure(string name, string prompt, Action draw, string extra)
    {
        _label.text = $"{name}\n<size=60%>{prompt}</size>";
        Debug.Log($"{Tag} starting {name}");

        float elapsed = 0f;
        _frameMs.Clear();
        _gpuMs.Clear();
        _workMs.Clear();
        while (elapsed < SettleSeconds + MeasureSeconds)
        {
            draw();
            yield return null;

            if (!Application.isFocused)
            {
                elapsed = 0f;
                _frameMs.Clear();
                _gpuMs.Clear();
                _workMs.Clear();
                continue;
            }

            float delta = Time.unscaledDeltaTime;
            elapsed += delta;
            if (elapsed < SettleSeconds)
            {
                _workMs.Clear();
                continue;
            }

            _frameMs.Add(delta * 1000f);
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timing) > 0) _gpuMs.Add((float)_timing[0].gpuFrameTime);
        }

        float budget = 1000f / RefreshRate();
        int missed = 0;
        foreach (float ms in _frameMs)
            if (ms > budget * 1.5f) missed++;
        Note($"{name}: frames={_frameMs.Count} frameMs avg={Average(_frameMs):F2} p95={Percentile(_frameMs, 0.95f):F2} " +
             $"gpuMs avg={Average(_gpuMs):F2} p95={Percentile(_gpuMs, 0.95f):F2} " +
             $"workMs avg={Average(_workMs):F2} p95={Percentile(_workMs, 0.95f):F2} missed={missed} {extra}");
    }

    private void Note(string line)
    {
        _results.AppendLine(line);
        Debug.Log($"{Tag} {line}");
    }

    private static float RefreshRate()
    {
        float rate = UnityEngine.XR.XRDevice.refreshRate;
        return rate > 1f ? rate : 72f;
    }

    private static float Average(List<float> values)
    {
        if (values.Count == 0) return 0f;
        float sum = 0f;
        foreach (float v in values) sum += v;
        return sum / values.Count;
    }

    private static float Percentile(List<float> values, float share)
    {
        if (values.Count == 0) return 0f;
        var sorted = new List<float>(values);
        sorted.Sort();
        return sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Count * share) - 1, 0, sorted.Count - 1)];
    }

    private void BuildUnitCube()
    {
        Vector3[] corners =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
        };
        int[] triangles =
        {
            0, 2, 1, 0, 3, 2, 1, 6, 5, 1, 2, 6, 5, 7, 4, 5, 6, 7,
            4, 3, 0, 4, 7, 3, 3, 6, 2, 3, 7, 6, 4, 1, 5, 4, 0, 1
        };
        _unitCube = new Mesh { name = "ExperimentCube", vertices = corners, triangles = triangles };
        _unitCube.bounds = Everywhere;

        _unitCorners = new NativeArray<float3>(corners.Length, Allocator.Persistent);
        for (int i = 0; i < corners.Length; i++) _unitCorners[i] = corners[i];
        _unitIndices = new NativeArray<uint>(triangles.Length, Allocator.Persistent);
        for (int i = 0; i < triangles.Length; i++) _unitIndices[i] = (uint)triangles[i];
    }

    private void BuildLabel()
    {
        _label = new GameObject("ExperimentLabel").AddComponent<TextMeshPro>();
        _label.transform.SetParent(transform, false);
        Style.ApplyWorldTitle(_label);
        _label.alignment = TextAlignmentOptions.Center;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.color = Style.White;
        _label.rectTransform.sizeDelta = new Vector2(60f, 10f);
        _label.transform.localScale = Vector3.one * (Style.WorldTextScale * 2f);
        Vector3 forward = CameraRig.FlatForward;
        _label.transform.SetPositionAndRotation(_anchor + Vector3.up * 0.55f, Quaternion.LookRotation(forward));
    }
}
#endif
