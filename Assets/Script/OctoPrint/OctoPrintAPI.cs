using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class OctoPrintAPI : MonoBehaviour
{
    [Header("OctoPrint Connection Settings")]
    [SerializeField] private string octoPrintAddress = "http://octopi.local";
    [SerializeField] private string apiKey = "SEU_API_KEY_AQUI"; // Coloque sua chave no Inspector
    [SerializeField] private string username = "USER";
    [SerializeField] private string password = "PASSWORD";
    [SerializeField] private bool rememberMe = true;

    [Header("Polling Settings")]
    [Tooltip("Quantas vezes por segundo pedir a posição da impressora.")]
    [SerializeField] private float pollingRatePerSecond = 5.0f; // 5 vezes por segundo é um bom começo

    private string sessionKey;
    private OctoPrintWebSocket octoPrintWebSocket;
    private bool isPolling = false;

    private string ApiCommandUrl => $"{octoPrintAddress}/api/printer/command";
    private string LoginUrl => $"{octoPrintAddress}/api/login";

    void Start()
    {
        octoPrintWebSocket = FindAnyObjectByType<OctoPrintWebSocket>();
        if (octoPrintWebSocket == null)
        {
            Debug.LogError("Componente OctoPrintWebSocket não encontrado na cena!");
            return;
        }

        StartCoroutine(LoginAndInitialize());
    }

    private void OnApplicationQuit()
    {
        // Para o loop de polling quando a aplicação fechar
        isPolling = false;
    }

    IEnumerator LoginAndInitialize()
    {
        string jsonPayload = $"{{\"user\": \"{username}\", \"pass\": \"{password}\", \"remember\": {rememberMe.ToString().ToLower()}}}";

        using (UnityWebRequest request = new UnityWebRequest(LoginUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Api-Key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Login bem-sucedido! Resposta: " + request.downloadHandler.text);

                var responseJson = JObject.Parse(request.downloadHandler.text);
                sessionKey = responseJson["session"]?.ToString();

                if (string.IsNullOrEmpty(sessionKey))
                {
                    Debug.LogError("Login bem-sucedido, mas nenhuma chave de sessão foi recebida!");
                    yield break;
                }

                octoPrintWebSocket.StartWebSocket(username, sessionKey, octoPrintAddress);

                // Inicia o polling da posição
                isPolling = true;
                StartCoroutine(PollPositionCoroutine());
            }
            else
            {
                Debug.LogError($"Falha no login com código: {request.responseCode}. Erro: {request.error}");
                Debug.LogError("Corpo da Resposta: " + request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Corrotina que envia o comando M114 em um loop para obter a posição da impressora.
    /// </summary>
    IEnumerator PollPositionCoroutine()
    {
        Debug.Log("Iniciando polling de posição com M114...");
        while (isPolling)
        {
            // Envia o comando para obter a posição atual
            yield return StartCoroutine(SendCommandCoroutine("M114"));

            // Espera pelo intervalo definido antes de enviar o próximo pedido
            yield return new WaitForSeconds(1.0f / pollingRatePerSecond);
        }
        Debug.Log("Polling de posição interrompido.");
    }

    private IEnumerator SendCommandCoroutine(string command)
    {
        string jsonPayload = $"{{\"command\": \"{command}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(ApiCommandUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Api-Key", apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Falha ao enviar comando '{command}'. Erro: {request.error}");
            }
            // Não precisa de um log de sucesso aqui para não poluir o console
        }
    }
}