using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
    }

    public void JoinLobby()
    {
        SceneManager.LoadSceneAsync("Join Lobby");
    }

    public void CreateLobby()
    {
        SceneManager.LoadSceneAsync("Create Lobby");
    }
}
