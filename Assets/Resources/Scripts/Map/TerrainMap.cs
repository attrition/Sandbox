using UnityEngine;
using System.Collections.Generic;

public class TerrainMap : MonoBehaviour
{
    public int Seed = 1337;
    public float ClipX = 0f;
    public float ClipY = 0f;
    public float ClipSize = 1f;

    public float Frequency = 1f;
    public float Lacunarity = 2f;
    public float Persistence = 0.5f;
    public int Octaves = 6;

    public int Size = 64;
    public int ChunksPerSide = 1;
    private int TotalSize = 64;
    public float Scaling = 15f;
    public float ScaleBias = 0f;

    public Texture2D HeightMap = null;
    public Texture2D NormalMap = null;
    public float[] floatMap = null;

    public Mesh Mesh = null;
    public Material Material = null;
    public GameObject Water = null;

    void Start()
    {
        TotalSize = ChunksPerSide * Size;

        Generate();
    }

    public void Generate()
    {
        GenerateHeightMap();
        GenerateMesh();
        FillWater();
    }

    public float HeightAt(int x, int y)
    {
        return floatMap[y * TotalSize + x];
    }

    public void FillWater()
    {
        if (Water)
        {
            Water.transform.position = new Vector3(TotalSize / 2 - 0.5f, 4.7f, TotalSize / 2 - 0.5f);
            Water.transform.localScale = new Vector3(TotalSize - 1, 10, TotalSize - 1);
        }
    }

    public void GenerateHeightMap()
    {
        var perlin = new LibNoise.Unity.Generator.Perlin(Frequency, Lacunarity, Persistence, Octaves, Seed, LibNoise.Unity.QualityMode.High);
        //var ridged = new LibNoise.Unity.Generator.RidgedMultifractal(Frequency, Lacunarity, Octaves, Seed, LibNoise.Unity.QualityMode.High);
        //var turb = new LibNoise.Unity.Operator.Turbulence(0.25, perlin);
        var scaled = new LibNoise.Unity.Operator.ScaleBias(Scaling, ScaleBias, perlin);
        var final = new LibNoise.Unity.Operator.Terrace(false, scaled);

        for (int i = 0; i < 40; i += 10)
            final.Add(i);

        var noise = new LibNoise.Unity.Noise2D(TotalSize, final);
        noise.GeneratePlanar(ClipX, ClipX + (ClipSize * ChunksPerSide), ClipY, ClipY + (ClipSize * ChunksPerSide));

        GameObject.Destroy(HeightMap);
        HeightMap = noise.GetTexture(LibNoise.Unity.Gradient.Grayscale);
        HeightMap.Apply();

        GameObject.Destroy(NormalMap);
        NormalMap = noise.GetNormalMap(1f);
        NormalMap.Apply();

        floatMap = new float[TotalSize * TotalSize];
        for (int y = 0; y < TotalSize; y++)
            for (int x = 0; x < TotalSize; x++)
                floatMap[y * TotalSize + x] = noise[x, y];
    }

    public void GenerateMesh()
    {
        if (HeightMap == null)
        {
            Debug.Log("Tried to generate terrain mesh, but heightmap is null");
            return;
        }

        for (int chunkZ = 0; chunkZ < ChunksPerSide; chunkZ++)
        {
            for (int chunkX = 0; chunkX < ChunksPerSide; chunkX++)
            {
                var verts = new List<Vector3>();
                var idxs = new List<int>();
                var uvs = new List<Vector2>();

                // very basic mesh
                var i = 0;
                var currSizeX = Size;
                var currSizeZ = Size;
                if (chunkX < ChunksPerSide - 1)
                    currSizeX = Size + 1;
                if (chunkZ < ChunksPerSide - 1)
                    currSizeZ = Size + 1;

                for (int z = 0; z < currSizeZ; z++)
                {
                    for (int x = 0; x < currSizeX; x++)
                    {
                        var absX = x + (chunkX * Size);
                        var absZ = z + (chunkZ * Size);

                        var height = HeightAt(absX, absZ);
                        //Debug.Log(string.Format("[{0},{1}] = {2}", absX, absZ, height));
                        verts.Add(new Vector3(x, height, z));
                        uvs.Add(new Vector2(x, z));

                        if (x < currSizeX - 1 && z < currSizeZ - 1)
                        {
                            idxs.Add(i);
                            idxs.Add(i + currSizeX);
                            idxs.Add(i + currSizeX + 1);

                            idxs.Add(i + currSizeX + 1);
                            idxs.Add(i + 1);
                            idxs.Add(i);
                        }
                        i++;
                    }
                }

                var name = "Terrain Chunk [" + chunkX + ":" + chunkZ + "]";
                var go = new GameObject(name);

                Mesh = new Mesh();
                Mesh.name = name;
                Mesh.vertices = verts.ToArray();
                Mesh.triangles = idxs.ToArray();
                Mesh.uv = uvs.ToArray();
                Mesh.RecalculateNormals();

                var rend = go.AddComponent<MeshRenderer>();
                var filt = go.AddComponent<MeshFilter>();
                var coll = go.AddComponent<MeshCollider>();

                GameObject.Destroy(rend.material);
                rend.material = Material;

                GameObject.Destroy(filt.sharedMesh);
                filt.sharedMesh = Mesh;

                GameObject.Destroy(coll.sharedMesh);
                coll.sharedMesh = Mesh;

                go.transform.position = new Vector3(chunkX * Size, 0f, chunkZ * Size);
                go.transform.parent = this.gameObject.transform;
            }
        }
    }

}

