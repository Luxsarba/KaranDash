using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ManAgent : MonoBehaviour
{
    public Transform[] waypoints; // Array of patrol points
    public float waypointDistanceThreshold = 1.5f; // Distance to consider waypoint reached
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;
    private bool isWaiting = false;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Check if waypoints are assigned
        if (waypoints.Length > 0)
        {
            animator.SetFloat("Speed", 15);

            // Set first waypoint as destination
            agent.destination = waypoints[currentWaypointIndex].position;
        }
        else
        {
            animator.SetFloat("Speed", 0);
            Debug.LogWarning("No waypoints assigned to ManAgent!");
        }
    }

    void Update()
    {

        // Only proceed if we have waypoints, agent is active, and not waiting
        if (waypoints.Length == 0 || !agent.isActiveAndEnabled || isWaiting) return;

        // Check if agent is close enough to current waypoint
        if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < waypointDistanceThreshold)
        {
            // Start waiting coroutine
            StartCoroutine(WaitAtWaypoint());
        }
    }

    IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        agent.isStopped = true; // Stop the agent movement
        animator.SetFloat("Speed", 0);
        yield return new WaitForSeconds(3f); // Wait for 3 seconds

        // Move to next waypoint
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        agent.destination = waypoints[currentWaypointIndex].position;
        agent.isStopped = false; // Resume agent movement
        isWaiting = false;
        animator.SetFloat("Speed", 15);

    }
}