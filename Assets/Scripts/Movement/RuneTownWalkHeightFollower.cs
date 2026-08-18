using UnityEngine;

public class RuneTownWalkHeightFollower : MonoBehaviour
{
    private RuneTownWalkDataProvider walkDataProvider;
    private CharacterController characterController;

    private void Start()
    {
        // 修改原因：XR Origin 與地圖 Provider 位於不同 Scene，
        // 因此不使用 Inspector 的跨 Scene Reference，
        // CharacterController 直接從目前 XR Origin 取得。
        characterController = GetComponent<CharacterController>();

        // 修改原因：地圖會在 Scene 切換後載入，
        // Runtime 自動尋找目前場景中的 WalkData Provider。
        walkDataProvider =
            FindFirstObjectByType<RuneTownWalkDataProvider>();
    }

    private void LateUpdate()
    {
        // 修改原因：Player 為 DontDestroyOnLoad，
        // 切換地圖後原本 Provider 可能已被銷毀，
        // 因此沒有 Provider 時重新尋找目前 Scene 的 Provider。
        if (walkDataProvider == null)
        {
            walkDataProvider =
                FindFirstObjectByType<RuneTownWalkDataProvider>();

            if (walkDataProvider == null)
                return;
        }

        if (characterController == null)
            return;

        Vector3 currentPosition = transform.position;

        Vector2Int tile =
            walkDataProvider.GetTilePositionForPoint(currentPosition);

        // 修改原因：只有 GAT 判定為 Walkable 的 Cell，
        // 才允許使用 WalkData 高度修正玩家位置。
        if (!walkDataProvider.IsCellWalkable(tile))
            return;

        float targetY =
            walkDataProvider.GetHeightForPosition(currentPosition);

        // 修改原因：GAT 計算的是玩家腳底應該所在的地面高度，
        // 不能直接拿 XR Origin 的 Y 比較。
        // CharacterController 膠囊底部才代表目前玩家腳底位置。
        float controllerBottomY =
            characterController.transform.TransformPoint(
                characterController.center
            ).y
            - (characterController.height * 0.5f);

        float deltaY =
            targetY - controllerBottomY;

        // 修改原因：暫時以獨立測試方式驗證 RO WalkData 計算出的高度，
        // 是否能讓 CharacterController 腳底正確跟隨 GAT 地面高度。
        if (Mathf.Abs(deltaY) > 0.001f)
        {
            characterController.Move(
                new Vector3(0f, deltaY, 0f)
            );
        }
    }
}