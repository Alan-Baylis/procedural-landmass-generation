using UnityEngine;

public static class MeshGenerator
{

    public static MeshData GenerateMeshFromHeighMap(float[,] heightMap, AnimationCurve _heightCurve, float heightMultipler, int levelOfDetail)
    {
        AnimationCurve heightCurve = _heightCurve;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        // to center the vertices of mesh we calculate with respect to top left coordinate on the x-z plane
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultipler, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                // triangles dont start at right edge and bottom edge vertices
                // hence ignored
                if (x < (width - 1) && y < (height - 1))
                {
                    /* in a mesh the vertices are
                     *     A i -- D i+1 --  i+2    .....
                     *      |   \  |     \
                     *     B i+w   C i+w+1 -- i+w+2  ....
                     *      follow one order
                     *      here clockwise
                     *      hence a triangle ACB A -> i, B->i+w C-> i+w+1
                     *      another triangle CAD C -> i+w+1 A -> i D-> i+1
                     */
                    meshData.AddTriangleABC(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangleABC(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                vertexIndex++;
            }
        }
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    private int triangleIndex = 0;

    public MeshData(int width, int height)
    {
        /* Total number of vertices in a mesh = width * height
         *                  width = 3
         *                  * * * height = 3
         *                  * * *
         *                  * * *
         * total vertices = 9
         */
        vertices = new Vector3[width * height];

        /* Total triangles in a mesh of width and height ->
         *              width = 3
         *              *---*---*
         *              |1\2|3\4|
         *              *---*---*   height = 3
         *              |5\6|7\8|
         *              *---*---*
         *              number of edges in all triangles = [(3-1)*(3-1)boxes]*(2 triangles in a box)*(3 edges in one triangle)
         *                                               = 24
         */
        triangles = new int[(width - 1) * (height - 1) * 6];
        // each point represented as a ration of x to width & y to height
        uvs = new Vector2[width * height];
    }

    public void AddTriangleABC(int vertexA, int vertexB, int vertexC)
    {
        triangles[triangleIndex] = vertexA;
        triangles[triangleIndex + 1] = vertexB;
        triangles[triangleIndex + 2] = vertexC;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        return mesh;
    }
}
