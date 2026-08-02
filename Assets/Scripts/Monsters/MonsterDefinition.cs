using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMonsterDefinition",
    menuName = "RuneTown/Monsters/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
    [SerializeField]
    private string monsterId;

    [SerializeField]
    private string displayName;

    public string MonsterId => monsterId;
    public string DisplayName => displayName;
}