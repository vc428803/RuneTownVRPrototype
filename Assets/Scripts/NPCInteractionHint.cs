using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class NPCInteractionHint : MonoBehaviour
{
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
            AskLLM("Where is the fountain?");
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            AskLLM("Where is the market?");
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            AskLLM("Where is the blacksmith?");
        }
    }

    private void AskLLM(string question)
    {
        if (llmClient == null)
        {
            Debug.LogError("LLMClient 尚未指定，請把 LLM_Manager 拖到 Inspector 的 llmClient 欄位。");
            return;
        }

        Debug.Log("Sending question to LLM: " + question);

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
            "guide_liora",
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
            },
            error =>
            {
                Debug.LogError(error);
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

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("Open question menu");

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
            questionMenuText.SetActive(true);
        }

        questionMenuOpen = true;
    }
}