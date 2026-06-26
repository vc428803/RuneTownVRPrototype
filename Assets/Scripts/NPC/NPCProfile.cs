using UnityEngine;

//這個檔案的用途是讓你可以在 Unity 裡建立：

//GuideLiora.asset
//KafraTown.asset
//PotionMerchant.asset

[CreateAssetMenu(
    fileName = "NewNPCProfile",
    menuName = "Rune Town/NPC Profile"
)]
public class NPCProfile : ScriptableObject
{
    [Header("NPC ID，給後端或遊戲系統識別用")]
    public string npcId;

    [Header("NPC 顯示名稱")]
    public string displayName;

    [Header("第一次互動時顯示的招呼語")]
    [TextArea(2, 4)]
    public string greetingText;

    [Header("NPC 可選互動選項")]
    public NPCOption[] options;
}