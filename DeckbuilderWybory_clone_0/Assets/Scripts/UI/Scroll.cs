using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  // Dodajemy przestrze� nazw, aby obs�ugiwa� drag-and-drop

public class ScrollPositionSaver : MonoBehaviour, IEndDragHandler
{
    public ScrollRect scrollRect;

    // Funkcja wywo�ywana po zako�czeniu przeci�gania
    public void OnEndDrag(PointerEventData eventData)
    {
        // Zapisz pozycj� scrolla do PlayerPrefs
        PlayerPrefs.SetFloat("horizontalValue", scrollRect.horizontalNormalizedPosition);
        PlayerPrefs.Save();  // Natychmiastowy zapis w PlayerPrefs
    }

    void Start()
    {
        // Odczytujemy zapisan� warto�� pozycji scrolla przy starcie
        if (PlayerPrefs.HasKey("horizontalValue"))
        {
            float horizontalPos = PlayerPrefs.GetFloat("horizontalValue");  // Odczytaj zapisany stan
            scrollRect.horizontalNormalizedPosition = horizontalPos;  // Przywracamy pozycj� scrolla
        }
        else
        {
            // Je�li nie ma zapisanego stanu, ustawi� pozycj� na pocz�tku
            scrollRect.horizontalNormalizedPosition = 0;
        }
    }
}
