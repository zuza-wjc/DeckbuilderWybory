using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateLobby : MonoBehaviour
{
    public void BackToMainMenu2()
    {
        SceneManager.LoadSceneAsync("Main Menu");
    }
}
