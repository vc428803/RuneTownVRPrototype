using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        // 修改原因：
        // MoveCameraToWorldLocation 會校正 VR 頭部在 XR Origin 內的偏移，
        // 讓玩家實際看到的位置落在出生點。
        xrOrigin.MoveCameraToWorldLocation(
            spawnTransform.position
        );

        // 修改原因：
        // 讓玩家進入新地圖時面向 Spawn Point 的正前方。
        xrOrigin.MatchOriginUpCameraForward(
            spawnTransform.up,
            spawnTransform.forward
        );

        Debug.Log(
            $"[SceneTransitionManager] 玩家已移動到：{targetSpawnId}"
        );
    }
}