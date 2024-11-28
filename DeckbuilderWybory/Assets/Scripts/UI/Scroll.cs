using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  // Dodajemy przestrzeñ nazw, aby obs³ugiwaæ drag-and-drop

public class ScrollPositionSaver : MonoBehaviour, IEndDragHandler
{
    public ScrollRect scrollRect;

    // Funkcja wywo³ywana po zakoñczeniu przeci¹gania
    public void OnEndDrag(PointerEventData eventData)
    {
        // Zapisz pozycjê scrolla do PlayerPrefs
        PlayerPrefs.SetFloat("horizontalValue", scrollRect.horizontalNormalizedPosition);
        PlayerPrefs.Save();  // Natychmiastowy zapis w PlayerPrefs
    }

    void Start()
    {
        // Odczytujemy zapisan¹ wartoœæ pozycji scrolla przy starcie
        if (PlayerPrefs.HasKey("horizontalValue"))
        {
            float horizontalPos = PlayerPrefs.GetFloat("horizontalValue");  // Odczytaj zapisany stan
            scrollRect.horizontalNormalizedPosition = horizontalPos;  // Przywracamy pozycjê scrolla
        }
        else
        {
            // Jeœli nie ma zapisanego stanu, ustawiæ pozycjê na pocz¹tku
            scrollRect.horizontalNormalizedPosition = 0;
        }
    }
}
