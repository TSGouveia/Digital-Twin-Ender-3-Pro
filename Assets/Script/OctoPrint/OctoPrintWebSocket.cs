using System;
using System.Text.RegularExpressions;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json.Linq;

public class OctoPrintWebSocket : MonoBehaviour
{
    private WebSocket websocket;
    private string octoprintSocketURL = "ws://octopi.local/sockjs/websocket";
    private NozzleController nozzleController;

    private void Start()
    {
        nozzleController = FindAnyObjectByType<NozzleController>();
    }
    public async void StartWebSocket(string sessionKey)
    {
        websocket = new WebSocket(octoprintSocketURL);

        websocket.OnOpen += () =>
        {
            Debug.Log("Socket Open...");
            websocket.SendText("{\"auth\": \"rics:" + sessionKey + "\"}");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
        };

        await websocket.Connect();
    }

    void Update()
    {
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    private void HandleMessage(string message)
    {
        try
        {
            var payload = JObject.Parse(message);

            if (payload["connected"] != null)
            {
                Debug.Log("Connected to WebSockets");
                return;
            }

            if (payload["reauthRequired"] != null)
            {
                Debug.Log("Re-authentication required: " + payload["reauthRequired"]["reason"]);
                websocket.SendText("{\"auth\": \"rics:ricsricsjabjab\"}");
                return;
            }

            if (payload["current"] != null)
            {
                var current = payload["current"];
                var logs = current["logs"];

                if (logs != null)
                {
                    foreach (var log in logs)
                    {
                        Debug.Log("Terminal Message: " + log.ToString());
                        ProcessTerminalMessage(log.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse message: " + ex.Message);
        }
    }

    private void ProcessTerminalMessage(string message)
    {
        // Regex to match the X, Y, and Z values in the terminal message
        Regex regex = new Regex(@"X:(?<x>[-+]?[0-9]*\.?[0-9]+) Y:(?<y>[-+]?[0-9]*\.?[0-9]+) Z:(?<z>[-+]?[0-9]*\.?[0-9]+)");
        Match match = regex.Match(message);

        if (match.Success)
        {
            float x = float.Parse(match.Groups["x"].Value);
            float y = float.Parse(match.Groups["y"].Value);
            float z = float.Parse(match.Groups["z"].Value);

            // Call the function with the extracted values
            nozzleController.SetNozzlePosition(new Vector3(x, z, y));
        }
    }

    public bool IsConnected()
    {
        return websocket != null && websocket.State == WebSocketState.Open;
    }
}
