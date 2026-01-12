using System;
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
    /// While true, we are in a "dragging a line" placement session.
    /// </summary>
    private bool isDraggingBridge = false;
    /// <summary>
    /// When true, prevents new bridges and now we can just move the existing bridge
    /// </summary>
    private bool initialBridgePiecesPlaced = false;

    /// <summary>
    /// The list of pieces that will be placed when the mouse is released.
    /// </summary>
    private readonly List<PlaceablePiece> piecesToPlace = new List<PlaceablePiece>();

    // Current drag-session state
    private Vector3 primaryAnchorPosition;
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
    private float _primaryYawAdditionalOffsetDegrees = 180f;

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
    
    private void UpdateDragPreview(Vector3 mousePosition)
    {
        if (prePlacementPiece == null)
            return;

        //anchor the first piece in place
        prePlacementPiece.transform.position = primaryAnchorPosition;

        //get direction from primary piece to mouse
        var directionFromOrigin = mousePosition - primaryAnchorPosition;
        directionFromOrigin.y = 0f;

        var dist = directionFromOrigin.magnitude;
        if (dist < MinDragDistance)
        {
            // Not enough distance to form a meaningful line yet: stack secondary on primary and clear middle pieces.
            if (secondaryPreviewPiece != null)
            {
                secondaryPreviewPiece.transform.position = primaryAnchorPosition;
            }

            ResizePieceList(tertiaryPreviewPieces, 0, _currentPlaceable.tertiaryPrefab);
            ResizePieceList(fillerPreviewPieces, 0, _currentPlaceable.fillerPrefab);
            return;
        }

        var forward = directionFromOrigin.normalized;

        // Apply a yaw offset so model forward matches our line direction.
        var rotForward = RotationLookingAlong(forward);
        var rotBack = RotationLookingAlong(-forward);

        // Rotate primary toward the current mouse.
        prePlacementPiece.transform.rotation = rotForward * Quaternion.Euler(0f, _primaryYawAdditionalOffsetDegrees, 0f);

        // Place secondary at the current mouse and point it back.
        if (secondaryPreviewPiece != null)
        {
            var secondaryPos = ApplyBottomYOffset(secondaryPreviewPiece, mousePosition);
            secondaryPreviewPiece.transform.position = secondaryPos;
            secondaryPreviewPiece.transform.rotation = rotBack;
        }

        // Fill between with tertiary and filler pieces.
        UpdateMiddlePieces(primaryAnchorPosition, mousePosition, forward, rotForward);
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
        
        //get sizes of the tertiary and filler pieces
        var tertiaryStep = _currentPlaceable.tertiaryPrefab.GetXSize();
        var fillerStep = _currentPlaceable.fillerPrefab.GetXSize();

        // Start placing pieces slightly away from the anchor to avoid immediate overlap.
        const float endInset = 0.5f;
        var usable = Mathf.Max(0f, distance - (2f * endInset));
        var tertiaryCount = Mathf.FloorToInt(usable / tertiaryStep);

        ResizePieceList(tertiaryPreviewPieces, tertiaryCount, _currentPlaceable.tertiaryPrefab);

        // Place tertiary pieces.
        var start = endInset + (tertiaryStep * 0.5f);
        for (var i = 0; i < tertiaryPreviewPieces.Count; i++)
        {
            var t = start + (i * tertiaryStep);
            var pos = primaryPos + (forward * t);
            var piece = tertiaryPreviewPieces[i];
            piece.transform.position = ApplyBottomYOffset(piece, pos);
            piece.transform.rotation = rotForward;
        }

        // Leftover space after tertiaries.
        var usedByTertiary = tertiaryCount * tertiaryStep;
        var leftover = Mathf.Max(0f, usable - usedByTertiary);

        // Fill leftover with filler pieces.
        var fillerCount = 0;
        if (_currentPlaceable.fillerPrefab != null && fillerStep > 0.0001f)
            fillerCount = Mathf.FloorToInt(leftover / fillerStep);

        ResizePieceList(fillerPreviewPieces, fillerCount, _currentPlaceable.fillerPrefab);

        // Place filler pieces immediately after the last tertiary segment.
        var fillerStart = endInset + usedByTertiary + (fillerStep * 0.5f);
        for (var i = 0; i < fillerPreviewPieces.Count; i++)
        {
            var t = fillerStart + (i * fillerStep);
            var pos = primaryPos + (forward * t);
            var piece = fillerPreviewPieces[i];
            piece.transform.position = ApplyBottomYOffset(piece, pos);
            piece.transform.rotation = rotForward;
        }
    }

    private Vector3 ApplyBottomYOffset(PlaceablePiece piece, Vector3 position)
    {
        if (piece == null)
            return position;

        var yOffset = piece.bottomYOffset;
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
    
    #region Listeners

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseDownAtPosition(Vector3 position)
    {
        // First click: anchor the primary piece at the clicked position (with y-offset),
        // and start a drag session that previews a full line until mouse up.
        if (isDraggingBridge || initialBridgePiecesPlaced)
            return;
 
        isDraggingBridge = true;

        // Anchor the current prePlacementPiece.
        primaryAnchorPosition = ApplyBottomYOffset(prePlacementPiece, position);
        prePlacementPiece.transform.position = primaryAnchorPosition;
        piecesToPlace.Clear();
        piecesToPlace.Add(prePlacementPiece);

        // Create secondary preview piece if available.
        if (_currentPlaceable.secondaryPrefab != null)
        {
            secondaryPreviewPiece = Instantiate(_currentPlaceable.secondaryPrefab, _level.transform);
            piecesToPlace.Add(secondaryPreviewPiece);
        }
        
        // Update once immediately so the user sees feedback even before first drag event.
        UpdateDragPreview(position);
    }

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseUpAtPosition(Vector3 position)
    {
        if (!isDraggingBridge || initialBridgePiecesPlaced)
            return;
        
        initialBridgePiecesPlaced = true;

        isDraggingBridge = false;

        // Final update at mouse-up to ensure placement matches final cursor position.
        UpdateDragPreview(position);

        // Lock everything in.
        foreach (var piece in piecesToPlace)
        {
            if (piece != null)
                piece.SetPieceToPlaced();
        }
        
        //remove placement pieces from the prePlacementPieces list
        prePlacementPiece = null;
        secondaryPreviewPiece = null;   
        piecesToPlace.Clear(); // clear the list cause they are no longer preview pieces
    }

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseDragAtPosition(Vector3 position)
    {
        if (!isDraggingBridge || initialBridgePiecesPlaced)
            return;

        UpdateDragPreview(position);
    }

    void PlacementPlane.IPlacementPlaneListener.PlacementPlaneMouseOverAtPosition(Vector3 position)
    {
        if (isDraggingBridge || initialBridgePiecesPlaced)
            return;

        if (prePlacementPiece == null)
            CreateNewPrePlacementPiece();

        prePlacementPiece.transform.position = ApplyBottomYOffset(prePlacementPiece, position);
    }
    
    #endregion
}

