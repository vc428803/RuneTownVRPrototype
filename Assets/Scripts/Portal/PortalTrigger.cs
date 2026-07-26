using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [Header("傳送目的地")]
    [SerializeField]
    private string targetSceneName;

    [SerializeField]
    private string targetSpawnId;

    [Header("防止重複觸發")]
    [SerializeField]
    private float triggerCooldown = 2f;

    private Collider portalCollider;
    private XROrigin xrOrigin;
    private bool isTeleporting;
    // 修改原因：限制除錯訊息頻率，避免 Console 每幀大量輸出。
    private float nextDebugTime;

    private void Awake()
    {
        portalCollider = GetComponent<Collider>();

        // 修改原因：
        // 傳送陣必須是 Trigger，不應阻擋玩家移動。
        portalCollider.isTrigger = true;
    }

    private void Start()
    {
        // 修改原因：
        // 確認 PortalTrigger 腳本在 Play Mode 時有正常啟動。
        Debug.Log(
            $"[PortalTrigger] Start 執行：{gameObject.name}"
        );

        FindPlayer();

        Debug.Log(
            xrOrigin != null
                ? "[PortalTrigger] 已找到 XR Origin"
                : "[PortalTrigger] 找不到 XR Origin"
        );
    }

    private void Update()
    {
        if (isTeleporting)
        {
            return;
        }

        if (xrOrigin == null)
        {
            FindPlayer();

            if (xrOrigin == null)
            {
                return;
            }
        }

        Camera xrCamera = xrOrigin.Camera;

        if (xrCamera == null)
        {
            return;
        }

        Vector3 playerHeadPosition =
            xrCamera.transform.position;

        // 修改原因：
        // 每秒顯示一次玩家頭部座標與 Portal 範圍，
        // 確認玩家是否真的移動到傳送區域。
        if (Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + 1f;

            Debug.Log(
                $"[PortalTrigger] 玩家頭部：{playerHeadPosition} | " +
                $"Portal Center：{portalCollider.bounds.center} | " +
                $"Portal Size：{portalCollider.bounds.size}"
            );
        }

        // 修改原因：
        // XR Simulator 或瞬移可能直接改變玩家 Transform，
        // 不一定會觸發 OnTriggerEnter。
        // 因此直接檢查玩家頭部是否位於 Portal Collider 範圍內。
        Vector3 closestPoint =
            portalCollider.ClosestPoint(playerHeadPosition);

        bool playerIsInside =
            Vector3.SqrMagnitude(
                closestPoint - playerHeadPosition
            ) < 0.0001f;

        if (playerIsInside)
        {
            ActivatePortal();
        }
    }

    private void FindPlayer()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
    }

    private void ActivatePortal()
    {
        if (isTeleporting)
        {
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError(
                "[PortalTrigger] 找不到 SceneTransitionManager。"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                $"[PortalTrigger] {gameObject.name} 沒有設定 Target Scene Name。"
            );
            return;
        }

        isTeleporting = true;

        Debug.Log(
            $"[PortalTrigger] 開始傳送至：{targetSceneName}"
        );

        SceneTransitionManager.Instance.LoadScene(
            targetSceneName,
            targetSpawnId
        );

        Invoke(
            nameof(ResetTeleportState),
            triggerCooldown
        );
    }

    private void ResetTeleportState()
    {
        isTeleporting = false;
    }

    private void Reset()
    {
        Collider currentCollider =
            GetComponent<Collider>();

        if (currentCollider != null)
        {
            currentCollider.isTrigger = true;
        }
    }
}