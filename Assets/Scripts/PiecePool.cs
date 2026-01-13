using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple pooling system for PlaceablePiece instances.
/// Prewarm prefabs up front and then Get/Return instances instead of instantiating/destroying repeatedly.
/// </summary>
public class PiecePool : MonoBehaviour
{
    [Header("Pool Settings")]
    /// <summary>
    ///the parent transform where we put the pooled objects
    /// </summary>
    [SerializeField] private Transform _poolRoot;

    //pool using prefab instance ID as key
    private readonly Dictionary<int, Stack<PlaceablePiece>> _availableByPrefabId = new Dictionary<int, Stack<PlaceablePiece>>();

    //track which prefab ID each instance ID belongs to, this way we can prevent "leaking" if the pool is used from a not pooled obj
    private readonly Dictionary<int, int> _instanceIdToPrefabId = new Dictionary<int, int>();
    
    /// <summary>
    /// Ensures a pool exists for the prefab.
    /// </summary>
    private Stack<PlaceablePiece> GetOrCreateStack(PlaceablePiece prefab)
    {
        var prefabId = prefab.GetInstanceID();
        if (!_availableByPrefabId.TryGetValue(prefabId, out var stack))
        {
            stack = new Stack<PlaceablePiece>();
            _availableByPrefabId.Add(prefabId, stack);
        }

        return stack;
    }

    /// <summary>
    /// Pre-instantiates a number of inactive objects for a prefab
    /// </summary>
    public void Prewarm(PlaceablePiece prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        var stack = GetOrCreateStack(prefab);
        for (var i = 0; i < count; i++)
        {
            var piece = Instantiate(prefab, _poolRoot);
            piece.gameObject.SetActive(false);

            _instanceIdToPrefabId[piece.GetInstanceID()] = prefab.GetInstanceID();
            stack.Push(piece);
        }
    }

    /// <summary>
    /// get prefab and return an instance from the pool
    /// </summary>
    public PlaceablePiece Get(PlaceablePiece prefab, Transform parent)
    {
        if (prefab == null)
            return null;

        var stack = GetOrCreateStack(prefab);
        PlaceablePiece piece;

        if (stack.Count > 0)
        {
            piece = stack.Pop();
        }
        else
        {
            piece = Instantiate(prefab);
            _instanceIdToPrefabId[piece.GetInstanceID()] = prefab.GetInstanceID();
        }

        if (parent != null)
            piece.transform.SetParent(parent, worldPositionStays: true);

        piece.gameObject.SetActive(true);
        return piece;
    }

    /// <summary>
    /// call this to return a piece to the pool for reuse
    /// </summary>
    public void Return(PlaceablePiece piece)
    {
        if (piece == null)
            return;

        var instanceId = piece.GetInstanceID();
        if (!_instanceIdToPrefabId.TryGetValue(instanceId, out var prefabId))
        {
            // Not created by this pool; destroy to avoid leaking objects.
            Destroy(piece.gameObject);
            return;
        }

        piece.gameObject.SetActive(false);
        piece.transform.SetParent(_poolRoot, worldPositionStays: false);

        if (!_availableByPrefabId.TryGetValue(prefabId, out var stack))
        {
            stack = new Stack<PlaceablePiece>();
            _availableByPrefabId.Add(prefabId, stack);
        }

        stack.Push(piece);
    }
}