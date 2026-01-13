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
        internal void DraggableMouseDownAtPosition(Draggable draggable, Vector3 position);
        internal void DraggableMouseUpAtPosition(Draggable draggable, Vector3 position);
        internal void DraggableMouseDragAtPosition(Draggable draggable, Vector3 position);
    }
    
    /// <summary>
    /// used to enable/disable dragging functionality
    /// </summary>
    [SerializeField] bool draggableEnabled = true;
    
    private IListener _listener;
    private Collider _collider;

    private void Awake()
    {
        // Find the listener in parent objects
        _collider = GetComponent<Collider>();
        if (_collider == null)
            throw new MissingComponentException("No collider found");
        
        _listener = GetComponentInParent<IListener>();
        if (_listener == null)
            throw new MissingComponentException("No listener found in parent objects");
        
    }
    
    private void OnMouseDown()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseDownAtPosition(this, placementPosition);
        MoveWithMouse();
    }
    
    private void OnMouseUp()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseUpAtPosition(this, placementPosition);
    }

    private void OnMouseDrag()
    {
        var placementPosition = GetMouseWorldPosition();
        _listener.DraggableMouseDragAtPosition(this, placementPosition);
        MoveWithMouse();
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
    
    public void SetDraggableEnabled(bool enabled)
    {
        draggableEnabled = enabled;
        _collider.enabled = enabled;
    }
}
