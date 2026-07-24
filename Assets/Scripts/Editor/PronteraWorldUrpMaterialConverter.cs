// 修改原因：
// PronteraWorldCleanExport 的材質是在 Reference 專案以 Standard Shader 建立。
// 匯入 URP 專案後會呈現粉紅色。
// 此工具會一次將整座 Prontera 的所有材質轉換為 URP/Lit，
// 並保留 Texture、顏色、透明與 Alpha Cutout 設定。

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public static class PronteraWorldUrpMaterialConverter
{
    private const string MaterialFolder =
        "Assets/PronteraWorldCleanExport/Materials";

    [MenuItem("Ragnarok/Convert Prontera Materials To URP")]
    private static void ConvertMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            EditorUtility.DisplayDialog(
                "Prontera URP Material Converter",
                $"找不到材質資料夾：\n{MaterialFolder}",
                "確定"
            );

            return;
        }

        Shader urpLitShader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (urpLitShader == null)
        {
            EditorUtility.DisplayDialog(
                "Prontera URP Material Converter",
                "找不到 Universal Render Pipeline/Lit Shader。\n" +
                "請確認專案已安裝並啟用 URP。",
                "確定"
            );

            return;
        }

        string[] materialGuids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { MaterialFolder }
            );

        if (materialGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Prontera URP Material Converter",
                "材質資料夾內找不到任何 Material。",
                "確定"
            );

            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Prontera URP Material Converter",
            $"即將轉換 {materialGuids.Length} 個材質為 URP/Lit。\n\n" +
            "此操作會直接修改 PronteraWorldCleanExport/Materials。",
            "開始轉換",
            "取消"
        );

        if (!confirmed)
            return;

        int convertedCount = 0;

        try
        {
            for (int i = 0; i < materialGuids.Length; i++)
            {
                string materialPath =
                    AssetDatabase.GUIDToAssetPath(
                        materialGuids[i]
                    );

                Material material =
                    AssetDatabase.LoadAssetAtPath<Material>(
                        materialPath
                    );

                if (material == null)
                    continue;

                EditorUtility.DisplayProgressBar(
                    "Prontera URP Material Converter",
                    $"轉換 {i + 1}/{materialGuids.Length}\n" +
                    material.name,
                    (float)i / materialGuids.Length
                );

                ConvertSingleMaterial(
                    material,
                    urpLitShader
                );

                EditorUtility.SetDirty(material);
                convertedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[PronteraWorldUrpMaterialConverter] " +
                $"轉換完成：{convertedCount} 個材質。"
            );

            EditorUtility.DisplayDialog(
                "Prontera URP Material Converter",
                $"轉換完成。\n\n材質數量：{convertedCount}",
                "確定"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Prontera URP Material Converter 失敗",
                exception.Message,
                "確定"
            );
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem(
        "Ragnarok/Convert Prontera Materials To URP",
        true
    )]
    private static bool ValidateConvertMaterials()
    {
        return AssetDatabase.IsValidFolder(MaterialFolder);
    }

    private static void ConvertSingleMaterial(
        Material material,
        Shader urpLitShader)
    {
        // 修改原因：
        // 切換 Shader 前先保存 Standard Material 的貼圖與顏色。
        Texture mainTexture = null;
        Color baseColor = Color.white;
        Vector2 textureScale = Vector2.one;
        Vector2 textureOffset = Vector2.zero;

        bool wasTransparent =
            material.renderQueue >=
            (int)RenderQueue.Transparent;

        bool wasAlphaCutout =
            material.IsKeywordEnabled("_ALPHATEST_ON") ||
            material.renderQueue ==
            (int)RenderQueue.AlphaTest;

        float cutoff = 0.5f;

        if (material.HasProperty("_MainTex"))
        {
            mainTexture =
                material.GetTexture("_MainTex");

            textureScale =
                material.GetTextureScale("_MainTex");

            textureOffset =
                material.GetTextureOffset("_MainTex");
        }
        else if (material.HasProperty("_BaseMap"))
        {
            mainTexture =
                material.GetTexture("_BaseMap");

            textureScale =
                material.GetTextureScale("_BaseMap");

            textureOffset =
                material.GetTextureOffset("_BaseMap");
        }

        if (material.HasProperty("_Color"))
        {
            baseColor =
                material.GetColor("_Color");
        }
        else if (material.HasProperty("_BaseColor"))
        {
            baseColor =
                material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Cutoff"))
        {
            cutoff =
                material.GetFloat("_Cutoff");
        }

        material.shader = urpLitShader;

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                mainTexture
            );

            material.SetTextureScale(
                "_BaseMap",
                textureScale
            );

            material.SetTextureOffset(
                "_BaseMap",
                textureOffset
            );
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                baseColor
            );
        }

        // 修改原因：
        // Ragnarok 的樹葉、旗幟及部分裝飾會使用透明或 Cutout。
        if (wasTransparent)
        {
            ConfigureTransparent(material);
        }
        else if (wasAlphaCutout)
        {
            ConfigureAlphaCutout(
                material,
                cutoff
            );
        }
        else
        {
            ConfigureOpaque(material);
        }

        // 修改原因：
        // 避免沒有 Metallic Map 的舊建築材質產生不合理金屬反光。
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                0f
            );
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                0.15f
            );
        }
    }

    private static void ConfigureOpaque(
        Material material)
    {
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", 0f);

        material.SetOverrideTag(
            "RenderType",
            "Opaque"
        );

        material.SetInt(
            "_SrcBlend",
            (int)BlendMode.One
        );

        material.SetInt(
            "_DstBlend",
            (int)BlendMode.Zero
        );

        material.SetInt("_ZWrite", 1);

        material.DisableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.DisableKeyword(
            "_ALPHATEST_ON"
        );

        material.renderQueue = -1;
    }

    private static void ConfigureAlphaCutout(
        Material material,
        float cutoff)
    {
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", cutoff);

        material.SetOverrideTag(
            "RenderType",
            "TransparentCutout"
        );

        material.SetInt(
            "_SrcBlend",
            (int)BlendMode.One
        );

        material.SetInt(
            "_DstBlend",
            (int)BlendMode.Zero
        );

        material.SetInt("_ZWrite", 1);

        material.EnableKeyword(
            "_ALPHATEST_ON"
        );

        material.DisableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.renderQueue =
            (int)RenderQueue.AlphaTest;
    }

    private static void ConfigureTransparent(
        Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_AlphaClip", 0f);

        material.SetOverrideTag(
            "RenderType",
            "Transparent"
        );

        material.SetInt(
            "_SrcBlend",
            (int)BlendMode.SrcAlpha
        );

        material.SetInt(
            "_DstBlend",
            (int)BlendMode.OneMinusSrcAlpha
        );

        material.SetInt("_ZWrite", 0);

        material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );

        material.DisableKeyword(
            "_ALPHATEST_ON"
        );

        material.renderQueue =
            (int)RenderQueue.Transparent;
    }
}