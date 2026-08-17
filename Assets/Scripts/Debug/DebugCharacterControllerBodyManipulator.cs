using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

[CreateAssetMenu(
    fileName = "DebugCharacterControllerBodyManipulator",
    menuName = "RuneTown/Debug/Character Controller Body Manipulator"
)]
public class DebugCharacterControllerBodyManipulator
    : CharacterControllerBodyManipulator
{
    public override CollisionFlags MoveBody(Vector3 motion)
    {
        // 修改原因：確認 XR Body Transformer 移動玩家時，
        // 是否真的經過 CharacterControllerBodyManipulator.MoveBody()，
        // 並觀察 CharacterController.Move() 回傳的碰撞結果。
        UnityEngine.Debug.Log(
            $"[XR MoveBody Debug] BEFORE " +
            $"Motion={motion}, " +
            $"ControllerNull={(characterController == null)}, " +
            $"ControllerEnabled={(characterController != null && characterController.enabled)}, " +
            $"IsGrounded={(characterController != null && characterController.isGrounded)}"
        );

        CollisionFlags flags =
            base.MoveBody(
                motion
            );

        UnityEngine.Debug.Log(
            $"[XR MoveBody Debug] AFTER " +
            $"Flags={flags}, " +
            $"Position={(characterController != null ? characterController.transform.position.ToString() : "null")}"
        );

        return flags;
    }
}