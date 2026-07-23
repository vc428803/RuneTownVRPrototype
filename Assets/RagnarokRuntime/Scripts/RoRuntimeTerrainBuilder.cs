using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class RoRuntimeTerrainBuilder : MonoBehaviour
{

    // 修改原因：
    // 對應 Reference Project 匯出的 prontera_atlas_lookup.json。
    // 每筆資料保存原始貼圖名稱及其在 prontera_atlas.png 中的正規化 Rect。
    [System.Serializable]
    private class AtlasLookupFile
    {
        public string mapName;
        public int atlasWidth;
        public int atlasHeight;
        public List<AtlasLookupEntry> entries;
    }

    [System.Serializable]
    private class AtlasLookupEntry
    {
        public string textureName;
        public int textureIndex;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    // 修改原因：
    // Runtime 建立 Mesh 時，依 cell.Top.Texture 快速找到對應 Atlas Rect。
    private readonly Dictionary<string, Rect> atlasRectLookup =
        new Dictionary<string, Rect>();

    // 修改原因：
    // 每一種 Ragnarok 貼圖需要保存自己獨立的頂點、三角形、UV 和顏色資料。
    private class TopTextureMeshGroup
    {
        public Texture2D Texture;

        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        public readonly List<Color> Colors = new List<Color>();

        public int TileCount;
    }

    [Header("Runtime mesh test")]
    [SerializeField] private float cellSize = 1f;

    // 修改原因：
    // 對應原專案 RoMapData.YScale 的用途。
    // 目前預設為 1，先維持既有高度比例；確認原專案實際 YScale 後可在 Inspector 調整。
    [SerializeField] private float heightScale = 0.20f;
    [Header("Runtime texture test")]
    [SerializeField] private Texture2D testTopTexture;

    // 修改原因：
    // Reference Project 已產生 prontera_atlas.png。
    // prontera.bytes 內保存的是各原始貼圖的局部 UV；Runtime 會依 Lookup Rect Remap 到 Atlas。
    [SerializeField] private Texture2D pronteraAtlas;

    // 修改原因：
    // 保存由 Reference Project 匯出的貼圖名稱 → Atlas Rect 對照表。
    [SerializeField] private TextAsset pronteraAtlasLookup;
    // 修改原因：保存 Prontera 測試 Chunk 實際需要的四張貼圖，
    // 下一步會根據 cell.Top.Texture 自動選擇正確貼圖。
    [SerializeField]
    private Texture2D prtGrass01Texture;

    [SerializeField]
    private Texture2D prtCity01Texture;

    [SerializeField]
    private Texture2D prtCity02Texture;

    [SerializeField]
    private Texture2D prtCity04Texture;
    [Header("Prontera terrain data")]
    [SerializeField] private TextAsset mapCellData;
    [SerializeField] private int mapWidth = 208;
    [SerializeField] private int mapHeight = 160;

    // 修改原因：
    // 完整對齊 Reference Project 的 RoMapEditor.ChunkSizeInTiles。
    private const int ChunkSizeInTiles = 16;

    // 修改原因：保存完整地圖 Cell 資料，
    // 供下一階段建立 Runtime Mesh 使用。
    private RoRuntimeCell[] runtimeCells;

    // 修改原因：
    // Reference Project 會先計算每一格 Top Quad 的 Cell Normal，
    // 再只對高度真正相連的鄰格做頂點法線平均。
    // Runtime 不再使用 mesh.RecalculateNormals()，避免每一格獨立計算法線造成規律明暗條紋。
    private Vector3[] runtimeCellNormals;
    private Vector3[][] runtimeTopNormals;

    private void Start()
    {
        if (mapCellData == null)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: Map Cell Data 尚未指定。",
                this
            );
            return;
        }

        if (!BuildAtlasRectLookup())
        {
            return;
        }

        byte[] compressedBytes = mapCellData.bytes;
        byte[] decompressedBytes = CLZF2.Decompress(compressedBytes);

        using (MemoryStream memoryStream = new MemoryStream(decompressedBytes))
        using (BinaryReader reader = new BinaryReader(memoryStream))
        {
            int cellCount = reader.ReadInt32();
            int expectedCellCount = mapWidth * mapHeight;

            if (cellCount != expectedCellCount)
            {
                Debug.LogError(
                    $"RoRuntimeTerrainBuilder: 地圖尺寸不符。Cell 數量 = {cellCount}，" +
                    $"但 {mapWidth} × {mapHeight} = {expectedCellCount}。",
                    this
                );
                return;
            }

            runtimeCells = new RoRuntimeCell[cellCount];

            int topCount = 0;
            int frontCount = 0;
            int rightCount = 0;

            // 修改原因：逐格解析並保存完整 Cell 資料，
            // 同時保留錯位偵錯資訊。
            for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                long cellStartPosition = reader.BaseStream.Position;

                try
                {
                    RoRuntimeCell runtimeCell = new RoRuntimeCell();

                    float h0 = reader.ReadSingle();
                    float h1 = reader.ReadSingle();
                    float h2 = reader.ReadSingle();
                    float h3 = reader.ReadSingle();

                    runtimeCell.Heights = new Vector4(h0, h1, h2, h3);

                    bool hasTop = reader.ReadBoolean();

                    if (hasTop)
                    {
                        runtimeCell.Top = ReadTile(reader);

                        if (runtimeCell.Top.Enabled)
                        {
                            topCount++;
                        }
                    }

                    bool hasFront = reader.ReadBoolean();

                    if (hasFront)
                    {
                        runtimeCell.Front = ReadTile(reader);

                        if (runtimeCell.Front.Enabled)
                        {
                            frontCount++;
                        }
                    }

                    bool hasRight = reader.ReadBoolean();

                    if (hasRight)
                    {
                        runtimeCell.Right = ReadTile(reader);

                        if (runtimeCell.Right.Enabled)
                        {
                            rightCount++;
                        }
                    }

                    runtimeCell.CellType = reader.ReadByte();

                    runtimeCells[cellIndex] = runtimeCell;
                }
                catch (EndOfStreamException)
                {
                    Debug.LogError(
                        $"解析 Cell 本體失敗：" +
                        $"Cell Index = {cellIndex}，" +
                        $"Cell 座標 = ({cellIndex % mapWidth}, {cellIndex / mapWidth})，" +
                        $"Cell 起點 = {cellStartPosition}，" +
                        $"目前位置 = {reader.BaseStream.Position}，" +
                        $"Stream 長度 = {reader.BaseStream.Length}。",
                        this
                    );
                    return;
                }
            }

            long remainingBytes =
                reader.BaseStream.Length - reader.BaseStream.Position;

            Debug.Log(
                $"RoRuntimeTerrainBuilder: 地圖尺寸驗證成功，" +
                $"{mapWidth} × {mapHeight} = {cellCount} Cells。" +
                $"解壓縮大小 = {decompressedBytes.Length} bytes。",
                this
            );

            Debug.Log(
                $"RoRuntimeTerrainBuilder: 全部 Cell 解析完成。" +
                $"Cells = {cellCount}，Top = {topCount}，" +
                $"Front = {frontCount}，Right = {rightCount}，" +
                $"剩餘 Bytes = {remainingBytes}。",
                this
            );

            // 修改原因：
            // 先完整對齊 Reference Project 的 PrepareTopFaces() + SmoothNormals()。
            // 法線必須以整張地圖資料計算，不能在每個 Chunk 內各自 RecalculateNormals()。
            PrepareRuntimeTopNormals();

            // 修改原因：
            // 不再建立單一 64 × 64 測試區域。
            // 改為沿用 RagnarokRebuildTcp 的 16 × 16 Chunk 架構，
            // 建立完整 208 × 160 Prontera Top Terrain。
            BuildFullTopChunkSystem();
        }
    }

    // 修改原因：
    // 驗證 prontera.bytes 內保存的 Top UV 是單張貼圖的 0～1 UV，
    // 還是已經 Remap 過的 Atlas UV。此方法只輸出一次摘要，不修改 Mesh。
    private void DiagnoseTopUvData()
    {
        if (runtimeCells == null ||
            runtimeCells.Length == 0)
        {
            return;
        }

        float minU = float.MaxValue;
        float maxU = float.MinValue;
        float minV = float.MaxValue;
        float maxV = float.MinValue;

        int enabledTopCount = 0;
        int uvValueCount = 0;
        int outsideZeroToOneCount = 0;

        RoRuntimeCell sampleCell = null;
        int sampleCellIndex = -1;

        for (int cellIndex = 0;
             cellIndex < runtimeCells.Length;
             cellIndex++)
        {
            RoRuntimeCell cell =
                runtimeCells[cellIndex];

            if (cell == null ||
                cell.Top == null ||
                !cell.Top.Enabled ||
                cell.Top.UVs == null ||
                cell.Top.UVs.Length != 4)
            {
                continue;
            }

            enabledTopCount++;

            if (sampleCell == null &&
                !string.IsNullOrEmpty(cell.Top.Texture))
            {
                sampleCell = cell;
                sampleCellIndex = cellIndex;
            }

            for (int i = 0;
                 i < cell.Top.UVs.Length;
                 i++)
            {
                Vector2 uv =
                    cell.Top.UVs[i];

                minU = Mathf.Min(minU, uv.x);
                maxU = Mathf.Max(maxU, uv.x);
                minV = Mathf.Min(minV, uv.y);
                maxV = Mathf.Max(maxV, uv.y);

                if (uv.x < 0f ||
                    uv.x > 1f ||
                    uv.y < 0f ||
                    uv.y > 1f)
                {
                    outsideZeroToOneCount++;
                }

                uvValueCount++;
            }
        }

        Debug.Log(
            $"RoRuntimeTerrainBuilder UV 診斷：" +
            $"Enabled Top = {enabledTopCount}，" +
            $"UV Values = {uvValueCount}，" +
            $"U Range = {minU} ～ {maxU}，" +
            $"V Range = {minV} ～ {maxV}，" +
            $"超出 0～1 數量 = {outsideZeroToOneCount}。",
            this
        );

        if (sampleCell == null)
        {
            Debug.LogWarning(
                "RoRuntimeTerrainBuilder UV 診斷：找不到可用的 Top Tile 樣本。",
                this
            );

            return;
        }

        Rect sampleRect =
            GetAtlasRect(sampleCell.Top.Texture);

        Vector2 remapped0 =
            RemapUV(sampleCell.Top.UVs[0], sampleRect);

        Vector2 remapped1 =
            RemapUV(sampleCell.Top.UVs[1], sampleRect);

        Vector2 remapped2 =
            RemapUV(sampleCell.Top.UVs[2], sampleRect);

        Vector2 remapped3 =
            RemapUV(sampleCell.Top.UVs[3], sampleRect);

        int sampleX =
            sampleCellIndex % mapWidth;

        int sampleY =
            sampleCellIndex / mapWidth;

        Debug.Log(
            $"RoRuntimeTerrainBuilder UV 樣本：" +
            $"Cell = ({sampleX}, {sampleY})，" +
            $"Texture = {sampleCell.Top.Texture}，" +
            $"Rect = {sampleRect}，" +
            $"Raw = [{sampleCell.Top.UVs[0]}, {sampleCell.Top.UVs[1]}, " +
            $"{sampleCell.Top.UVs[2]}, {sampleCell.Top.UVs[3]}]，" +
            $"Remapped = [{remapped0}, {remapped1}, {remapped2}, {remapped3}]。",
            this
        );
    }


    // 修改原因：
    // 完整對齊 Reference Project 的 RoMapSharedMeshData.PrepareTopFaces() 與 SmoothNormals()。
    // 第一階段先計算每一格 Quad 的 Cell Normal；第二階段只平均高度相連的鄰格法線。
    private void PrepareRuntimeTopNormals()
    {
        int cellCount = mapWidth * mapHeight;

        runtimeCellNormals = new Vector3[cellCount];
        runtimeTopNormals = new Vector3[cellCount][];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int cellIndex = x + y * mapWidth;
                RoRuntimeCell cell = runtimeCells[cellIndex];

                if (cell == null)
                {
                    runtimeCellNormals[cellIndex] = Vector3.up;
                    runtimeTopNormals[cellIndex] = CreateRepeatedNormals(Vector3.up);
                    continue;
                }

                Vector4 heights = cell.Heights;

                Vector3 p0 = new Vector3(
                    (x + 0f) * cellSize,
                    heights[0] * heightScale,
                    (y + 1f) * cellSize
                );

                Vector3 p1 = new Vector3(
                    (x + 1f) * cellSize,
                    heights[1] * heightScale,
                    (y + 1f) * cellSize
                );

                Vector3 p2 = new Vector3(
                    (x + 0f) * cellSize,
                    heights[2] * heightScale,
                    (y + 0f) * cellSize
                );

                Vector3 p3 = new Vector3(
                    (x + 1f) * cellSize,
                    heights[3] * heightScale,
                    (y + 0f) * cellSize
                );

                runtimeCellNormals[cellIndex] =
                    CalculateQuadNormal(p0, p1, p2, p3);
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int cellIndex = x + y * mapWidth;
                Vector3 initialNormal = runtimeCellNormals[cellIndex];

                runtimeTopNormals[cellIndex] = new[]
                {
                    AverageConnectedNormal(x, y, 0, initialNormal),
                    AverageConnectedNormal(x, y, 1, initialNormal),
                    AverageConnectedNormal(x, y, 2, initialNormal),
                    AverageConnectedNormal(x, y, 3, initialNormal)
                };
            }
        }

        Debug.Log(
            $"RoRuntimeTerrainBuilder: Top 平滑法線建立完成。" +
            $"Cells = {cellCount}，" +
            $"規則 = 僅平均高度相連的鄰格。",
            this
        );
    }

    // 修改原因：
    // Reference 的 CalcQuadNormal 會以同一個 Quad 的兩個 Triangle 建立單一 Cell Normal。
    private static Vector3 CalculateQuadNormal(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3)
    {
        Vector3 normalA =
            Vector3.Cross(p1 - p0, p3 - p0);

        Vector3 normalB =
            Vector3.Cross(p3 - p0, p2 - p0);

        Vector3 normal = normalA + normalB;

        if (normal.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.up;
        }

        return normal.normalized;
    }

    private static Vector3[] CreateRepeatedNormals(Vector3 normal)
    {
        return new[]
        {
            normal,
            normal,
            normal,
            normal
        };
    }

    // 修改原因：
    // 完整對齊 Reference 的 GetNeighborsForVertex()：
    // Vertex 0 = 左上、1 = 右上、2 = 左下、3 = 右下。
    private static int[] GetNeighborIdsForVertex(int vertexId)
    {
        switch (vertexId)
        {
            case 0:
                return new[] { 0, 1, 3 };

            case 1:
                return new[] { 1, 2, 4 };

            case 2:
                return new[] { 3, 5, 6 };

            case 3:
                return new[] { 4, 6, 7 };

            default:
                throw new System.ArgumentOutOfRangeException(
                    nameof(vertexId),
                    vertexId,
                    "Invalid vertex id."
                );
        }
    }

    // 修改原因：
    // Reference 的鄰居順序為：
    // 0 左上、1 上、2 右上、3 左、4 右、5 左下、6 下、7 右下。
    private static Vector2Int GetNeighborDirection(int neighborId)
    {
        switch (neighborId)
        {
            case 0:
                return new Vector2Int(-1, 1);

            case 1:
                return new Vector2Int(0, 1);

            case 2:
                return new Vector2Int(1, 1);

            case 3:
                return new Vector2Int(-1, 0);

            case 4:
                return new Vector2Int(1, 0);

            case 5:
                return new Vector2Int(-1, -1);

            case 6:
                return new Vector2Int(0, -1);

            case 7:
                return new Vector2Int(1, -1);

            default:
                throw new System.ArgumentOutOfRangeException(
                    nameof(neighborId),
                    neighborId,
                    "Invalid neighbor id."
                );
        }
    }

    private Vector3 AverageConnectedNormal(
        int x,
        int y,
        int vertexId,
        Vector3 initialNormal)
    {
        Vector3 normal = initialNormal;
        int count = 1;

        int[] neighborIds =
            GetNeighborIdsForVertex(vertexId);

        for (int i = 0; i < neighborIds.Length; i++)
        {
            Vector2Int relation =
                GetNeighborDirection(neighborIds[i]);

            int neighborX = x + relation.x;
            int neighborY = y + relation.y;

            if (!IsInsideMap(neighborX, neighborY))
            {
                continue;
            }

            if (!IsNeighborVertexConnected(
                    x,
                    y,
                    neighborX,
                    neighborY,
                    relation,
                    vertexId))
            {
                continue;
            }

            int neighborIndex =
                neighborX + neighborY * mapWidth;

            normal += runtimeCellNormals[neighborIndex];
            count++;
        }

        Vector3 averagedNormal = normal / count;

        if (averagedNormal.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.up;
        }

        return averagedNormal.normalized;
    }

    private bool IsInsideMap(int x, int y)
    {
        return x >= 0 &&
               x < mapWidth &&
               y >= 0 &&
               y < mapHeight;
    }

    // 修改原因：
    // 完整搬入 Reference RoMapData.IsNeighborVertexConnected() 的高度連接判斷。
    private bool IsNeighborVertexConnected(
        int originX,
        int originY,
        int neighborX,
        int neighborY,
        Vector2Int relation,
        int vertexId)
    {
        RoRuntimeCell origin =
            runtimeCells[originX + originY * mapWidth];

        RoRuntimeCell neighbor =
            runtimeCells[neighborX + neighborY * mapWidth];

        if (origin == null || neighbor == null)
        {
            return false;
        }

        Vector4 mask =
            GetConnectedNeighborHeightMask(
                origin.Heights,
                neighbor.Heights,
                relation
            );

        Vector4 flipped =
            FlipHeightVector(mask, relation);

        return flipped[vertexId] > 0f;
    }

    private static Vector4 GetConnectedNeighborHeightMask(
        Vector4 origin,
        Vector4 neighbor,
        Vector2Int relation)
    {
        Vector4 flipped =
            FlipHeightVector(origin, relation);

        if (relation.x == -1 && relation.y == -1)
        {
            return CloseEnough(flipped[1], neighbor[1])
                ? new Vector4(0f, 1f, 0f, 0f)
                : Vector4.zero;
        }

        if (relation.x == 1 && relation.y == -1)
        {
            return CloseEnough(flipped[0], neighbor[0])
                ? new Vector4(1f, 0f, 0f, 0f)
                : Vector4.zero;
        }

        if (relation.x == -1 && relation.y == 1)
        {
            return CloseEnough(flipped[3], neighbor[3])
                ? new Vector4(0f, 0f, 0f, 1f)
                : Vector4.zero;
        }

        if (relation.x == 1 && relation.y == 1)
        {
            return CloseEnough(flipped[2], neighbor[2])
                ? new Vector4(0f, 0f, 1f, 0f)
                : Vector4.zero;
        }

        if (relation.x == -1)
        {
            return new Vector4(
                0f,
                CloseEnough(flipped[1], neighbor[1]) ? 1f : 0f,
                0f,
                CloseEnough(flipped[3], neighbor[3]) ? 1f : 0f
            );
        }

        if (relation.x == 1)
        {
            return new Vector4(
                CloseEnough(flipped[0], neighbor[0]) ? 1f : 0f,
                0f,
                CloseEnough(flipped[2], neighbor[2]) ? 1f : 0f,
                0f
            );
        }

        if (relation.y == -1)
        {
            return new Vector4(
                CloseEnough(flipped[0], neighbor[0]) ? 1f : 0f,
                CloseEnough(flipped[1], neighbor[1]) ? 1f : 0f,
                0f,
                0f
            );
        }

        if (relation.y == 1)
        {
            return new Vector4(
                0f,
                0f,
                CloseEnough(flipped[2], neighbor[2]) ? 1f : 0f,
                CloseEnough(flipped[3], neighbor[3]) ? 1f : 0f
            );
        }

        return Vector4.zero;
    }

    private static Vector4 FlipHeightVector(
        Vector4 vector,
        Vector2Int relation)
    {
        if (Mathf.Abs(relation.x) == 1)
        {
            vector = new Vector4(
                vector[1],
                vector[0],
                vector[3],
                vector[2]
            );
        }

        if (Mathf.Abs(relation.y) == 1)
        {
            vector = new Vector4(
                vector[2],
                vector[3],
                vector[0],
                vector[1]
            );
        }

        return vector;
    }

    // 修改原因：
    // Reference 使用 MathHelper.CloseEnough()。
    // Runtime 以小幅絕對誤差比較序列化後的 float 高度，避免直接使用 ==。
    private static bool CloseEnough(float a, float b)
    {
        return Mathf.Abs(a - b) <= 0.0001f;
    }

    // 修改原因：
    // 完整對齊 Reference Project 的 Terrain Chunk 分割方式。
    // Prontera 208 × 160 會建立為 13 × 10，共 130 個 Chunk 位置。
    private void BuildFullTopChunkSystem()
    {
        if (runtimeCells == null || runtimeCells.Length == 0)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: runtimeCells 尚未建立，無法建立完整 Chunk System。",
                this
            );

            return;
        }

        int chunkWidth =
            Mathf.CeilToInt((float)mapWidth / ChunkSizeInTiles);

        int chunkHeight =
            Mathf.CeilToInt((float)mapHeight / ChunkSizeInTiles);

        // 修改原因：
        // 避免停用再啟用元件或重新執行建立流程時產生重複 Terrain。
        Transform existingRoot =
            transform.Find("RuntimeTerrainChunks");

        if (existingRoot != null)
        {
            Destroy(existingRoot.gameObject);
        }

        GameObject chunkRoot =
            new GameObject("RuntimeTerrainChunks");

        chunkRoot.transform.SetParent(transform, false);

        int createdChunkCount = 0;
        int createdTopTileCount = 0;

        for (int chunkY = 0; chunkY < chunkHeight; chunkY++)
        {
            for (int chunkX = 0; chunkX < chunkWidth; chunkX++)
            {
                int startX =
                    chunkX * ChunkSizeInTiles;

                int startY =
                    chunkY * ChunkSizeInTiles;

                int topTileCount =
                    BuildTopChunk(
                        chunkRoot.transform,
                        chunkX,
                        chunkY,
                        startX,
                        startY
                    );

                if (topTileCount <= 0)
                {
                    continue;
                }

                createdChunkCount++;
                createdTopTileCount += topTileCount;
            }
        }

        Debug.Log(
            $"RoRuntimeTerrainBuilder: 完整 Top Chunk System 建立完成。" +
            $"地圖尺寸 = {mapWidth} × {mapHeight}，" +
            $"Chunk 數量 = {chunkWidth} × {chunkHeight}，" +
            $"實際建立 Chunk = {createdChunkCount}，" +
            $"Top Tiles = {createdTopTileCount}。",
            this
        );
    }

    // 修改原因：
    // 每次建立一個與 Reference Project 相同的 16 × 16 Chunk。
    // Mesh 使用 Chunk 局部座標，Chunk Transform 負責地圖位置。
    private int BuildTopChunk(
        Transform parent,
        int chunkX,
        int chunkY,
        int startX,
        int startY)
    {
        int endX =
            Mathf.Min(mapWidth, startX + ChunkSizeInTiles);

        int endY =
            Mathf.Min(mapHeight, startY + ChunkSizeInTiles);

        List<Vector3> vertices =
            new List<Vector3>();

        List<int> triangles =
            new List<int>();

        List<Vector2> uvs =
            new List<Vector2>();

        List<Vector3> normals =
            new List<Vector3>();

        List<Color> colors =
            new List<Color>();

        int topTileCount = 0;
        int rightFaceCount = 0;
        int frontFaceCount = 0;

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int cellIndex =
                    y * mapWidth + x;

                RoRuntimeCell cell =
                    runtimeCells[cellIndex];

                if (cell == null)
                {
                    continue;
                }

                int localX =
                    x - startX;

                int localY =
                    y - startY;

                Vector4 heights =
                    cell.Heights;

                // =========================
                // Top
                // =========================

                // 修改原因：
                // 完整對齊 Reference Project 的：
                // if (true) // always draw top cell, no matter what
                //
                // 即使 Top Tile 未啟用，也必須建立幾何面；
                // 未啟用或不存在的 Top 會使用 transparent Atlas Rect。
                Vector3 p0 = new Vector3(
                    (localX + 0f) * cellSize,
                    heights[0] * heightScale,
                    (localY + 1f) * cellSize
                );

                Vector3 p1 = new Vector3(
                    (localX + 1f) * cellSize,
                    heights[1] * heightScale,
                    (localY + 1f) * cellSize
                );

                Vector3 p2 = new Vector3(
                    (localX + 0f) * cellSize,
                    heights[2] * heightScale,
                    (localY + 0f) * cellSize
                );

                Vector3 p3 = new Vector3(
                    (localX + 1f) * cellSize,
                    heights[3] * heightScale,
                    (localY + 0f) * cellSize
                );

                RoRuntimeTile topTile =
                    cell.Top;

                Color topColor =
                    topTile != null
                        ? topTile.Color
                        : Color.white;

                AddTerrainQuad(
                    vertices,
                    triangles,
                    uvs,
                    normals,
                    colors,
                    p0,
                    p1,
                    p2,
                    p3,
                    topTile,
                    runtimeTopNormals[cellIndex],
                    new[]
                    {
                        topColor,
                        topColor,
                        topColor,
                        topColor
                    }
                );

                topTileCount++;

                // =========================
                // Right
                // =========================

                if (cell.Right != null &&
                    cell.Right.Enabled &&
                    x + 1 < mapWidth)
                {
                    RoRuntimeCell rightNeighbor =
                        runtimeCells[x + 1 + y * mapWidth];

                    if (rightNeighbor != null)
                    {
                        Vector3 r1 = new Vector3(
                            (localX + 1f) * cellSize,
                            heights[1] * heightScale,
                            (localY + 1f) * cellSize
                        );

                        Vector3 r2 = new Vector3(
                            (localX + 1f) * cellSize,
                            rightNeighbor.Heights[0] * heightScale,
                            (localY + 1f) * cellSize
                        );

                        Vector3 r3 = new Vector3(
                            (localX + 1f) * cellSize,
                            heights[3] * heightScale,
                            (localY + 0f) * cellSize
                        );

                        Vector3 r4 = new Vector3(
                            (localX + 1f) * cellSize,
                            rightNeighbor.Heights[2] * heightScale,
                            (localY + 0f) * cellSize
                        );

                        Color upperColor =
                            Color.white;

                        if (x + 1 < mapWidth &&
                            y + 1 < mapHeight)
                        {
                            RoRuntimeCell northEastCell =
                                runtimeCells[
                                    x + 1 +
                                    (y + 1) * mapWidth
                                ];

                            if (northEastCell != null &&
                                northEastCell.Top != null)
                            {
                                upperColor =
                                    northEastCell.Top.Color;
                            }
                        }

                        Color lowerColor =
                            Color.white;

                        if (rightNeighbor.Top != null)
                        {
                            lowerColor =
                                rightNeighbor.Top.Color;
                        }

                        float sideAlpha =
                            cell.Right.IsUnlit
                                ? 0f
                                : 1f;

                        upperColor.a = sideAlpha;
                        lowerColor.a = sideAlpha;

                        AddTerrainQuad(
                            vertices,
                            triangles,
                            uvs,
                            normals,
                            colors,
                            r1,
                            r2,
                            r3,
                            r4,
                            cell.Right,
                            null,
                            new[]
                            {
                                upperColor,
                                upperColor,
                                lowerColor,
                                lowerColor
                            }
                        );

                        rightFaceCount++;
                    }
                }

                // =========================
                // Front
                // =========================

                if (cell.Front != null &&
                    cell.Front.Enabled &&
                    y - 1 >= 0)
                {
                    RoRuntimeCell frontNeighbor =
                        runtimeCells[
                            x +
                            (y - 1) * mapWidth
                        ];

                    if (frontNeighbor != null)
                    {
                        Vector3 f1 = new Vector3(
                            (localX + 0f) * cellSize,
                            heights[2] * heightScale,
                            (localY + 0f) * cellSize
                        );

                        Vector3 f2 = new Vector3(
                            (localX + 1f) * cellSize,
                            heights[3] * heightScale,
                            (localY + 0f) * cellSize
                        );

                        Vector3 f3 = new Vector3(
                            (localX + 0f) * cellSize,
                            frontNeighbor.Heights[0] * heightScale,
                            (localY + 0f) * cellSize
                        );

                        Vector3 f4 = new Vector3(
                            (localX + 1f) * cellSize,
                            frontNeighbor.Heights[1] * heightScale,
                            (localY + 0f) * cellSize
                        );

                        Color leftColor =
                            cell.Top != null
                                ? cell.Top.Color
                                : Color.white;

                        Color rightColor =
                            Color.white;

                        if (x + 1 < mapWidth)
                        {
                            RoRuntimeCell eastCell =
                                runtimeCells[
                                    x + 1 +
                                    y * mapWidth
                                ];

                            if (eastCell != null &&
                                eastCell.Top != null)
                            {
                                rightColor =
                                    eastCell.Top.Color;
                            }
                        }

                        float sideAlpha =
                            cell.Front.IsUnlit
                                ? 0f
                                : 1f;

                        leftColor.a = sideAlpha;
                        rightColor.a = sideAlpha;

                        AddTerrainQuad(
                            vertices,
                            triangles,
                            uvs,
                            normals,
                            colors,
                            f1,
                            f2,
                            f3,
                            f4,
                            cell.Front,
                            null,
                            new[]
                            {
                                leftColor,
                                rightColor,
                                leftColor,
                                rightColor
                            }
                        );

                        frontFaceCount++;
                    }
                }
            }
        }

        if (vertices.Count == 0)
        {
            return 0;
        }

        GameObject chunkObject =
            new GameObject($"Chunk{chunkX}-{chunkY}");

        chunkObject.transform.SetParent(parent, false);

        chunkObject.transform.localPosition =
            new Vector3(
                startX * cellSize,
                0f,
                startY * cellSize
            );

        MeshFilter meshFilter =
            chunkObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            chunkObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh
        {
            name = $"TerrainChunkMesh_{chunkX}_{chunkY}"
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);

        // 修改原因：
        // Top 使用整張地圖預先計算的平滑法線；側面使用各自的平面法線。
        // 不可再呼叫 RecalculateNormals()，否則會覆蓋 Reference 對齊結果。
        mesh.RecalculateBounds();

        meshFilter.sharedMesh =
            mesh;

        // 修改原因：
        // 優先使用從 Reference MapShaderAccurate 簡化而來的
        // Runtime URP Shader，以保留 Alpha Cutoff、Vertex Color、
        // 雙面顯示及主要光源計算。
        Shader shader =
            Shader.Find("Custom/RuntimeMapShaderAccurate");

        if (shader == null)
        {
            shader =
                Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader =
                Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader =
                Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: 找不到可用的 Terrain Shader。",
                this
            );

            return topTileCount;
        }

        Material material = new Material(shader)
        {
            name = "Prontera_Atlas_Material"
        };

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                pronteraAtlas
            );
        }
        else if (material.HasProperty("_MainTex"))
        {
            material.SetTexture(
                "_MainTex",
                pronteraAtlas
            );
        }
        else
        {
            material.mainTexture =
                pronteraAtlas;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor(
                "_Color",
                Color.white
            );
        }

        if (material.HasProperty("_Cutoff"))
        {
            material.SetFloat(
                "_Cutoff",
                0.5f
            );
        }

        if (material.HasProperty("_AmbientStrength"))
        {
            material.SetFloat(
                "_AmbientStrength",
                0.65f
            );
        }

        meshRenderer.sharedMaterial =
            material;

        Debug.Log(
            $"RoRuntimeTerrainBuilder: Chunk{chunkX}-{chunkY} 建立完成。" +
            $"Top = {topTileCount}，" +
            $"Right = {rightFaceCount}，" +
            $"Front = {frontFaceCount}。",
            this
        );

        return topTileCount;
    }

    // 修改原因：
    // Top、Right、Front 都使用相同的四頂點與 Triangle 寫入流程，
    // 並完整套用 Reference Project 的 Atlas UV Remap。
    private void AddTerrainQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        List<Vector3> normals,
        List<Color> colors,
        Vector3 vertex0,
        Vector3 vertex1,
        Vector3 vertex2,
        Vector3 vertex3,
        RoRuntimeTile tile,
        Vector3[] quadNormals,
        Color[] quadColors)
    {
        int vertexStartIndex =
            vertices.Count;

        vertices.Add(vertex0);
        vertices.Add(vertex1);
        vertices.Add(vertex2);
        vertices.Add(vertex3);

        triangles.Add(vertexStartIndex + 0);
        triangles.Add(vertexStartIndex + 1);
        triangles.Add(vertexStartIndex + 3);

        triangles.Add(vertexStartIndex + 3);
        triangles.Add(vertexStartIndex + 2);
        triangles.Add(vertexStartIndex + 0);

        // 修改原因：
        // 完整對齊 TranslateTileTextureUVs(Tile tile)：
        // Tile 不存在或未啟用時，一律使用 transparent 貼圖區域。
        string textureName =
            tile != null && tile.Enabled
                ? tile.Texture
                : "transparent";

        Rect atlasRect =
            GetAtlasRect(textureName);

        if (tile != null &&
            tile.UVs != null &&
            tile.UVs.Length == 4)
        {
            uvs.Add(RemapUV(tile.UVs[0], atlasRect));
            uvs.Add(RemapUV(tile.UVs[1], atlasRect));
            uvs.Add(RemapUV(tile.UVs[2], atlasRect));
            uvs.Add(RemapUV(tile.UVs[3], atlasRect));
        }
        else
        {
            uvs.Add(RemapUV(new Vector2(0f, 1f), atlasRect));
            uvs.Add(RemapUV(new Vector2(1f, 1f), atlasRect));
            uvs.Add(RemapUV(new Vector2(0f, 0f), atlasRect));
            uvs.Add(RemapUV(new Vector2(1f, 0f), atlasRect));
        }

        if (quadNormals != null &&
            quadNormals.Length == 4)
        {
            normals.Add(quadNormals[0]);
            normals.Add(quadNormals[1]);
            normals.Add(quadNormals[2]);
            normals.Add(quadNormals[3]);
        }
        else
        {
            Vector3 flatNormal =
                CalculateQuadNormal(
                    vertex0,
                    vertex1,
                    vertex2,
                    vertex3
                );

            normals.Add(flatNormal);
            normals.Add(flatNormal);
            normals.Add(flatNormal);
            normals.Add(flatNormal);
        }

        if (quadColors != null &&
            quadColors.Length == 4)
        {
            colors.Add(quadColors[0]);
            colors.Add(quadColors[1]);
            colors.Add(quadColors[2]);
            colors.Add(quadColors[3]);
        }
        else
        {
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(Color.white);
        }
    }

    // 修改原因：
    // 讀取 JSON 並建立 textureName → Rect 對照表。
    // 沒有這份資料時，Atlas 圖片本身無法知道每張原始貼圖的位置。
    private bool BuildAtlasRectLookup()
    {
        atlasRectLookup.Clear();

        if (pronteraAtlas == null)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: Prontera Atlas 尚未指定。",
                this
            );

            return false;
        }

        if (pronteraAtlasLookup == null)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: Prontera Atlas Lookup 尚未指定。請把 prontera_atlas_lookup.json 拖到 Inspector。",
                this
            );

            return false;
        }

        AtlasLookupFile lookupFile =
            JsonUtility.FromJson<AtlasLookupFile>(
                pronteraAtlasLookup.text
            );

        if (lookupFile == null ||
            lookupFile.entries == null ||
            lookupFile.entries.Count == 0)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: Prontera Atlas Lookup JSON 無有效 entries。",
                this
            );

            return false;
        }

        for (int i = 0;
             i < lookupFile.entries.Count;
             i++)
        {
            AtlasLookupEntry entry =
                lookupFile.entries[i];

            if (entry == null ||
                string.IsNullOrEmpty(entry.textureName))
            {
                continue;
            }

            Rect rect = new Rect(
                entry.x,
                entry.y,
                entry.width,
                entry.height
            );

            atlasRectLookup[entry.textureName] =
                rect;
        }

        if (!atlasRectLookup.ContainsKey("transparent"))
        {
            Debug.LogWarning(
                "RoRuntimeTerrainBuilder: Atlas Lookup 沒有 transparent 項目。",
                this
            );
        }

        Debug.Log(
            $"RoRuntimeTerrainBuilder: Atlas Lookup 載入完成。" +
            $"Map = {lookupFile.mapName}，" +
            $"Atlas = {lookupFile.atlasWidth} × {lookupFile.atlasHeight}，" +
            $"Entries = {atlasRectLookup.Count}。",
            this
        );

        return true;
    }

    // 修改原因：
    // 對齊 Reference Project 的 GetUVsForTexture()。
    // 找不到指定貼圖時，退回 transparent Rect，避免整張 Atlas 被套到單一 Tile。
    private Rect GetAtlasRect(string textureName)
    {
        if (string.IsNullOrEmpty(textureName))
        {
            textureName =
                "transparent";
        }

        if (atlasRectLookup.TryGetValue(
                textureName,
                out Rect rect))
        {
            return rect;
        }

        if (atlasRectLookup.TryGetValue(
                "transparent",
                out Rect transparentRect))
        {
            Debug.LogWarning(
                $"RoRuntimeTerrainBuilder: Atlas Lookup 找不到貼圖：{textureName}，改用 transparent。",
                this
            );

            return transparentRect;
        }

        Debug.LogWarning(
            $"RoRuntimeTerrainBuilder: Atlas Lookup 找不到貼圖：{textureName}，且沒有 transparent，改用完整 UV。",
            this
        );

        return new Rect(
            0f,
            0f,
            1f,
            1f
        );
    }

    // 修改原因：
    // 完整對齊 Reference Project 的 VectorHelper.RemapUV()。
    private static Vector2 RemapUV(
        Vector2 position,
        Rect space)
    {
        float x =
            Mathf.Lerp(
                space.xMin,
                space.xMax,
                position.x
            );

        float y =
            Mathf.Lerp(
                space.yMin,
                space.yMax,
                position.y
            );

        return new Vector2(
            x,
            y
        );
    }

    // 修改原因：建立可擴大的 Prontera 測試區塊，
    // 並把所有有效 Top Tiles 按貼圖分組建立 Mesh。
    private void BuildTopTileChunkTestMesh()
    {
        if (runtimeCells == null || runtimeCells.Length == 0)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: runtimeCells 尚未建立。",
                this
            );
            return;
        }

        // 修改原因：
        // 將 64 × 64 測試區域移到 208 × 160 地圖中央，
        // 確認普隆德拉主要城區的 Top Tile 排列是否正確。
        int startX = 72;
        int startY = 48;
        int chunkWidth = 64;
        int chunkHeight = 64;

        int endX = Mathf.Min(mapWidth, startX + chunkWidth);
        int endY = Mathf.Min(mapHeight, startY + chunkHeight);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        int tileCount = 0;

        // 修改原因：收集測試 Chunk 實際使用的不同貼圖名稱，
        // 確認下一步需要匯入哪些 Ragnarok 地面貼圖。
        HashSet<string> textureNames = new HashSet<string>();

        // 修改原因：
        // 按照 Ragnarok 貼圖名稱保存不同的 Mesh 群組。
        // 目前先測試 Tile 是否能正確分配，尚未改變實際 Mesh 建立方式。
        Dictionary<string, TopTextureMeshGroup> textureGroups =
            new Dictionary<string, TopTextureMeshGroup>();

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int cellIndex = y * mapWidth + x;
                RoRuntimeCell cell = runtimeCells[cellIndex];

                if (cell == null ||
                    cell.Top == null ||
                    !cell.Top.Enabled)
                {
                    continue;
                }

                // 修改原因：
                // 保留目前 Tile 所屬的貼圖群組，讓頂點、UV、三角形與顏色
                // 可以同時寫入原本總 Mesh 和正確的貼圖分組 Mesh。
                TopTextureMeshGroup textureGroup = null;

                if (!string.IsNullOrEmpty(cell.Top.Texture))
                {
                    textureNames.Add(cell.Top.Texture);

                    textureGroup = GetOrCreateTopTextureGroup(
                        textureGroups,
                        cell.Top.Texture
                    );

                    textureGroup.TileCount++;

                    if (textureGroup.Texture == null)
                    {
                        Debug.LogError(
                            $"RoRuntimeTerrainBuilder: 貼圖欄位尚未指定：{cell.Top.Texture}",
                            this
                        );
                    }
                }

                // 修改原因：
                // 原專案的 Chunk Mesh 使用局部座標，Chunk 本身再負責世界位置。
                // 這裡先把測試區域的左下角視為 (0, 0)，避免 startX / startY
                // 直接把 Mesh 推到很遠的位置。
                float localX = x - startX;
                float localY = y - startY;

                Vector4 heights = cell.Heights;

                // 修改原因：
                // 完整對齊原專案 RoMapSharedMeshData.PrepareTopFaces() 的頂點順序：
                // 0 = 左上、1 = 右上、2 = 左下、3 = 右下。
                Vector3 p0 = new Vector3(
                    (localX + 0f) * cellSize,
                    heights[0] * heightScale,
                    (localY + 1f) * cellSize
                );

                Vector3 p1 = new Vector3(
                    (localX + 1f) * cellSize,
                    heights[1] * heightScale,
                    (localY + 1f) * cellSize
                );

                Vector3 p2 = new Vector3(
                    (localX + 0f) * cellSize,
                    heights[2] * heightScale,
                    (localY + 0f) * cellSize
                );

                Vector3 p3 = new Vector3(
                    (localX + 1f) * cellSize,
                    heights[3] * heightScale,
                    (localY + 0f) * cellSize
                );

                // =========================
                // 原本總 Mesh
                // =========================

                int vertexStartIndex = vertices.Count;

                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                // 修改原因：
                // 完整對齊原專案 RoMapChunk.RebuildMeshData() 的三角形順序。
                triangles.Add(vertexStartIndex + 0);
                triangles.Add(vertexStartIndex + 1);
                triangles.Add(vertexStartIndex + 3);

                triangles.Add(vertexStartIndex + 3);
                triangles.Add(vertexStartIndex + 2);
                triangles.Add(vertexStartIndex + 0);

                if (cell.Top.UVs != null &&
                    cell.Top.UVs.Length == 4)
                {
                    uvs.Add(cell.Top.UVs[0]);
                    uvs.Add(cell.Top.UVs[1]);
                    uvs.Add(cell.Top.UVs[2]);
                    uvs.Add(cell.Top.UVs[3]);
                }
                else
                {
                    // 修改原因：
                    // 防止個別 Tile 沒有 UV 時造成頂點與 UV 數量不一致。
                    uvs.Add(new Vector2(0f, 0f));
                    uvs.Add(new Vector2(1f, 0f));
                    uvs.Add(new Vector2(0f, 1f));
                    uvs.Add(new Vector2(1f, 1f));
                }

                colors.Add(cell.Top.Color);
                colors.Add(cell.Top.Color);
                colors.Add(cell.Top.Color);
                colors.Add(cell.Top.Color);

                // =========================
                // 貼圖分組 Mesh
                // =========================

                if (textureGroup != null)
                {
                    int groupVertexStartIndex =
                        textureGroup.Vertices.Count;

                    textureGroup.Vertices.Add(p0);
                    textureGroup.Vertices.Add(p1);
                    textureGroup.Vertices.Add(p2);
                    textureGroup.Vertices.Add(p3);

                    // 修改原因：
                    // 完整對齊原專案 RoMapChunk.RebuildMeshData() 的三角形順序。
                    textureGroup.Triangles.Add(groupVertexStartIndex + 0);
                    textureGroup.Triangles.Add(groupVertexStartIndex + 1);
                    textureGroup.Triangles.Add(groupVertexStartIndex + 3);

                    textureGroup.Triangles.Add(groupVertexStartIndex + 3);
                    textureGroup.Triangles.Add(groupVertexStartIndex + 2);
                    textureGroup.Triangles.Add(groupVertexStartIndex + 0);

                    if (cell.Top.UVs != null &&
                        cell.Top.UVs.Length == 4)
                    {
                        textureGroup.UVs.Add(cell.Top.UVs[0]);
                        textureGroup.UVs.Add(cell.Top.UVs[1]);
                        textureGroup.UVs.Add(cell.Top.UVs[2]);
                        textureGroup.UVs.Add(cell.Top.UVs[3]);
                    }
                    else
                    {
                        // 修改原因：
                        // 分組 Mesh 同樣需要補上預設 UV，
                        // 避免 UV 數量與頂點數量不一致。
                        textureGroup.UVs.Add(new Vector2(0f, 0f));
                        textureGroup.UVs.Add(new Vector2(1f, 0f));
                        textureGroup.UVs.Add(new Vector2(0f, 1f));
                        textureGroup.UVs.Add(new Vector2(1f, 1f));
                    }

                    textureGroup.Colors.Add(cell.Top.Color);
                    textureGroup.Colors.Add(cell.Top.Color);
                    textureGroup.Colors.Add(cell.Top.Color);
                    textureGroup.Colors.Add(cell.Top.Color);
                }

                tileCount++;
            }
        }

        if (tileCount == 0)
        {
            Debug.LogWarning(
                "RoRuntimeTerrainBuilder: 測試區域沒有有效的 Top Tile。",
                this
            );
            return;
        }

        GameObject chunkObject =
            new GameObject($"TopChunk_Test_{startX}_{startY}");

        chunkObject.transform.SetParent(transform, false);

        MeshFilter meshFilter =
            chunkObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            chunkObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh
        {
            name = $"TopChunkMesh_{startX}_{startY}"
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader != null)
        {
            Material material = new Material(shader)
            {
                name = "TopChunk_Test_Material"
            };

            // 修改原因：Chunk Mesh 也要指定測試貼圖。
            if (testTopTexture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", testTopTexture);
                }
                else if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", testTopTexture);
                }
            }

            meshRenderer.sharedMaterial = material;

            // 修改原因：
            // 使用四個貼圖群組建立真正的分組 Mesh 子物件。
            BuildTopTextureGroupObjects(
                chunkObject.transform,
                textureGroups
            );

            // 修改原因：
            // 分組 Mesh 已經負責顯示地面，因此暫時關閉舊的單一貼圖 Renderer，
            // 避免兩組 Mesh 重疊閃爍。
            meshRenderer.enabled = false;
        }

        Debug.Log(
            $"RoRuntimeTerrainBuilder: 已建立合併 Top Chunk。" +
            $"範圍 X = {startX}～{endX - 1}，" +
            $"Y = {startY}～{endY - 1}，" +
            $"Tiles = {tileCount}，" +
            $"Vertices = {vertices.Count}，" +
            $"Triangles = {triangles.Count / 3}。",
            this
        );

        // 修改原因：顯示測試區域需要的所有不同地面貼圖。
        Debug.Log(
            $"RoRuntimeTerrainBuilder: 此 Chunk 使用 {textureNames.Count} 種貼圖：\n" +
            string.Join("\n", textureNames),
            this
        );

        // 修改原因：
        // 確認 221 個 Tile 是否正確分配到四個貼圖群組。
        foreach (KeyValuePair<string, TopTextureMeshGroup> pair in textureGroups)
        {
            Debug.Log(
                $"RoRuntimeTerrainBuilder: 貼圖群組 {pair.Key}，" +
                $"Tiles = {pair.Value.TileCount}，" +
                $"Vertices = {pair.Value.Vertices.Count}，" +
                $"Triangles = {pair.Value.Triangles.Count / 3}",
                this
            );
        }
    }

    // 修改原因：先找到第一個具有非零高度的 Top Tile，
    // 再建立其周圍 3 × 3 區域，方便確認高度銜接及座標方向。
    private void BuildFirstTopTileTestMesh()
    {
        if (runtimeCells == null || runtimeCells.Length == 0)
        {
            Debug.LogError(
                "RoRuntimeTerrainBuilder: runtimeCells 尚未建立。",
                this
            );
            return;
        }

        int centerX = -1;
        int centerY = -1;
        int centerCellIndex = -1;

        // 修改原因：尋找第一個不是全零高度的 Top Tile，
        // 作為 3 × 3 測試區域的中心。
        for (int cellIndex = 0; cellIndex < runtimeCells.Length; cellIndex++)
        {
            RoRuntimeCell cell = runtimeCells[cellIndex];

            if (cell == null ||
                cell.Top == null ||
                !cell.Top.Enabled)
            {
                continue;
            }

            bool isCompletelyFlatAtZero =
                Mathf.Approximately(cell.Heights.x, 0f) &&
                Mathf.Approximately(cell.Heights.y, 0f) &&
                Mathf.Approximately(cell.Heights.z, 0f) &&
                Mathf.Approximately(cell.Heights.w, 0f);

            if (isCompletelyFlatAtZero)
            {
                continue;
            }

            centerCellIndex = cellIndex;
            centerX = cellIndex % mapWidth;
            centerY = cellIndex / mapWidth;

            break;
        }

        if (centerCellIndex < 0)
        {
            Debug.LogWarning(
                "RoRuntimeTerrainBuilder: 找不到具有非零高度的 Top Tile。",
                this
            );
            return;
        }

        GameObject testAreaObject =
            new GameObject($"TopTile_TestArea_{centerX}_{centerY}");

        testAreaObject.transform.SetParent(transform, false);

        int createdTileCount = 0;

        // 修改原因：建立中心格周圍 3 × 3 的 Top Tiles，
        // 用來檢查相鄰地形是否能正確連接。
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int x = centerX + offsetX;
                int y = centerY + offsetY;

                if (x < 0 || x >= mapWidth ||
                    y < 0 || y >= mapHeight)
                {
                    continue;
                }

                int cellIndex = y * mapWidth + x;
                RoRuntimeCell cell = runtimeCells[cellIndex];

                if (cell == null ||
                    cell.Top == null ||
                    !cell.Top.Enabled)
                {
                    continue;
                }

                CreateTopTileTestMesh(
                    testAreaObject.transform,
                    cell,
                    cellIndex,
                    x,
                    y
                );

                createdTileCount++;
            }
        }

        Debug.Log(
            $"RoRuntimeTerrainBuilder: 已建立 3 × 3 Top Tile 測試區域。" +
            $"中心 Cell Index = {centerCellIndex}，" +
            $"中心座標 = ({centerX}, {centerY})，" +
            $"中心 Heights = {runtimeCells[centerCellIndex].Heights}，" +
            $"實際建立數量 = {createdTileCount}。",
            this
        );
    }

    // 修改原因：將單一 Top Tile 的 Mesh 建立邏輯獨立成方法，
    // 方便稍後擴充為整張地圖。
    private void CreateTopTileTestMesh(
        Transform parent,
        RoRuntimeCell cell,
        int cellIndex,
        int x,
        int y)
    {
        GameObject tileObject =
            new GameObject($"TopTile_{x}_{y}");

        tileObject.transform.SetParent(parent, false);

        MeshFilter meshFilter =
            tileObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            tileObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh
        {
            name = $"TopTileMesh_{x}_{y}"
        };

        float x0 = x * cellSize;
        float x1 = (x + 1) * cellSize;

        float z0 = y * cellSize;
        float z1 = (y + 1) * cellSize;

        Vector4 heights = cell.Heights;

        // 修改原因：
        // 完整對齊原專案 RoMapSharedMeshData.PrepareTopFaces() 的頂點順序：
        // 0 = 左上、1 = 右上、2 = 左下、3 = 右下。
        Vector3[] vertices =
        {
            new Vector3(x0, heights[0] * heightScale, z1),
            new Vector3(x1, heights[1] * heightScale, z1),
            new Vector3(x0, heights[2] * heightScale, z0),
            new Vector3(x1, heights[3] * heightScale, z0)
        };

        // 修改原因：
        // 完整對齊原專案 RoMapChunk.RebuildMeshData() 的三角形順序。
        int[] triangles =
        {
            0, 1, 3,
            3, 2, 0
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = cell.Top.UVs;

        mesh.colors = new[]
        {
        cell.Top.Color,
        cell.Top.Color,
        cell.Top.Color,
        cell.Top.Color
    };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        // 修改原因：目前專案使用 URP，改用 URP 相容的 Shader 顯示貼圖。
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader != null)
        {
            Material material = new Material(shader)
            {
                name = "TopChunk_Test_Material"
            };

            // 修改原因：先把找到的 prt_도시01 貼圖套到測試 Chunk，
            // 確認 Ragnarok Tile 的 UV 是否能正確顯示。
            if (testTopTexture != null)
            {
                // 修改原因：Unity 6 URP 材質通常使用 _BaseMap，而不是只依賴 mainTexture。
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", testTopTexture);
                }
                else if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", testTopTexture);
                }
                else
                {
                    material.mainTexture = testTopTexture;
                }
            }
            else
            {
                Debug.LogWarning(
                    "RoRuntimeTerrainBuilder: 尚未指定 Test Top Texture。",
                    this
                );
            }

            meshRenderer.sharedMaterial = material;
        }
    }

    // 修改原因：讀取並保存完整 Tile 資料，
    // 供後續建立 Runtime Mesh 使用。

    private RoRuntimeTile ReadTile(BinaryReader reader)
    {
        RoRuntimeTile tile = new RoRuntimeTile();

        tile.Enabled = reader.ReadBoolean();

        if (!tile.Enabled)
        {
            tile.UVs = new Vector2[4];
            tile.Texture = string.Empty;
            tile.Color = Color.white;

            return tile;
        }

        tile.IsUnlit = reader.ReadBoolean();
        tile.Texture = reader.ReadString();

        tile.UVs = new Vector2[4];

        for (int i = 0; i < 4; i++)
        {
            float u = reader.ReadSingle();
            float v = reader.ReadSingle();

            tile.UVs[i] = new Vector2(u, v);
        }

        float r = reader.ReadSingle();
        float g = reader.ReadSingle();
        float b = reader.ReadSingle();
        float a = reader.ReadSingle();

        tile.Color = new Color(r, g, b, a);

        return tile;
    }

    // 修改原因：根據 Ragnarok 地圖資料中的貼圖名稱，
    // 回傳 Inspector 已指定的對應 Texture2D。
    private Texture2D GetTopTexture(string textureName)
    {
        switch (textureName)
        {
            case "prt_초원01":
                return prtGrass01Texture;

            case "prt_도시01":
                return prtCity01Texture;

            case "prt_도시02":
                return prtCity02Texture;

            case "prt_도시04":
                return prtCity04Texture;

            default:
                // 找不到對應貼圖時，暫時使用測試貼圖，
                // 避免地面變成粉紅色或完全沒有材質。
                Debug.LogWarning(
                    $"RoRuntimeTerrainBuilder: 找不到貼圖：{textureName}",
                    this
                );

                return testTopTexture;
        }
    }

    // 修改原因：
    // 根據 Ragnarok 貼圖名稱取得對應的 Mesh 群組。
    // 如果這張貼圖尚未建立群組，就新增一個群組並保存起來。
    private TopTextureMeshGroup GetOrCreateTopTextureGroup(
        Dictionary<string, TopTextureMeshGroup> groups,
        string textureName)
    {
        if (groups.TryGetValue(textureName, out TopTextureMeshGroup existingGroup))
        {
            return existingGroup;
        }

        Texture2D texture = GetTopTexture(textureName);

        TopTextureMeshGroup newGroup = new TopTextureMeshGroup
        {
            Texture = texture
        };

        groups.Add(textureName, newGroup);

        return newGroup;
    }

    // 修改原因：
    // 將每一種 Ragnarok 地面貼圖建立成獨立的子物件與 MeshRenderer，
    // 讓同一個 Chunk 可以同時顯示不同貼圖。
    private void BuildTopTextureGroupObjects(
        Transform parentTransform,
        Dictionary<string, TopTextureMeshGroup> textureGroups)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        foreach (KeyValuePair<string, TopTextureMeshGroup> pair in textureGroups)
        {
            string textureName = pair.Key;
            TopTextureMeshGroup group = pair.Value;

            if (group.Vertices.Count == 0)
            {
                continue;
            }

            GameObject groupObject =
                new GameObject($"Top_{textureName}");

            groupObject.transform.SetParent(
                parentTransform,
                false
            );

            MeshFilter meshFilter =
                groupObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer =
                groupObject.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh
            {
                name = $"TopMesh_{textureName}"
            };

            mesh.SetVertices(group.Vertices);
            mesh.SetTriangles(group.Triangles, 0);
            mesh.SetUVs(0, group.UVs);
            mesh.SetColors(group.Colors);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;

            Material material = new Material(shader)
            {
                name = $"TopMaterial_{textureName}"
            };

            if (group.Texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture(
                        "_BaseMap",
                        group.Texture
                    );
                }
                else if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture(
                        "_MainTex",
                        group.Texture
                    );
                }
            }

            meshRenderer.sharedMaterial = material;

            Debug.Log(
                $"RoRuntimeTerrainBuilder: 已建立 {textureName} Mesh，" +
                $"Tiles = {group.TileCount}，" +
                $"Vertices = {group.Vertices.Count}。",
                this
            );
        }
    }
}