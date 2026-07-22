using UnityEngine;

// 修改原因：保存從 prontera.bytes 讀出的 Tile 資料，
// 供後續 Runtime Mesh 建立流程使用。
[System.Serializable]
public class RoRuntimeTile
{
    public bool Enabled;
    public bool IsUnlit;
    public string Texture;
    public Vector2[] UVs;
    public Color Color;
}

// 修改原因：保存單一地圖 Cell 的高度、三個面及 Cell Type，
// 避免解析後立即丟失資料。
[System.Serializable]
public class RoRuntimeCell
{
    public Vector4 Heights;

    public RoRuntimeTile Top;
    public RoRuntimeTile Front;
    public RoRuntimeTile Right;

    public byte CellType;
}