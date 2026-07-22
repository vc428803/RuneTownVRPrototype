using UnityEngine;

public class RoWorldLoader : MonoBehaviour
{
    public Assets.Scripts.MapEditor.Editor.RagnarokWorld world;

    private void Start()
    {
        if (world == null)
        {
            Debug.LogError("World is null.");
            return;
        }

        Debug.Log($"Map : {world.MapName}");
        Debug.Log($"Models : {world.Models.Count}");
        Debug.Log($"Effects : {world.Effects.Count}");
        Debug.Log($"Sounds : {world.Sounds.Count}");
    }
}