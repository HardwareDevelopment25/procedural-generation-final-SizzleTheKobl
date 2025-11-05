using Unity.Mathematics;
using UnityEngine;

public static class perlinNoise
{



    public static float[,] CreatePerlinNoise(int height, int width, int octaves, float scalar, float lacunarity, float persistence, int seed, Vector2 offset) 
    {
        float[,] noiseMap = new float[height, width];
        if (scalar == 0) { scalar = 0.001f; }

        float maxPossibleHeight = float.MinValue; //0 is black, 1 is white
        float minPossibleHeight = float.MaxValue;

        System.Random random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offx = random.Next(-100000, 100000) + offset.x;
            float offy = random.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offx, offy);
        }

        for (int y = 0; y < height; y++) 
        {
            for (int x = 0; x < width; x++) 
            {
                float amp = 1, freq = 1, noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (float)(x - (width / 2)) / scalar * freq + octaveOffsets[i].x;
                    float sampleY = (float)(y - (height / 2)) / scalar * freq + octaveOffsets[i].y;
                    float perlinResult = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinResult * amp;
                    amp *= persistence;
                    freq *= lacunarity;

                }

                if (noiseHeight > maxPossibleHeight) { maxPossibleHeight = noiseHeight; }
                else if (noiseHeight < minPossibleHeight) { minPossibleHeight = noiseHeight; }
                noiseMap[x, y] = Mathf.InverseLerp(minPossibleHeight, maxPossibleHeight, noiseHeight);
            }
        }
        return noiseMap;
    }
}
