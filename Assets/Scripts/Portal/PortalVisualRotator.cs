using UnityEngine;

public class PortalVisualRotator : MonoBehaviour
{
    // 修改原因：
    // 讓傳送陣圖案持續旋轉，玩家更容易辨認入口位置。
    [SerializeField]
    private float rotationSpeed = 45f;

    private void Update()
    {
        transform.Rotate(
            0f,
            rotationSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }
}