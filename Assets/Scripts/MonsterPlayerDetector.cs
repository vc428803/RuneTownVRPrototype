using UnityEngine;

public class MonsterPlayerDetector : MonoBehaviour
{
    [SerializeField]
    private float detectionDistance = 2f;

    [SerializeField]
    private GameObject alertIndicator;

    private Transform playerCamera;
    private bool isPlayerInside;

    private void Start()
    {
        // 修改原因：VR 玩家實際位置以頭戴裝置使用的 Main Camera 為準，
        // 不依賴 XR Origin 的 Collider 或 Trigger 事件。
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                "MonsterPlayerDetector：找不到 Main Camera。",
                this);
        }

        // 修改原因：進入遊戲時先隱藏警戒符號，
        // 避免場景中的啟用狀態設定錯誤。
        if (alertIndicator != null)
        {
            alertIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 monsterPosition = transform.position;
        Vector3 playerPosition = playerCamera.position;

        // 修改原因：只計算地面上的水平距離，
        // 避免玩家頭部高度影響魔物的靠近判定。
        monsterPosition.y = 0f;
        playerPosition.y = 0f;

        float distance = Vector3.Distance(monsterPosition, playerPosition);
        bool isCurrentlyInside = distance <= detectionDistance;

        if (isCurrentlyInside && !isPlayerInside)
        {
            isPlayerInside = true;

            // 修改原因：玩家進入偵測範圍時顯示波利的警戒符號。
            if (alertIndicator != null)
            {
                alertIndicator.SetActive(true);
            }

            UnityEngine.Debug.Log("玩家進入波利偵測範圍。", this);
        }
        else if (!isCurrentlyInside && isPlayerInside)
        {
            isPlayerInside = false;

            // 修改原因：玩家離開偵測範圍時隱藏波利的警戒符號。
            if (alertIndicator != null)
            {
                alertIndicator.SetActive(false);
            }

            UnityEngine.Debug.Log("玩家離開波利偵測範圍。", this);
        }
    }
}