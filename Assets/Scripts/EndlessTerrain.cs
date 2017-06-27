using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public LODInfo[] detailLevels;
    public static float maxViewDist;
    public Transform viewer;
    public Material mapMaterial;
    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    int chunkSize;
    int chunksVisibleInViewDst;
    const float viewerDistForUpdate = 25f;
    const float sqrViewerDistForUpdate = viewerDistForUpdate * viewerDistForUpdate;

    Dictionary<Vector2, TerrainChunk> terrainChunksDictionary;
    List<TerrainChunk> terrainChunksVisibleLastUpdate;

    static MapGenerator mapGenerator;

    // Use this for initialization
    void Start()
    {
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        mapGenerator = FindObjectOfType<MapGenerator>();
        terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
        terrainChunksDictionary = new Dictionary<Vector2, TerrainChunk>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDist / chunkSize);
        viewerPositionOld = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        if ((viewerPositionOld - viewerPosition).sqrMagnitude >= viewerDistForUpdate)
        {
            UpdateVisibleChunks();
            viewerPositionOld = viewerPosition;
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentCoorX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentCoorY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 currentChunkPos = new Vector2(currentCoorX + xOffset, currentCoorY + yOffset);
                if (terrainChunksDictionary.ContainsKey(currentChunkPos))
                {
                    terrainChunksDictionary[currentChunkPos].UpdateTerrainChunk();
                    if (terrainChunksDictionary[currentChunkPos].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunksDictionary[currentChunkPos]);
                    }
                }
                else
                {
                    terrainChunksDictionary.Add(currentChunkPos, new TerrainChunk(currentChunkPos, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool hasMapData;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coor, int size, LODInfo[] detailLevels, Transform parent, Material mapMaterial)
        {
            this.detailLevels = detailLevels;
            position = coor * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = mapMaterial;
            meshObject.transform.position = positionV3;
            meshObject.transform.SetParent(parent);
            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }
            SetVisible(false);

            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            hasMapData = true;
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            UpdateTerrainChunk();
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        public void UpdateTerrainChunk()
        {
            if (hasMapData)
            {
                float minDistanceFromViewer = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = minDistanceFromViewer <= maxViewDist;
                if (visible)
                {
                    // Check in which range doesn the distanceFromViewer falls into
                    int currentLODIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (minDistanceFromViewer > detailLevels[i].visibleDistThreshold)
                        {
                            currentLODIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Load the subsequent LODMesh
                    if (currentLODIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[currentLODIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = currentLODIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMeshData(mapData);
                        }
                    }
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
    }
    public class LODMesh
    {
        public bool hasRequestedMesh;
        public bool hasMesh;
        public Mesh mesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        public void RequestMeshData(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshRecieved);
        }

        void OnMeshRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public int visibleDistThreshold;
    }
}
