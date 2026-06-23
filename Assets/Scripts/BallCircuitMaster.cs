using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BallCircuitMaster : Agent
{
    [Header("Referencias de Ruta")]
    public Transform parentCheckpoints;
    public Transform goalTransform;

    [HideInInspector]
    public Transform[] waypoints;

    [Header("Movimiento")]
    public float forceMultiplier = 150f; // Ajustado a tu nueva configuración de alta fuerza
    public float maxSpeed = 20f;         // Ajustado a tu nueva configuración de alta velocidad
    public float waypointReachDistance = 1.5f;

    [Header("Sistema Anti-Estancamiento")]
    [Tooltip("Número máximo de pasos permitidos entre checkpoints antes de reiniciar el episodio.")]
    public int maxStepsWithoutCheckpoint = 800;
    private int stepsSinceLastCheckpoint = 0;

    [Header("Recompensas")]
    public float rewardProgress = 0.5f;
    public float rewardOnBridgeBonus = 0.3f;
    public float penaltyOffBridge = -0.001f;
    public float penaltyStep = -0.001f;
    public float penaltyDeathZone = -1f;
    public float rewardWaypoint = 0.5f;
    public float rewardGoalFull = 3.0f;

    Rigidbody rb;
    Vector3 startPosition;
    int currentWaypointIndex = 0;
    Transform currentTarget;
    float prevDistance;
    bool onBridge;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;

        // Límite global del episodio para evitar simulaciones infinitas absolutas
        MaxStep = 8000;

        if (parentCheckpoints != null)
        {
            int totalCheckpoints = parentCheckpoints.childCount;
            waypoints = new Transform[totalCheckpoints];

            for (int i = 0; i < totalCheckpoints; i++)
            {
                // Solo extrae la jerarquía, sin alterar físicas
                waypoints[i] = parentCheckpoints.GetChild(i);
            }
            Debug.Log($"[Arquitectura] IA inicializada. {totalCheckpoints} Checkpoints mapeados.");
        }
        else
        {
            Debug.LogError("[Error Fatal] No has asignado el contenedor 'parentCheckpoints' en el Inspector.");
        }
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currentWaypointIndex = 0;
        onBridge = false;

        // Reseteo del contador de inactividad al iniciar un nuevo intento
        stepsSinceLastCheckpoint = 0;

        if (waypoints != null && waypoints.Length > 0)
            currentTarget = waypoints[0];
        else
            currentTarget = goalTransform;

        transform.localPosition = startPosition + new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.1f, 0.1f));
        transform.rotation = Quaternion.identity;

        if (currentTarget != null)
            prevDistance = Vector3.Distance(transform.localPosition, currentTarget.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(Mathf.Clamp(localVel.x / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVel.y / maxSpeed, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localVel.z / maxSpeed, -1f, 1f));

        if (currentTarget != null)
        {
            Vector3 toTarget = currentTarget.localPosition - transform.localPosition;
            sensor.AddObservation(transform.InverseTransformDirection(toTarget.normalized));
            sensor.AddObservation(Mathf.Clamp(toTarget.magnitude / 20f, 0f, 1f));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        if (goalTransform != null)
        {
            Vector3 toGoal = goalTransform.localPosition - transform.localPosition;
            sensor.AddObservation(transform.InverseTransformDirection(toGoal.normalized));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }

        sensor.AddObservation(onBridge ? 1f : 0f);

        float completionPercentage = (waypoints != null && waypoints.Length > 0) ? (float)currentWaypointIndex / waypoints.Length : 0f;
        sensor.AddObservation(completionPercentage);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float ax = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float az = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(new Vector3(ax, 0f, az) * forceMultiplier, ForceMode.Acceleration);
        }

        if (currentTarget == null) return;

        float distToTarget = Vector3.Distance(transform.localPosition, currentTarget.localPosition);

        if (distToTarget < waypointReachDistance && currentTarget != goalTransform)
        {
            currentWaypointIndex++;
            AddReward(rewardWaypoint);

            // La IA logró avanzar un checkpoint. Se le otorga más tiempo reseteando el contador.
            stepsSinceLastCheckpoint = 0;

            if (currentWaypointIndex < waypoints.Length)
            {
                currentTarget = waypoints[currentWaypointIndex];
            }
            else
            {
                currentTarget = goalTransform;
            }
            distToTarget = Vector3.Distance(transform.localPosition, currentTarget.localPosition);
            prevDistance = distToTarget;
        }

        float progress = prevDistance - distToTarget;
        if (progress > 0f)
        {
            AddReward(progress * rewardProgress);
            if (onBridge) AddReward(progress * rewardOnBridgeBonus);
        }

        if (!onBridge) AddReward(penaltyOffBridge);
        AddReward(penaltyStep);

        prevDistance = distToTarget;
        onBridge = false;

        if (transform.position.y < -5f)
        {
            AddReward(penaltyDeathZone);
            EndEpisode();
        }

        // LÓGICA DE INTERRUPCIÓN POR ESTANCAMIENTO
        stepsSinceLastCheckpoint++;
        if (stepsSinceLastCheckpoint >= maxStepsWithoutCheckpoint)
        {
            AddReward(-0.25f); // Penalización moderada por atascarse
            EndEpisode();      // Destruye y reinicia el agente para liberar la instancia
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            AddReward(penaltyDeathZone);
            EndEpisode();
        }

        if (other.CompareTag("Goal"))
        {
            if (waypoints != null && currentWaypointIndex >= waypoints.Length - 1)
            {
                AddReward(rewardGoalFull);
            }
            EndEpisode();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Bridge"))
            onBridge = true;
    }
}