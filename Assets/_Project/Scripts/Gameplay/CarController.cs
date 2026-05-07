using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    [Header("Track Settings")]
    public SplineContainer trackSpline;
    
    [Header("Car Stats")]
    public float topSpeed = 50f; 
    public float acceleration = 40f; 
    public float deceleration = 60f; 
    
    [Header("State")]
    public bool canMove = false; 
    public bool isPedalPressed = false; 
    public bool isFlawlessRun = true;

    [Header("Crash Mechanics")]
    public float lookAheadDistance = 3f; // Дистанція самої аварії
    public float maxSafeTurnAngle = 25f; 
    public float crashFlySpeed = 30f; 

    [Header("Warning System (Red Track)")]
    public LineRenderer warningLine;
    public float warningDistance = 15f; // На скільки метрів вперед малюємо лінію
    public Color dangerColor = Color.red; // Колір небезпеки
    public Color safeColor = new Color(0, 0, 0, 0); // Прозорий колір, коли все ок

    [Header("QTE UI & Logic")]
    public GameObject qtePanel; 
    public RectTransform shrinkingRing; 
    public float startRingSize = 3f; 

    private float progress = 0f;
    private float currentSpeed = 0f; 
    private bool isCrashed = false; 

    private QTEZone currentZone;
    private float qteTimer = 0;
    private float initialQteTime = 0;
    private bool waitingForInput = false;

    void Update()
    {
        if (trackSpline == null) return;

        if (isCrashed)
        {
            transform.position += transform.forward * crashFlySpeed * Time.deltaTime;
            transform.Rotate(Vector3.forward, 360f * Time.deltaTime);
            return;
        }

        if (!canMove) return;

        HandleQTEInput();
        CheckForCrash();
        UpdateWarningLine(); // МАЛЮЄМО ЛІНІЮ КОЖЕН КАДР

        if (isCrashed) return; 

        if (isPedalPressed)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, topSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        if (currentSpeed > 0f)
        {
            float trackLength = trackSpline.CalculateLength();
            progress += (currentSpeed / trackLength) * Time.deltaTime;
            if (progress > 1f) progress -= 1f; 
        }

        trackSpline.Evaluate(progress, out float3 localPos, out float3 localTangent, out float3 localUp);
        transform.position = trackSpline.transform.TransformPoint(localPos);
        
        if (math.lengthsq(localTangent) > 0)
        {
            Vector3 worldTangent = trackSpline.transform.TransformDirection(localTangent);
            Vector3 worldUp = trackSpline.transform.TransformDirection(localUp);
            transform.rotation = Quaternion.LookRotation(worldTangent, worldUp);
        }
    }

    private void UpdateWarningLine()
    {
        if (warningLine == null) return;

        float trackLength = trackSpline.CalculateLength();
        int points = 15; // Кількість точок для плавності лінії
        warningLine.positionCount = points;
        
        bool dangerAhead = false;

        for (int i = 0; i < points; i++)
        {
            float dist = (warningDistance / (points - 1)) * i;
            float p = (progress + (dist / trackLength)) % 1f;
            
            trackSpline.Evaluate(p, out float3 localPos, out float3 currentTangent, out _);
            // Піднімаємо лінію трохи вище дороги, щоб вона не провалювалася в асфальт
            warningLine.SetPosition(i, trackSpline.transform.TransformPoint(localPos) + Vector3.up * 0.3f); 

            // Шукаємо небезпеку попереду
            if (!dangerAhead && i < points - 1)
            {
                float nextP = (progress + ((dist + 1f) / trackLength)) % 1f; 
                trackSpline.Evaluate(nextP, out _, out float3 nextTangent, out _);
                
                if (Vector3.Angle(math.normalize(currentTangent), math.normalize(nextTangent)) > maxSafeTurnAngle)
                {
                    dangerAhead = true;
                }
            }
        }

        warningLine.startColor = dangerAhead ? dangerColor : safeColor;
        warningLine.endColor = dangerAhead ? dangerColor : safeColor;
    }

    private void CheckForCrash()
    {
        if (!isPedalPressed || waitingForInput) return; 

        float trackLength = trackSpline.CalculateLength();
        float lookAheadProgress = lookAheadDistance / trackLength;
        float nextProgress = (progress + lookAheadProgress) % 1f;

        trackSpline.Evaluate(progress, out _, out float3 currentTangent, out _);
        trackSpline.Evaluate(nextProgress, out _, out float3 nextTangent, out _);

        Vector3 currentDir = math.normalize(currentTangent);
        Vector3 nextDir = math.normalize(nextTangent);

        if (Vector3.Angle(currentDir, nextDir) > maxSafeTurnAngle)
        {
            isCrashed = true;
            Debug.Log("OUCH! Аварія на повороті!");
        }
    }

    public void PressPedal() 
    { 
        isPedalPressed = true; 
        if (waitingForInput) CompleteQTE(true);
    }
    public void ReleasePedal() { isPedalPressed = false; }

    public void StartQTE(QTEZone zone)
    {
        currentZone = zone;
        qteTimer = zone.timeToReact;
        initialQteTime = zone.timeToReact;
        waitingForInput = true;
        if (qtePanel != null) qtePanel.SetActive(true); 
    }

    private void HandleQTEInput()
    {
        if (!waitingForInput) return;
        qteTimer -= Time.deltaTime;
        if (shrinkingRing != null)
        {
            float timeRatio = qteTimer / initialQteTime; 
            float currentScale = Mathf.Lerp(1f, startRingSize, timeRatio);
            shrinkingRing.localScale = new Vector3(currentScale, currentScale, 1f);
        }
        if (qteTimer <= 0) CompleteQTE(false);
    }

    private void CompleteQTE(bool success)
    {
        waitingForInput = false;
        if (qtePanel != null) qtePanel.SetActive(false);

        if (success)
        {
            Debug.Log("<color=green>PERFECT TRICK!</color>");
            ApplyTemporaryBoost(currentZone.boostMultiplier, 1f); 
        }
        else
        {
            Debug.Log("<color=red>TRICK FAILED!</color>");
            isFlawlessRun = false;
            currentSpeed = 0f; 
        }
    }

    public void ApplyTemporaryBoost(float multiplier, float duration)
    {
        StartCoroutine(BoostRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator BoostRoutine(float multiplier, float duration)
    {
        float originalTopSpeed = topSpeed;
        topSpeed *= multiplier;
        currentSpeed = topSpeed; 
        yield return new WaitForSeconds(duration);
        topSpeed = originalTopSpeed;
    }
}