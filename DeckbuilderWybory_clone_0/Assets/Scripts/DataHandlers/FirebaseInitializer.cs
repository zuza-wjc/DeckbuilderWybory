using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    private static bool firebaseInitialized = false;
    private static DatabaseReference databaseReference;
    private string databaseURL = "https://deckbuilderwybory-default-rtdb.europe-west1.firebasedatabase.app/";

    public static DatabaseReference DatabaseReference => databaseReference;

    void Awake()
    {
        if (!firebaseInitialized)
        {
            InitializeFirebase();
            firebaseInitialized = true;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                databaseReference = FirebaseDatabase.GetInstance(app, databaseURL).RootReference;
                Debug.Log("Firebase is ready to use!");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }
}