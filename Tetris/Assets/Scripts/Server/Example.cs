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

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DataPacket
{
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ // color ���� ��� �� ���� �Ǻ� ����
    public int color;
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int move; // 0 ���� 1������ 2�Ʒ� 3�����̽��� 4ȸ�� 5�ڵ�
}

public class Example : MonoBehaviour
{
    public static DataPacket SendPacket = new DataPacket();
    public static DataPacket RecvPacket = new DataPacket();
    public static Vector3 moveDir2 = Vector3.zero;
    public static bool isRotate2 = false;

    private TcpClient socketGameConnection;
    private NetworkStream Stream;

    private Byte[] buffer = new byte[1024];
    public static bool move = false;

    // Start is called before the first frame update
    void Start()
    {
        ConnectToTcpServer(7777);
        SendPacket.move = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if(move)
        {
            move = false;
            SendPackect(SendPacket);
        }
        if(Stream.DataAvailable)
        {
            RecvMessageStruct(buffer);
        }
    }

    private void ConnectToTcpServer(int portnum)
    {
        try
        {
            if (portnum == 7777) // ���Ӽ���
            {
                socketGameConnection = new TcpClient();
                socketGameConnection.Connect("127.0.0.1", portnum);

                Stream = socketGameConnection.GetStream();
            }
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    public void SendPackect(System.Object struc)
    {
        if (socketGameConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = socketGameConnection.GetStream();

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
        if (socketGameConnection == null)
        {
            return;
        }

        try
            {
            // Get a stream object for writing.             
            NetworkStream stream = socketGameConnection.GetStream();
            if (stream.CanRead)
            {
                // Write byte array to socketConnection stream.                 
                stream.Read(buffer, 0, buffer.Length);
            }
            DataPacket Recv = new DataPacket();
            Recv = Deserialize<DataPacket>(buffer);
            RecvPacket = Recv;

            switch (RecvPacket.move)
            {
                case 0:
                    moveDir2.x = -1;
                    break;
                case 1:
                    moveDir2.x = 1;
                    break;
                case 2:
                    moveDir2.y = -1;
                    break;
                case 3:
                    while (GameObject.Find("Stage").GetComponent<Stage>().MoveTetromino(Vector3.down, false, 1))
                    {

                    }
                    break;
                case 4:
                    isRotate2 = true;
                    break;
                case 5:
                    moveDir2.y = -1;
                    break;
                default:
                    break;
            }

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
            //����ü ������
            int structSize = Marshal.SizeOf(data);
            //����ü ������ ��ŭ �޸� �Ҵ�
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
        //����ü ������
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

    void OnApplicationQuit()
    {
        if(socketGameConnection!=null)
        {
            socketGameConnection.Close();
        }
    }
}
