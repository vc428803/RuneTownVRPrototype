using System.IO;

public static class PathHelper
{
    // 修改原因：提供 Ragnarok 地圖匯入流程所需的資料夾建立功能，
    // 取代 RebuildClient 中目前無法定位來源的 PathHelper。
    public static void CreateDirectoryIfNotExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}