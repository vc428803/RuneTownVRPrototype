using UnityEngine;

public class PersistentPlayer : MonoBehaviour
{
    private static PersistentPlayer instance;

    private void Awake()
    {
        // 修改原因：
        // Scene 切換時保留完整 Player 與 XR Origin，
        // 避免進入新地圖後失去 VR Camera、控制器及移動系統。
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}