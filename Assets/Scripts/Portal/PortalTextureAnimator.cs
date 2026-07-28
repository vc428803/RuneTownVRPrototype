using UnityEngine;

public class PortalTextureAnimator : MonoBehaviour
{
    // 修改原因：
    // 依序切換 ring_00～ring_04，
    // 讓靜態地面圖案呈現簡單循環動畫。
    [SerializeField]
    private Texture2D[] frames;

    [SerializeField]
    private float framesPerSecond = 8f;

    private Renderer targetRenderer;
    private Material runtimeMaterial;
    private int currentFrame;
    private float nextFrameTime;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            UnityEngine.Debug.LogError(
                $"[PortalTextureAnimator] {gameObject.name} 找不到 Renderer。"
            );
            enabled = false;
            return;
        }

        // 修改原因：
        // 使用 material 取得此物件專屬的材質實例，
        // 避免切換貼圖時影響其他共用同一材質的 Portal。
        runtimeMaterial = targetRenderer.material;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (Time.time < nextFrameTime)
        {
            return;
        }

        float safeFps = Mathf.Max(1f, framesPerSecond);
        nextFrameTime = Time.time + (1f / safeFps);

        Texture2D frame = frames[currentFrame];

        if (frame != null)
        {
            runtimeMaterial.mainTexture = frame;
        }

        currentFrame = (currentFrame + 1) % frames.Length;
    }
}