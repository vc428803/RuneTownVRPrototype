using UnityEngine;

public class CharacterControllerFeetDebug : MonoBehaviour
{
    [SerializeField]
    private CharacterController characterController;

    private void LateUpdate()
    {
        if (characterController == null)
            return;

        // 修改原因：把 DebugFeetMarker 固定到 CharacterController
        // 真正膠囊底部的位置，方便第三人稱確認玩家是否貼地。
        Vector3 centerWorld =
            characterController.transform.TransformPoint(
                characterController.center
            );

        // 修改原因：XR Toolkit 設定 CharacterController.center 時
        // 會額外加入 Skin Width，因此 Debug 腳底位置也要扣除 Skin Width，
        // 才能對齊實際的 Body Ground Position。
        float bottomY =
            centerWorld.y -
            (characterController.height * 0.5f) -
            characterController.skinWidth;

        transform.position =
            new Vector3(
                centerWorld.x,
                bottomY,
                centerWorld.z
            );
    }
}