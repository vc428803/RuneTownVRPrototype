using UnityEngine;

public class PortalSpawnPoint : MonoBehaviour
{
    // 修改原因：
    // 每個傳送出口需要一個唯一 ID，
    // Scene 載入完成後，系統會依照這個 ID 找到正確出生點。
    [SerializeField]
    private string spawnId = "FromProntera";

    public string SpawnId => spawnId;
}