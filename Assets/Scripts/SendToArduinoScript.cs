using System;
using System.IO.Ports;
using UnityEngine;

// Beginner-friendly Unity -> Arduino sender.
// Protocol (one newline-terminated command): CTRL,<servoAngle 0-180>,<lightState 0|1>
public class SendToArduinoScript : MonoBehaviour
{
    [Header("Serial Connection")]
    public string portName = "COM3";
    public bool autoSelectPort = true;
    public int baudRate = 115200;
    [Min(10)] public int sendIntervalMs = 50;
    [Min(0.25f)] public float reconnectIntervalSeconds = 2f;
    [Min(0.25f)] public float readyTimeoutSeconds = 3f;
    public bool requireReadyHandshake = true;
    public bool verboseLogging = false;

    [Header("Unity Data Sources")]
    public Transform dataSource;
    public Transform rotationSource;

    [Header("Map X Position To Servo Angle")]
    public float xMin = -10f;
    public float xMax = 10f;
    private const float centerAngle = 67f;  // servo angle when the ball is in the middle
    private const float sweepAngle = 80f;   // degrees moved each side of center

    private SerialPort serialPort;
    private float nextSendTime;
    private float nextReconnectTime;
    private float readyDeadline;
    private bool hasWarnedMissingReferences;
    private bool hasWarnedWaitingForReady;
    private bool arduinoReady;

    void Start()
    {
        nextSendTime = Time.time;
        TryOpenPort();
    }

    void Update()
    {
        if (!IsPortOpen())
        {
            TryReconnectIfNeeded();
            return;
        }

        ReadIncomingSerialMessages();

        if (requireReadyHandshake && !arduinoReady)
        {
            if (!hasWarnedWaitingForReady)
            {
                Debug.LogWarning("Waiting for Arduino READY handshake on " + portName + ".");
                hasWarnedWaitingForReady = true;
            }

            if (Time.time >= readyDeadline)
            {
                Debug.LogWarning("READY handshake timed out. Reconnecting...");
                nextReconnectTime = Time.time + reconnectIntervalSeconds;
                ClosePort();
            }

            return;
        }

        float intervalSeconds = Mathf.Max(0.01f, sendIntervalMs / 1000f);
        if (Time.time < nextSendTime)
        {
            return;
        }

        SendControlPacket();

        // Keep cadence steady even if a frame is delayed.
        while (nextSendTime <= Time.time)
        {
            nextSendTime += intervalSeconds;
        }
    }

    void OnDisable()
    {
        ClosePort();
    }

    void OnApplicationQuit()
    {
        ClosePort();
    }

    private bool IsPortOpen()
    {
        return serialPort != null && serialPort.IsOpen;
    }

    private void TryReconnectIfNeeded()
    {
        if (Time.time < nextReconnectTime)
        {
            return;
        }

        TryOpenPort();
    }

    private void TryOpenPort()
    {
        ClosePort();

        string selectedPort = ResolvePortName();
        if (string.IsNullOrEmpty(selectedPort))
        {
            nextReconnectTime = Time.time + reconnectIntervalSeconds;
            Debug.LogWarning("No serial ports detected. Reconnect will be retried.");
            return;
        }

        try
        {
            serialPort = new SerialPort(selectedPort, baudRate)
            {
                NewLine = "\n",
                WriteTimeout = 100,
                ReadTimeout = 100,
                DtrEnable = true,
                RtsEnable = true
            };

            serialPort.Open();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();

            arduinoReady = false;
            hasWarnedWaitingForReady = false;
            readyDeadline = Time.time + readyTimeoutSeconds;
            serialPort.WriteLine("PING");

            portName = selectedPort;
            Debug.Log("Serial connected on " + selectedPort + " @ " + baudRate + " baud.");
        }
        catch (Exception e)
        {
            nextReconnectTime = Time.time + reconnectIntervalSeconds;
            Debug.LogWarning("Serial open failed: " + e.Message);
            Debug.LogWarning("Available ports: " + string.Join(", ", SerialPort.GetPortNames()));
            ClosePort();
        }
    }

    private void ReadIncomingSerialMessages()
    {
        if (!IsPortOpen())
        {
            return;
        }

        try
        {
            while (serialPort.BytesToRead > 0)
            {
                string line = serialPort.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string trimmed = line.Trim();
                if (trimmed.Equals("READY", StringComparison.OrdinalIgnoreCase))
                {
                    arduinoReady = true;
                    Debug.Log("Arduino handshake received (READY).");
                }
                else if (verboseLogging)
                {
                    Debug.Log("Arduino: " + trimmed);
                }
            }
        }
        catch (TimeoutException)
        {
            // No complete line available yet.
        }
        catch (Exception e)
        {
            Debug.LogWarning("Serial read failed: " + e.Message + ". Reconnecting...");
            arduinoReady = false;
            nextReconnectTime = Time.time + reconnectIntervalSeconds;
            ClosePort();
        }
    }

    private string ResolvePortName()
    {
        if (!autoSelectPort)
        {
            return portName;
        }

        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            return string.Empty;
        }

        Array.Sort(ports, StringComparer.OrdinalIgnoreCase);

        foreach (string candidate in ports)
        {
            string lower = candidate.ToLowerInvariant();
            if (lower.Contains("usb") || lower.Contains("acm") || lower.Contains("modem") || lower.Contains("serial"))
            {
                return candidate;
            }
        }

        return ports[0];
    }

    private void SendControlPacket()
    {
        if (dataSource == null || rotationSource == null) //changed to check roationSource
        {
            if (!hasWarnedMissingReferences)
            {
                Debug.LogWarning("Assign both dataSource and rotationSource in the Inspector."); //warning updated
                hasWarnedMissingReferences = true;
            }

            return;
        }

        hasWarnedMissingReferences = false;

        int servoAngle = MapXToServoAngle(dataSource.position.x);
        int lightState = rotationSource.eulerAngles.y > 180f ? 1 : 0; //changed for rotation
        string command = "CTRL," + servoAngle + "," + lightState;

        try
        {
            serialPort.WriteLine(command);

            if (verboseLogging)
            {
                Debug.Log("Sent: " + command);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Serial write failed: " + e.Message + ". Reconnecting...");
            nextReconnectTime = Time.time + reconnectIntervalSeconds;
            ClosePort();
        }
    }
    public void SendTone()
{
    Debug.Log("SendTone called. Port open: " + IsPortOpen() + ", ready: " + arduinoReady);

    if (!IsPortOpen() || (requireReadyHandshake && !arduinoReady)) return;

    try
    {
        serialPort.WriteLine("TONE");
        Debug.Log("TONE sent");
    }
    catch (Exception e)
    {
        Debug.LogWarning("Serial write failed: " + e.Message);
    }
}

    private int MapXToServoAngle(float xValue)
{
    if (Mathf.Approximately(xMin, xMax))
    {
        return Mathf.RoundToInt(centerAngle);
    }

    float normalized = Mathf.InverseLerp(xMin, xMax, xValue);
    float angle = centerAngle - (normalized - 0.5f) * 2f * sweepAngle;
    return Mathf.Clamp(Mathf.RoundToInt(angle), 0, 180);
}

    private void ClosePort()
    {
        arduinoReady = false;

        if (serialPort == null)
        {
            return;
        }

        try
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
        catch
        {
            // Ignore cleanup failures during shutdown/reconnect.
        }

        serialPort.Dispose();
        serialPort = null;
    }
}
