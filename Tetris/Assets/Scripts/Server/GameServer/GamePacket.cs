//using UnityEngine;
/*
using System.Collections;
using System.Runtime.InteropServices;//marshal
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

//헤더 판별용 열거형
public enum LoginPacket { NONE = 0, LOGIN_req, LOGIN_success, LOGIN_fail };



//바이트형으로 바꿔주기 위함. serialize(클래스 위에 써 주어야 하며 아마 패딩비트때문에 써 주는 것 같음)
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
            //구조체 사이즈
            int structSize = Marshal.SizeOf(data);
            //구조체 사이즈 만큼 메모리 할당
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
            //구조체 사이즈
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

    // 문자를 보내야 할 경우, string(c#) -> char(c++)로 변환해야 할 때 위에 써 주어야 한다
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