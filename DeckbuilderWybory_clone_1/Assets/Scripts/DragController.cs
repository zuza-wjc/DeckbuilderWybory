using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Firebase;
using Firebase.Database;

public class DragController : MonoBehaviour
{
    public Draggable LastDragged => _lastDragged;
    private bool _isDragActive = false;
    private Vector2 _screenPosition;
    private Vector3 _worldPosition;
    private Draggable _lastDragged;
    public TextMeshProUGUI textToChange;

    DatabaseReference dbRef;
    string playerId;
    string lobbyId;
    int money;

    private void Awake()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeœli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        lobbyId = PlayerPrefs.GetString("LobbyId");
        playerId = PlayerPrefs.GetString("PlayerId");
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");

        DragController[] controllers = FindObjectsOfType<DragController>();
        if (controllers.Length > 1)
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        if (_isDragActive)
        {
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                Drop();
                return;
            }
            
        }

        if (Input.touchCount > 0)
        {
            _screenPosition = Input.GetTouch(0).position;
        }
        else
        {
            return;
        }

        _worldPosition = Camera.main.ScreenToWorldPoint(_screenPosition);

        if (_isDragActive)
        {
            Drag();
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(_worldPosition, Vector2.zero);
            if (hit.collider != null)
            {
                Draggable draggable = hit.transform.gameObject.GetComponent<Draggable>();
                if (draggable != null)
                {
                    _lastDragged = draggable;
                    InitDrag();
                }
            }
        }
    }

    void InitDrag()
    {
        _lastDragged.LastPosition = _lastDragged.transform.position;
        UpdateDragStatus(true);
        dbRef.Child(playerId).Child("stats").Child("money").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("Error getting data from Firebase: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                money = int.Parse(snapshot.Value.ToString());
            }
        });
    }

    void Drag()
    {
        _lastDragged.transform.position = (Vector2)_worldPosition;
    }

    void Drop()
    {
        if (_lastDragged != null)
        {
            if (_lastDragged.IsDragging)
            {
                if (!IsInDropArea(_lastDragged.transform.position))
                {
                    _lastDragged.transform.position = _lastDragged.OriginalPosition;
                }
                else
                {
                    StartCoroutine(RemoveAfterDelay(_lastDragged.gameObject, 2f));

                    string cardTag = _lastDragged.gameObject.tag;

                    if (textToChange != null)
                    {
                        if (int.TryParse(textToChange.text, out int currentNumber))
                        {
                            if (cardTag == "PlusFive")
                            {
                                money += 500;
                                dbRef.Child(playerId).Child("stats").Child("money").SetValueAsync(money);
                                currentNumber = 0;
                            }
                            else if (cardTag == "MinusThree")
                            {
                                currentNumber -= 3;
                            }
                            textToChange.text = currentNumber.ToString();
                        }
                    }
                }
            }
        }

        UpdateDragStatus(false);
    }


    private IEnumerator RemoveAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(obj);
    }



    bool IsInDropArea(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(position);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("DropArea"))
            {
                return true;
            }
        }
        return false;
    }

    void UpdateDragStatus(bool isDragging)
    {
        _isDragActive = _lastDragged.IsDragging = isDragging;
        _lastDragged.gameObject.layer = isDragging ? Layer.Dragging : Layer.Default;
    }
}
