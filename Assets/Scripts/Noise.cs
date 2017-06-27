using UnityEngine;
public static class Noise
{
    /// <summary>
    /// Generates a noise map
    /// </summary>
    /// <param name="mapWidth">Width of Map</param>
    /// <param name="mapHeight">Height of Map</param>
    /// <param name="seed">To get a random map for each seed value</param>
    /// <param name="noiseScale">zoom in or out on the map</param>
    /// <param name="octaves">controls how every parameter affect final texture with higher octaves</param>
    /// <param name="persistance">Clamps the amplitude as octaves increases</param>
    /// <param name="lacunarity">increases the frequency as octaves increases</param>
    /// <param name="offset">To scroll through the noiseMap</param>
    /// <returns></returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float noiseScale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        if (noiseScale <= 0)
        {
            noiseScale = 0.0001f;
        }
        // psuedo random number gen
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 1000000) + offset.x;
            float offsetY = prng.Next(-100000, 1000000) + offset.y;
            Vector2 octaveOffset = new Vector2(offsetX, offsetY);
            octaveOffsets[i] = octaveOffset;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // To zoom in or out using noiseScale from the center rather than top right corner
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {

            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    // higer the frequency, more far apart the sampled values will be
                    float sampleX = (x - halfWidth) / noiseScale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / noiseScale * frequency + octaveOffsets[i].y;

                    // perlinnoise returns from 0 to 1
                    // for prelinvalue be between =1 to 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    // Amplitude decreases every iteration as persistance < 0
                    amplitude *= persistance;
                    // frequency increases every iteration as lacunarity > 0
                    frequency *= lacunarity;

                }
                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}
