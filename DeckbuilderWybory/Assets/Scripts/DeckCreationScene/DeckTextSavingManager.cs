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

public class DeckTextSavingManager : MonoBehaviour
{
    private Text Deck1Label;

    void Start()
    {
        // Automatyczne znajdowanie obiektu typu Text w scenie
        Deck1Label = FindObjectOfType<Text>();

        if (Deck1Label == null)
        {
            Debug.LogWarning("Nie znaleziono obiektu Text w scenie!");
        }
    }

    public void SaveDeckName()
    {
        if (Deck1Label != null)
        {
            // Pobierz tekst z obiektu Deck1Label i zapisz go w DataManager
            DataManager.Instance.deckName = Deck1Label.text;
            Debug.Log("Zapisano deckName: " + DataManager.Instance.deckName);
            SceneManager.LoadScene("Deck Creation"); // Zmiana sceny
        }
        else
        {
            Debug.LogWarning("Deck1Label nie jest przypisany lub nie istnieje!");
        }
    }
}
