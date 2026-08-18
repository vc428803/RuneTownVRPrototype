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

    private void Awake()
    {
        portalCollider = GetComponent<Collider>();

        // 修改原因：
        // 傳送陣必須是 Trigger，不應阻擋玩家移動。
        portalCollider.isTrigger = true;
    }

    private void Start()
    {
        FindPlayer();
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

        CharacterController characterController =
            xrOrigin.Origin.GetComponent<CharacterController>();

        if (characterController == null)
        {
            return;
        }

        // 修改原因：Portal 應以玩家身體位置判斷是否進入傳送區，
        // 不應使用 Main Camera / 頭部高度，避免玩家身高或 XR Camera Offset
        // 改變後頭部超出 Portal Collider 而無法觸發傳送。
        Vector3 playerBodyPosition =
            characterController.transform.TransformPoint(
                characterController.center
            );

        Vector3 closestPoint =
            portalCollider.ClosestPoint(playerBodyPosition);

        bool playerIsInside =
            Vector3.SqrMagnitude(
                closestPoint - playerBodyPosition
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