using System;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject monsterPrefab;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private Transform activeMonstersParent;

    [SerializeField]
    private MonsterManager monsterManager;

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

        // 修改原因：依照設定數量自動生成多隻魔物，
        // 避免逐隻手動建立與放置。
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomOffset =
                UnityEngine.Random.insideUnitCircle * spawnRadius;

            // 修改原因：以 Spawn Point 為中心，
            // 只在水平 X、Z 範圍內隨機分布，保留地面 Y 高度。
            Vector3 spawnPosition = new Vector3(
                spawnPoint.position.x + randomOffset.x,
                spawnPoint.position.y,
                spawnPoint.position.z + randomOffset.y);

            // 修改原因：將所有執行中的魔物統一放入 ActiveMonsters，
            // 避免大量生成物件散落在 Hierarchy 根目錄。
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
                    $"MonsterSpawner：{spawnedMonster.name} 缺少 MonsterIdentity。",
                    spawnedMonster);

                continue;
            }

            // 修改原因：將管理器交給新生成的魔物保存，
            // 讓魔物被銷毀時可以自動取消註冊。
            monsterIdentity.Initialize(
                monsterManager);

            // 修改原因：魔物生成完成後立即向 MonsterManager 註冊，
            // 讓其他系統可以取得目前場景中的有效魔物資料。
            monsterManager.RegisterMonster(
                monsterIdentity);
        }

        UnityEngine.Debug.Log(
            $"MonsterSpawner：已生成 {spawnCount} 隻魔物，" +
            $"目前登記數量為 {monsterManager.ActiveMonsterCount}。",
            this);
    }
}