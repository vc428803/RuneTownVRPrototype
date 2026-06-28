using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class NPCInteractionHint : MonoBehaviour
{
    [Header("VR 選項按鈕")]
    [SerializeField] private Button[] optionButtons;

    [Header("NPC 資料設定")]
    [SerializeField] private NPCProfile npcProfile;

    [Header("VR 選項選單 Canvas")]
    [SerializeField] private GameObject optionMenuCanvas;

    [Header("Ray 指到 NPC 時顯示的提示文字")]
    public GameObject talkHintText;

    [Header("按 Trigger / Select 後顯示的對話文字")]
    public GameObject dialogueText;

    [Header("問題選單文字")]
    public GameObject questionMenuText;

    [Header("NPC 對話句子")]
    public string[] dialogueLines =
    {
        "Welcome to Rune Town!",
        "Feel free to explore the town.",
        "Can you visit the fountain??"
    };

    [SerializeField] private LLMClient llmClient;
    private bool isWaitingForLLMReply = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private TextMeshPro dialogueTMP;
    private int dialogueIndex = 0;
    private bool questionMenuOpen = false;

    private void Awake()
    {
        if (questionMenuText != null)
        {
            questionMenuText.SetActive(false);
        }

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
        SetupOptionButtons();
    }
    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!questionMenuOpen)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            SelectOption(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            SelectOption(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            SelectOption(2);
        }
    }

    public void SelectOption(int optionIndex)
    {
        Debug.Log("Button clicked. optionIndex = " + optionIndex);

        if (npcProfile == null)
        {
            Debug.LogError("NPCProfile 尚未指定，請把 NPCProfile asset 拖到 Inspector。");
            return;
        }

        if (npcProfile.options == null || npcProfile.options.Length == 0)
        {
            Debug.LogError("NPCProfile 沒有設定任何 options。");
            return;
        }

        if (optionIndex < 0 || optionIndex >= npcProfile.options.Length)
        {
            Debug.LogError("Invalid option index: " + optionIndex);
            return;
        }

        NPCOption selectedOption = npcProfile.options[optionIndex];

        Debug.Log("Selected option: " + selectedOption.displayText);

        HandleOption(selectedOption);
    }

    private void AskLLM(string question)
    {
        if (isWaitingForLLMReply)
        {
            Debug.Log("Already waiting for LLM reply. Please wait.");
            return;
        }

        isWaitingForLLMReply = true;
        Debug.Log("isWaitingForLLMReply set to TRUE");

        if (llmClient == null)
        {
            Debug.LogError("LLMClient 尚未指定，請把 LLM_Manager 拖到 Inspector 的 llmClient 欄位。");
            isWaitingForLLMReply = false;
            return;
        }

        Debug.Log("Sending question to LLM: " + question);

        if (npcProfile == null)
        {
            Debug.LogError("NPCProfile 尚未指定，請把 GuideLiora.asset 拖到 Inspector 的 npcProfile 欄位。");
            isWaitingForLLMReply = false;
            return;
        }

        string currentNpcId = npcProfile.npcId;

        Debug.Log("Sending question to LLM. npcId = " + currentNpcId + ", question = " + question);

        // 這樣玩家按 1 / 2 / 3 之後，問題選單會自動關閉。
        if (questionMenuText != null)
        {
            questionMenuText.SetActive(false);
        }

        if (optionMenuCanvas != null)
        {
            optionMenuCanvas.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.SetActive(true);

            TMP_Text textComponent = dialogueText.GetComponent<TMP_Text>();

            if (textComponent != null)
            {
                textComponent.text = "Thinking...";
            }
        }



        StartCoroutine(llmClient.SendChat(
            currentNpcId,
            question,
            reply =>
            {
                Debug.Log("LLM Reply: " + reply);

                if (dialogueText != null)
                {
                    dialogueText.SetActive(true);

                    TMP_Text textComponent = dialogueText.GetComponent<TMP_Text>();

                    if (textComponent != null)
                    {
                        textComponent.text = reply;
                    }
                    else
                    {
                        Debug.LogError("dialogueText 物件上找不到 TMP_Text 元件。請確認你拖的是 Dialogue_Text。");
                    }
                }

                isWaitingForLLMReply = false;
                Debug.Log("isWaitingForLLMReply set to FALSE after success");
            },
            error =>
            {
                Debug.LogError(error);

                if (dialogueText != null)
                {
                    dialogueText.SetActive(true);

                    TMP_Text textComponent = dialogueText.GetComponent<TMP_Text>();

                    if (textComponent != null)
                    {
                        textComponent.text = "Sorry, I cannot answer right now.";
                    }
                }

                isWaitingForLLMReply = false;
                Debug.Log("isWaitingForLLMReply set to FALSE after error");
            }
        ));


    }

    private void SelectQuestion(string question)
    {
        Debug.Log("Selected question: " + question);

        if (questionMenuText != null)
        {
            questionMenuText.SetActive(false);
        }

        if (dialogueText != null && dialogueTMP != null)
        {
            dialogueText.SetActive(true);
            dialogueTMP.text = "You asked: " + question;
        }

        questionMenuOpen = false;
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

    //NPC 正在等 LLM 回答時
    //→ 玩家再按 G / Select
    //→ 不會重新打開 Question_Menu_Text
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (isWaitingForLLMReply)
        {
            Debug.Log("NPC is thinking. Please wait.");
            return;
        }

        Debug.Log("Open VR option menu");

        if (talkHintText != null)
        {
            talkHintText.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.SetActive(false);
        }

        if (questionMenuText != null)
        {
            questionMenuText.SetActive(false);
        }

        if (optionMenuCanvas != null)
        {
            optionMenuCanvas.SetActive(true);
        }

        questionMenuOpen = true;
    }
    private void CloseDialogue()
    {
        if (questionMenuText != null)
        {
            questionMenuText.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.SetActive(false);
        }

        if (talkHintText != null)
        {
            talkHintText.SetActive(false);
        }

        questionMenuOpen = false;

        Debug.Log("Dialogue closed.");
    }

    private void HandleOption(NPCOption option)
    {
        switch (option.optionType)
        {
            case NPCOptionType.AskLLM:
                AskLLM(option.playerMessage);
                break;

            case NPCOptionType.CloseDialogue:
                CloseDialogue();
                break;

            case NPCOptionType.Teleport:
                Debug.Log("Teleport action not implemented yet. TargetId: " + option.targetId);
                break;

            case NPCOptionType.OpenShop:
                Debug.Log("OpenShop action not implemented yet. TargetId: " + option.targetId);
                break;

            case NPCOptionType.OpenStorage:
                Debug.Log("OpenStorage action not implemented yet.");
                break;

            case NPCOptionType.StartQuest:
                Debug.Log("StartQuest action not implemented yet. QuestId: " + option.targetId);
                break;

            case NPCOptionType.CompleteQuest:
                Debug.Log("CompleteQuest action not implemented yet. QuestId: " + option.targetId);
                break;

            case NPCOptionType.SavePoint:
                Debug.Log("SavePoint action not implemented yet.");
                break;

            case NPCOptionType.Dialogue:
                Debug.Log("Dialogue action not implemented yet.");
                break;

            default:
                Debug.LogWarning("Unhandled NPC option type: " + option.optionType);
                break;
        }
    }

    private void UpdateOptionMenuText()
    {
        if (questionMenuText == null)
        {
            Debug.LogError("questionMenuText 尚未指定。");
            return;
        }

        TMP_Text menuTMP = questionMenuText.GetComponent<TMP_Text>();

        if (menuTMP == null)
        {
            Debug.LogError("questionMenuText 物件上找不到 TMP_Text 元件。");
            return;
        }

        if (npcProfile == null || npcProfile.options == null || npcProfile.options.Length == 0)
        {
            menuTMP.text = "No options available.";
            return;
        }

        string menuText = "";

        for (int i = 0; i < npcProfile.options.Length; i++)
        {
            menuText += $"{i + 1}. {npcProfile.options[i].displayText}\n";
        }

        menuTMP.text = menuText;
    }

    private void SetupOptionButtons()
    {
        if (npcProfile == null)
        {
            Debug.LogError("NPCProfile 尚未指定，無法設定選項按鈕。");
            return;
        }

        if (optionButtons == null || optionButtons.Length == 0)
        {
            Debug.LogError("optionButtons 尚未指定，請把 Option_Button_1/2/3 拖到 Inspector。");
            return;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            Button button = optionButtons[i];

            if (button == null)
            {
                Debug.LogWarning("optionButtons[" + i + "] 是空的。");
                continue;
            }

            bool hasOption = npcProfile.options != null && i < npcProfile.options.Length;

            button.gameObject.SetActive(hasOption);

            if (!hasOption)
            {
                continue;
            }

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = npcProfile.options[i].displayText;
            }
            else
            {
                Debug.LogWarning(button.gameObject.name + " 找不到 TMP_Text 子物件。");
            }

            int optionIndex = i;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                SelectOption(optionIndex);
            });
        }
    }
}