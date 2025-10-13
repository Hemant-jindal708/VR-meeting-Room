using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;

public class ScreenReceiver : MonoBehaviour
{
    [Header("Python Streamer Settings")]
    public string streamerIP = "172.16.211.23"; // Set Python streamer IP here
    public int streamerPort = 9999;            // Set Python streamer port here
    public float reconnectDelay = 3f;

    private TcpClient client;
    private NetworkStream stream;
    private Thread tcpReceiverThread;
    private bool connected = false;
    private bool attemptingConnection = false;

    private ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
    public RawImage rawImage;
    private Texture2D screenTexture;

    void Start()
    {
        screenTexture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        rawImage.texture = screenTexture;

        // Start connection thread
        Thread connectThread = new Thread(() => ConnectToServer(streamerIP, streamerPort));
        connectThread.IsBackground = true;
        connectThread.Start();
    }

    void Update()
    {
        while (frameQueue.TryDequeue(out byte[] frameData))
        {
            if (screenTexture.LoadImage(frameData))
            {
                screenTexture.Apply();
            }
            else
            {
                Debug.LogWarning("Invalid frame received");
            }
        }
    }

    void ConnectToServer(string ip, int port)
    {
        if (attemptingConnection) return;
        attemptingConnection = true;

        while (!connected)
        {
            try
            {
                client = new TcpClient();
                client.Connect(ip, port);
                stream = client.GetStream();
                connected = true;
                Debug.Log("Connected to Python streamer!");

                // Start receiving frames
                tcpReceiverThread = new Thread(ReceiveFrames);
                tcpReceiverThread.IsBackground = true;
                tcpReceiverThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError("Connection failed: " + e.Message);
                connected = false;
                Thread.Sleep((int)(reconnectDelay * 1000));
            }
        }

        attemptingConnection = false;
    }

    byte[] ReadExactly(NetworkStream stream, int size)
    {
        byte[] buffer = new byte[size];
        int offset = 0;
        while (offset < size)
        {
            int read = stream.Read(buffer, offset, size - offset);
            if (read == 0) throw new Exception("Disconnected");
            offset += read;
        }
        return buffer;
    }

    void ReceiveFrames()
    {
        try
        {
            while (connected)
            {
                byte[] lengthBuffer = ReadExactly(stream, 8);
                long frameSize = BitConverter.ToInt64(lengthBuffer, 0);
                byte[] frameData = ReadExactly(stream, (int)frameSize);
                frameQueue.Enqueue(frameData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Frame receive error: " + e.Message);
            connected = false;

            // Try reconnecting automatically
            Thread reconnectThread = new Thread(() => ConnectToServer(streamerIP, streamerPort));
            reconnectThread.IsBackground = true;
            reconnectThread.Start();
        }
    }

    void OnApplicationQuit()
    {
        connected = false;
        stream?.Close();
        client?.Close();
        tcpReceiverThread?.Abort();
    }
}
