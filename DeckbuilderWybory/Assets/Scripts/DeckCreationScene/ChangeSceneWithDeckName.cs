using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Nie zapomnij doda� tego, aby mie� dost�p do Text

public class ChangeSceneWithDeckName : MonoBehaviour
{
    public string sceneName; // Nazwa nowej sceny, kt�r� chcesz za�adowa�

  
    public void ChangeToScene()
    {
        SceneManager.LoadScene(sceneName); // Zmiana sceny
    }
}
