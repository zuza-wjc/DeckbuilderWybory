using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Firebase.Extensions;

public class AddCardsPanelInitializerScript : MonoBehaviour
{
    public GameObject prefab; // Twój prefab
    public Transform parent; // Miejsce, gdzie chcesz dodaæ prefab
    public EditListCardsPanel editListCardsPanel; // Skrypt, który ma byæ uruchamiany

    private void Start()
    {
        // Instancja prefaba
        GameObject instance = Instantiate(prefab, parent);

        // Pobierz przycisk z instancji
        Button button = instance.GetComponent<Button>();

        if (button != null && editListCardsPanel != null)
        {
            // Dodaj listener do przycisku
            button.onClick.AddListener(() => editListCardsPanel.OnCardButtonClick(button));
        }
        else
        {
            Debug.LogWarning("Nie uda³o siê przypisaæ funkcji do przycisku!");
        }
    }
}
