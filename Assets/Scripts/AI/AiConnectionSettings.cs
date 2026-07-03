using UnityEngine;

[CreateAssetMenu(
    fileName = "AiConnectionSettings",
    menuName = "Rune Town/AI Connection Settings"
)]
public class AiConnectionSettings : ScriptableObject
{
    [Header("AI Provider Mode")]
    public AiProviderMode providerMode = AiProviderMode.LocalEditor;

    [Header("Unity Editor / PC Local API")]
    public string localEditorBaseUrl = "http://127.0.0.1:5062";

    [Header("Quest / LAN PC API")]
    public string lanServerBaseUrl = "http://192.168.1.50:5062";

    [Header("Cloud API")]
    public string cloudBaseUrl = "https://your-cloud-api.com";

    [Header("Use Mock Reply")]
    public bool useMockWhenUnavailable = true;

    public string GetChatUrl()
    {
        string baseUrl;

        switch (providerMode)
        {
            case AiProviderMode.LocalEditor:
                baseUrl = localEditorBaseUrl;
                break;

            case AiProviderMode.LanServer:
                baseUrl = lanServerBaseUrl;
                break;

            case AiProviderMode.Cloud:
                baseUrl = cloudBaseUrl;
                break;

            case AiProviderMode.Mock:
                return "mock://chat";

            default:
                baseUrl = localEditorBaseUrl;
                break;
        }

        return baseUrl.TrimEnd('/') + "/api/chat";
    }
}