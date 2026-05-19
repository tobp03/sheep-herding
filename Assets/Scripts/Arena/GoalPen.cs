using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Attach to a trigger collider that defines the goal pen area.
/// Tracks which sheep are currently inside and fires events.
/// </summary>
public class GoalPen : MonoBehaviour
{
    public event Action<GameObject> OnSheepEntered;
    public event Action<GameObject> OnSheepExited;

    private readonly HashSet<GameObject> sheepInsidePen = new HashSet<GameObject>();

    public int SheepInPen => sheepInsidePen.Count;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Sheep") && sheepInsidePen.Add(other.gameObject))
        {
            OnSheepEntered?.Invoke(other.gameObject);
            Debug.Log($"[GoalPen] {other.gameObject.name} entered. Sheep in pen: {sheepInsidePen.Count}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Sheep") && sheepInsidePen.Remove(other.gameObject))
        {
            OnSheepExited?.Invoke(other.gameObject);
            Debug.Log($"[GoalPen] {other.gameObject.name} exited. Sheep in pen: {sheepInsidePen.Count}");
        }
    }

    /// <summary>Called by ArenaManager at the start of each episode.</summary>
    public void ResetPen()
    {
        sheepInsidePen.Clear();
    }

    public bool IsSheepInPen(GameObject sheep) => sheepInsidePen.Contains(sheep);
}
