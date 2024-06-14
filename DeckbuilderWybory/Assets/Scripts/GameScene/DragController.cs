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
    
    public GameObject playerListPanel;
    public GameObject mapPanel;

    DatabaseReference dbRef;
    string playerId;
    string lobbyId;
    string cardId;

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

        playerListPanel.SetActive(false);

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck");

        DragController[] controllers = FindObjectsOfType<DragController>();
        if (controllers.Length > 1)
        {
            Destroy(gameObject);
        }

        MapManager mapManager = FindObjectOfType<MapManager>();
        if (mapManager != null)
        {
            mapManager.OnMapManagerActionCompleted += CloseMapPanel;
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
                    // Usuwanie karty po zagraniu i wczytanie tagu z Unity
                    //StartCoroutine(RemoveAfterDelay(_lastDragged.gameObject, 2f));
                    //string cardTag = _lastDragged.gameObject.tag;

                    cardId = _lastDragged.gameObject.tag;
                    dbRef.Child(cardId).Child("played").SetValueAsync(true);

                    if (int.Parse(cardId) == 0)
                    {
                        CardTypeOnMe cardTypeOnMe = FindObjectOfType<CardTypeOnMe>();
                        if (cardTypeOnMe != null)
                        {
                            cardTypeOnMe.OnCardDropped(cardId);
                        }
                        else
                        {
                            Debug.LogError("CardTypeOnMe component not found in the scene!");
                        }
                    }
                    if (int.Parse(cardId) == 1 || int.Parse(cardId) == 3)
                    {
                        playerListPanel.SetActive(true);

                        PlayerListManager playerListManager = FindObjectOfType<PlayerListManager>();
                        if (playerListManager != null)
                        {
                            playerListManager.SetCardIdOnEnemy(cardId);
                        }
                    }
                    if (int.Parse(cardId) == 2)
                    {
                        mapPanel.SetActive(true);

                        MapManager mapManager = FindObjectOfType<MapManager>();
                        if (mapManager != null)
                        {
                            mapManager.FetchDataFromDatabase();
                            mapManager.SetCardIdMap(cardId);
                        }
                    }



                    // Resetowanie pozycji karty po zagraniu 
                    _lastDragged.transform.position = _lastDragged.OriginalPosition;
                }
            }
        }

        UpdateDragStatus(false);
    }


    /*private IEnumerator RemoveAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(obj);
    }*/



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

    void CloseMapPanel()
    {
        mapPanel.SetActive(false);
    }

    void OnDestroy()
    {
        MapManager mapManager = FindObjectOfType<MapManager>();
        if (mapManager != null)
        {
            mapManager.OnMapManagerActionCompleted -= CloseMapPanel;
        }
    }
}
