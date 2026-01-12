using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes user input/clicks and places on the plane accordingly.
/// </summary>
public class PlacementPlane : MonoBehaviour
{
    /// <summary>
    /// Interface for listening to placement plane events.
    /// </summary>
    public interface IPlacementPlaneListener
    {
        void MouseDownAtPosition(Vector3 position);
        void MouseUpAtPosition(Vector3 position);
        void MouseDragAtPosition(Vector3 position);
        void MouseOverAtPosition(Vector3 position);
    }
    
    /// <summary>
    /// All the placeables listening to this placement plane.
    /// </summary>
    private IPlacementPlaneListener _placementPlaneListener;
    
    private void Awake()
    {
        // Find the placement plane listener in parent objects
        _placementPlaneListener = GetComponentInParent<IPlacementPlaneListener>();
        if (_placementPlaneListener == null)
        {
            throw new Exception("No placement plane listener found in parent objects");
        }
    }
    
    private void OnMouseDown()
    {
        var placementPosition = GetMouseWorldPosition();
        _placementPlaneListener.MouseDownAtPosition(placementPosition);
    }
    
    private void OnMouseUp()
    {
        var placementPosition = GetMouseWorldPosition();
        _placementPlaneListener.MouseUpAtPosition(placementPosition);
    }

    private void OnMouseDrag()
    {
        var placementPosition = GetMouseWorldPosition();
        _placementPlaneListener.MouseDragAtPosition(placementPosition);
    }

    private void OnMouseOver()
    {
        var placementPosition = GetMouseWorldPosition();
        _placementPlaneListener.MouseOverAtPosition(placementPosition);
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
