using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    // Defines the drawmode
    public enum DrawMode { NoiseMap, ColorMap,MeshMap};
    public DrawMode drawMode;
    // makes so that levelOfDetail can be between 0 to 6
    const int mapChunkSize = 241;
    [Range(0,6)]
    public int levelOfDetail;
    public AnimationCurve heightCurve;

    public int octaves;
    public int seed;

    public float heightMultiplier;
    public float noiseScale;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public Vector2 offset;

    public bool autoUpdate;

    public TerrainType[] regions;

	public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int x = 0; x < mapChunkSize; x++)
        {
            for(int y = 0; y < mapChunkSize; y++)
            {
                float noiseHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++)
                {
                    if(noiseHeight < regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }


        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        if (drawMode == DrawMode.ColorMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }else if(drawMode == DrawMode.NoiseMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }else if(drawMode == DrawMode.MeshMap)
        {
            mapDisplay.DrawMesh(MeshGenerator.GenerateMeshFromHeighMap(noiseMap, heightCurve, heightMultiplier, levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }


    }
    private void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 0)
        {
            octaves = 0;
        }
        if (noiseScale < 0)
        {
            noiseScale = 0;
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
