using UnityEngine;

/// <summary>
/// Handles Rigidbody-based movement for the sheepdog.
/// Kept separate from the ML-Agents Agent so movement logic can be
/// tested with keyboard input without touching the RL code.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SheepdogMovement : MonoBehaviour
{
    private Rigidbody rb;
    private SheepHerdingSettings settings;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.drag = 4f;
    }

    public void Initialize(SheepHerdingSettings s)
    {
        settings = s;
    }

    /// <summary>
    /// Called from SheepdogAgent.OnActionReceived every FixedUpdate.
    /// All inputs are in the range [-1, 1].
    /// </summary>
    /// <param name="forward">Positive = forward, Negative = backward</param>
    /// <param name="strafe">Positive = right, Negative = left</param>
    /// <param name="rotate">Positive = clockwise, Negative = counter-clockwise</param>
    public void ApplyMovement(float forward, float strafe, float rotate)
    {
        if (settings == null) return;

        // Translate in local space, then convert to world
        Vector3 localMove = new Vector3(strafe * settings.dogStrafeSpeed,
                                         0f,
                                         forward * settings.dogMoveSpeed);
        Vector3 worldMove = transform.TransformDirection(localMove);
        worldMove.y = 0f;

        rb.velocity = new Vector3(worldMove.x, rb.velocity.y, worldMove.z);

        // Rotation around Y
        float rotDelta = rotate * settings.dogRotateSpeed * Time.fixedDeltaTime;
        transform.Rotate(Vector3.up, rotDelta, Space.Self);
    }

    /// <summary>Zero horizontal velocity. Called on episode reset.</summary>
    public void Stop()
    {
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        rb.angularVelocity = Vector3.zero;
    }

    public Vector3 GetVelocity() => rb.velocity;
}
