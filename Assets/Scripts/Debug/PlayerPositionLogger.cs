using UnityEngine;

public class PlayerPositionLogger : MonoBehaviour
{
    [SerializeField]
    private float logInterval = 1f;

    private Transform playerCamera;
    private float nextLogTime;

    private void Start()
    {
        // 修改原因：VR 玩家在場景中的實際位置以 Main Camera 為準，
        // 方便測試時記錄玩家目前所在的地圖座標。
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                "PlayerPositionLogger：找不到 Main Camera。",
                this);
        }
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (Time.time < nextLogTime)
        {
            return;
        }

        Vector3 position = playerCamera.position;

        UnityEngine.Debug.Log(
            $"玩家座標：X={position.x:F2}, Y={position.y:F2}, Z={position.z:F2}",
            this);

        nextLogTime = Time.time + logInterval;
    }
}