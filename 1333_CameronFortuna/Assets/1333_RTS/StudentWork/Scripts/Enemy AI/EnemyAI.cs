using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyPathPlanner planner;
    [SerializeField] private float repathRate = 1.5f;
    private Coroutine moveRoutine;

    private void Start()
    {
        moveRoutine = StartCoroutine(RepathRoutine());
    }

    private IEnumerator RepathRoutine()
    {
        while (true)
        {
            List<GridNode> path = planner.GetPathToTarget(transform.position);

            if (path != null && path.Count > 0)
            {
                // Move along path or send to your movement system
                Debug.Log($"{name} received new path to target.");
                // StartCoroutine(MoveAlong(path)); // your movement logic
            }

            yield return new WaitForSeconds(repathRate);
        }
    }
}