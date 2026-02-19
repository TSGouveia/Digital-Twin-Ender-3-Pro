using UnityEngine;
using TMPro;

public class NozzleController : MonoBehaviour
{
    [Header("Virtual Axis Objects")]
    [SerializeField] private GameObject xAxis;
    [SerializeField] private GameObject yAxis;
    [SerializeField] private GameObject zAxis;

    [Header("Movement Settings (Speed per Axis)")]
    [Tooltip("Velocidade máxima para cada eixo no Unity (Unidades/seg).")]
    [SerializeField] private Vector3 axisVelocities = new Vector3(1.0f, 0.5f, 1.0f); // X, Y (Altura), Z (Profundidade)

    [Tooltip("Tempo mínimo de suavização para evitar jitter.")]
    [SerializeField] private float minSmoothTime = 0.02f;

    [Header("Virtual Bed Dimensions (Unity Units)")]
    [SerializeField] private Vector3 nozzleMinPosition = new Vector3(-0.5f, 0, -0.5f);
    [SerializeField] private Vector3 nozzleMaxPosition = new Vector3(0.5f, 1, 0.5f);

    [Header("Real Printer Dimensions (mm)")]
    [SerializeField] private Vector3 nozzleMinHardwarePosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 nozzleMaxHardwarePosition = new Vector3(220, 250, 220);

    [Header("Debugging")]
    [SerializeField] private TMP_Text debugText;

    [Header("Simulação")]
    [SerializeField] private PrintSimulationController printSimulator;

    private Vector3 targetHardwarePosition;
    private Vector3 currentVirtualPosition;
    private Vector3 smoothingVelocity = Vector3.zero;

    private void Start()
    {
        targetHardwarePosition = new Vector3(nozzleMinHardwarePosition.x, nozzleMinHardwarePosition.z, nozzleMinHardwarePosition.y);
        currentVirtualPosition = MapInputToPosition(targetHardwarePosition);
        ApplyPositionToTransforms(currentVirtualPosition);
    }

    private void Update()
    {
        Vector3 targetVirtualPosition = MapInputToPosition(targetHardwarePosition);

        // 1. Calcula a distância absoluta necessária para cada eixo
        float distX = Mathf.Abs(targetVirtualPosition.x - currentVirtualPosition.x);
        float distY = Mathf.Abs(targetVirtualPosition.y - currentVirtualPosition.y);
        float distZ = Mathf.Abs(targetVirtualPosition.z - currentVirtualPosition.z);

        // 2. Calcula o tempo que cada eixo levaria na sua própria velocidade
        float timeX = distX / Mathf.Max(axisVelocities.x, 0.001f);
        float timeY = distY / Mathf.Max(axisVelocities.y, 0.001f);
        float timeZ = distZ / Mathf.Max(axisVelocities.z, 0.001f);

        // 3. O tempo de suavização será o maior tempo entre os 3 eixos
        // Isso garante que o movimento seja sincronizado (diagonal perfeita)
        float dynamicSmoothTime = Mathf.Max(timeX, timeY, timeZ);
        dynamicSmoothTime = Mathf.Max(dynamicSmoothTime, minSmoothTime);

        // 4. Aplica o SmoothDamp com o tempo calculado
        currentVirtualPosition = Vector3.SmoothDamp(
            currentVirtualPosition,
            targetVirtualPosition,
            ref smoothingVelocity,
            dynamicSmoothTime
        );

        ApplyPositionToTransforms(currentVirtualPosition);

        if (printSimulator != null)
        {
            printSimulator.UpdatePrintHeight(currentVirtualPosition.y);
        }

        UpdateDebugText(targetHardwarePosition);
    }

    public void SetNozzlePosition(Vector3 receivedPosition)
    {
        this.targetHardwarePosition = receivedPosition;
    }

    private void ApplyPositionToTransforms(Vector3 virtualPosition)
    {
        // Aplica as posições individuais respeitando a hierarquia do seu rig
        xAxis.transform.localPosition = new Vector3(virtualPosition.x, xAxis.transform.localPosition.y, xAxis.transform.localPosition.z);
        yAxis.transform.localPosition = new Vector3(yAxis.transform.localPosition.x, virtualPosition.y, yAxis.transform.localPosition.z);
        zAxis.transform.localPosition = new Vector3(zAxis.transform.localPosition.x, zAxis.transform.localPosition.y, virtualPosition.z);
    }

    private Vector3 MapInputToPosition(Vector3 hardwarePositionWithSwappedAxes)
    {
        float mappedX = Mathf.Lerp(nozzleMinPosition.x, nozzleMaxPosition.x, Mathf.InverseLerp(nozzleMinHardwarePosition.x, nozzleMaxHardwarePosition.x, hardwarePositionWithSwappedAxes.x));
        float mappedY = Mathf.Lerp(nozzleMinPosition.y, nozzleMaxPosition.y, Mathf.InverseLerp(nozzleMinHardwarePosition.z, nozzleMaxHardwarePosition.z, hardwarePositionWithSwappedAxes.y));
        float mappedZ = Mathf.Lerp(nozzleMinPosition.z, nozzleMaxPosition.z, Mathf.InverseLerp(nozzleMinHardwarePosition.y, nozzleMaxHardwarePosition.y, hardwarePositionWithSwappedAxes.z));

        return new Vector3(mappedX, mappedY, mappedZ);
    }

    private void UpdateDebugText(Vector3 hardwareTargetWithSwappedAxes)
    {
        if (debugText != null)
        {
            debugText.text = $"Target MM - X: {hardwareTargetWithSwappedAxes.x:F1} Y: {hardwareTargetWithSwappedAxes.z:F1} Z: {hardwareTargetWithSwappedAxes.y:F1}";
        }
    }
}