using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMonsterSpawnDefinition",
    menuName = "RuneTown/Monsters/Monster Spawn Definition")]
public class MonsterSpawnDefinition : ScriptableObject
{
    [SerializeField]
    private MonsterDefinition monsterDefinition;

    [SerializeField]
    private GameObject monsterPrefab;

    [SerializeField]
    private int spawnCount = 1;

    [SerializeField]
    private float spawnRadius = 5f;

    public MonsterDefinition MonsterDefinition =>
        monsterDefinition;

    public GameObject MonsterPrefab =>
        monsterPrefab;

    public int SpawnCount =>
        spawnCount;

    public float SpawnRadius =>
        spawnRadius;
}