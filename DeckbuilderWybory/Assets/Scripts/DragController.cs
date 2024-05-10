using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DragController : MonoBehaviour
{
    public Draggable LastDragged => _lastDragged;
    private bool _isDragActive = false;
    private Vector2 _screenPosition;
    private Vector3 _worldPosition;
    private Draggable _lastDragged;
    public TextMeshProUGUI textToChange;

    private void Awake()
    {
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
                    if (textToChange != null)
                    {
                        if (int.TryParse(textToChange.text, out int currentNumber))
                        {
                            currentNumber += 5;
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
