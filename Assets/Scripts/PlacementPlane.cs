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
    }
    
    /// <summary>
    /// All the placeables listening to this placement plane.
    /// </summary>
    private IPlacementPlaneListener _placementPlaneListener;
    
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
    
    /// <summary>
    /// Cast a ray from the mouse position to the plane and get the world position.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetMouseWorldPosition()
    {
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
