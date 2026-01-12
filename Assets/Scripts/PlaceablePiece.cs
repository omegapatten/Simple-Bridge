using UnityEngine;

/// <summary>
/// An object that can be places on a placement plane
/// </summary>
public class PlaceablePiece : MonoBehaviour
{
    [SerializeField] private Renderer renderer;
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
    /// Has this placeable been placed yet?
    /// Used to determine if it's parts of the current placement logic and how it presents
    /// </summary>
    public bool isPlaced = false;
    public float bottomYOffset = 0f;
    
    private void Start()
    {
        SetBottomYOffset();
        renderer.material = previewMaterial;
    }
    
    private void SetBottomYOffset()
    {
        bottomYOffset = yOffsset;
    }
    
    public void SetPieceToPlaced()
    {
        isPlaced = true;
        renderer.material = placedMaterial;
    }
}
