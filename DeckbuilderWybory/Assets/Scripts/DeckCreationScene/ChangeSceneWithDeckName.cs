using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Nie zapomnij dodaæ tego, aby mieæ dostêp do Text

public class ChangeSceneWithDeckName : MonoBehaviour
{
    public string sceneName; // Nazwa nowej sceny, któr¹ chcesz za³adowaæ

  
    public void ChangeToScene()
    {
        SceneManager.LoadScene(sceneName); // Zmiana sceny
    }
}
