using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject monsterPrefab;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private int spawnCount = 10;

    [SerializeField]
    private float spawnRadius = 8f;

    private void Start()
    {
        SpawnMonsters();
    }

    private void SpawnMonsters()
    {
        if (monsterPrefab == null)
        {
            UnityEngine.Debug.LogWarning(
                "MonsterSpawner：尚未指定 Monster Prefab。",
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

        // 修改原因：依照設定數量自動生成多隻魔物，
        // 避免逐隻手動建立與放置。
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle * spawnRadius;

            // 修改原因：以 Spawn Point 為中心，
            // 只在水平 X、Z 範圍內隨機分布，保留地面 Y 高度。
            Vector3 spawnPosition = new Vector3(
                spawnPoint.position.x + randomOffset.x,
                spawnPoint.position.y,
                spawnPoint.position.z + randomOffset.y);

            GameObject spawnedMonster = Instantiate(
                monsterPrefab,
                spawnPosition,
                spawnPoint.rotation);

            spawnedMonster.name =
                $"{monsterPrefab.name}_{i + 1:00}";
        }

        UnityEngine.Debug.Log(
            $"MonsterSpawner：已生成 {spawnCount} 隻魔物。",
            this);
    }
}