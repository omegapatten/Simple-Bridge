using UnityEngine;

/// <summary>
/// An object that can be places on a placement plane
/// </summary>
public class PlaceablePiece : MonoBehaviour
{
    
    [SerializeField] private Renderer pieceRenderer;
    /// <summary>
    /// material used when previewing placement
    /// </summary>
    [SerializeField] private Material previewMaterial;
    /// <summary>
    /// material used when placed in the level
    /// </summary>
    [SerializeField] private Material placedMaterial;

    [SerializeField] private float yOffsset;
    /// <summary>
    /// reference to the draggable component for this placeable piece
    /// enable and disable as needed
    /// </summary>
    [SerializeField] private Draggable draggable;
    
    /// <summary>
    /// Has this placeable been placed yet?
    /// Used to determine if it's parts of the current placement logic and how it presents
    /// </summary>
    public bool isPlaced = false;
    /// <summary>
    /// used for offesting off the plane when placing
    /// </summary>
    public float bottomYOffset = 0f;
    
    private void Start()
    {
        SetBottomYOffset();
        pieceRenderer.material = previewMaterial;
    }
    
    private void SetBottomYOffset()
    {
        bottomYOffset = yOffsset;
    }
    
    /// <summary>
    /// mark this piece as placed and change its material and draggable state
    /// </summary>
    public void SetPlaceablePieceState(bool isPlaced)
    {
        this.isPlaced = isPlaced;
        pieceRenderer.material = isPlaced ? placedMaterial : previewMaterial;
        
        //if it's a draggable, enable it after placement
        if (draggable != null)
        {
            draggable.SetDraggableEnabled(isPlaced);
        }
    }

    /// <summary>
    /// get the size of the placeable piece on the x axis
    /// </summary>
    /// <returns></returns>
    public float GetXSize()
    {
        return pieceRenderer.bounds.size.x;
    }
}
