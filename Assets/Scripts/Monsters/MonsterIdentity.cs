using UnityEngine;

public class MonsterIdentity : MonoBehaviour
{
    [SerializeField]
    private MonsterDefinition monsterDefinition;

    public string MonsterId =>
        monsterDefinition != null
            ? monsterDefinition.MonsterId
            : string.Empty;

    public string DisplayName =>
        monsterDefinition != null
            ? monsterDefinition.DisplayName
            : string.Empty;

    public MonsterDefinition Definition =>
        monsterDefinition;
}