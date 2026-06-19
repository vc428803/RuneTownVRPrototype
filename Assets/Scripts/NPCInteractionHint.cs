using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NPCInteractionHint : MonoBehaviour
{
    [Header("要顯示 / 隱藏的提示文字")]
    public GameObject talkHintText;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (talkHintText != null)
        {
            talkHintText.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            interactable = GetComponent<XRBaseInteractable>();
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
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("Talk to Guide");
    }
}