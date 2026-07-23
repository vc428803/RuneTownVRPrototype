using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ApplyMaterialToChildren
{
    [MenuItem("Tools/Prontera/Apply Material To All Chunks")]
    private static void Apply()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Apply Material",
                "請先在 Hierarchy 選取 PronteraClean 根物件。",
                "OK"
            );
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/PronteraCleanExport/Prontera_URP.mat"
        );

        if (material == null)
        {
            EditorUtility.DisplayDialog(
                "Apply Material",
                "找不到 Assets/PronteraCleanExport/Prontera_URP.mat",
                "OK"
            );
            return;
        }

        MeshRenderer[] renderers =
            root.GetComponentsInChildren<MeshRenderer>(true);

        Undo.RecordObjects(renderers, "Apply Prontera Material");

        foreach (MeshRenderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                continue;
            }

            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        Debug.Log(
            $"[ApplyMaterialToChildren] Applied material to {renderers.Length} renderers."
        );
    }

    [MenuItem("Tools/Prontera/Apply Material To All Chunks", true)]
    private static bool ValidateApply()
    {
        return Selection.activeGameObject != null;
    }
}