using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_EditorPlaceables", menuName = "Scriptable Objects/SO_EditorPlaceables")]
//// <summary>
//// Holds the placeables available to the level editor
//// </summary>
public class SO_EditorPlaceables : ScriptableObject
{
    public IEnumerable<SO_Placeable> _placeables;
}
