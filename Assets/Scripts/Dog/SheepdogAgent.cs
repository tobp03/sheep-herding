using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

/// <summary>
/// The ML-Agents Agent for the sheepdog.
///
/// Observations (vector, 7 floats total):
///   [0-1]  Normalised horizontal velocity (vx, vz)
///   [2-3]  Forward direction of the dog (forward.x, forward.z)
///   [4]    Fraction of sheep within NEARBY_RADIUS  (0..1)
///   [5]    Normalised distance to goal pen          (0..1)
///   [6]    Fraction of sheep already in pen         (0..1)
///
/// Ray sensors (configured in Inspector) perceive:
///   Tags: Sheep, Wall, Obstacle, GoalPen
///
/// Actions (continuous, 3):
///   [0]  Forward / backward  (-1..1)
///   [1]  Strafe right / left (-1..1)
///   [2]  Rotate right / left (-1..1)
/// </summary>
[RequireComponent(typeof(SheepdogMovement))]
public class SheepdogAgent : Agent
{
    [Header("References")]
    public SheepHerdingSettings settings;
    public ArenaManager arenaManager;

    // Distance within which we report nearby sheep count
    private const float NEARBY_SHEEP_RADIUS = 10f;
    // Used to normalise distance observations
    private const float MAX_ARENA_DISTANCE = 30f;

    private SheepdogMovement movement;
    private float episodeTimer;
    private float prevAvgDistToPen;
    private float prevFlockSpread;
    private int prevSheepInPen;

    // Tracks which sheep have already been rewarded for entering the pen
    private readonly HashSet<GameObject> rewardedSheep = new HashSet<GameObject>();

    // ── Agent lifecycle ────────────────────────────────────────────────────────

    public override void Initialize()
    {
        movement = GetComponent<SheepdogMovement>();
        movement.Initialize(settings);
        arenaManager.InitializeArena(gameObject);
    }

    public override void OnEpisodeBegin()
    {
        arenaManager.ResetEpisode();
        episodeTimer = 0f;
        prevSheepInPen = 0;
        rewardedSheep.Clear();
        prevAvgDistToPen = arenaManager.GetAverageSheepDistanceToPen();
        prevFlockSpread = arenaManager.GetFlockSpread();
    }

    // ── Observations ──────────────────────────────────────────────────────────

    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalised velocity (2 floats)
        Vector3 vel = movement.GetVelocity();
        sensor.AddObservation(vel.x / settings.dogMoveSpeed);
        sensor.AddObservation(vel.z / settings.dogMoveSpeed);

        // Dog facing direction (2 floats)
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        // Nearby sheep fraction (1 float) number of sheeps in x units radius
        int nearby = arenaManager.GetNearbySheepCount(transform.position, NEARBY_SHEEP_RADIUS);
        sensor.AddObservation((float)nearby / Mathf.Max(1, arenaManager.TotalSheep));

        // Distance to pen, normalised (1 float)
        float distToPen = Vector3.Distance(transform.position, arenaManager.GetGoalPenCenter());
        sensor.AddObservation(Mathf.Clamp01(distToPen / MAX_ARENA_DISTANCE));

        // Sheep-in-pen progress (1 float)
        sensor.AddObservation((float)arenaManager.SheepInPen / Mathf.Max(1, arenaManager.TotalSheep));
    }

    // ── Actions & Rewards ─────────────────────────────────────────────────────

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float forward = actionBuffers.ContinuousActions[0];
        float strafe  = actionBuffers.ContinuousActions[1];
        float rotate  = actionBuffers.ContinuousActions[2];

        movement.ApplyMovement(forward, strafe, rotate);

        episodeTimer += Time.fixedDeltaTime;

        //  Reward: time alive (encourages speed)
        AddReward(settings.timePenalty);

        //  Reward: sheep moving closer to pen
        float currentAvgDist = arenaManager.GetAverageSheepDistanceToPen();
        float distImprovement = prevAvgDistToPen - currentAvgDist;
        if (distImprovement > 0f)
            AddReward(distImprovement * settings.sheepTowardPenReward);
        prevAvgDistToPen = currentAvgDist;

        //  Reward: sheep entering pen for the first time only
        int currentInPen = arenaManager.SheepInPen;
        foreach (var sheep in arenaManager.AllSheep)
        {
            if (sheep == null) continue;
            if (arenaManager.GoalPen.IsSheepInPen(sheep.gameObject) &&
                rewardedSheep.Add(sheep.gameObject))
            {
                AddReward(settings.sheepEnteredPenReward);
                Debug.Log($"[Reward] {sheep.gameObject.name} entered pen for first time: +{settings.sheepEnteredPenReward}");
            }
        }
        prevSheepInPen = currentInPen;

        // ── Reward: persistence — keep sheep in pen each step
        if (currentInPen > 0)
            AddReward(currentInPen * settings.sheepInPenPersistenceReward);

        // ── Penalty: flock scattering
        float currentSpread = arenaManager.GetFlockSpread();
        if (currentSpread > prevFlockSpread + 0.3f)
            AddReward(settings.sheepScatterPenalty);
        prevFlockSpread = currentSpread;

        // ── Terminal: all sheep herded (reward scales with flock size)
        if (currentInPen >= arenaManager.TotalSheep)
        {
            AddReward(settings.completionReward * arenaManager.TotalSheep);
            EndEpisode();
            return;
        }

        // ── Terminal: timeout
        if (episodeTimer >= settings.maxEpisodeDuration)
        {
            AddReward(settings.timeoutPenalty);
            EndEpisode();
        }
    }

    // ── Collision penalties ───────────────────────────────────────────────────

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
            AddReward(settings.wallCollisionPenalty);
    }

    // ── Heuristic (keyboard control for testing) ─────────────────────────────

    /// <summary>
    /// Lets you drive the dog manually with:
    ///   W/S = forward/back   A/D = strafe   Q/E = rotate
    /// Set Behaviour Type to "Heuristic Only" in the Behaviour Parameters
    /// component to test this before training.
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxisRaw("Vertical");
        ca[1] = Input.GetAxisRaw("Horizontal");
        ca[2] = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
    }
}
