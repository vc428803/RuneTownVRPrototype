using System.IO;

namespace Assets.Scripts
{
    public static class AssetHelper
    {
        // 修改原因：RSM 模型匯入時需要建立 mesh / material 等資產輸出路徑，
        // 沿用 RebuildClient 原本的路徑建立邏輯。
        public static string GetAssetPath(string path, string filename)
        {
            path = path.Replace("\\", "/");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return Path.Combine(path, filename).Replace("\\", "/");
        }
    }
}