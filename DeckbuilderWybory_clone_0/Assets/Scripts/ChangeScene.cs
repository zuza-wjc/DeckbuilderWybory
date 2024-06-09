using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public string sceneName; // Nazwa nowej sceny, kt�r� chcesz za�adowa�

    public void ChangeToScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
