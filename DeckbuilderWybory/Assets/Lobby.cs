using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Lobby : MonoBehaviour
{
    public void BackFromLobby()
    {
        SceneManager.LoadSceneAsync("Join Lobby");
    }
    public void ToLoadingScreen()
    {
        SceneManager.LoadSceneAsync("Loading Screen");
    }
}
