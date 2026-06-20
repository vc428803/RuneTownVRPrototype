using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class NPCInteractionHint : MonoBehaviour
{
    [Header("Ray 指到 NPC 時顯示的提示文字")]
    public GameObject talkHintText;

    [Header("按 Trigger / Select 後顯示的對話文字")]
    public GameObject dialogueText;

    [Header("NPC 對話句子")]
    public string[] dialogueLines =
    {
        "Welcome to Rune Town!",
        "Feel free to explore the town."
    };

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private TextMeshPro dialogueTMP;
    private int dialogueIndex = 0;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        if (talkHintText != null)
        {
            talkHintText.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueTMP = dialogueText.GetComponent<TextMeshPro>();
            dialogueText.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        }

        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // 當玩家的游標或控制器「懸停」在物件上方時觸發

        // 條件檢查：
        // 1. 提示文字物件存在 (talkHintText != null)
        // 2. 對話框物件存在 (dialogueText != null)
        // 3. 對話框目前尚未顯示 (dialogueText.activeSelf == false)
        if (talkHintText != null && dialogueText != null && !dialogueText.activeSelf)
        {
            // 顯示提示文字，提醒玩家可以互動
            talkHintText.SetActive(true);
        }
    }


    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (talkHintText != null)
        {
            talkHintText.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.SetActive(false);
        }

        dialogueIndex = 0;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // 當物件被選取時觸發，輸出除錯訊息
        Debug.Log("Talk to Guide");

        // 如果提示文字存在，將其隱藏 (避免對話開始時仍顯示提示)
        if (talkHintText != null)
        {
            talkHintText.SetActive(false);
        }

        // 檢查對話框物件、TMP 元件是否存在，以及對話內容是否有資料
        if (dialogueText == null || dialogueTMP == null || dialogueLines.Length == 0)
        {
            return; // 若有任何缺失，直接結束
        }

        // 顯示對話框物件
        dialogueText.SetActive(true);

        // 將目前索引位置的文字顯示在 TMP 元件上
        dialogueTMP.text = dialogueLines[dialogueIndex];

        // 對話索引往下移動，準備下一句
        dialogueIndex++;

        // 如果索引超過陣列長度，重置回 0（循環播放）
        if (dialogueIndex >= dialogueLines.Length)
        {
            dialogueIndex = 0;
        }
    }

}