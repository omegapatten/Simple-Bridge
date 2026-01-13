using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Draggable : MonoBehaviour
{
    /// <summary>
    /// Interface for listening to placement plane events.
    /// </summary>
    internal interface IListener
    {
        internal void DraggableMouseDownAtPosition(Vector3 position);
        internal void DraggableMouseUpAtPosition(Vector3 position);
        internal void DraggableMouseDragAtPosition(Vector3 position);
        internal void DraggableMouseOverAtPosition(Vector3 position);
    }
    
    [SerializeField] bool enabled = true;
    
    private IListener _listener;

    private void Awake()
    {
        // Find the listener in parent objects
        _listener = GetComponentInParent<IListener>();
        if (_listener == null)
        {
            throw new Exception("No listener found in parent objects");
        }
    }
    
    private void OnMouseDown()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseDownAtPosition(placementPosition);
        MoveWithMouse();
    }
    
    private void OnMouseUp()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseUpAtPosition(placementPosition);
    }

    private void OnMouseDrag()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseDragAtPosition(placementPosition);
        MoveWithMouse();
    }

    private void OnMouseOver()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseOverAtPosition(placementPosition);
    }
    
    //method that moves the object with the moouse but keeps it on the y
    private void MoveWithMouse()
    {
        Vector3 mousePosition = GetMouseWorldPosition();
        transform.position = new Vector3(mousePosition.x, transform.position.y, mousePosition.z);
    }
    
    /// <summary>
    /// Cast a ray from the mouse position to the plane and get the world position.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null)
            throw new Exception("No Camera found");
        
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        
        Debug.LogWarning("Mouse point not found");
        return Vector3.zero;
    }
}
