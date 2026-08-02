using UnityEngine;

public class MonsterPlayerDetector : MonoBehaviour
{
    [SerializeField]
    private float detectionDistance = 2f;

    [SerializeField]
    private float moveSpeed = 0.8f;

    [SerializeField]
    private float stoppingDistance = 1.2f;

    [SerializeField]
    private GameObject alertIndicator;

    [SerializeField]
    private Animator monsterAnimator;

    private Transform playerCamera;
    private bool isPlayerInside;

    private static readonly int IsWalkingHash =
        Animator.StringToHash("IsWalking");

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

        // 修改原因：進入遊戲時先隱藏警戒符號。
        if (alertIndicator != null)
        {
            alertIndicator.SetActive(false);
        }

        // 修改原因：進入遊戲時先維持待機動畫。
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(IsWalkingHash, false);
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

        // 修改原因：只比較水平距離，避免 VR 頭部高度影響判斷。
        Vector3 horizontalMonsterPosition = monsterPosition;
        Vector3 horizontalPlayerPosition = playerPosition;

        horizontalMonsterPosition.y = 0f;
        horizontalPlayerPosition.y = 0f;

        float distance = Vector3.Distance(
            horizontalMonsterPosition,
            horizontalPlayerPosition);

        bool isCurrentlyInside =
            distance <= detectionDistance;

        UpdateDetectionState(isCurrentlyInside);
        UpdateMovement(distance, playerPosition);
    }

    private void UpdateDetectionState(bool isCurrentlyInside)
    {
        if (isCurrentlyInside && !isPlayerInside)
        {
            isPlayerInside = true;

            if (alertIndicator != null)
            {
                alertIndicator.SetActive(true);
            }

            UnityEngine.Debug.Log(
                "玩家進入波利偵測範圍。",
                this);
        }
        else if (!isCurrentlyInside && isPlayerInside)
        {
            isPlayerInside = false;

            if (alertIndicator != null)
            {
                alertIndicator.SetActive(false);
            }

            if (monsterAnimator != null)
            {
                monsterAnimator.SetBool(IsWalkingHash, false);
            }

            UnityEngine.Debug.Log(
                "玩家離開波利偵測範圍。",
                this);
        }
    }

    private void UpdateMovement(
        float distance,
        Vector3 playerPosition)
    {
        bool shouldMove =
            isPlayerInside &&
            distance > stoppingDistance;

        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(
                IsWalkingHash,
                shouldMove);
        }

        if (!shouldMove)
        {
            return;
        }

        // 修改原因：波利只沿地面朝玩家靠近，
        // 保留目前 Y 座標，避免追蹤玩家頭部高度而飛起來。
        Vector3 targetPosition = new Vector3(
            playerPosition.x,
            transform.position.y,
            playerPosition.z);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);
    }
}