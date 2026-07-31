using System.Diagnostics;
using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Transform targetCamera;

    private void Start()
    {
        // 修改原因：取得 VR 玩家目前使用的主攝影機，讓 2D 魔物面向玩家。
        if (Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                $"{nameof(BillboardToCamera)}：找不到標記為 MainCamera 的攝影機。",
                this);
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 direction = targetCamera.position - transform.position;

        // 修改原因：只允許魔物繞 Y 軸旋轉，避免波利向上或向下傾斜。
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }
}