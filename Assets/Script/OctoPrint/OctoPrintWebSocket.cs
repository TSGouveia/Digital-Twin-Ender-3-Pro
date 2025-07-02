using System;
using System.Text.RegularExpressions;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json.Linq;

public class OctoPrintWebSocket : MonoBehaviour
{
    private WebSocket websocket;
    private NozzleController nozzleController;

    // Store auth details for re-authentication
    private string username;
    private string sessionKey;

    // Compile Regex once for performance
    private static readonly Regex PositionRegex = new Regex(@"X:(?<x>[-+]?[0-9]*\.?[0-9]+) Y:(?<y>[-+]?[0-9]*\.?[0-9]+) Z:(?<z>[-+]?[0-9]*\.?[0-9]+)");

    private void Start()
    {
        nozzleController = FindAnyObjectByType<NozzleController>();
        if (nozzleController == null)
        {
            Debug.LogError("NozzleController component not found in the scene!");
        }
    }

    public async void StartWebSocket(string user, string session, string octoPrintAddress)
    {
        if (websocket != null && websocket.State != WebSocketState.Closed)
        {
            Debug.LogWarning("WebSocket is already connecting or connected.");
            return;
        }

        // Store credentials for potential re-authentication
        this.username = user;
        this.sessionKey = session;

        // Construct WebSocket URL from base address
        string wsAddress = octoPrintAddress.Replace("http://", "ws://").Replace("https://", "wss://");
        string socketURL = $"{wsAddress}/sockjs/websocket";

        websocket = new WebSocket(socketURL);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection opened. Authenticating...");
            SendAuthMessage();
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            // OctoPrint often sends an 'o' frame on open, ignore it
            if (message == "o") return;
            HandleMessage(message);
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket Connection closed.");
        };

        await websocket.Connect();
    }

    private void Update()
    {
        // Required by NativeWebSocket to dispatch messages on the main thread
        websocket?.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }

    private void SendAuthMessage()
    {
        // Use the stored session key for authentication
        string authPayload = $"{{\"auth\": \"{this.username}:{this.sessionKey}\"}}";
        websocket.SendText(authPayload);
    }

    private void HandleMessage(string message)
    {
        // The actual payload is often inside a JSON array with a single element
        if (message.StartsWith("a["))
        {
            message = message.Substring(2, message.Length - 4); // Strip "a[\"" and "\"]"
            message = message.Replace("\\\"", "\""); // Un-escape quotes
        }

        try
        {
            var payload = JObject.Parse(message);

            if (payload["reauthRequired"] != null)
            {
                Debug.LogWarning("Re-authentication required. Re-sending auth message.");
                SendAuthMessage(); // CRITICAL FIX: Use the correct auth method
                return;
            }

            if (payload["current"]?["logs"] is JArray logs)
            {
                foreach (var log in logs)
                {
                    ProcessTerminalMessage(log.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            // Don't log errors for non-JSON messages like 'h' (heartbeat)
            if (message != "h")
            {
                Debug.LogWarning($"Could not parse WebSocket message as JSON. Message: {message} | Error: {ex.Message}");
            }
        }
    }

    private void ProcessTerminalMessage(string message)
    {
        // Example message: "Recv: ok P15 B3.9 X:10.00 Y:20.00 Z:5.00 E:0.00 Count X:800 Y:1600 Z:2000"
        Match match = PositionRegex.Match(message);

        if (match.Success)
        {
            // Use InvariantCulture to ensure '.' is used as the decimal separator
            float x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
            float z = float.Parse(match.Groups["z"].Value, System.Globalization.CultureInfo.InvariantCulture);

            // Map printer coordinates (Z-up) to Unity coordinates (Y-up)
            // Printer X -> Unity X
            // Printer Y -> Unity Z
            // Printer Z -> Unity Y
            nozzleController.SetNozzlePosition(new Vector3(x, z, y));
        }
    }
}