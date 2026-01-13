using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds references to components needed to edit a level and the level itself
/// May allow exporting of levels eventually too
/// </summary>
public class LevelEditor : MonoBehaviour, PlacementPlane.IPlacementPlaneListener, Draggable.IListener
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
    /// While true, we are in a "dragging a line" placement session.
    /// </summary>
    private bool isDraggingPieces = false;
    private PlaceablePiece cachedAnchorPiece;
    private PlaceablePiece cachedMobilePiece;
    /// <summary>
    /// When true, prevents new bridges 
    /// </summary>
    private bool initialPiecesPlaced = false;

    /// <summary>
    /// The list of pieces that will be placed when the mouse is released.
    /// </summary>
    private readonly List<PlaceablePiece> piecesToPlace = new List<PlaceablePiece>();

    /// <summary>
    /// the piece that follows the cursor around waiting to be placed
    /// </summary>
    private PlaceablePiece prePlacementPiece;
    /// <summary>
    /// The "end piece" that follows the cursor around during drag placement
    /// </summary>
    private PlaceablePiece secondaryPreviewPiece;
    /// <summary>
    /// The full size middle pieces of the placeable being previewed during drag placement
    /// </summary>
    private readonly List<PlaceablePiece> tertiaryPreviewPieces = new List<PlaceablePiece>();
    /// <summary>
    /// The pieces that fill in when the middle pieces don't fit exactly during drag placement
    /// </summary>
    private readonly List<PlaceablePiece> fillerPreviewPieces = new List<PlaceablePiece>();

    /// <summary>
    /// minimum distance the mouse must move to register as a drag
    /// </summary>
    private const float MinDragDistance = 0.01f;
    
    /// <summary>
    /// some hardcoded rotations to make the pieces face the right way.
    /// TODO: put these in the SO_Placeable instead so that different placeables can have different offsets. Out of scope for this prototype.
    /// </summary>
    private float _lookRotationYawOffsetDegrees = -90f;

    private void Awake()
    {
        SetDefaultSelectedPlaceable();
        CreateNewPrePlacementPiece();
    }

    /// <summary>
    /// Creates the initial prePlacementPiece based on the current selected placeable.
    /// For this prototype we will only have one but could add more later pretty easily
    /// </summary>
    private void CreateNewPrePlacementPiece()
    {
        if (_currentPlaceable == null || _currentPlaceable.primaryPrefab == null)
        {
            Debug.LogError("LevelEditor: No primary prefab available to create a prePlacementPiece.");
            return;
        }

        prePlacementPiece = Instantiate(_currentPlaceable.primaryPrefab, _level.transform);
    }

    /// <summary>
    /// Sets the default selected placeable to the first in the list
    /// </summary>
    /// <exception cref="Exception"></exception>
    private void SetDefaultSelectedPlaceable()
    {
        if (_placeables._placeables == null)
        {
            throw new System.Exception("No placeables assigned to level editor");
        }

        // Select the first placeable as the default
        _currentPlaceable = System.Linq.Enumerable.First(_placeables._placeables);
    }
    
    /// <summary>
    /// Updates the drag preview pieces based on the current position of the mouse/mobile piece
    /// </summary>
    /// <param name="mousePosition"> the position of the mouse/piece that is being moved</param>
    /// <param name="anchorPiece">the anchor point of the bride. origin or just the other end not moving</param>
    /// <param name="mobilePiece">the piece that is moving</param>
    private void UpdateDragPreview(Vector3 mousePosition, PlaceablePiece anchorPiece, PlaceablePiece mobilePiece)
    {
        if (prePlacementPiece == null)
        {
            return;
        }
        
        //get direction from primary piece to mouse
        var directionFromOrigin = mousePosition - anchorPiece.transform.position;
        directionFromOrigin.y = 0f;

        var dist = directionFromOrigin.magnitude;
        if (dist < MinDragDistance)
        {
            // Too close to form a line. Just show the primary piece at the anchor.
            //TODO: could do something better hear because right now it looks bad with them stacked
            // maybe some UI treatment showing an "outward arrow" to tell them to drag away? 
            mobilePiece.transform.position = anchorPiece.transform.position;
            
            ResizePieceList(tertiaryPreviewPieces, 0, _currentPlaceable.tertiaryPrefab);
            ResizePieceList(fillerPreviewPieces, 0, _currentPlaceable.fillerPrefab);
            return;
        }

        var forward = directionFromOrigin.normalized;

        // Apply a yaw offset so model forward matches our line direction.
        var rotForward = RotationLookingAlong(forward);
        var rotBack = RotationLookingAlong(-forward);

        // Anchor piece towards the mobile piece
        anchorPiece.transform.rotation = rotForward * Quaternion.Euler(0f, anchorPiece.yawRotationOffset, 0f);

        // Place mobile piece at the current mouse and point it back.
        mobilePiece.transform.position = ApplyBottomYOffset(mobilePiece, mousePosition);
        mobilePiece.transform.rotation = rotBack * Quaternion.Euler(0f, mobilePiece.yawRotationOffset, 0f);
        
        // Fill between with tertiary and filler pieces.
        UpdateMiddlePieces(anchorPiece.transform.position, mousePosition, forward, rotBack);
    }

    private Quaternion RotationLookingAlong(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return Quaternion.identity;

        // LookRotation assumes the model is authored facing +Z.
        // Many bridge pieces are authored facing +X, so we offset around Y.
        return Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, _lookRotationYawOffsetDegrees, 0f);
    }

    private void UpdateMiddlePieces(Vector3 primaryPos, Vector3 mousePos, Vector3 forward, Quaternion rotForward)
    {
        // No middle pieces available.
        if (_currentPlaceable.tertiaryPrefab == null && _currentPlaceable.fillerPrefab == null)
        {
            throw new Exception("LevelEditor: No middle pieces (tertiary or filler) available for current placeable.");
        }

        //put the origin pos and mouse pos on the same plane and get distance
        var primaryPosOnPlane = primaryPos;
        var mousePosOnPlane = mousePos;
        primaryPosOnPlane.y = 0f;
        mousePosOnPlane.y = 0f;
        var distance = Vector3.Distance(primaryPosOnPlane, mousePosOnPlane);
        
        //get sizes of the pieces for positioning
        var tertiaryWidth = _currentPlaceable.tertiaryPrefab.GetXSize();
        var fillerWidth = _currentPlaceable.fillerPrefab.GetXSize();
        var firstPlaceableWidth = prePlacementPiece.GetXSize();

        //start tiertiary pieces
        var areaToFill = Mathf.Max(0f, distance - firstPlaceableWidth);
        var tertiaryCount = Mathf.FloorToInt(areaToFill / tertiaryWidth);
        ResizePieceList(tertiaryPreviewPieces, tertiaryCount, _currentPlaceable.tertiaryPrefab);
        
        // Prepare filler pieces to fill any leftover space after tertiary pieces.
        var usedByTertiary = tertiaryCount * tertiaryWidth;
        //area needed to be filled by fillers
        var fillerArea = Mathf.Max(0f, areaToFill - usedByTertiary);
        var fillerCount = Mathf.CeilToInt(fillerArea / fillerWidth); //add 2 to cover gaps while spawning
        
        if (tertiaryCount > 0)
            fillerCount += 2; //covers gaps when there are tertiary pieces present
        else 
            fillerCount += 1; 
        
        //make sure filler count is even to balance pieces on both sides
        if (fillerCount % 2 != 0)
            fillerCount += 1;
        
        ResizePieceList(fillerPreviewPieces, fillerCount, _currentPlaceable.fillerPrefab);

        // Place tertiary pieces.
        var halfOfFillersOffset = fillerArea * 0.5f; // center the tertiary pieces if fillers are present
        var tertiaryStart = (tertiaryWidth * 0.5f) + halfOfFillersOffset + firstPlaceableWidth/2;
        for (var i = 0; i < tertiaryPreviewPieces.Count; i++)
        {
            var distanceFromOrigin = tertiaryStart + (i * tertiaryWidth);
            var pos = primaryPos + (forward * distanceFromOrigin);
            var piece = tertiaryPreviewPieces[i];
            piece.transform.position = ApplyBottomYOffset(piece, pos);
            piece.transform.rotation = rotForward;
        }

        //place filler pieces before and after tertiary pieces
        var fillerStart = fillerWidth * 0.25f + firstPlaceableWidth * 0.5f;
        for (var i = 0; i < fillerPreviewPieces.Count; i++)
        {
            var distanceFromOrigin = fillerStart + (i * fillerWidth);
            
            if (i > fillerPreviewPieces.Count / 2 - 1 && tertiaryCount > 0)
            {
                //second half of fillers go after tertiary pieces
                distanceFromOrigin += (usedByTertiary - fillerWidth*1.75f);
            }
            
            var pos = primaryPos + (forward * distanceFromOrigin);
            var piece = fillerPreviewPieces[i];
            piece.transform.position = ApplyBottomYOffset(piece, pos);
            piece.transform.rotation = rotForward;
        }
    }

    private Vector3 ApplyBottomYOffset(PlaceablePiece piece, Vector3 position)
    {
        if (piece == null)
            return position;

        var yOffset = piece.yOffsset;
        return new Vector3(position.x, position.y + yOffset, position.z);
    }

    private void ResizePieceList(List<PlaceablePiece> list, int targetCount, PlaceablePiece prefab)
    {
        if (targetCount < 0)
            targetCount = 0;

        // Grow
        while (list.Count < targetCount)
        {
            var piece = Instantiate(prefab, _level.transform);
            list.Add(piece);
            piecesToPlace.Add(piece);
        }

        // Shrink
        while (list.Count > targetCount)
        {
            var idx = list.Count - 1;
            var piece = list[idx];
            list.RemoveAt(idx);
            piecesToPlace.Remove(piece);
            if (piece != null)
                Destroy(piece.gameObject);
        }
    }

    /// <summary>
    /// Given one end of the chain of placeable pieces, give the other
    /// TODO: very specific to this protoype of a bridge. 
    /// </summary>
    /// <param name="piece"></param>
    /// <returns></returns>
    private PlaceablePiece GetAnchorPieceGivenMobilePiece(PlaceablePiece piece)
    {
        if (piece == prePlacementPiece)
            return secondaryPreviewPiece;
        else
            return prePlacementPiece;
    }
    
    #region Listeners

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseDownAtPosition(Vector3 position)
    {
        // First click: anchor the primary piece at the clicked position (with y-offset),
        // and start a drag session that previews a full line until mouse up.
        if (isDraggingPieces || initialPiecesPlaced)
            return;
 
        isDraggingPieces = true;

        // Anchor the current prePlacementPiece.
        var offsetPosition = ApplyBottomYOffset(prePlacementPiece, position);
        prePlacementPiece.transform.position = offsetPosition;
        piecesToPlace.Add(prePlacementPiece);

        // Create secondary preview piece if available.
        if (_currentPlaceable.secondaryPrefab != null)
        {
            secondaryPreviewPiece = Instantiate(_currentPlaceable.secondaryPrefab, _level.transform);
            piecesToPlace.Add(secondaryPreviewPiece);
        }
        
        // Update once immediately so the user sees feedback even before first drag event.
        UpdateDragPreview(position, prePlacementPiece, secondaryPreviewPiece);
    }

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseUpAtPosition(Vector3 position)
    {
        if (!isDraggingPieces || initialPiecesPlaced)
            return;
        
        initialPiecesPlaced = true;

        isDraggingPieces = false;

        // Final update at mouse-up to ensure placement matches final cursor position.
        UpdateDragPreview(position, prePlacementPiece, secondaryPreviewPiece);

        // Lock everything in.
        foreach (var piece in piecesToPlace)
        {
            if (piece != null)
                piece.SetPlaceablePieceState(true);
        }
    }

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseDragAtPosition(Vector3 position)
    {
        if (!isDraggingPieces || initialPiecesPlaced)
            return;

        UpdateDragPreview(position, prePlacementPiece, secondaryPreviewPiece);
    }

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseOverAtPosition(Vector3 position)
    {
        if (isDraggingPieces || initialPiecesPlaced)
            return;

        if (prePlacementPiece == null)
            CreateNewPrePlacementPiece();

        prePlacementPiece.transform.position = ApplyBottomYOffset(prePlacementPiece, position);
    }
    

    void Draggable.IListener.DraggableMouseDownAtPosition(Draggable draggable, Vector3 position)
    {
        //if it's already dragging, 
        if (isDraggingPieces)
            return;
 
        isDraggingPieces = true;
        
        // un place the placeables
        foreach (var piece in piecesToPlace)
        {
            if (piece != null)
                piece.SetPlaceablePieceState(false);
        }
        
        // Set anchor and mobile pieces, this way they are cached for drag updates
        cachedAnchorPiece = GetAnchorPieceGivenMobilePiece(draggable.GetComponent<PlaceablePiece>());
        cachedMobilePiece = draggable.GetComponent<PlaceablePiece>();
        
        UpdateDragPreview(position, cachedAnchorPiece, cachedMobilePiece);
    }

    void Draggable.IListener.DraggableMouseUpAtPosition(Draggable draggable, Vector3 position)
    {
        if (!isDraggingPieces)
            return;

        isDraggingPieces = false;

        UpdateDragPreview(position, cachedAnchorPiece, cachedMobilePiece);

        // Clear cached pieces
        cachedAnchorPiece = null;
        cachedMobilePiece = null;
        
        // Lock everything in.
        foreach (var piece in piecesToPlace)
        {
            if (piece != null)
                piece.SetPlaceablePieceState(true);
        }
    }

    void Draggable.IListener.DraggableMouseDragAtPosition(Draggable draggable, Vector3 position)
    {
        if (!isDraggingPieces)
            return;

        Debug.Log("dragging at pos " + position);
        UpdateDragPreview(position, cachedAnchorPiece, cachedMobilePiece);
    }
    
    #endregion

}

