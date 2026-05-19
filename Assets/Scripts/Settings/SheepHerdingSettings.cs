using UnityEngine;

/// <summary>
/// Central ScriptableObject for all tunable game and RL parameters.
/// Create via Assets > Create > SheepHerding > Settings.
/// </summary>
[CreateAssetMenu(fileName = "SheepHerdingSettings", menuName = "SheepHerding/Settings")]
public class SheepHerdingSettings : ScriptableObject
{
    [Header("Dog Movement")]
    [Tooltip("Forward/backward speed in units per second")]
    public float dogMoveSpeed = 8f;
    [Tooltip("Degrees per second for rotation")]
    public float dogRotateSpeed = 180f;
    [Tooltip("Strafe speed multiplier (fraction of dogMoveSpeed)")]
    public float dogStrafeSpeed = 5f;

    [Header("Sheep Behaviour")]
    [Tooltip("Base wander speed")]
    public float sheepMoveSpeed = 3.5f;
    [Tooltip("Radius at which a sheep starts fleeing the dog")]
    public float sheepFearRadius = 6f;
    [Tooltip("Speed boost when actively fleeing")]
    public float sheepFleeSpeed = 6f;
    [Tooltip("Multiplier on random wander direction force")]
    public float sheepWanderStrength = 1.2f;
    [Tooltip("Multiplier on flocking cohesion + alignment force")]
    public float sheepFlockingStrength = 1.5f;
    [Tooltip("Minimum comfortable distance between sheep before separation kicks in")]
    public float sheepSeparationRadius = 1.8f;
    [Tooltip("Radius in which sheep consider others as flock neighbours")]
    public float sheepCohesionRadius = 8f;

    [Header("Episode")]
    [Tooltip("Seconds before a timeout ends the episode")]
    public float maxEpisodeDuration = 120f;
    [Tooltip("Number of sheep to spawn each episode")]
    public int sheepCount = 5;

    [Header("Arena")]
    [Tooltip("Half-size of the square area sheep are randomly spawned within")]
    public float spawnAreaHalfSize = 7f;

    [Header("Rewards")]
    [Tooltip("Reward per unit that the average sheep-to-pen distance decreases each step")]
    public float sheepTowardPenReward = 0.05f;
    [Tooltip("Reward each time one sheep enters the pen")]
    public float sheepEnteredPenReward = 2.0f;
    [Tooltip("Reward per sheep per step while inside the pen (keeps agent from ignoring them)")]
    public float sheepInPenPersistenceReward = 0.002f;
    [Tooltip("Penalty applied each step the flock spread is increasing (proportional to increase)")]
    public float sheepScatterPenalty = -0.005f;
    [Tooltip("Small penalty applied every fixed-update step to incentivise speed")]
    public float timePenalty = -0.0002f;
    [Tooltip("Penalty applied on timeout")]
    public float timeoutPenalty = -1f;
    [Tooltip("Bonus per sheep applied when every sheep is in the pen (scales with flock size)")]
    public float completionReward = 8f;
    [Tooltip("Penalty when dog collides with a wall")]
    public float wallCollisionPenalty = -0.02f;
}
