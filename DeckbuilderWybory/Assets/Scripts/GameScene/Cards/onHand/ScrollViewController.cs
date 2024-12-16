using UnityEngine;
using UnityEngine.UI;

public class ScrollViewController : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollStep = 1f;

    public void ScrollLeft()
    {
        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - scrollStep);
    }

    public void ScrollRight()
    {
        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + scrollStep);
    }
}
