using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Placeable", menuName = "Scriptable Objects/SO_Placeable")]
public class SO_Placeable : ScriptableObject
{
    public string PlaceableName;
    /// <summary>
    /// Prefab to show when placing this object alone
    /// if the object can be placed in a line, this is the prefab used at the beginning
    /// </summary>
    public GameObject primaryPrefab;
    /// <summary>
    /// Shown at the end of a line if the object can be placed in a line
    /// </summary>
    public GameObject secondaryPrefab;
    /// <summary>
    /// shown in the middle of a line if the object can be placed in a line
    /// </summary>
    public GameObject tertiaryPrefab;
    /// <summary>
    /// used to fill in gaps if the object can be placed in a line and the tertiary portion doesn't fit exactly
    /// </summary>
    public GameObject fillerPrefab;
}
