using UnityEngine;

public class FountainQuestTrigger : MonoBehaviour
{
    [Header("玩家靠近噴水池時顯示的任務完成文字")]
    public GameObject questCompleteText; // 指向 UI 文字物件，用來顯示「任務完成」

    private bool questCompleted = false; // 紀錄任務是否已完成，避免重複觸發

    private void Awake()
    {
        // 遊戲開始時，先把任務完成文字隱藏
        if (questCompleteText != null)
        {
            questCompleteText.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 如果任務已經完成，就不再觸發
        if (questCompleted)
        {
            return;
        }

        // 判斷是否是玩家進入：
        // 這裡用最簡單的方式，只要進入的物件有 CharacterController，就當作玩家。
        if (other.GetComponent<CharacterController>() != null)
        {
            questCompleted = true; // 標記任務完成

            // 顯示任務完成文字
            if (questCompleteText != null)
            {
                questCompleteText.SetActive(true);
            }

            // 在 Console 輸出訊息，方便除錯或測試
            Debug.Log("Quest Complete: Fountain found!");
        }
    }
}
