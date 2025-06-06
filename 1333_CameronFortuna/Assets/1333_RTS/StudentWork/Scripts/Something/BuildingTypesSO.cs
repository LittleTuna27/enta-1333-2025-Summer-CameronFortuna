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
}