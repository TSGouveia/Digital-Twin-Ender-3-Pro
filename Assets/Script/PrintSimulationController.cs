using UnityEngine;

public class PrintSimulationController : MonoBehaviour
{
    [Header("Configuração da Impressão")]
    [Tooltip("O Prefab do objeto 3D que será impresso.")]
    [SerializeField] private GameObject objectToPrintPrefab;

    [Tooltip("O GameObject que representa a mesa de impressão. A peça será instanciada na superfície dele.")]
    [SerializeField] private Transform printBedTransform;

    private GameObject currentPrintObject;
    private Material printMaterial;
    private static readonly int SliceHeightID = Shader.PropertyToID("_SliceHeight");

    void Start()
    {
        // Validação inicial para garantir que tudo está configurado corretamente
        if (objectToPrintPrefab == null)
        {
            Debug.LogError("O Prefab do objeto a ser impresso não foi definido!");
            this.enabled = false;
            return;
        }
        if (printBedTransform == null)
        {
            Debug.LogError("A Transform da mesa de impressão não foi definida!");
            this.enabled = false;
            return;
        }

        StartNewPrint();
    }

    /// <summary>
    /// Inicia uma nova simulação de impressão. Destrói qualquer objeto anterior.
    /// </summary>
    public void StartNewPrint()
    {
        // Se já existe um objeto sendo impresso, destrói ele antes de começar um novo.
        if (currentPrintObject != null)
        {
            Destroy(currentPrintObject);
        }

        // Instancia o novo objeto a partir do Prefab.
        // A posição é o centro da mesa (printBedTransform.position).
        // A rotação é a mesma da mesa.
        currentPrintObject = Instantiate(objectToPrintPrefab, printBedTransform.position, printBedTransform.rotation);

        // Garante que o objeto instanciado é filho da mesa, para se mover junto com ela se necessário.
        currentPrintObject.transform.SetParent(printBedTransform);

        // Pega o Renderer do objeto que acabamos de instanciar.
        Renderer printObjectRenderer = currentPrintObject.GetComponentInChildren<Renderer>();
        if (printObjectRenderer == null)
        {
            Debug.LogError("O Prefab instanciado não contém um componente Renderer!");
            Destroy(currentPrintObject);
            this.enabled = false;
            return;
        }

        // Cria uma instância única do material para este objeto.
        printMaterial = printObjectRenderer.material;

        // Aplica o shader de corte, se não estiver já aplicado.
        // Isso é opcional, mas garante que o material correto está em uso.
        printMaterial.shader = Shader.Find("Custom/SliceShader");

        // Começa com a peça totalmente invisível, definindo a altura do corte na superfície da mesa.
        ResetPrint();
    }

    /// <summary>
    /// Reseta a simulação, escondendo completamente a peça.
    /// </summary>
    public void ResetPrint()
    {
        // A altura inicial do corte é a posição Y da superfície da mesa no mundo.
        UpdatePrintHeight(0);
    }

    /// <summary>
    /// Atualiza a altura de "corte" do shader para revelar a peça.
    /// </summary>
    /// <param name="currentPrintHeight">A altura atual da impressão em unidades do Unity (eixo Y), relativa à base.</param>
    public void UpdatePrintHeight(float currentPrintHeight)
    {
        if (printMaterial == null) return;

        // A altura do corte no mundo é a altura da base (mesa) + a altura atual da impressão.
        float worldSliceHeight = printBedTransform.position.y + currentPrintHeight;

        // Define a propriedade "_SliceHeight" no shader.
        printMaterial.SetFloat(SliceHeightID, worldSliceHeight);
    }
}