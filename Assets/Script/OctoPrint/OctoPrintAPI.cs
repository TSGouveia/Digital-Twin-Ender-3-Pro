using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Text;

public class OctoPrintAPI : MonoBehaviour
{
    [Header("OctoPrint Connection Settings")]
    [SerializeField] private string octoPrintAddress = "http://octopi.local"; // IP do script Python
    [SerializeField] private string apiKey = "API-KEY-HERE";

    [Header("Polling Settings")]
    [Tooltip("Intervalo em segundos entre pedidos M114 (evite valores menores que 1.0 para n�o travar o buffer)")]
    [SerializeField] private float pollingInterval = 2.0f;

    private OctoPrintWebSocket octoPrintWebSocket;
    private bool isPolling = false;

    // Endere�o correto para comandos de impressora no OctoPrint
    private string ApiCommandUrl => $"{octoPrintAddress}/api/printer/command";
    private string LoginUrl => $"{octoPrintAddress}/api/login";

    void Start()
    {
        octoPrintWebSocket = GetComponent<OctoPrintWebSocket>();
        if (octoPrintWebSocket == null)
        {
            octoPrintWebSocket = FindFirstObjectByType<OctoPrintWebSocket>();
        }

        if (octoPrintWebSocket == null)
        {
            Debug.LogError("Componente OctoPrintWebSocket n�o encontrado!");
            return;
        }

        StartCoroutine(LoginAndInitialize());
    }

    private void OnApplicationQuit()
    {
        isPolling = false;
    }

    IEnumerator LoginAndInitialize()
    {
        // O payload de login passivo � o que o script Python usa para pegar a sess�o
        string jsonPayload = "{\"passive\": true}";

        using (UnityWebRequest request = new UnityWebRequest(LoginUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Api-Key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseJson = JObject.Parse(request.downloadHandler.text);
                string sessionKey = responseJson["session"]?.ToString();
                string userName = responseJson["name"]?.ToString();

                if (string.IsNullOrEmpty(sessionKey))
                {
                    Debug.LogError("Sess�o n�o encontrada na resposta do login.");
                    yield break;
                }

                // Inicia o WebSocket passando os dados de autentica��o
                octoPrintWebSocket.StartWebSocket(userName, sessionKey, octoPrintAddress);

                // Inicia o loop de requisi��o de coordenadas
                isPolling = true;
                StartCoroutine(PollPositionCoroutine());
            }
            else
            {
                Debug.LogError($"Erro no Login: {request.responseCode} - {request.error}");
            }
        }
    }

    IEnumerator PollPositionCoroutine()
    {
        while (isPolling)
        {
            // O comando M114 faz a impressora devolver "X:0.00 Y:0.00..." no log do WebSocket
            yield return StartCoroutine(SendCommandCoroutine("M114"));
            yield return new WaitForSeconds(pollingInterval);
        }
    }

    private IEnumerator SendCommandCoroutine(string command)
    {
        // Formato exato que o OctoPrint espera para comandos GCODE
        string jsonPayload = $"{{\"command\": \"{command}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(ApiCommandUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Api-Key", apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Comando {command} falhou: {request.error}");
            }
        }
    }
}