using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BallBridgeProgressive : Agent
{
    [Header("Referencias")]
    public Transform targetTransform;

    [Header("Movimiento")]
    public float forceMultiplier = 18f;
    public float maxSpeed = 8f;

    [Header("Debug")]
    public bool mostrarLogs = false;

    [Header("Recompensas")]
    public float rewardProgress = 0.5f;
    public float rewardOnBridgeBonus = 0.3f;
    public float penaltyOffBridge = -0.001f;
    public float penaltyStep = -0.001f;
    public float penaltyDeathZone = -1f;
    public float rewardGoalWithBridge = 2.0f;
    public float rewardGoalWithoutBridge = 0.5f;

    Rigidbody rb;
    Vector3 startPosition;
    float prevDistance;
    bool onBridge;
    bool crossedBridge;
    float totalRewardEpisode = 0f;
    int stepCount = 0;

    void Log(string mensaje, float recompensa)
    {
        if (!mostrarLogs) return;
        totalRewardEpisode += recompensa;
        Debug.Log($"[BallBridge] {mensaje} | Recompensa: {recompensa:+0.0000;-0.0000} | Acumulado: {totalRewardEpisode:+0.000;-0.000} | Step: {stepCount}");
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        onBridge = false;
        crossedBridge = false;
        totalRewardEpisode = 0f;
        stepCount = 0;

        transform.localPosition = startPosition
        + new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.1f, 0.1f));
        transform.rotation = Quaternion.identity;

        prevDistance = Vector3.Distance(transform.localPosition, targetTransform.localPosition);

        if (mostrarLogs)
            Debug.Log($"[BallBridge] ═══ EPISODIO INICIADO | Distancia inicial a meta: {prevDistance:0.00} ═══");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(Mathf.Clamp(localVel.x / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVel.y / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVel.z / maxSpeed, -1f, 1f));

        Vector3 toTarget = targetTransform.localPosition - transform.localPosition;
        sensor.AddObservation(transform.InverseTransformDirection(toTarget.normalized));
        sensor.AddObservation(Mathf.Clamp(toTarget.magnitude / 20f, 0f, 1f));

        sensor.AddObservation(onBridge ? 1f : 0f);

        // Total: 8 observaciones → Space Size = 8
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;

        float ax = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float az = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        rb.AddForce(new Vector3(ax, 0f, az) * forceMultiplier, ForceMode.Acceleration);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        bool wasOnBridge = onBridge;
        float currentDist = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
        float progress = prevDistance - currentDist;

        if (progress > 0f)
        {
            float r = progress * rewardProgress;
            AddReward(r);
            if (stepCount % 30 == 0)
                Log($"Progreso hacia meta ({progress:0.0000})", r);
        }

        if (wasOnBridge)
        {
            float r = progress * rewardOnBridgeBonus;
            AddReward(r);
            if (stepCount % 30 == 0)
                Log($"Bono puente (progress={progress:0.0000})", r);
        }

        if (!wasOnBridge)
        {
            AddReward(penaltyOffBridge);
            if (stepCount % 30 == 0)
                Log("Fuera del puente", penaltyOffBridge);
        }

        prevDistance = currentDist;
        AddReward(penaltyStep);
        onBridge = false;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> a = actionsOut.ContinuousActions;
        a[0] = Input.GetAxis("Horizontal");
        a[1] = Input.GetAxis("Vertical");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            AddReward(penaltyDeathZone);
            Log("💀 CAÍDA - DeathZone", penaltyDeathZone);
            if (mostrarLogs)
                Debug.Log($"[BallBridge] ═══ FIN EPISODIO por caída | Recompensa total: {totalRewardEpisode:0.000} | Steps: {stepCount} ═══");
            EndEpisode();
        }

        if (other.CompareTag("Goal"))
        {
            float r = crossedBridge ? rewardGoalWithBridge : rewardGoalWithoutBridge;
            AddReward(r);

            if (crossedBridge)
                Log("🏆 META alcanzada POR EL PUENTE", r);
            else
                Log("⚠️  META alcanzada SIN pasar por el puente", r);

            if (mostrarLogs)
                Debug.Log($"[BallBridge] ═══ FIN EPISODIO exitoso | Recompensa total: {totalRewardEpisode:0.000} | Steps: {stepCount} ═══");
            EndEpisode();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Bridge"))
        {
            if (!onBridge)
                Log("🌉 Entró al puente", 0f);
            onBridge = true;
            crossedBridge = true;
        }
    }
}
