using System.Collections.Generic;
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

    /// <summary>
    /// the piece that follows the cursor around waiting to be placed
    /// </summary>
    private PlaceablePiece prePlacementPiece;
    
    /// <summary>
    /// The list of pieces that will be placed when the mouse is released.
    /// </summary>
    private List<PlaceablePiece> piecesToPlace = new List<PlaceablePiece>();

    private void Awake()
    {
        SetDefaultSelectedPlaceable();
    }

    private void Start()
    {
        var primaryPrefab = _currentPlaceable.primaryPrefab;
        prePlacementPiece = Instantiate(primaryPrefab, _level.transform);
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
        // if we're not already placing a placeable then we take the current pre-placement piece and put it in the level at this spot and dont have 
        // it follow anymore, it's still in preview mode though, 
        // after we release the mouse then we lock it into where it is
        
        if (!isCurrentlyPlacingObject)
        {
            isCurrentlyPlacingObject = true;
            
            //place the pre-placement piece at the current position
            prePlacementPiece.transform.position = position;
            
            //get the y offset so that the piece sits on the plane
            var yOffset = prePlacementPiece.bottomYOffset;
            
            //move the bottom of the piece sit on the plane
            prePlacementPiece.transform.position = new Vector3(
                position.x, 
                position.y + yOffset, 
                position.z);
            
            Debug.Log("Placed piece at: " + position);
        }
    }

    public void MouseUpAtPosition(Vector3 position)
    {
        if (isCurrentlyPlacingObject)
        {
            isCurrentlyPlacingObject = false;
            
            //loop through all pieces to place and set them as placed
            foreach (var piece in piecesToPlace)
            {
                piece.SetPieceToPlaced();
            }
        }
    }
    
    public void MouseDragAtPosition(Vector3 position)
    {
        
    }
    
    public void MouseOverAtPosition(Vector3 position)
    {
        if (isCurrentlyPlacingObject)
            return;
        
        prePlacementPiece.transform.position = position;
        
        //get the y offset so that the piece sits on the plane
        var yOffset = prePlacementPiece.bottomYOffset;
        
        //move the bottom of the piece sit on the plane
        prePlacementPiece.transform.position = new Vector3(
            position.x, 
            position.y + yOffset, 
            position.z);
        
        Debug.Log("Placed pre-placement piece at: " + position);
    }
}
