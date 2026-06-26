using System;
using UnityEngine;

//例如：

//displayText = Where is the fountain?
//optionType = AskLLM
//playerMessage = Where is the fountain?
//targetId = 空

//或未來：

//displayText = Teleport to North Field
//optionType = Teleport
//playerMessage = 空
//targetId = north_field

[Serializable]
public class NPCOption
{
    [Header("UI 顯示文字")]
    public string displayText;

    [Header("選項類型")]
    public NPCOptionType optionType;

    [Header("給 LLM 的玩家訊息，AskLLM 時使用")]
    [TextArea(2, 4)]
    public string playerMessage;

    [Header("目標 ID，例如地圖、商店、任務 ID")]
    public string targetId;
}