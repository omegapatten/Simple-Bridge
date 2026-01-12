using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains placed objects for a level and eventually level logic
/// </summary>
public class Level : MonoBehaviour
{
    /// <summary>
    /// All the placed objects in the level
    /// </summary>
    private IEnumerable<GameObject> _levelObjects;

    public void AddLevelObjects(IEnumerable<GameObject> levelObjects)
    {
        _levelObjects = levelObjects;
    }
}
