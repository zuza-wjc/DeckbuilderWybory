using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public string sceneName; // Nazwa nowej sceny, któr¹ chcesz za³adowaæ

    public void ChangeToScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
