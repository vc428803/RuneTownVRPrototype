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
        if (talkHintText != null)
        {
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
        Debug.Log("Talk to Guide");

        if (dialogueText == null || dialogueTMP == null || dialogueLines.Length == 0)
        {
            return;
        }

        dialogueText.SetActive(true);
        dialogueTMP.text = dialogueLines[dialogueIndex];

        dialogueIndex++;

        if (dialogueIndex >= dialogueLines.Length)
        {
            dialogueIndex = 0;
        }
    }
}