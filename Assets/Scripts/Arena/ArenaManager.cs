using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Owns the arena lifecycle: sheep spawning, episode reset, and
/// aggregate statistics used by SheepdogAgent for reward calculation.
///
/// Setup: drag the ArenaManager onto any empty GameObject in the scene
/// and fill in the Inspector references before Play.
/// </summary>
public class ArenaManager : MonoBehaviour
{
    [Header("Settings")]
    public SheepHerdingSettings settings;

    [Header("Scene References")]
    [Tooltip("The GoalPen trigger object in the scene")]
    public GoalPen goalPen;
    [Tooltip("Where the dog is placed at episode start")]
    public Transform dogSpawnPoint;
    [Tooltip("Centre of the area where sheep are randomly scattered")]
    public Transform sheepSpawnCenter;

    [Header("Prefabs")]
    public GameObject sheepPrefab;

    // ── Runtime state ──────────────────────────────────────────────────────────
    private readonly List<SheepController> allSheep = new List<SheepController>();
    private GameObject dogGameObject;

    public List<SheepController> AllSheep => allSheep;
    public int TotalSheep => settings != null ? settings.sheepCount : 0;
    public int SheepInPen => goalPen != null ? goalPen.SheepInPen : 0;
    public GoalPen GoalPen => goalPen;

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>Called once by SheepdogAgent.Initialize() to link the dog.</summary>
    public void InitializeArena(GameObject dog)
    {
        dogGameObject = dog;
        SpawnAllSheep();
    }

    private void SpawnAllSheep()
    {
        // Destroy any sheep left over from a previous play session in the Editor
        foreach (var s in allSheep)
            if (s != null) Destroy(s.gameObject);
        allSheep.Clear();

        for (int i = 0; i < settings.sheepCount; i++)
        {
            Vector3 pos = RandomSpawnPosition();
            GameObject obj = Instantiate(sheepPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            obj.name = $"Sheep_{i}";
            SheepController sc = obj.GetComponent<SheepController>();
            allSheep.Add(sc);
        }

        // Wire up shared references after all sheep exist
        foreach (var s in allSheep)
            s.Initialize(settings, allSheep, dogGameObject.transform);
    }

    // ── Episode Reset ──────────────────────────────────────────────────────────

    /// <summary>Resets dog position and scatters sheep. Called from OnEpisodeBegin.</summary>
    public void ResetEpisode()
    {
        goalPen.ResetPen();

        // Reset dog
        if (dogGameObject != null)
        {
            dogGameObject.transform.SetPositionAndRotation(
                dogSpawnPoint.position, dogSpawnPoint.rotation);
            dogGameObject.GetComponent<SheepdogMovement>()?.Stop();
        }

        // Scatter sheep to random positions
        foreach (var sheep in allSheep)
        {
            if (sheep != null)
                sheep.ResetSheep(RandomSpawnPosition());
        }
    }

    private Vector3 RandomSpawnPosition()
    {
        Vector3 centre = sheepSpawnCenter != null ? sheepSpawnCenter.position : Vector3.zero;
        float h = settings.spawnAreaHalfSize;
        return new Vector3(
            centre.x + Random.Range(-h, h),
            centre.y,
            centre.z + Random.Range(-h, h));
    }

    // ── Aggregate Stats (used by SheepdogAgent for rewards) ───────────────────

    public Vector3 GetGoalPenCenter() =>
        goalPen != null ? goalPen.transform.position : Vector3.zero;

    /// <summary>Total distance of all unpenned sheep to the pen centre.
    /// Using total instead of average means reward scales with flock size —
    /// herding multiple sheep at once gives proportionally more reward.</summary>
    public float GetAverageSheepDistanceToPen()
    {
        Vector3 penPos = GetGoalPenCenter();
        float total = 0f;
        foreach (var s in allSheep)
        {
            if (s == null) continue;
            if (goalPen.IsSheepInPen(s.gameObject)) continue;
            total += Vector3.Distance(s.transform.position, penPos);
        }
        return total;
    }

    /// <summary>Average distance from each sheep to the flock centroid.</summary>
    public float GetFlockSpread()
    {
        if (allSheep.Count == 0) return 0f;
        Vector3 centroid = Vector3.zero;
        int valid = 0;
        foreach (var s in allSheep)
        {
            if (s == null) continue;
            centroid += s.transform.position;
            valid++;
        }
        if (valid == 0) return 0f;
        centroid /= valid;

        float spread = 0f;
        foreach (var s in allSheep)
            if (s != null) spread += Vector3.Distance(s.transform.position, centroid);
        return spread / valid;
    }

    /// <summary>Number of sheep within <paramref name="radius"/> of <paramref name="point"/>.</summary>
    public int GetNearbySheepCount(Vector3 point, float radius)
    {
        int count = 0;
        foreach (var s in allSheep)
            if (s != null && Vector3.Distance(point, s.transform.position) <= radius)
                count++;
        return count;
    }
}
