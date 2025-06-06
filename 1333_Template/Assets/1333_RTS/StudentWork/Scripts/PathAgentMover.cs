using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathAgentMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    public void StartMoving(List<GridNode> path)
    {
        StopAllCoroutines();
        StartCoroutine(MoveAlongPath(path));
    }

    private IEnumerator MoveAlongPath(List<GridNode> path)
    {
        foreach (GridNode node in path)
        {
            Vector3 target = node.WorldPosition + Vector3.up * 0.5f;
            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }
}