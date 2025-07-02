using UnityEngine;

public class PrintSimulationController : MonoBehaviour
{
    [Header("Configura��o da Impress�o")]
    [Tooltip("O Prefab do objeto 3D que ser� impresso.")]
    [SerializeField] private GameObject objectToPrintPrefab;

    [Tooltip("O GameObject que representa a mesa de impress�o. A pe�a ser� instanciada na superf�cie dele.")]
    [SerializeField] private Transform printBedTransform;

    private GameObject currentPrintObject;
    private Material printMaterial;
    private static readonly int SliceHeightID = Shader.PropertyToID("_SliceHeight");

    void Start()
    {
        // Valida��o inicial para garantir que tudo est� configurado corretamente
        if (objectToPrintPrefab == null)
        {
            Debug.LogError("O Prefab do objeto a ser impresso n�o foi definido!");
            this.enabled = false;
            return;
        }
        if (printBedTransform == null)
        {
            Debug.LogError("A Transform da mesa de impress�o n�o foi definida!");
            this.enabled = false;
            return;
        }

        StartNewPrint();
    }

    /// <summary>
    /// Inicia uma nova simula��o de impress�o. Destr�i qualquer objeto anterior.
    /// </summary>
    public void StartNewPrint()
    {
        // Se j� existe um objeto sendo impresso, destr�i ele antes de come�ar um novo.
        if (currentPrintObject != null)
        {
            Destroy(currentPrintObject);
        }

        // Instancia o novo objeto a partir do Prefab.
        // A posi��o � o centro da mesa (printBedTransform.position).
        // A rota��o � a mesma da mesa.
        currentPrintObject = Instantiate(objectToPrintPrefab, printBedTransform.position, printBedTransform.rotation);

        // Garante que o objeto instanciado � filho da mesa, para se mover junto com ela se necess�rio.
        currentPrintObject.transform.SetParent(printBedTransform);

        // Pega o Renderer do objeto que acabamos de instanciar.
        Renderer printObjectRenderer = currentPrintObject.GetComponentInChildren<Renderer>();
        if (printObjectRenderer == null)
        {
            Debug.LogError("O Prefab instanciado n�o cont�m um componente Renderer!");
            Destroy(currentPrintObject);
            this.enabled = false;
            return;
        }

        // Cria uma inst�ncia �nica do material para este objeto.
        printMaterial = printObjectRenderer.material;

        // Aplica o shader de corte, se n�o estiver j� aplicado.
        // Isso � opcional, mas garante que o material correto est� em uso.
        printMaterial.shader = Shader.Find("Custom/SliceShader");

        // Come�a com a pe�a totalmente invis�vel, definindo a altura do corte na superf�cie da mesa.
        ResetPrint();
    }

    /// <summary>
    /// Reseta a simula��o, escondendo completamente a pe�a.
    /// </summary>
    public void ResetPrint()
    {
        // A altura inicial do corte � a posi��o Y da superf�cie da mesa no mundo.
        UpdatePrintHeight(0);
    }

    /// <summary>
    /// Atualiza a altura de "corte" do shader para revelar a pe�a.
    /// </summary>
    /// <param name="currentPrintHeight">A altura atual da impress�o em unidades do Unity (eixo Y), relativa � base.</param>
    public void UpdatePrintHeight(float currentPrintHeight)
    {
        if (printMaterial == null) return;

        // A altura do corte no mundo � a altura da base (mesa) + a altura atual da impress�o.
        float worldSliceHeight = printBedTransform.position.y + currentPrintHeight;

        // Define a propriedade "_SliceHeight" no shader.
        printMaterial.SetFloat(SliceHeightID, worldSliceHeight);
    }
}