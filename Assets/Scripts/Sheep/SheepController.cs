using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Individual sheep AI. Driven by three behaviours blended each FixedUpdate:
///   1. Wander  – gentle random drift
///   2. Flee    – run away from the dog when within fear radius
///   3. Flock   – cohesion / separation / alignment with other sheep
///
/// Uses Rigidbody for physics. Must be Initialised before first FixedUpdate.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SheepController : MonoBehaviour
{
    // Expose velocity so FlockingSystem can read it
    public Vector3 Velocity => rb.velocity;

    private SheepHerdingSettings settings;
    private List<SheepController> flock;
    private Transform dogTransform;
    private Rigidbody rb;

    private Vector3 wanderDir;
    private float wanderChangeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.drag = 3f;
        PickNewWanderDir();
    }

    /// <summary>Call once after spawning to wire up shared references.</summary>
    public void Initialize(SheepHerdingSettings s, List<SheepController> allSheep, Transform dog)
    {
        settings = s;
        flock = allSheep;
        dogTransform = dog;
    }

    private void FixedUpdate()
    {
        if (settings == null) return;

        Vector3 desiredVelocity = Vector3.zero;

        float distToDog = dogTransform != null
            ? Vector3.Distance(transform.position, dogTransform.position)
            : float.MaxValue;

        bool fleeing = distToDog < settings.sheepFearRadius;

        if (fleeing)
        {
            // Run directly away from dog
            Vector3 fleeDir = (transform.position - dogTransform.position);
            fleeDir.y = 0f;
            desiredVelocity += fleeDir.normalized * settings.sheepFleeSpeed;
        }
        else
        {
            // Random wander
            wanderChangeTimer -= Time.fixedDeltaTime;
            if (wanderChangeTimer <= 0f) PickNewWanderDir();
            desiredVelocity += wanderDir * settings.sheepMoveSpeed * settings.sheepWanderStrength;
        }

        // Flocking force (always active regardless of flee state)
        if (flock != null && flock.Count > 1)
        {
            Vector3 flockForce = FlockingSystem.CalculateFlockForce(
                transform, flock, settings.sheepSeparationRadius, settings.sheepCohesionRadius);
            desiredVelocity += flockForce * settings.sheepFlockingStrength;
        }

        // Wall avoidance — raycast in 4 directions, push away from nearby walls
        desiredVelocity += GetWallAvoidanceForce();

        desiredVelocity.y = 0f;

        // Clamp to max speed
        float maxSpeed = fleeing ? settings.sheepFleeSpeed : settings.sheepMoveSpeed;
        if (desiredVelocity.magnitude > maxSpeed)
            desiredVelocity = desiredVelocity.normalized * maxSpeed;

        // Smooth velocity change
        Vector3 targetVel = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);
        rb.velocity = Vector3.Lerp(rb.velocity, targetVel, Time.fixedDeltaTime * 6f);

        // Face movement direction
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (horizontalVel.magnitude > 0.2f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(horizontalVel),
                Time.fixedDeltaTime * 10f);
        }
    }

    /// <summary>
    /// Casts 4 short rays outward. If a Wall is nearby, returns a push
    /// force away from it — prevents sheep getting pinned in corners.
    /// </summary>
    private Vector3 GetWallAvoidanceForce()
    {
        Vector3 avoidance = Vector3.zero;
        float checkDist = 1.5f;

        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (Vector3 dir in directions)
        {
            Vector3 worldDir = transform.TransformDirection(dir);
            if (Physics.Raycast(transform.position, worldDir, out RaycastHit hit, checkDist))
            {
                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("GoalPen"))
                {
                    // Push strength increases the closer the wall is
                    float strength = 1f - (hit.distance / checkDist);
                    avoidance -= worldDir * strength * settings.sheepMoveSpeed;
                }
            }
        }

        avoidance.y = 0f;
        return avoidance;
    }

    private void PickNewWanderDir()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        wanderDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        wanderChangeTimer = Random.Range(1.5f, 4.5f);
    }

    /// <summary>Teleport to a new position and clear all motion. Called on episode reset.</summary>
    public void ResetSheep(Vector3 position)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        PickNewWanderDir();
    }
}
