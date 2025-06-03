using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "AvailableTeamUnits", menuName = "AvailableTeamUnits")]
public class AvailableTeamUnits : ScriptableObject 
{
    [SerializeField] List<AvailableTeamUnits> availableTeamUnits = new List<AvailableTeamUnits> ();


}
