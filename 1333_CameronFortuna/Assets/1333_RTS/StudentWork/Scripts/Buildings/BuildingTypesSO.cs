using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingTypesSO", menuName = "ScriptableObjects/BuildingTypes")]
public class BuildingTypesSO : ScriptableObject
{
    // Make the list public so that it can be seen in the Inspector
    public List<BuildingData> Buildings = new();
}

[System.Serializable]
public class BuildingData
{
    public string BuildingName;
    public Sprite BuildingIcon;
    public GameObject BuildingPrefab;
    public int BuildingWidth = 1;
    public int BuildingDepth = 1;
    public Vector3 Scale = Vector3.one;
    public int BuildingCost = 1;
}