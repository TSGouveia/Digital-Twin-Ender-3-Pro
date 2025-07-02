using UnityEngine;
using TMPro;

public class NozzleController : MonoBehaviour
{
    [Header("Virtual Axis Objects")]
    [SerializeField] private GameObject xAxis;
    [SerializeField] private GameObject yAxis; // Eixo virtual da ALTURA (Y do Unity)
    [SerializeField] private GameObject zAxis; // Eixo virtual da PROFUNDIDADE (Z do Unity)

    [Header("Movement Smoothing")]
    [Tooltip("Tempo (em segundos) para o bico alcan�ar a posi��o-alvo. 0 para movimento instant�neo.")]
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Virtual Bed Dimensions (Unity Units)")]
    [SerializeField] private Vector3 nozzleMinPosition = new Vector3(-0.5f, 0, -0.5f);
    [SerializeField] private Vector3 nozzleMaxPosition = new Vector3(0.5f, 1, 0.5f);

    [Header("Real Printer Dimensions (mm)")]
    [SerializeField] private Vector3 nozzleMinHardwarePosition = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 nozzleMaxHardwarePosition = new Vector3(220, 250, 220);

    [Header("Debugging")]
    [SerializeField] private TMP_Text debugText;

    [Header("Simula��o")]
    [SerializeField] private PrintSimulationController printSimulator;

    // --- Vari�veis para a transi��o suave ---
    private Vector3 targetHardwarePosition;   // A �ltima posi��o recebida do WebSocket (coordenadas da impressora com Y/Z trocados)
    private Vector3 currentVirtualPosition;   // A posi��o visual atual no espa�o do Unity, que ser� suavizada
    private Vector3 smoothingVelocity = Vector3.zero; // Refer�ncia de velocidade para o SmoothDamp (deve ser mantida entre frames)

    private void Start()
    {
        // Inicializa a posi��o-alvo com a posi��o inicial do hardware.
        // A troca de eixos � feita aqui para consist�ncia.
        targetHardwarePosition = new Vector3(nozzleMinHardwarePosition.x, nozzleMinHardwarePosition.z, nozzleMinHardwarePosition.y);

        // Define a posi��o virtual inicial sem suaviza��o para evitar um "salto" no in�cio.
        currentVirtualPosition = MapInputToPosition(targetHardwarePosition);
        ApplyPositionToTransforms(currentVirtualPosition);

        // Inicializa o simulador de impress�o na altura inicial.
        if (printSimulator != null)
        {
            printSimulator.UpdatePrintHeight(currentVirtualPosition.y);
        }
    }

    private void Update()
    {
        // 1. Calcula a posi��o-alvo no espa�o virtual (Unity) a partir das coordenadas do hardware.
        Vector3 targetVirtualPosition = MapInputToPosition(targetHardwarePosition);

        // 2. Suaviza o movimento da posi��o virtual atual em dire��o � posi��o-alvo.
        currentVirtualPosition = Vector3.SmoothDamp(currentVirtualPosition, targetVirtualPosition, ref smoothingVelocity, smoothTime);

        // 3. Aplica a posi��o suavizada aos objetos visuais (eixos).
        ApplyPositionToTransforms(currentVirtualPosition);

        // 4. Atualiza a simula��o de impress�o com a altura virtual suavizada.
        if (printSimulator != null)
        {
            printSimulator.UpdatePrintHeight(currentVirtualPosition.y);
        }

        // 5. Atualiza o texto de debug com a posi��o-alvo (a posi��o real da impressora).
        UpdateDebugText(targetHardwarePosition);
    }

    /// <summary>
    /// Esta fun��o � chamada pelo WebSocket. Agora, ela apenas atualiza a posi��o-alvo.
    /// O movimento real e a interpola��o acontecem no Update().
    /// </summary>
    /// <param name="position">Coordenadas da impressora com Y e Z j� trocados (printer.X, printer.Z, printer.Y).</param>
    public void SetNozzlePosition(Vector3 receivedPosition)
    {
        this.targetHardwarePosition = receivedPosition;
    }

    /// <summary>
    /// Aplica os valores de posi��o virtual calculados aos transforms dos eixos.
    /// Esta fun��o foi criada para evitar repeti��o de c�digo.
    /// </summary>
    private void ApplyPositionToTransforms(Vector3 virtualPosition)
    {
        xAxis.transform.localPosition = new Vector3(virtualPosition.x, xAxis.transform.localPosition.y, xAxis.transform.localPosition.z);
        yAxis.transform.localPosition = new Vector3(yAxis.transform.localPosition.x, virtualPosition.y, yAxis.transform.localPosition.z);
        zAxis.transform.localPosition = new Vector3(zAxis.transform.localPosition.x, zAxis.transform.localPosition.y, virtualPosition.z);
    }

    /// <summary>
    /// Mapeia uma posi��o do hardware (com eixos j� trocados) para uma posi��o no espa�o virtual do Unity.
    /// </summary>
    private Vector3 MapInputToPosition(Vector3 hardwarePositionWithSwappedAxes)
    {
        // Mapeia o X do hardware para o X virtual
        float mappedX = Mathf.Lerp(nozzleMinPosition.x, nozzleMaxPosition.x, Mathf.InverseLerp(nozzleMinHardwarePosition.x, nozzleMaxHardwarePosition.x, hardwarePositionWithSwappedAxes.x));

        // Mapeia o Z do hardware (que est� em .y) para o Y virtual (altura)
        float mappedY = Mathf.Lerp(nozzleMinPosition.y, nozzleMaxPosition.y, Mathf.InverseLerp(nozzleMinHardwarePosition.z, nozzleMaxHardwarePosition.z, hardwarePositionWithSwappedAxes.y));

        // Mapeia o Y do hardware (que est� em .z) para o Z virtual (profundidade)
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
            // Mostra a posi��o-alvo (real da impressora), desfazendo a troca de eixos para exibi��o.
            // hardwareTarget.x -> printer.X
            // hardwareTarget.y -> printer.Z
            // hardwareTarget.z -> printer.Y
            debugText.text = $"Printer Target (mm) - X: {hardwareTargetWithSwappedAxes.x:F2}, Y: {hardwareTargetWithSwappedAxes.z:F2}, Z: {hardwareTargetWithSwappedAxes.y:F2}";
        }
    }
}