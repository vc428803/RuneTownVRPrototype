using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMonsterSpawnAreaDefinition",
    menuName = "RuneTown/Monsters/Monster Spawn Area Definition")]
public class MonsterSpawnAreaDefinition : ScriptableObject
{
    [SerializeField]
    private string areaId;

    [SerializeField]
    private float spawnRadius = 8f;

    [SerializeField]
    private List<MonsterSpawnEntry> spawnEntries =
        new List<MonsterSpawnEntry>();

    public string AreaId =>
        areaId;

    public float SpawnRadius =>
        spawnRadius;

    // 修改原因：讓同一個生成區域可以保存多種魔物設定，
    // 避免每種魔物都需要建立一個獨立的 Spawner。
    public IReadOnlyList<MonsterSpawnEntry> SpawnEntries =>
        spawnEntries;
}