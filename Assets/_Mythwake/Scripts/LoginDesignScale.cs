using UnityEngine;

[ExecuteAlways]
public sealed class LoginDesignScale : MonoBehaviour
{
    private void OnEnable() { Resize(); }
    private void LateUpdate() { Resize(); }
    private void OnRectTransformDimensionsChange() { Resize(); }
    private void Resize()
    {
        if (transform.parent is RectTransform parent)
            transform.localScale = Vector3.one * Mathf.Max(.001f, Mathf.Min(parent.rect.width / 1080f, parent.rect.height / 1920f));
    }
}
