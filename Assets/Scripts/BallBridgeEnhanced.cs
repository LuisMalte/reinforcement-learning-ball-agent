using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BallBridgeCurve : Agent
{
    [Header("Referencias")]
    public Transform cornerTransform;   // El cilindro esquina (tag: Esquina)
    public Transform goalTransform;     // La plataforma final (tag: Goal)

    [Header("Movimiento")]
    public float forceMultiplier = 18f;
    public float maxSpeed = 8f;

    [Header("Waypoint")]
    public float cornerReachDistance = 1.5f; // distancia para considerar que llegó a la Esquina

    [Header("Recompensas")]
    public float rewardProgress       =  0.5f;
    public float rewardOnBridgeBonus  =  0.3f;
    public float penaltyOffBridge     = -0.001f;
    public float penaltyStep          = -0.001f;
    public float penaltyDeathZone     = -1f;
    public float rewardCorner         =  0.8f;   // bonus por llegar a la esquina
    public float rewardGoalFull       =  2.0f;   // llegó por el camino correcto
    public float rewardGoalPartial    =  0.5f;   // llegó pero sin pasar la esquina

    [Header("Debug")]
    public bool mostrarLogs = false;

    // --- estado interno ---
    Rigidbody rb;
    Vector3 startPosition;

    // Fases: 0 = yendo a Esquina, 1 = yendo a Goal
    int fase = 0;
    Transform currentTarget;

    float prevDistance;
    bool onBridge;
    bool passedCorner;
    float totalRewardEpisode;
    int stepCount;

    // ──────────────────────────────────────────
    void Log(string msg, float r)
    {
        if (!mostrarLogs) return;
        totalRewardEpisode += r;
        Debug.Log($"[BallCurve] {msg} | R:{r:+0.0000;-0.0000} | Σ:{totalRewardEpisode:+0.000;-0.000} | step:{stepCount}");
    }

    // ──────────────────────────────────────────
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        fase          = 0;
        onBridge      = false;
        passedCorner  = false;
        totalRewardEpisode = 0f;
        stepCount     = 0;

        currentTarget = cornerTransform;   // primero va a la esquina

        transform.localPosition = startPosition
        + new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.1f, 0.1f));
        transform.rotation = Quaternion.identity;

        prevDistance = Vector3.Distance(transform.localPosition, currentTarget.localPosition);

        if (mostrarLogs)
            Debug.Log($"[BallCurve] ═══ EPISODIO | dist ini a Esquina: {prevDistance:0.00} ═══");
    }

    // ──────────────────────────────────────────
    public override void CollectObservations(VectorSensor sensor)
    {
        // Velocidad normalizada (3)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(Mathf.Clamp(localVel.x / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVel.y / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVel.z / maxSpeed, -1f, 1f));

        // Dirección y distancia al target ACTUAL (4)
        Vector3 toTarget = currentTarget.localPosition - transform.localPosition;
        sensor.AddObservation(transform.InverseTransformDirection(toTarget.normalized));
        sensor.AddObservation(Mathf.Clamp(toTarget.magnitude / 20f, 0f, 1f));

        // Dirección al Goal (aunque no sea el target actual) (3)
        // Esto le da contexto del objetivo final desde el inicio
        Vector3 toGoal = goalTransform.localPosition - transform.localPosition;
        sensor.AddObservation(transform.InverseTransformDirection(toGoal.normalized));

        // Estado actual
        sensor.AddObservation(onBridge ? 1f : 0f);   // (1)
        sensor.AddObservation(fase == 1 ? 1f : 0f);  // (1) ¿ya pasó la esquina?

        // Total: 12 observaciones → Space Size = 12
    }

    // ──────────────────────────────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;

        float ax = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float az = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        rb.AddForce(new Vector3(ax, 0f, az) * forceMultiplier, ForceMode.Acceleration);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // ── Revisar si llegó a la Esquina (waypoint) ──
        if (fase == 0)
        {
            float distCorner = Vector3.Distance(transform.localPosition, cornerTransform.localPosition);
            if (distCorner < cornerReachDistance)
            {
                fase = 1;
                passedCorner = true;
                currentTarget = goalTransform;
                prevDistance = Vector3.Distance(transform.localPosition, currentTarget.localPosition);

                AddReward(rewardCorner);
                Log("🔄 ESQUINA alcanzada — ahora va al Goal", rewardCorner);
                return; // saltar el resto del step para recalcular progreso en el siguiente
            }
        }

        // ── Recompensa de progreso hacia currentTarget ──
        float currentDist = Vector3.Distance(transform.localPosition, currentTarget.localPosition);
        float progress = prevDistance - currentDist;

        if (progress > 0f)
        {
            float r = progress * rewardProgress;
            AddReward(r);
            if (stepCount % 30 == 0) Log($"Progreso ({progress:0.0000}) fase:{fase}", r);
        }

        if (onBridge && progress > 0f)
        {
            float r = progress * rewardOnBridgeBonus;
            AddReward(r);
            if (stepCount % 30 == 0) Log($"Bono puente fase:{fase}", r);
        }

        if (!onBridge)
        {
            AddReward(penaltyOffBridge);
            if (stepCount % 60 == 0) Log("Fuera del puente", penaltyOffBridge);
        }

        prevDistance = currentDist;
        AddReward(penaltyStep);
        onBridge = false; // se refresca en OnCollisionStay
    }

    // ──────────────────────────────────────────
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> a = actionsOut.ContinuousActions;
        a[0] = Input.GetAxis("Horizontal");
        a[1] = Input.GetAxis("Vertical");
    }

    // ──────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            AddReward(penaltyDeathZone);
            Log("💀 CAÍDA", penaltyDeathZone);
            EndEpisode();
        }

        if (other.CompareTag("Goal"))
        {
            float r = passedCorner ? rewardGoalFull : rewardGoalPartial;
            AddReward(r);
            Log(passedCorner ? "🏆 META por ruta correcta" : "⚠️ META sin esquina", r);
            EndEpisode();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Bridge"))
            onBridge = true;
    }
}
