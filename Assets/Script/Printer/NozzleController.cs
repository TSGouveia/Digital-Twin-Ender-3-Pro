using UnityEngine;
using TMPro;

public class NozzleController : MonoBehaviour
{
    [Header("Virtual Axis Objects")]
    [SerializeField] private GameObject xAxis;
    [SerializeField] private GameObject yAxis; // Eixo virtual da ALTURA (Y do Unity)
    [SerializeField] private GameObject zAxis; // Eixo virtual da PROFUNDIDADE (Z do Unity)

    [Header("Movement Smoothing")]
    [Tooltip("Tempo (em segundos) para o bico alcançar a posição-alvo. 0 para movimento instantâneo.")]
    [SerializeField] private float smoothTime = 0.05f;

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

    // --- Variáveis para a transição suave ---
    private Vector3 targetHardwarePosition;   // A última posição recebida do WebSocket (coordenadas da impressora com Y/Z trocados)
    private Vector3 currentVirtualPosition;   // A posição visual atual no espaço do Unity, que será suavizada
    private Vector3 smoothingVelocity = Vector3.zero; // Referência de velocidade para o SmoothDamp (deve ser mantida entre frames)

    private void Start()
    {
        // Inicializa a posição-alvo com a posição inicial do hardware.
        // A troca de eixos é feita aqui para consistência.
        targetHardwarePosition = new Vector3(nozzleMinHardwarePosition.x, nozzleMinHardwarePosition.z, nozzleMinHardwarePosition.y);

        // Define a posição virtual inicial sem suavização para evitar um "salto" no início.
        currentVirtualPosition = MapInputToPosition(targetHardwarePosition);
        ApplyPositionToTransforms(currentVirtualPosition);

        // Inicializa o simulador de impressão na altura inicial.
        if (printSimulator != null)
        {
            printSimulator.UpdatePrintHeight(currentVirtualPosition.y);
        }
    }

    private void Update()
    {
        // 1. Calcula a posição-alvo no espaço virtual (Unity) a partir das coordenadas do hardware.
        Vector3 targetVirtualPosition = MapInputToPosition(targetHardwarePosition);

        // 2. Suaviza o movimento da posição virtual atual em direção à posição-alvo.
        currentVirtualPosition = Vector3.SmoothDamp(currentVirtualPosition, targetVirtualPosition, ref smoothingVelocity, smoothTime);

        // 3. Aplica a posição suavizada aos objetos visuais (eixos).
        ApplyPositionToTransforms(currentVirtualPosition);

        // 4. Atualiza a simulação de impressão com a altura virtual suavizada.
        if (printSimulator != null)
        {
            printSimulator.UpdatePrintHeight(currentVirtualPosition.y);
        }

        // 5. Atualiza o texto de debug com a posição-alvo (a posição real da impressora).
        UpdateDebugText(targetHardwarePosition);
    }

    /// <summary>
    /// Esta função é chamada pelo WebSocket. Agora, ela apenas atualiza a posição-alvo.
    /// O movimento real e a interpolação acontecem no Update().
    /// </summary>
    /// <param name="position">Coordenadas da impressora com Y e Z já trocados (printer.X, printer.Z, printer.Y).</param>
    public void SetNozzlePosition(Vector3 receivedPosition)
    {
        this.targetHardwarePosition = receivedPosition;
    }

    /// <summary>
    /// Aplica os valores de posição virtual calculados aos transforms dos eixos.
    /// Esta função foi criada para evitar repetição de código.
    /// </summary>
    private void ApplyPositionToTransforms(Vector3 virtualPosition)
    {
        xAxis.transform.localPosition = new Vector3(virtualPosition.x, xAxis.transform.localPosition.y, xAxis.transform.localPosition.z);
        yAxis.transform.localPosition = new Vector3(yAxis.transform.localPosition.x, virtualPosition.y, yAxis.transform.localPosition.z);
        zAxis.transform.localPosition = new Vector3(zAxis.transform.localPosition.x, zAxis.transform.localPosition.y, virtualPosition.z);
    }

    /// <summary>
    /// Mapeia uma posição do hardware (com eixos já trocados) para uma posição no espaço virtual do Unity.
    /// </summary>
    private Vector3 MapInputToPosition(Vector3 hardwarePositionWithSwappedAxes)
    {
        // Mapeia o X do hardware para o X virtual
        float mappedX = Mathf.Lerp(nozzleMinPosition.x, nozzleMaxPosition.x, Mathf.InverseLerp(nozzleMinHardwarePosition.x, nozzleMaxHardwarePosition.x, hardwarePositionWithSwappedAxes.x));

        // Mapeia o Z do hardware (que está em .y) para o Y virtual (altura)
        float mappedY = Mathf.Lerp(nozzleMinPosition.y, nozzleMaxPosition.y, Mathf.InverseLerp(nozzleMinHardwarePosition.z, nozzleMaxHardwarePosition.z, hardwarePositionWithSwappedAxes.y));

        // Mapeia o Y do hardware (que está em .z) para o Z virtual (profundidade)
        float mappedZ = Mathf.Lerp(nozzleMinPosition.z, nozzleMaxPosition.z, Mathf.InverseLerp(nozzleMinHardwarePosition.y, nozzleMaxHardwarePosition.y, hardwarePositionWithSwappedAxes.z));

        return new Vector3(mappedX, mappedY, mappedZ);
    }

    /// <summary>
    /// Atualiza o texto de debug para mostrar as coordenadas reais da impressora.
    /// </summary>
    private void UpdateDebugText(Vector3 hardwareTargetWithSwappedAxes)
    {
        if (debugText != null)
        {
            // Mostra a posição-alvo (real da impressora), desfazendo a troca de eixos para exibição.
            // hardwareTarget.x -> printer.X
            // hardwareTarget.y -> printer.Z
            // hardwareTarget.z -> printer.Y
            debugText.text = $"Printer Target (mm) - X: {hardwareTargetWithSwappedAxes.x:F2}, Y: {hardwareTargetWithSwappedAxes.z:F2}, Z: {hardwareTargetWithSwappedAxes.y:F2}";
        }
    }
}