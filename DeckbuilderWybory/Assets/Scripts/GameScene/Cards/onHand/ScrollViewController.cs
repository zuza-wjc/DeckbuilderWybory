using UnityEngine;
using UnityEngine.UI;

public class ScrollViewController : MonoBehaviour
{
    public ScrollRect scrollRect;
    public int visibleItems = 4;
    private float scrollStep;
    private int currentIndex = 0;
    private int totalItems;   

    void Start()
    {
        UpdateElementCount();
    }

    public void UpdateElementCount()
    {
        totalItems = scrollRect.content.childCount;

        scrollStep = totalItems > visibleItems ? 1f / (totalItems - visibleItems) : 0;

        if (currentIndex > totalItems - visibleItems)
        {
            currentIndex = Mathf.Max(0, totalItems - visibleItems);
            UpdateScrollPosition();
        }
    }

    public void ScrollLeft()
    {
        if (totalItems <= visibleItems) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateScrollPosition();
        }
    }

    public void ScrollRight()
    {
        if (totalItems <= visibleItems) return; 

        if (currentIndex < totalItems - visibleItems)
        {
            currentIndex++;
            UpdateScrollPosition();
        }
    }

    private void UpdateScrollPosition()
    {
        float targetPosition = scrollStep * currentIndex;

        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(targetPosition);
    }
}
