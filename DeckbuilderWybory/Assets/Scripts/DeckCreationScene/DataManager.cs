using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public string deckName;  // Make sure this is initialized

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

       
    }
}
