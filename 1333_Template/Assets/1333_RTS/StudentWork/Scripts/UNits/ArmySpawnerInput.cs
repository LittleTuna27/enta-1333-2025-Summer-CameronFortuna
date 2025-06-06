using UnityEngine;

public class ArmySpawnerInput : MonoBehaviour
{
    [Header("Army Managers")]
    public CurrentTeamArmyManager playerArmyManager;
    public CurrentTeamArmyManager enemyArmyManager;

    [Header("Spawn Points")]
    public Vector3 playerSpawnPoint = new Vector3(2, 0, 2);
    public Vector3 enemySpawnPoint = new Vector3(10, 0, 10);

    [Header("Movement Test")]
    public Vector3 testMoveTarget = new Vector3(5, 0, 5);

    void Update()
    {
        HandleSpawning();
    }

    private void HandleSpawning()
    {
        //spawn in unit form player manager
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (playerArmyManager != null)
            {
                playerArmyManager.SpawnUnit(playerSpawnPoint);
                Debug.Log("Spawned Player Unit");
            }
            else
            {
                Debug.LogError("Player Army Manager is not assigned!");
            }
        }
        //spawn in unit form enemy manager
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (enemyArmyManager != null)
            {
                enemyArmyManager.SpawnUnit(enemySpawnPoint);
                Debug.Log("Spawned Enemy Unit");
            }
            else
            {
                Debug.LogError("Enemy Army Manager is not assigned!");
            }
        }
    }
}