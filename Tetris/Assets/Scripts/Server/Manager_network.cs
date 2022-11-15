/*
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Console;
using System.Runtime.InteropServices;//marshal

public class Manager_network : MonoBehaviour
{
    public Text EX;

    UdpClient client = new UdpClient();

    public static DataPacket SendPacket = new DataPacket();
    public static DataPacket RecvPacket = new DataPacket();
    public static Vector3 moveDir2 = Vector3.zero;
    public static bool isRotate2 = false;

    public static bool move = false;

    public void Start()
    {
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.EnableBroadcast = true;
        SendPacket.Type = 13;
    }

    public void Update()
    {
        //보내기
        if (move) // Input.GetKeyDown(KeyCode.Space)
        {
            //move = false;
            //Send();
        }


        //받기
        if (client.Available > 0)
        {
            
            IPEndPoint epRemote = new IPEndPoint(IPAddress.Broadcast, 9999);

            byte[] bytes = client.Receive(ref epRemote);
            RecvPacket = Deserialize<DataPacket>(bytes);
            Debug.Log(RecvPacket.userID);
            Debug.Log(RecvPacket.move);
            Debug.Log(RecvPacket.color);
            if (RecvPacket.userID != SendPacket.userID)
            {

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

                EX.text = RecvPacket.userID;
            }
        }
    }

    public void Send()
    {
        byte[] datagram = Serialize(SendPacket);
        client.Send(datagram, datagram.Length, "192.168.0.3", 9999);
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
*/