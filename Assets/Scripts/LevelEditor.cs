using UnityEngine;

/// <summary>
/// Holds references to componeents needed to edit a level and the level itself
/// May allow exporting of levels eventually too
/// </summary>
public class LevelEditor : MonoBehaviour, PlacementPlane.IPlacementPlaneListener
{
    /// <summary>
    /// The level object used to hold the placed objects 
    /// </summary>
    [SerializeField] private Level _level;
    [SerializeField] private PlacementPlane _placementPlane;

    /// <summary>
    /// The collection of palceables that can be used in the level editor
    /// </summary>
    [SerializeField] private SO_EditorPlaceables _placeables;
    
    /// <summary>
    /// The selected placeable to be placed in the level
    /// </summary>
    private SO_Placeable _currentPlaceable;
    
    /// <summary>
    /// Tracks if the user is currently placing a placeable.
    /// </summary>
    private bool isCurrentlyPlacingObject = false;

    private void Awake()
    {
        SetDefaultSelectedPlaceable();
    }

    private void SetDefaultSelectedPlaceable()
    {
        if (_placeables._placeables == null)
        {
            throw new System.Exception("No placeables assigned to level editor");
        }
        
        // Select the first placeable as the default
        _currentPlaceable = System.Linq.Enumerable.First(_placeables._placeables);
    }

    public void MouseDownAtPosition(Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    public void MouseUpAtPosition(Vector3 position)
    {
        throw new System.NotImplementedException();
    }
    
    public void MouseDragAtPosition(Vector3 position)
    {
        throw new System.NotImplementedException();
    }
    
    public void MouseOverAtPosition(Vector3 position)
    {
        if (!isCurrentlyPlacingObject)
            return;
        
        //TODO: show the preview of the currently selected placeable at the position
        throw new System.NotImplementedException();
    }
}
