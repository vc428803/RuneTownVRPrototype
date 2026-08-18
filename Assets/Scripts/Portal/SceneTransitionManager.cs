using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using Unity.XR.CoreUtils;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private string targetSpawnId;

    private void Awake()
    {
        // 修改原因：
        // 防止每次切換 Scene 都重新建立一個管理器。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 修改原因：
        // 讓傳送管理器在切換 Scene 後繼續存在。
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string targetSceneName, string spawnId)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                "[SceneTransitionManager] Target Scene Name 不可為空。"
            );
            return;
        }

        targetSpawnId = spawnId;

        StartCoroutine(
            LoadSceneCoroutine(targetSceneName)
        );
    }

    private IEnumerator LoadSceneCoroutine(string targetSceneName)
    {
        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(targetSceneName);

        if (loadOperation == null)
        {
            Debug.LogError(
                $"[SceneTransitionManager] 無法載入 Scene：{targetSceneName}"
            );
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // 修改原因：
        // 等待一個 Frame，讓新 Scene 的物件完成初始化。
        yield return null;

        MovePlayerToSpawnPoint();

        // 修改原因：
        // 新 Scene 載入後，XR Interaction Toolkit 與 Input System
        // 仍可能繼續進行初始化。若立即重啟 Input Action Manager，
        // 後續初始化可能再次停用 Input Actions。
        // 因此先等待一秒，再重新啟用輸入。
        yield return new WaitForSecondsRealtime(1f);

        yield return RestartInputActionManager();
    }

    private IEnumerator RestartInputActionManager()
    {
        // 修改原因：
        // 不再使用 FindFirstObjectByType<InputActionManager>()，
        // 避免 Scene 切換期間抓到新 Scene 內其他或即將被刪除的
        // Input Action Manager。
        // 改成從 DontDestroyOnLoad 保留下來的 PersistentPlayer 底下尋找，
        // 確保操作的是玩家實際使用的 Input Action Manager。
        PersistentPlayer persistentPlayer =
            FindFirstObjectByType<PersistentPlayer>();

        if (persistentPlayer == null)
        {
            Debug.LogWarning(
                "[SceneTransitionManager] 找不到 PersistentPlayer，無法重新啟用輸入。"
            );
            yield break;
        }

        InputActionManager inputActionManager =
            persistentPlayer.GetComponentInChildren<InputActionManager>(
                true
            );

        if (inputActionManager == null)
        {
            Debug.LogWarning(
                "[SceneTransitionManager] PersistentPlayer 底下找不到 Input Action Manager。"
            );
            yield break;
        }

        Debug.Log(
            $"[SceneTransitionManager] 準備重新啟用 Input Action Manager：" +
            $"{inputActionManager.gameObject.name}，" +
            $"Instance ID：{inputActionManager.GetInstanceID()}"
        );

        // 修改原因：
        // 模擬 Inspector 中手動取消勾選 Input Action Manager。
        inputActionManager.enabled = false;

        // 修改原因：
        // 等待一個 Frame，確保 OnDisable() 與 Input Actions 停用流程完成。
        yield return null;

        // 修改原因：
        // 模擬 Inspector 中重新勾選 Input Action Manager，
        // 讓 OnEnable() 重新啟用玩家的 Input Actions。
        inputActionManager.enabled = true;

        Debug.Log(
            $"[SceneTransitionManager] PersistentPlayer 的 Input Action Manager 已重新啟用，" +
            $"Instance ID：{inputActionManager.GetInstanceID()}"
        );
    }

    private void MovePlayerToSpawnPoint()
    {
        if (string.IsNullOrWhiteSpace(targetSpawnId))
        {
            Debug.LogWarning(
                "[SceneTransitionManager] 沒有指定 Spawn ID。"
            );
            return;
        }

        PortalSpawnPoint[] spawnPoints =
            FindObjectsByType<PortalSpawnPoint>(
                FindObjectsSortMode.None
            );

        PortalSpawnPoint targetSpawnPoint = null;

        foreach (PortalSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.SpawnId == targetSpawnId)
            {
                targetSpawnPoint = spawnPoint;
                break;
            }
        }

        if (targetSpawnPoint == null)
        {
            Debug.LogError(
                $"[SceneTransitionManager] 找不到 Spawn ID：{targetSpawnId}"
            );
            return;
        }

        XROrigin xrOrigin =
            FindFirstObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError(
                "[SceneTransitionManager] 新 Scene 中找不到 XR Origin。"
            );
            return;
        }

        Transform spawnTransform =
            targetSpawnPoint.transform;

        // 修改原因：確認 Scene 切換定位前 XR Origin、Camera 與 Spawn
        // 的實際世界座標，用來追查玩家之後被移回原點附近的問題。
        Debug.Log(
            "[SceneTransition Debug] BEFORE SPAWN " +
            "Origin=" + xrOrigin.Origin.transform.position +
            ", Camera=" + xrOrigin.Camera.transform.position +
            ", Spawn=" + spawnTransform.position
        );

        // 修改原因：
        // MoveCameraToWorldLocation 會校正 VR 頭部在 XR Origin 內的偏移，
        // 讓玩家實際看到的位置落在出生點。
        xrOrigin.MoveCameraToWorldLocation(
            spawnTransform.position
        );

        // 修改原因：確認 MoveCameraToWorldLocation 執行後，
        // XR Origin 是否已正確移到 Spawn 對應位置。
        Debug.Log(
            "[SceneTransition Debug] AFTER MOVE " +
            "Origin=" + xrOrigin.Origin.transform.position +
            ", Camera=" + xrOrigin.Camera.transform.position
        );

        // 修改原因：
        // 讓玩家進入新地圖時面向 Spawn Point 的正前方。
        xrOrigin.MatchOriginUpCameraForward(
            spawnTransform.up,
            spawnTransform.forward
        );

        // 修改原因：確認旋轉校正後是否意外改變
        // XR Origin 或 Camera 的世界座標。
        Debug.Log(
            "[SceneTransition Debug] AFTER ROTATE " +
            "Origin=" + xrOrigin.Origin.transform.position +
            ", Camera=" + xrOrigin.Camera.transform.position
        );

        Debug.Log(
            $"[SceneTransitionManager] 玩家已移動到：{targetSpawnId}"
        );
    }
}