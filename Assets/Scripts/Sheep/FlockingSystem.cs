using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure static helper that calculates a combined flocking direction vector
/// for one sheep given the rest of the flock.
///
/// Classic Reynolds rules:
///   Separation  – steer away from neighbours that are too close
///   Cohesion    – steer toward the average position of neighbours
///   Alignment   – steer toward the average heading of neighbours
/// </summary>
public static class FlockingSystem
{
    /// <param name="self">The sheep needing a steering direction.</param>
    /// <param name="allSheep">Full list including self (self is skipped internally).</param>
    /// <param name="separationRadius">Distance below which separation activates.</param>
    /// <param name="cohesionRadius">Distance within which a sheep is considered a neighbour.</param>
    /// <returns>A world-space direction vector (not normalised — magnitude encodes urgency).</returns>
    public static Vector3 CalculateFlockForce(
        Transform self,
        List<SheepController> allSheep,
        float separationRadius,
        float cohesionRadius)
    {
        Vector3 separation = Vector3.zero;
        Vector3 cohesionSum = Vector3.zero;
        Vector3 alignmentSum = Vector3.zero;
        int neighbours = 0;

        foreach (SheepController other in allSheep)
        {
            if (other == null || other.transform == self) continue;

            float dist = Vector3.Distance(self.position, other.transform.position);
            if (dist > cohesionRadius) continue;

            neighbours++;
            cohesionSum += other.transform.position;
            alignmentSum += other.Velocity;

            if (dist < separationRadius && dist > 0.001f)
            {
                // Inversely proportional: closer = stronger push
                separation += (self.position - other.transform.position).normalized / dist;
            }
        }

        if (neighbours == 0) return Vector3.zero;

        Vector3 cohesionDir = ((cohesionSum / neighbours) - self.position).normalized;
        Vector3 alignmentDir = (alignmentSum / neighbours).normalized;

        // Weights: separation dominates to prevent overlap
        Vector3 combined = (separation * 2.0f) + (cohesionDir * 0.8f) + (alignmentDir * 0.6f);
        combined.y = 0f;
        return combined;
    }
}
