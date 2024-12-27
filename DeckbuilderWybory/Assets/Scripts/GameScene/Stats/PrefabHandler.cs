using UnityEngine;
using UnityEngine.EventSystems;

public class PrefabHandler : MonoBehaviour, IPointerClickHandler
{
    public string displayText;

    public void OnPointerClick(PointerEventData eventData)
    {
        TooltipManager.Instance.ShowTooltip(displayText);
    }

}
