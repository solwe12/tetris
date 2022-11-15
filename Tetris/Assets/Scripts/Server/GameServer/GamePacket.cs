//using UnityEngine;
/*
using System.Collections;
using System.Runtime.InteropServices;//marshal
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

//��� �Ǻ��� ������
public enum LoginPacket { NONE = 0, LOGIN_req, LOGIN_success, LOGIN_fail };



//����Ʈ������ �ٲ��ֱ� ����. serialize(Ŭ���� ���� �� �־�� �ϸ� �Ƹ� �е���Ʈ������ �� �ִ� �� ����)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketHeader
{
    private int PacketSize;
    public LoginPacket Type;

    public PacketHeader()
    {
        Type = LoginPacket.NONE;
        PacketSize = 0;
    }

    public PacketHeader(LoginPacket _Type, int _PacketSize)
    {

        Type = _Type;
        PacketSize = _PacketSize;

    }

    public static byte[] Serialize(Object data)
    {
        try
        {
            //����ü ������
            int structSize = Marshal.SizeOf(data);
            //����ü ������ ��ŭ �޸� �Ҵ�
            byte[] array = new byte[structSize];

            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            //
            Marshal.StructureToPtr(data, ptr, false);
            Marshal.Copy(ptr, array, 0, structSize);
            Marshal.FreeHGlobal(ptr);

            return array;
        }
        catch
        {
            return null;
        }
    }

    public static T Deserialize<T>(byte[] buffer)
    {
            //����ü ������
            int structSize = Marshal.SizeOf(typeof(T));
            if(structSize > buffer.Length)
            {
                throw new Exception();
            }
            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            Marshal.Copy(buffer, 0, ptr, structSize);
            T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return obj;
    }
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class Login_info : PacketHeader
{

    // ���ڸ� ������ �� ���, string(c#) -> char(c++)�� ��ȯ�ؾ� �� �� ���� �� �־�� �Ѵ�
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    private string ID_value;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    private string PW_value;

    public Login_info() { }
    public Login_info(string _ID, string _PW)
        : base(LoginPacket.LOGIN_req, Marshal.SizeOf(new Login_info()))
    {
        ID_value = _ID;
        PW_value = _PW;
    }

};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class Login_Success : PacketHeader
{
    private int Userserial;

    public Login_Success() { }
    public Login_Success(int _Userserial)
        : base(LoginPacket.LOGIN_success, Marshal.SizeOf(new Login_Success()))
    {
        Userserial = _Userserial;
    }

    public int get_Userserial()
    {
        return Userserial;
    }
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class Login_Fail : PacketHeader
{

    public Login_Fail()
        : base(LoginPacket.LOGIN_fail, 8)
    {
    }

};

namespace MyLogin_Packet
{
    [Serializable]
    public class Login : PacketHeader
    {
        public string id_str { get; set; }
        public string pw_str { get; set; }

    }
}

namespace MyLogin_Packet
{
    [Serializable]
    public class MemberRegister : PacketHeader
    {
        public string id_str { get; set; }
        public string pw_str { get; set; }
        public string nickname_str { get; set; }
    }
}

*/