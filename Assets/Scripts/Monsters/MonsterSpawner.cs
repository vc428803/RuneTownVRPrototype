using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private MonsterSpawnAreaDefinition spawnAreaDefinition;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private Transform activeMonstersParent;

    [SerializeField]
    private MonsterManager monsterManager;

    private void Start()
    {
        SpawnMonsters();
    }

    private void SpawnMonsters()
    {
        if (spawnAreaDefinition == null)
        {
            UnityEngine.Debug.LogWarning(
                "MonsterSpawner：尚未指定 Spawn Area Definition。",
                this);

            return;
        }

        if (spawnPoint == null)
        {
            UnityEngine.Debug.LogWarning(
                "MonsterSpawner：尚未指定 Spawn Point。",
                this);

            return;
        }

        if (activeMonstersParent == null)
        {
            UnityEngine.Debug.LogWarning(
                "MonsterSpawner：尚未指定 Active Monsters Parent。",
                this);

            return;
        }

        if (monsterManager == null)
        {
            UnityEngine.Debug.LogWarning(
                "MonsterSpawner：尚未指定 Monster Manager。",
                this);

            return;
        }

        int totalSpawnedCount = 0;

        // 修改原因：依照區域設定中的多筆 Spawn Entry，
        // 讓同一個生成區域可以自動生成多種不同魔物。
        foreach (MonsterSpawnEntry spawnEntry
                 in spawnAreaDefinition.SpawnEntries)
        {
            if (spawnEntry == null)
            {
                continue;
            }

            GameObject monsterPrefab =
                spawnEntry.MonsterPrefab;

            if (monsterPrefab == null)
            {
                UnityEngine.Debug.LogWarning(
                    "MonsterSpawner：Spawn Entry 尚未指定 Monster Prefab。",
                    this);

                continue;
            }

            int spawnCount =
                Mathf.Max(0, spawnEntry.SpawnCount);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 randomOffset =
                    UnityEngine.Random.insideUnitCircle *
                    spawnAreaDefinition.SpawnRadius;

                // 修改原因：以 Spawn Point 為區域中心，
                // 在水平 X、Z 範圍內隨機分布並保留地面 Y 高度。
                Vector3 spawnPosition = new Vector3(
                    spawnPoint.position.x + randomOffset.x,
                    spawnPoint.position.y,
                    spawnPoint.position.z + randomOffset.y);

                // 修改原因：將生成中的魔物統一放入 ActiveMonsters，
                // 避免大量執行物件散落在 Hierarchy 根目錄。
                GameObject spawnedMonster = Instantiate(
                    monsterPrefab,
                    spawnPosition,
                    spawnPoint.rotation,
                    activeMonstersParent);

                spawnedMonster.name =
                    $"{monsterPrefab.name}_{i + 1:00}";

                MonsterIdentity monsterIdentity =
                    spawnedMonster.GetComponent<MonsterIdentity>();

                if (monsterIdentity == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"MonsterSpawner：{spawnedMonster.name} " +
                        "缺少 MonsterIdentity。",
                        spawnedMonster);

                    continue;
                }

                // 修改原因：將管理器交給新生成的魔物保存，
                // 讓魔物被銷毀時可以自動取消註冊。
                monsterIdentity.Initialize(
                    monsterManager);

                // 修改原因：魔物生成完成後立即註冊，
                // 讓其他系統可以取得目前有效的魔物資料。
                monsterManager.RegisterMonster(
                    monsterIdentity);

                totalSpawnedCount++;
            }
        }

        UnityEngine.Debug.Log(
            $"MonsterSpawner：區域 {spawnAreaDefinition.AreaId} " +
            $"已生成 {totalSpawnedCount} 隻魔物，" +
            $"目前登記總數為 {monsterManager.ActiveMonsterCount}。",
            this);
    }
}