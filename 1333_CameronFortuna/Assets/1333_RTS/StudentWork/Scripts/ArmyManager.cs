//using UnityEngine;
//using System.Collections.Generic;

//public class ArmyManager : MonoBehaviour
//{
//    public List<CurrentTeamArmyManager> allTeams = new();

//    public void RegisterTeam(CurrentTeamArmyManager team)
//    {
//        if (!allTeams.Contains(team))
//            allTeams.Add(team);
//    }
//    public CurrentTeamArmyManager GetTeam(int armyID)
//    {
//        return allTeams.Find(team => team.armyID == armyID);
//    }
//}