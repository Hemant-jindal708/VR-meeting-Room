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

    // Chunk assembly data
    private List<byte> receivedFrameBuffer = new List<byte>();
    private int expectedChunks = 0;
    private int receivedChunks = 0;

    const int MaxChunkSize = 1019;  // NGO-safe
    const int TextureWidth = 1920;
    const int TextureHeight = 1080;

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
        if (!IsOwner) return;

        while (frameQueue.TryDequeue(out byte[] frameData))
        {
            if (screenTexture.LoadImage(frameData))
            {
                screenTexture.Apply();

                // Compress frame for network
                byte[] compressed = screenTexture.EncodeToJPG(40);
                SendCompressedFrame(compressed);
            }
        }
    }

    // --------- Chunked Sending ----------
    void SendCompressedFrame(byte[] data)
    {
        int totalChunks = Mathf.CeilToInt((float)data.Length / MaxChunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int chunkSize = Math.Min(MaxChunkSize, data.Length - i * MaxChunkSize);
            byte[] chunk = new byte[chunkSize];
            Buffer.BlockCopy(data, i * MaxChunkSize, chunk, 0, chunkSize);
            SendFrameChunkServerRpc(chunk, i, totalChunks, new NetworkObjectReference(GetComponent<NetworkObject>()));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SendFrameChunkServerRpc(byte[] chunk, int chunkIndex, int totalChunks, NetworkObjectReference objRef)
    {
        if (chunkIndex == 0)
        {
            receivedFrameBuffer.Clear();
            receivedChunks = 0;
            expectedChunks = totalChunks;
        }

        receivedFrameBuffer.AddRange(chunk);
        receivedChunks++;

        if (receivedChunks >= expectedChunks)
        {
            ApplyReceivedFrame(receivedFrameBuffer.ToArray());
            receivedFrameBuffer.Clear();
            receivedChunks = 0;
            expectedChunks = 0;
        }
        ReceiveFrameChunkClientRpc(chunk, chunkIndex, totalChunks, objRef);
    }
    [ClientRpc]
    void ReceiveFrameChunkClientRpc(byte[] chunk, int chunkIndex, int totalChunks, NetworkObjectReference objRef)
    {
        if (!objRef.TryGet(out NetworkObject netObj))
        {
            if(netObj != GetComponent<NetworkObject>())
                return;
        }
        

        if (chunkIndex == 0)
        {
            receivedFrameBuffer.Clear();
            receivedChunks = 0;
            expectedChunks = totalChunks;
        }

        receivedFrameBuffer.AddRange(chunk);
        receivedChunks++;

        if (receivedChunks >= expectedChunks)
        {
            ApplyReceivedFrame(receivedFrameBuffer.ToArray());
            receivedFrameBuffer.Clear();
            receivedChunks = 0;
            expectedChunks = 0;
        }
    }
    void ApplyReceivedFrame(byte[] imageBytes)
    {
        try
        {
            Texture2D tex = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGB24, false);
            tex.LoadImage(imageBytes);
            tex.Apply();
            rawImage.texture = tex;
        }
        catch (Exception e)
        {
            Debug.LogError("Error applying received frame: " + e.Message);
        }
    }

    // --------- TCP Receiver ----------
    void ConnectToServer(string ip, int port)
    {
        if (!IsOwner) return;
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
                Debug.Log("✅ Connected to Python streamer");

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
