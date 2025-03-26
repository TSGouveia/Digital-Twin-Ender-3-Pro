using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class OctoPrintAPI : MonoBehaviour
{
    private string apiUrl = "http://octopi.local/api/printer/command";
    private string loginUrl = "http://octopi.local/api/login";
    private string apiKey = "a7XjCKS9jXoBsnQPTng1O9gb-cddiTcMwM4Gvd4ArJQ";
    private string username = "rics";
    private string password = "ricsricsjabjab";
    private bool rememberMe = true;
    [SerializeField] private int messagesPerSecond = 100;

    OctoPrintWebSocket octoPrintWebSocket;

    void Start()
    {
        StartCoroutine(Login());
        octoPrintWebSocket = FindAnyObjectByType<OctoPrintWebSocket>();
    }

    IEnumerator Login()
    {
        string jsonPayload = "{\"user\": \"" + username + "\", \"pass\": \"" + password + "\", \"remember\": " + rememberMe.ToString().ToLower() + "}";
        Debug.Log("Login payload: " + jsonPayload);

        UnityWebRequest request = new UnityWebRequest(loginUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-Api-Key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login successful!");
            Debug.Log("Response: " + request.downloadHandler.text);

            // Parse the response to extract the session key
            var responseJson = JObject.Parse(request.downloadHandler.text);
            var sessionKey = responseJson["session"]?.ToString();

            InvokeRepeating(nameof(SendM114Command), 0f, 1f/messagesPerSecond);
            octoPrintWebSocket.StartWebSocket(sessionKey);
        }
        else if (request.responseCode == 403)
        {
            Debug.LogError("Login failed: Username/password mismatch, unknown user or deactivated account");
        }
        else
        {
            Debug.LogError("Login failed: " + request.error);
            Debug.LogError("Response Code: " + request.responseCode);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    void SendM114Command()
    {
        StartCoroutine(SendM114CommandCoroutine());
    }

    IEnumerator SendM114CommandCoroutine()
    {
        string jsonPayload = "{\"command\": \"M114\"}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-Api-Key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Comando M114 enviado com sucesso!");
        }
        else
        {
            Debug.LogError("Erro ao enviar comando: " + request.error);
        }
    }
}
