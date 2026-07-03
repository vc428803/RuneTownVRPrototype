using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// LLM 對話客戶端：負責與後端 API 通訊，發送玩家訊息並接收 NPC 回覆
/// 掛載到 Unity GameObject 上使用
/// </summary>
public class LLMClient : MonoBehaviour
{
    [Header("AI Connection Settings")]
    [SerializeField] private AiConnectionSettings aiConnectionSettings;

    /// <summary>
    /// 發送給 API 的請求格式：包含 NPC 識別碼與玩家訊息
    /// </summary>
    [Serializable]
    public class ChatRequest
    {
        public string npcId;           // 目標 NPC 的唯一識別碼，讓後端知道要哪個角色回應
        public string playerMessage;   // 玩家輸入的對話內容
    }

    /// <summary>
    /// API 回傳的回應格式：包含 NPC 識別碼與回覆文字
    /// </summary>
    [Serializable]
    public class ChatResponse
    {
        public string npcId;   // 回覆的 NPC 是誰（驗證用）
        public string reply;   // NPC 的回話內容，這是要顯示在遊戲中的文字
    }

    /// <summary>
    /// 發送對話請求給 LLM API（協程方法，需用 StartCoroutine() 呼叫）
    /// </summary>
    /// <param name="npcId">要對話的 NPC 識別碼</param>
    /// <param name="playerMessage">玩家輸入的訊息</param>
    /// <param name="onSuccess">請求成功時的回呼，參數為 NPC 的回覆文字</param>
    /// <param name="onError">請求失敗時的回呼，參數為錯誤訊息</param>
    public IEnumerator SendChat(string npcId, string playerMessage, Action<string> onSuccess, Action<string> onError)
    {
        // 建立請求資料物件，填入 NPC 識別碼與玩家訊息
        ChatRequest requestData = new ChatRequest
        {
            npcId = npcId,
            playerMessage = playerMessage
        };

        // 將 C# 物件序列化為 JSON 字串，準備放入 HTTP Body
        string jsonBody = JsonUtility.ToJson(requestData);

        if (aiConnectionSettings == null)
        {
            onError?.Invoke("AiConnectionSettings 尚未指定，請把 AiConnectionSettings.asset 拖到 LLMClient。");
            yield break;
        }

        string chatUrl = aiConnectionSettings.GetChatUrl();

        if (chatUrl == "mock://chat")
        {
            onSuccess?.Invoke("This is a mock NPC reply. The AI server is not connected.");
            yield break;
        }

        Debug.Log("LLM API URL: " + chatUrl);
        Debug.Log("LLM Request Body: " + jsonBody);

        // 建立 HTTP POST 請求，using 確保完成後自動釋放資源
        using (UnityWebRequest request = new UnityWebRequest(chatUrl, "POST"))
        {
            // 將 JSON 字串轉為 UTF-8 位元組陣列，作為上傳內容
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);      // 設定上傳處理器：發送 JSON 資料
            request.downloadHandler = new DownloadHandlerBuffer();         // 設定下載處理器：接收回應內容

            request.SetRequestHeader("Content-Type", "application/json"); // 設定標頭：告訴後端這是 JSON 格式
            request.timeout = 30;

            // 發送請求並暫停協程，等待網路回應（非同步，不阻塞遊戲主執行緒）
            Debug.Log("Sending UnityWebRequest...");
            yield return request.SendWebRequest();
            Debug.Log("UnityWebRequest finished. Result: " + request.result);
            // 檢查請求結果
            if (request.result == UnityWebRequest.Result.Success)
            {
                // HTTP 請求成功（狀態碼 2xx），取得回應文字
                string responseText = request.downloadHandler.text;
                Debug.Log("Raw API Response: " + responseText);

                // 將 JSON 回應反序列化為 ChatResponse 物件
                ChatResponse responseData = JsonUtility.FromJson<ChatResponse>(responseText);

                // 驗證回應資料：確保解析成功且 reply 欄位不為空
                if (responseData != null && !string.IsNullOrEmpty(responseData.reply))
                {
                    onSuccess?.Invoke(responseData.reply);  // 呼叫成功回呼，傳入 NPC 回覆文字
                }
                else
                {
                    // API 回傳成功但內容異常（reply 為空或解析失敗）
                    onError?.Invoke("API 回傳成功，但 reply 是空的。Response: " + responseText);
                }
            }
            else
            {
                // HTTP 請求失敗：網路錯誤、連線逾時、後端回傳 4xx/5xx 等
                onError?.Invoke("API 呼叫失敗：" + request.error);
            }
        } // using 結束，自動釋放 UnityWebRequest 資源
    }
}