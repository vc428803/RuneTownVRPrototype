using UnityEngine;
// 引入 Unity 引擎的命名空間，讓程式能使用 Unity 提供的類別與功能。

public class LookAtCamera : MonoBehaviour
// 宣告一個名為 LookAtCamera 的類別，繼承自 MonoBehaviour，表示這是一個可掛在遊戲物件上的 Unity 元件。
{
    private Camera mainCamera;
    // 宣告一個私有變數 mainCamera，用來儲存場景中的主攝影機。

    private void Start()
    {
        mainCamera = Camera.main;
        // 在遊戲開始時（Start 事件），取得場景中的主攝影機並存入 mainCamera。
    }

    private void LateUpdate()
    // LateUpdate 在所有 Update 執行完後才會呼叫，通常用來處理相機或物件的跟隨行為。
    {
        if (mainCamera == null)
        {
            return;
            // 如果場景中沒有主攝影機，直接跳出，不執行後續程式。
        }

        Vector3 direction = transform.position - mainCamera.transform.position;
        // 計算物件與攝影機之間的方向向量（物件位置 - 攝影機位置）。

        direction.y = 0;
        // 將方向向量的 y 軸設為 0，避免物件在垂直方向上旋轉，只保留水平面上的朝向。

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            // 如果方向向量不是零向量，將物件的旋轉設為朝向該方向。
            // Quaternion.LookRotation 會建立一個四元數，讓物件面向指定方向。
        }
    }
}
