using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;//marshal
using System.Threading;
using System.Collections;
using System.Collections.Generic;



public class Chat : MonoBehaviour
{
    public ChattingPacket ChattingPacket = new ChattingPacket();

    private TcpClient socketChattingConnection;
    private NetworkStream ChattingStream;

    private Byte[] buffer = new byte[1024];
    public static bool move = false;

    // Start is called before the first frame update
    void Start()
    {
        ConnectToTcpServer(9001);
        ChattingPacket.UserID = "";
        ChattingPacket.ChattingMsg = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (move)
        {
            move = false;
            SendPackect(ChattingPacket);
        }
        if (ChattingStream.DataAvailable)
        {
            RecvMessageStruct(buffer);
        }
    }

    private void ConnectToTcpServer(int portnum)
    {
        try
        {
            if (portnum == 9001) // 채팅서버
            {
                socketChattingConnection = new TcpClient();
                socketChattingConnection.Connect("127.0.0.1", portnum);

                ChattingStream = socketChattingConnection.GetStream();
            }
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    public void SendPackect(System.Object struc)
    {
        if (socketChattingConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = socketChattingConnection.GetStream();

            byte[] buffer;
            buffer = Serialize(struc);
            if (stream.CanWrite)
            {
                // Write byte array to socketConnection stream.                 
                stream.Write(buffer, 0, buffer.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    public void RecvMessageStruct(Byte[] buffer)
    {
        if (socketChattingConnection == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = socketChattingConnection.GetStream();
            if (stream.CanRead)
            {
                // Write byte array to socketConnection stream.                 
                stream.Read(buffer, 0, buffer.Length);
            }
            DataPacket Recv = new DataPacket();
            Recv = Deserialize<DataPacket>(buffer);

        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }







    public byte[] Serialize(System.Object data)
    {
        try
        {
            //구조체 사이즈
            int structSize = Marshal.SizeOf(data);
            //구조체 사이즈 만큼 메모리 할당
            byte[] array = new byte[structSize];

            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(data, ptr, false);
            Marshal.Copy(ptr, array, 0, structSize);
            Marshal.FreeHGlobal(ptr);

            return array;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public T Deserialize<T>(byte[] buffer)
    {
        //구조체 사이즈
        int structSize = Marshal.SizeOf(typeof(T));
        if (structSize > buffer.Length)
        {
            throw new Exception();
        }
        IntPtr ptr = Marshal.AllocHGlobal(structSize);
        Marshal.Copy(buffer, 0, ptr, structSize);
        T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);
        return obj;
    }

}
