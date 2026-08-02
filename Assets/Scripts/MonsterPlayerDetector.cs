using UnityEngine;
using System.Collections;

public class MonsterPlayerDetector : MonoBehaviour
{
    [SerializeField]
    private float detectionDistance = 2f;

    [SerializeField]
    private float moveSpeed = 0.8f;

    [SerializeField]
    private float stoppingDistance = 1.2f;

    [SerializeField]
    private float attackCooldown = 1.5f;

    [SerializeField]
    private GameObject alertIndicator;

    [SerializeField]
    private float alertDisplayDuration = 1f;

    private Coroutine alertCoroutine;

    [SerializeField]
    private Animator monsterAnimator;

    private Transform playerCamera;
    private bool isPlayerInside;
    private float nextAttackTime;

    private static readonly int IsWalkingHash =
        Animator.StringToHash("IsWalking");

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

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
        UpdateAttack(distance);
    }

    private void UpdateDetectionState(bool isCurrentlyInside)
    {
        if (isCurrentlyInside && !isPlayerInside)
        {
            isPlayerInside = true;

            // 修改原因：警戒表情只在首次發現玩家時短暫顯示，
            // 不讓它在追蹤或連續攻擊期間一直留在魔物頭上。
            ShowAlertIndicator();

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
                monsterAnimator.ResetTrigger(AttackHash);
            }

            // 修改原因：玩家離開後重設攻擊計時，
            // 下次再次進入攻擊距離時可以立即攻擊。
            nextAttackTime = 0f;

            UnityEngine.Debug.Log(
                "玩家離開波利偵測範圍。",
                this);
        }
    }

    private void ShowAlertIndicator()
    {
        if (alertIndicator == null)
        {
            return;
        }

        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
            alertCoroutine = null;
        }


        alertCoroutine = StartCoroutine(
            ShowAlertIndicatorCoroutine());
    }

    private IEnumerator ShowAlertIndicatorCoroutine()
    {
        alertIndicator.SetActive(true);

        yield return new WaitForSeconds(
            alertDisplayDuration);

        alertIndicator.SetActive(false);
        alertCoroutine = null;
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

    private void UpdateAttack(float distance)
    {
        bool isWithinAttackDistance =
            isPlayerInside &&
            distance <= stoppingDistance;

        if (!isWithinAttackDistance)
        {
            // 修改原因：玩家離開攻擊距離後重設計時，
            // 再次靠近時讓波利立即發動下一次攻擊。
            nextAttackTime = 0f;
            return;
        }

        if (monsterAnimator == null)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        // 修改原因：玩家仍在攻擊距離內時，
        // 依照冷卻時間重複觸發 Attack 動畫。
        monsterAnimator.SetTrigger(AttackHash);
        nextAttackTime = Time.time + attackCooldown;

        UnityEngine.Debug.Log(
            "波利發動攻擊。",
            this);
    }
}