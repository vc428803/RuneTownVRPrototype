using System;
using System.IO;

public static class DirectoryHelper
{
    // 修改原因：提供 Ragnarok 地圖匯入流程所需的相對路徑計算，
    // 取代 RebuildClient 中目前無法定位來源的 DirectoryHelper。
    public static string GetRelativeDirectory(string baseDirectory, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be empty.", nameof(baseDirectory));

        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new ArgumentException("Target directory cannot be empty.", nameof(targetDirectory));

        var baseFullPath = Path.GetFullPath(baseDirectory);
        var targetFullPath = Path.GetFullPath(targetDirectory);

        return Path.GetRelativePath(baseFullPath, targetFullPath);
    }
}