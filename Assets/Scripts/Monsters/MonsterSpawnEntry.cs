using System;
using UnityEngine;

[Serializable]
public class MonsterSpawnEntry
{
    [SerializeField]
    private MonsterDefinition monsterDefinition;

    [SerializeField]
    private GameObject monsterPrefab;

    [SerializeField]
    private int spawnCount = 1;

    // 修改原因：保存單一魔物種類在生成區域內的設定，
    // 讓同一個區域未來可以配置多種不同魔物。
    public MonsterDefinition MonsterDefinition =>
        monsterDefinition;

    public GameObject MonsterPrefab =>
        monsterPrefab;

    public int SpawnCount =>
        spawnCount;
}