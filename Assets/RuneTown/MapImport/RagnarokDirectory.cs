using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Editor
{
    // 修改原因：RSM / RO 地圖匯入工具需要共用 Ragnarok Data 根目錄，
    // 以便從 model、texture 等資料夾讀取原始 RO 資源。
    public static class RagnarokDirectory
    {
        private static string NormalizeDataPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        public static string GetRagnarokDataDirectory
        {
            get
            {
                var path = NormalizeDataPath(
                    EditorPrefs.GetString("RagnarokDataPath", null)
                );

                if (path == null)
                    throw new Exception(
                        "You must set a ragnarok data directory first!"
                    );

                return path;
            }
        }

        // 修改原因：提供 RuneTown Editor 一個最小設定入口，
        // 不需要搬入 RebuildClient 原本完整的 EditorWindow。
        [MenuItem("RuneTown/Map Import/Set Ragnarok Data Directory")]
        public static void SetDataDirectory()
        {
            var oldPath = NormalizeDataPath(
                EditorPrefs.GetString("RagnarokDataPath", null)
            );

            var startPath = oldPath;

            if (string.IsNullOrWhiteSpace(startPath) ||
                !Directory.Exists(startPath))
            {
                startPath =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.Personal
                    );
            }

            var path = NormalizeDataPath(
                EditorUtility.OpenFolderPanel(
                    "Locate Ragnarok Data Folder",
                    startPath,
                    ""
                )
            );

            if (!string.IsNullOrWhiteSpace(path) &&
                Directory.Exists(path))
            {
                EditorPrefs.SetString("RagnarokDataPath", path);
                Debug.Log(
                    "Ragnarok data directory set to: " + path
                );
            }
        }
    }
}