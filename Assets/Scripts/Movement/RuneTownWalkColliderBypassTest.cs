using UnityEngine;

public class RuneTownWalkColliderBypassTest : MonoBehaviour
{
    [SerializeField]
    private LayerMask bypassLayers;

    private CharacterController characterController;

    private void Start()
    {
        // 修改原因：取得目前 XR Origin 上的 CharacterController，
        // 供 Walkable 高低差測試使用。
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        // 修改原因：暫時讓 CharacterController 不與 Ground / WalkMap
        // 發生物理阻擋，用來驗證 GAT Walkable + Height Follow 方案。
        Physics.IgnoreLayerCollision(
            gameObject.layer,
            LayerMask.NameToLayer("Ground"),
            true
        );

        Physics.IgnoreLayerCollision(
            gameObject.layer,
            LayerMask.NameToLayer("WalkMap"),
            true
        );
    }

    private void OnDisable()
    {
        // 修改原因：測試結束時恢復原本 Layer Collision。
        Physics.IgnoreLayerCollision(
            gameObject.layer,
            LayerMask.NameToLayer("Ground"),
            false
        );

        Physics.IgnoreLayerCollision(
            gameObject.layer,
            LayerMask.NameToLayer("WalkMap"),
            false
        );
    }
}