using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ScreenReceiver : NetworkBehaviour
{
    [Header("Python Streamer Settings")]
    public string streamerIP = "172.16.211.23";
    public int streamerPort = 9999;
    public float reconnectDelay = 3f;

    [Header("UI")]
    public RawImage rawImage;

    private TcpClient client;
    private NetworkStream stream;
    private Thread tcpReceiverThread;
    private bool connected = false;
    private bool attemptingConnection = false;

    private ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
    private Texture2D screenTexture;

    // Frame chunk assembly data
    private List<byte> receivedFrameBuffer = new List<byte>();
    private int expectedChunks = 0;
    private int receivedChunks = 0;

    const int MaxChunkSize = 950; // NGO safe size
    const int TextureWidth = 1280; // optimize bandwidth (was 1920)
    const int TextureHeight = 720; // optimize bandwidth (was 1080)
    const int JpegQuality = 50; // 50% compression - good balance

    void Start()
    {
        screenTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGB24, false);
        rawImage.texture = screenTexture;

        if (IsOwner)
        {
            Thread connectThread = new Thread(() => ConnectToServer(streamerIP, streamerPort));
            connectThread.IsBackground = true;
            connectThread.Start();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Apply next received frame
        while (frameQueue.TryDequeue(out byte[] frameData))
        {
            try
            {
                if (screenTexture.LoadImage(frameData))
                {
                    screenTexture.Apply();

                    // Send compressed frame through relay
                    byte[] compressed = screenTexture.EncodeToJPG(JpegQuality);
                    SendCompressedFrame(compressed);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Frame decode error: " + e.Message);
            }
        }
    }

    // ======================== CHUNK STREAMING ========================

    void SendCompressedFrame(byte[] data)
    {
        int totalChunks = Mathf.CeilToInt((float)data.Length / MaxChunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int chunkSize = Math.Min(MaxChunkSize, data.Length - i * MaxChunkSize);
            byte[] chunk = new byte[chunkSize];
            Buffer.BlockCopy(data, i * MaxChunkSize, chunk, 0, chunkSize);

            SendFrameChunkServerRpc(chunk, i, totalChunks);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SendFrameChunkServerRpc(byte[] chunk, int chunkIndex, int totalChunks, ServerRpcParams rpcParams = default)
    {
        // When first chunk arrives, reset
        if (chunkIndex == 0)
        {
            receivedFrameBuffer.Clear();
            receivedChunks = 0;
            expectedChunks = totalChunks;
        }

        receivedFrameBuffer.AddRange(chunk);
        receivedChunks++;

        // When full frame received -> broadcast to all clients
        if (receivedChunks >= expectedChunks)
        {
            byte[] fullImage = receivedFrameBuffer.ToArray();
            receivedFrameBuffer.Clear();
            receivedChunks = 0;
            expectedChunks = 0;

            // Send to everyone else
            UpdateTextureClientRpc(fullImage);
        }
    }

    [ClientRpc]
    void UpdateTextureClientRpc(byte[] imageBytes, ClientRpcParams rpcParams = default)
    {
        try
        {
            Texture2D tex = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGB24, false);
            tex.LoadImage(imageBytes);
            tex.Apply();

            if (rawImage != null)
                rawImage.texture = tex;
        }
        catch (Exception e)
        {
            Debug.LogError("Error applying received frame: " + e.Message);
        }
    }

    // ======================== TCP STREAM INPUT ========================

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

            // Auto reconnect
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
