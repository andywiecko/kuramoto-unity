using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Kuramoto : MonoBehaviour
{
    [Min(2)] public int width = 100;
    [Min(2)] public int height = 100;

    [Range(-math.PI, math.PI)] public float alpha = 0;
    public float omega = 1;
    public float K = 1;
    [Range(0, 5)] public int R = 1;
    public float dt = 0.016f;
    public int steps = 16;
    public FilterMode filterMode = FilterMode.Point;

    private Texture2D texture;
    private Mesh mesh;

    private NativeArray<float> positions, velocities;
    private NativeArray<Color32> colors;

    private void Start()
    {
        texture = new(width, height, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = filterMode,
            wrapMode = TextureWrapMode.Clamp
        };

        mesh = new()
        {
            vertices = new Vector3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(1, 1, 0),
                new(0, 1, 0),
            },
            uv = new Vector2[]
            {
                new(0, 0),
                new(1, 0),
                new(1, 1),
                new(0, 1),
            },
            triangles = new int[]
            {
                1, 0, 2, 0, 3, 2,
            }
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        var renderer = GetComponent<MeshRenderer>();
        renderer.material.mainTexture = texture;
        var filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;

        positions = new NativeArray<float>(width * height, Allocator.Persistent);
        velocities = new NativeArray<float>(width * height, Allocator.Persistent);
        colors = new NativeArray<Color32>(width * height, Allocator.Persistent);

        Randomize();
    }

    public void Randomize()
    {
        new InitializeJob(this).Run();
    }

    private void OnDestroy()
    {
        positions.Dispose();
        velocities.Dispose();
        colors.Dispose();
    }

    private void Update()
    {
        var handle = default(JobHandle);

        for (int i = 0; i < steps; i++)
        {
            handle = new UpdateVelocitiesJob(this).Schedule(positions.Length, 64, handle);
            handle = new UpdatePositionsJob(this).Schedule(positions.Length, 64, handle);
        }

        handle = new UpdateImageJob(this).Schedule(colors.Length, 64, handle);

        handle.Complete();

        texture.SetPixelData(colors, 0);
        texture.Apply();
    }

    [BurstCompile]
    private struct UpdateVelocitiesJob : IJobParallelFor
    {
        private NativeArray<float>.ReadOnly theta;
        private NativeArray<float> dtheta;
        private readonly float omega, K, alpha;
        private readonly int width, height, R;

        public UpdateVelocitiesJob(Kuramoto k)
        {
            theta = k.positions.AsReadOnly();
            dtheta = k.velocities;
            omega = k.omega;
            K = k.K;
            R = k.R;
            width = k.width;
            height = k.height;
            alpha = k.alpha;
        }

        public void Execute(int i)
        {
            var x0 = i % width;
            var y0 = i / width;
            var ti = theta[i];

            var sum = 0f;
            var count = 0;
            for (int x = -R; x <= R; x++)
            {
                for (int y = -R; y <= R; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var x1 = (x0 + x + width) % width;
                    var y1 = (y0 + y + height) % height;
                    var j = y1 * width + x1;
                    var tj = theta[j];
                    sum += K * math.sin(tj - ti + alpha);
                    count++;
                }
            }

            count = count == 0 ? 1 : count;
            dtheta[i] = omega + sum / count;
        }
    }

    [BurstCompile]
    private struct UpdatePositionsJob : IJobParallelFor
    {
        private NativeArray<float>.ReadOnly dtheta;
        private NativeArray<float> theta;
        private readonly float dt;

        public UpdatePositionsJob(Kuramoto k)
        {
            dtheta = k.velocities.AsReadOnly();
            theta = k.positions;
            dt = k.dt / k.steps;
        }

        public void Execute(int i)
        {
            var t = theta[i];
            t += dtheta[i] * dt;
            t -= math.PI2 * math.floor((t + math.PI) / math.PI2);
            theta[i] = t;
        }
    }

    [BurstCompile]
    private struct InitializeJob : IJob
    {
        private NativeArray<float> data;

        public InitializeJob(Kuramoto k)
        {
            data = k.positions;
        }

        public void Execute()
        {
            const float pi = math.PI;
            var random = new Unity.Mathematics.Random(seed: 42);
            foreach (ref var i in data.AsSpan())
            {
                i = random.NextFloat(-pi, pi);
            }
        }
    }

    [BurstCompile]
    private struct UpdateImageJob : IJobParallelFor
    {
        public NativeArray<float>.ReadOnly positions;
        public NativeArray<Color32> colors;
        public int width;
        public float omega;

        public UpdateImageJob(Kuramoto k)
        {
            positions = k.positions.AsReadOnly();
            colors = k.colors;
            width = k.width;
            omega = k.omega;
        }

        public void Execute(int index)
        {
            var t = positions[index];
            math.sincos(t, out var s, out var c);
            var r = 0.5f * (1 + c);
            var g = 0.5f * (1 + s);
            var b = 0.5f * (1 + math.cos(t + math.PI)); ;
            colors[index] = new Color(r, g, b, 1);
        }
    }
}
