using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

[CreateAssetMenu(
    fileName = "DebugCharacterControllerBodyManipulator",
    menuName = "RuneTown/Debug/Character Controller Body Manipulator"
)]
public class DebugCharacterControllerBodyManipulator
    : CharacterControllerBodyManipulator
{
    private RuneTownWalkDataProvider walkDataProvider;

    private RuneTownWalkDataProvider GetWalkDataProvider()
    {
        // 修改原因：正式 GAT-driven movement 需要取得目前場景中的
        // RuneTownWalkDataProvider，讓玩家移動前可以依 GAT WalkData 判斷是否可走。
        if (walkDataProvider == null)
        {
            walkDataProvider =
                Object.FindFirstObjectByType<RuneTownWalkDataProvider>();
        }

        return walkDataProvider;
    }

    public override CollisionFlags MoveBody(Vector3 motion)
    {
        // 修改原因：確認 XR Body Transformer 傳入的移動量，
        // 並保留 GAT-driven movement 的除錯資訊。
        UnityEngine.Debug.Log(
            $"[XR MoveBody Debug] BEFORE " +
            $"Motion={motion}, " +
            $"ControllerNull={(characterController == null)}, " +
            $"ControllerEnabled={(characterController != null && characterController.enabled)}, " +
            $"IsGrounded={(characterController != null && characterController.isGrounded)}"
        );

        var provider = GetWalkDataProvider();

        // 修改原因：非 RuneTown / RO WalkData 場景仍維持 XR Toolkit
        // 原本的 CharacterController.Move() 行為，避免影響其他場景。
        if (provider == null || characterController == null)
        {
            CollisionFlags normalFlags =
                base.MoveBody(motion);

            UnityEngine.Debug.Log(
                $"[XR MoveBody Debug] AFTER NORMAL " +
                $"Flags={normalFlags}, " +
                $"Position={(characterController != null ? characterController.transform.position.ToString() : "null")}"
            );

            return normalFlags;
        }

        Vector3 currentPosition =
            characterController.transform.position;

        // 修改原因：RO 地圖的垂直位置由 GAT WalkData 決定，
        // 因此只採用 XR locomotion 傳入的水平 X/Z 位移，
        // 不讓 XR gravity 或 Ground MeshCollider 改變玩家高度。
        Vector3 targetPosition = new Vector3(
            currentPosition.x + motion.x,
            currentPosition.y,
            currentPosition.z + motion.z
        );

        Vector2Int targetCell =
            provider.GetTilePositionForPoint(targetPosition);

        // 修改原因：GAT Walkable flag 是 RO 地圖正式的通行規則；
        // 不可行走的 Cell 直接阻止玩家進入。
        if (!provider.IsCellWalkable(targetCell))
        {
            UnityEngine.Debug.Log(
                $"[RuneTown GAT Move Debug] " +
                $"BLOCKED Cell={targetCell}, " +
                $"Current={currentPosition}, " +
                $"Target={targetPosition}"
            );

            return CollisionFlags.Sides;
        }

        // 修改原因：沿用已驗證的 RebuildClient GAT 四角高度插值，
        // 直接取得目標 X/Z 對應的正式玩家地面高度。
        float targetGroundY =
            provider.GetHeightForPosition(targetPosition);

        targetPosition.y = targetGroundY;

        // 修改原因：RO 的高低差與樓梯應由 GAT 高度資料控制，
        // 不再經過 CharacterController.Move()，
        // 避免 Ground / WalkMap 的垂直 Mesh 面把 Walkable 台階當成牆。
        characterController.transform.position =
            targetPosition;

        UnityEngine.Debug.Log(
            $"[RuneTown GAT Move Debug] " +
            $"MOVED Cell={targetCell}, " +
            $"From={currentPosition}, " +
            $"To={targetPosition}, " +
            $"DeltaY={targetGroundY - currentPosition.y}"
        );

        // 修改原因：GAT 已提供玩家腳下的正式地面高度，
        // 因此回傳 Below 表示這次移動維持在可行走地面上。
        return CollisionFlags.Below;
    }

}