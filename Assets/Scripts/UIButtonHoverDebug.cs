using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverDebug : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private void Start()
    {
        Debug.Log("UIButtonHoverDebug attached to: " + gameObject.name);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("UI Hover Enter: " + gameObject.name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("UI Hover Exit: " + gameObject.name);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("UI Click: " + gameObject.name);
    }
}