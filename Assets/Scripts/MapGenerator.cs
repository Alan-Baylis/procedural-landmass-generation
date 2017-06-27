using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // Defines the drawmode
    public enum DrawMode { NoiseMap, ColorMap, MeshMap };
    public DrawMode drawMode;
    // makes so that levelOfDetail can be between 0 to 6
    public const int mapChunkSize = 241;
    [Range(0, 6)]
    public int editorLOD;
    public AnimationCurve heightCurve;

    public int octaves;
    public int seed;

    public float heightMultiplier;
    public float noiseScale;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public Vector2 offset;

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


    public void DrawMapInEditor()
    {
        MapData mapdata = GenerateMapData(Vector2.zero);
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.ColorMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapdata.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.NoiseMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapdata.heightMap));
        }
        else if (drawMode == DrawMode.MeshMap)
        {
            mapDisplay.DrawMesh(MeshGenerator.GenerateMeshFromHeighMap(mapdata.heightMap, heightCurve, heightMultiplier, editorLOD), TextureGenerator.TextureFromColorMap(mapdata.colorMap, mapChunkSize, mapChunkSize));
        }

    }

    // Request Mapdata by starting it's work on a new thread
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }

    // This method runs on a different thread
    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        // critical section for the thread
        // modifying the queue at the same time when another thread is reading from it can
        // cause race condition
        // to avoid we use locks
        lock (mapThreadInfoQueue)
        {
            // producer part
            mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    // Request Mapdata by starting it's work on a new thread
    public void RequestMeshData(MapData mapData, int LOD, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, LOD, callback);
        };

        new Thread(threadStart).Start();
    }

    // This method runs on a different thread
    void MeshDataThread(MapData mapData, int LOD, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateMeshFromHeighMap(mapData.heightMap, heightCurve, heightMultiplier, LOD);
        // critical section for the thread
        // modifying the queue at the same time when another thread is reading from it can
        // cause race condition
        // to avoid we use locks
        lock (meshThreadInfoQueue)
        {
            // producer part
            meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }


    private void Update()
    {
        // like producer consumer problem
        // consumer part
        if (mapThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int x = 0; x < mapChunkSize; x++)
        {
            for (int y = 0; y < mapChunkSize; y++)
            {
                float noiseHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (noiseHeight < regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);

    }
    private void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (noiseScale < 0)
        {
            noiseScale = 0;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
[System.Serializable]
public struct TerrainType
{
    public string name;
    public Color color;
    public float height;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
