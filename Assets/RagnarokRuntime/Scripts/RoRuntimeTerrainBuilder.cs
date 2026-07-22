using UnityEngine;
using System.IO;

public class RoRuntimeTerrainBuilder : MonoBehaviour
{
    [Header("Prontera terrain data")]
    [SerializeField] private TextAsset mapCellData;
    [SerializeField] private int mapWidth = 208;
    [SerializeField] private int mapHeight = 160;

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

            int topCount = 0;
            int frontCount = 0;
            int rightCount = 0;

            // 修改原因：逐格解析並記錄發生錯位的 Cell、面向及資料位置，
            // 方便確認 Top、Front、Right 的實際二進位格式。
            for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                long cellStartPosition = reader.BaseStream.Position;

                try
                {
                    reader.ReadSingle();
                    reader.ReadSingle();
                    reader.ReadSingle();
                    reader.ReadSingle();

                    bool hasTop = reader.ReadBoolean();

                    if (hasTop)
                    {
                        if (ReadTile(reader))
                        {
                            topCount++;
                        }
                    }
                    bool hasFront = reader.ReadBoolean();

                    if (hasFront)
                    {
                        if (ReadTile(reader))
                        {
                            frontCount++;
                        }
                    }
                    bool hasRight = reader.ReadBoolean();

                    if (hasRight)
                    {
                        if (ReadTile(reader))
                        {
                            rightCount++;
                        }
                    }
                    reader.ReadByte();
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
        }
    }
    // 修改原因：回傳 Tile.Enabled，
    // 讓統計數字代表真正需要建立 Mesh 的地面或牆面。
    private bool ReadTile(BinaryReader reader)
    {
        bool enabled = reader.ReadBoolean();

        if (!enabled)
        {
            return false;
        }

        reader.ReadBoolean(); // IsUnlit
        reader.ReadString();  // Texture

        // 四組 UV。
        for (int i = 0; i < 4; i++)
        {
            reader.ReadSingle();
            reader.ReadSingle();
        }

        // Color：R、G、B、A。
        reader.ReadSingle();
        reader.ReadSingle();
        reader.ReadSingle();
        reader.ReadSingle();

        return true;
    }
}