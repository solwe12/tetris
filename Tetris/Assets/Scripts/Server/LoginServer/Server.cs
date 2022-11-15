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

/*
������ int��bool�� 4����Ʈ ���� string�� SizeConst�� ũ�� ���� ����
[MarshalAs(UnmanagedType.Bool)] //4����Ʈ
public bool isLogin;
[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2)]
public string istest;
[MarshalAs(UnmanagedType.I1)] //4����Ʈ
public int istest2;
[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2)]
public string istest3;
 */


[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct userInfo
{
    //���..? Ÿ��
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int Type;

    //������
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public string ID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public string PW;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
    public string ErrorMsg;
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int ProfileType;
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int WinNum;
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int LoseNum;
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IsReady
{
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int Type;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public string userID;
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ChattingPacket
{
    [MarshalAs(UnmanagedType.I1)] //4����Ʈ
    public int Type;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public string UserID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
    public string ChattingMsg;
}


public class Server : MonoBehaviour
{
    //ENUM
    private int NONE = 0; // �ϴ��� �Ⱦ�
    private string LOGIN = "LOGIN";
    private int LOGIN_req = 1;
    private int LOGIN_success = 2;
    private int LOGIN_fail = 3;
    private string SIGNUP = "SIGNUP";
    private int SIGNUP_req = 4;
    private int SIGNUP_success = 5;
    private int SIGNUP_fail = 6;
    private string ISREADY = "ISREADY";
    private int ISGAME_req = 10;
    private int ISGAME_notReady = 11;
    private int ISGAME_ready = 12;
    
    [Header("LoginArea")]
    public GameObject LoginPanel;
    public InputField inputField_ID;
    public InputField inputField_PW;
    public Text ErrorMsg;
    public Text ServerConnect;

    private bool isCheck = false;

    [Header("WatingArea")]
    public GameObject WaitingPanel;
    public GameObject OptionPanel;
    public GameObject InfoPanel; 
    public Text userID;
    public Text userState;
    public Image ProfileImage;
    public Sprite Profile0;
    public Sprite Profile1;
    public Sprite Profile2;
    public Sprite Profile3;
    public Sprite Profile4;
    public Sprite Profile5;
    public Sprite Profile6;
    public Sprite Profile7;
    public Text WinNum;
    public Text LoseNum;
    public bool isWating;
    //ä��
    public InputField SendInput;
    public RectTransform ChatContent;
    public Text ChatText;
    public ScrollRect ChatScrollRect;
    public Slider BGM;
    public Slider SoundEffect;

    [Header("SignUpArea")]
    public GameObject SignUpPanel;
    public InputField inputField_ID_2;
    public InputField inputField_PW_2;
    public Text ErrorMsg_2;
    public Text ServerConnect_2;
    public Dropdown ProfileNum;

    [Header("Audio")]
    public AudioClip BgmSound1;
    public AudioClip BgmSound2;
    public AudioClip EffectSound;
    private AudioSource audio;
    private AudioSource audio2;
    



    //LoginPacekt, ReadyPacket
    public static userInfo user = new userInfo();
    public IsReady isReady = new IsReady();
    public ChattingPacket ChattingPacket = new ChattingPacket();

    //LoginServer, GameServer TcpClient
    private TcpClient socketLoginConnection;
    private TcpClient socketWaitingConnection;
    private TcpClient socketChattingConnection;
    private NetworkStream WaitingStream;
    private NetworkStream ChattingStream;
    //
    //
    private Byte[] buffer = new byte[1024];





    public void Start()
    {
        DontDestroyOnLoad(gameObject);
        ConnectToTcpServer(9090);
        LoginPanel.SetActive(true);
        WaitingPanel.SetActive(false);
        SignUpPanel.SetActive(false);

        isWating = true;
        this.audio = this.gameObject.AddComponent<AudioSource>();
        this.audio.loop = false;
        this.audio.volume = 0.1f;
        this.audio2 = this.gameObject.AddComponent<AudioSource>();
        this.audio2.loop = false;
        this.audio2.volume = 0.1f;
    }

    

    public void Update()
    {
        // ä�� ��Ŷ �ޱ�
        if (socketChattingConnection != null && ChattingStream.DataAvailable)
        {
            RecvMessageStruct(buffer, "Chatting");
        }
        //���Ӵ�� ��Ŷ �ޱ�
        if (socketWaitingConnection != null && WaitingStream.DataAvailable)
        {   
            RecvMessageStruct(buffer, ISREADY);
        }

        
        if (this.audio.isPlaying == false && isWating)
        {
            this.audio.clip = this.BgmSound1;
            this.audio.Play();
        }

        if(Input.GetMouseButtonDown(0))
        {
            if (this.audio2.isPlaying == false)
            {
                this.audio2.clip = this.EffectSound;
                this.audio2.Play();
            }
            else
            {
                this.audio2.Stop();
                this.audio2.clip = this.EffectSound;
                this.audio2.Play();
            }
        }

        audio.volume = BGM.value;
        audio2.volume = SoundEffect.value;
    }

    //ȸ������ â ����
    public void SignupPanel()
    {
        LoginPanel.SetActive(false);
        inputField_ID_2.text = "";
        inputField_PW_2.text = "";
        SignUpPanel.SetActive(true);
        
    }

    // ȸ������ ������ ������
    public void Signup()
    {
        isCheck = true;

        if (isCheck)
        {
            user.Type = SIGNUP_req;
            user.ID = inputField_ID_2.text;
            user.PW = inputField_PW_2.text;
            user.ErrorMsg = "";
            user.ProfileType = ProfileNum.value;
            user.WinNum = 0;
            user.LoseNum = 0;
            //����ü�� ������
            SendPackect(user, SIGNUP);
            RecvMessageStruct(buffer, SIGNUP);

            //�ʱ�ȭ
            isCheck = false;
        }
    }
    public void Back()
    {
        inputField_ID.text = "";
        inputField_PW.text = "";
        LoginPanel.SetActive(true);
        SignUpPanel.SetActive(false);
    }

    // ��ư�� ������ �� üũ
    public void Login()
    {
        isCheck = true;

        if (isCheck)
        {
            user.Type = LOGIN_req;
            user.ID = inputField_ID.text;
            user.PW = inputField_PW.text;
            user.ErrorMsg = "";
            //����ü�� ������
            SendPackect(user, LOGIN);
            RecvMessageStruct(buffer, LOGIN);

            //�ʱ�ȭ
            isCheck = false;
        }
    }
    public void LoginOut()
    {
        if (socketLoginConnection != null)
        {
            socketLoginConnection.Close();

            if(socketWaitingConnection != null)
                socketWaitingConnection.Close();

            LoginPanel.SetActive(true);
            WaitingPanel.SetActive(false);

            ConnectToTcpServer(9090);
        }
        if (socketChattingConnection != null)
        {
            ChattingPacket.Type = 3;
            ChattingPacket.UserID = userID.text;
            ChattingPacket.ChattingMsg = "";
            SendPackect(ChattingPacket, "Chatting");

            socketChattingConnection.Close();
            ChatText.text = "";
        }
    }

    public void InOption()
    {
        OptionPanel.SetActive(true);
    }

    public void OutOption()
    {
        OptionPanel.SetActive(false);
    }

    public void InInfo()
    {
        InfoPanel.SetActive(true);
    }

    public void OutInfo()
    {
        InfoPanel.SetActive(false);
    }
    public void SearchGame()
    {
        if(socketWaitingConnection == null || socketWaitingConnection.Connected==false)
        {
            ConnectToTcpServer(9000);

            isReady.Type = ISGAME_req;
            isReady.userID = user.ID;
            SendPackect(isReady, ISREADY);
        }
    }

    public void CancelGame()
    {
        if (socketWaitingConnection != null || socketWaitingConnection.Client != null)
        {
            
            WaitingStream.Close();
            socketWaitingConnection.Close();
            
            userState.text = "�����";
        }
        else
        {
            Debug.Log("���� ã������ �ƴ�");
        }
    }

    public void ShowMessage(string data)
    {
        ChatText.text += ChatText.text == "" ? data : "\n" + data;
        LayoutRebuilder.ForceRebuildLayoutImmediate(ChatText.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(ChatContent);
        Invoke("ScrollDelay", 0.03f);
    }

    void ScrollDelay()
    {
        ChatScrollRect.verticalScrollbar.value = 0;
    }

    public void enter()
    {
        if (SendInput.text.Length != 0)
        {
            ChattingPacket.Type = 2;
            ChattingPacket.ChattingMsg = SendInput.text;
            SendPackect(ChattingPacket, "Chatting");
            SendInput.text = "";
        }
    }



    private void ConnectToTcpServer(int portnum)
    {
        try
        {
            if (portnum==9090) // �α��μ���
            {
                socketLoginConnection = new TcpClient();
                socketLoginConnection.Connect("127.0.0.1", portnum);
                ServerConnect.text = "������ ����Ǿ����ϴ�.";
                ServerConnect_2.text = "������ ����Ǿ����ϴ�.";
            }
            else if (portnum == 9000) // ��⼭��
            {
                socketWaitingConnection = new TcpClient();
                socketWaitingConnection.Connect("127.0.0.1", portnum);

                WaitingStream = socketWaitingConnection.GetStream();

                userState.text = "����ã����";
            }
            else if (portnum == 9002) // ä�ü���
            {
                socketChattingConnection = new TcpClient();
                socketChattingConnection.Connect("127.0.0.1", portnum);

                ChattingStream = socketChattingConnection.GetStream();
            }
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
            if (portnum == 9090) // �α��μ���
            {
                ServerConnect.text = "�α��μ����� ��������ʾҽ��ϴ�. \n ���������ֽñ�ٶ��ϴ�.";
                ServerConnect_2.text = "�α��μ����� ��������ʾҽ��ϴ�. \n ���������ֽñ�ٶ��ϴ�.";
            }
            else if (portnum == 9000) // ��⼭��
            {
                userState.text = "��⼭���� ��������ʾҽ��ϴ�. \n �ٽýõ����ֽñ�ٶ��ϴ�.";
            }
            else if (portnum == 9002) // ä�ü���
            {
                userState.text = "ä�ü����� ��������ʾҽ��ϴ�. \n �ٽýõ����ֽñ�ٶ��ϴ�.";
            }
        }
    }




    // ���Ⱑ ���� �ڵ�
    public void SendPackect(System.Object struc, string Type)
    {
        if(Type==LOGIN)
        {
            if (socketLoginConnection == null)
            {
                return;
            }
            try
            {
                // Get a stream object for writing.             
                NetworkStream stream = socketLoginConnection.GetStream();

                byte[] buffer;
                buffer = Serialize(struc);
                //buffer = StringToByte(user);
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

        if (Type == SIGNUP)
        {
            if (socketLoginConnection == null)
            {
                return;
            }
            try
            {
                // Get a stream object for writing.             
                NetworkStream stream = socketLoginConnection.GetStream();

                byte[] buffer;
                buffer = Serialize(struc);
                //buffer = StringToByte(user);
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

        if (Type==ISREADY)
        {
            if (socketWaitingConnection == null)
            {
                return;
            }
            try
            {
                // Get a stream object for writing.             
                NetworkStream stream = socketWaitingConnection.GetStream();

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

        if (Type == "Chatting")
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
        
    }

    public void RecvMessageStruct(Byte[] buffer, string Type)
    {
        if(Type == LOGIN)
        {
            if (socketLoginConnection == null)
            {
                return;
            }

            try
            {
                // Get a stream object for writing.             
                NetworkStream stream = socketLoginConnection.GetStream();
                if (stream.CanRead)
                {
                    // Write byte array to socketConnection stream.                 
                    stream.Read(buffer, 0, buffer.Length);

                    userInfo userInfo = new userInfo();
                    userInfo = Deserialize<userInfo>(buffer);

                    if (userInfo.Type == LOGIN_success)
                    {
                        // ���ȭ������ �̵�
                        LoginPanel.SetActive(false);
                        WaitingPanel.SetActive(true);
                        userID.text = inputField_ID.text;
                        WinNum.text = userInfo.WinNum.ToString();
                        Debug.Log(userInfo.WinNum);
                        LoseNum.text = userInfo.LoseNum.ToString();
                        Debug.Log(userInfo.LoseNum);
                        // ���⼭ ������ ���� ���� ���ֱ�
                        switch (userInfo.ProfileType)
                        {
                            case 0:
                                ProfileImage.sprite = Profile0;
                                break;
                            case 1:
                                ProfileImage.sprite = Profile1;
                                break;
                            case 2:
                                ProfileImage.sprite = Profile2;
                                break;
                            case 3:
                                ProfileImage.sprite = Profile3;
                                break;
                            case 4:
                                ProfileImage.sprite = Profile4;
                                break;
                            case 5:
                                ProfileImage.sprite = Profile5;
                                break;
                            case 6:
                                ProfileImage.sprite = Profile6;
                                break;
                            case 7:
                                ProfileImage.sprite = Profile7;
                                break;
                        }
                        //
                        inputField_ID.text = "";
                        inputField_PW.text = "";
                        userState.text = "�����";
                        // ä�ü��� ����
                        ConnectToTcpServer(9002);
                        ChattingPacket.UserID = userID.text;
                        ChattingPacket.Type = 1;
                        ChattingPacket.ChattingMsg = "";
                        SendPackect(ChattingPacket, "Chatting");
                    }
                    if (userInfo.Type == LOGIN_fail)
                    {
                        Debug.Log("�α��ν���");
                        ErrorMsg.text = userInfo.ErrorMsg;
                    }
                }

                //stream.Close();
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        if (Type == SIGNUP)
        {
            if (socketLoginConnection == null)
            {
                return;
            }

            try
            {
                // Get a stream object for writing.             
                NetworkStream stream = socketLoginConnection.GetStream();
                if (stream.CanRead)
                {
                    // Write byte array to socketConnection stream.                 
                    stream.Read(buffer, 0, buffer.Length);

                    userInfo userInfo = new userInfo();
                    userInfo = Deserialize<userInfo>(buffer);

                    if (userInfo.Type == SIGNUP_success)
                    {
                        Debug.Log("ȸ�����Լ���");
                        ErrorMsg_2.text = userInfo.ErrorMsg;
                    }
                    if (userInfo.Type == SIGNUP_fail)
                    {
                        Debug.Log("ȸ�����Խ���");
                        ErrorMsg_2.text = userInfo.ErrorMsg;
                    }
                }

                //stream.Close();
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        if (Type == "Chatting")
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

                    ChattingPacket chatting = new ChattingPacket();
                    chatting = Deserialize<ChattingPacket>(buffer);
                    
                    if (chatting.Type==1)
                    {
                        ShowMessage(chatting.UserID + " ���� ä�ü����� �����Ͽ����ϴ�.");
                    }
                    if(chatting.Type==2)
                    {
                        ShowMessage(chatting.UserID + " : " + chatting.ChattingMsg);
                    }
                    if (chatting.Type == 3)
                    {
                        ShowMessage(chatting.UserID + " ���� ä�ü������� �����Ͽ����ϴ�.");
                    }
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        if (Type==ISREADY)
        {
            if (socketWaitingConnection == null)
            {
                return;
            }

            try
            {
                // Get a stream object for writing.             
                NetworkStream stream = socketWaitingConnection.GetStream();
                if (stream.CanRead)
                {
                    // Write byte array to socketConnection stream.                 
                    stream.Read(buffer, 0, buffer.Length);

                    IsReady Ready = new IsReady();
                    Ready = Deserialize<IsReady>(buffer);
                    isReady.Type = Ready.Type;
                    Debug.Log(isReady.Type);
                    if (isReady.Type == ISGAME_req)
                    {
                        isWating = false;
                        this.audio.Stop();
                        SceneManager.LoadScene("Tetris");
                    }
                    if (isReady.Type == ISGAME_notReady)
                    {
                        //Debug.Log("���� ���� �غ� �ȵ���");
                    }
                    /*
                    if(buffer[0]== ISGAME_ready || buffer[0]== ISGAME_notReady)
                    {
                        IsReady Ready = new IsReady();
                        Ready = Deserialize<IsReady>(buffer);
                        isReady.Type = Ready.Type;
                        if (isReady.Type == ISGAME_ready)
                        {
                            isWating = false;
                            this.audio.Stop();
                            SceneManager.LoadScene("Tetris");
                        }
                        if (isReady.Type == ISGAME_notReady)
                        {
                            //Debug.Log("���� ���� �غ� �ȵ���");
                        }
                    }
                    */
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
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
        catch(Exception e)
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


    /*
    private void SendPackectClass(System.Object userInfo)
    {
        if (socketLoginConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = socketLoginConnection.GetStream();

            byte[] buffer;
            buffer = SerializeClass(userInfo);
            //buffer = StringToByte(user);
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
    
    

        public byte[] SerializeClass(System.Object data)
    {
        try
        {
            //����ü ������
            int structSize = Marshal.SizeOf(data);
            //����ü ������ ��ŭ �޸� �Ҵ�
            byte[] array = new byte[structSize];

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
            Marshal.StructureToPtr(data, ptr, false);

            Login_info login_Info;
            login_Info = (Login_info)Marshal.PtrToStructure(ptr, typeof(Login_info));

            Console.WriteLine("The value of new point is " + login_Info.ID_value + " and " + login_Info.PW_value + ".");
            //Marshal.Copy(ptr, array, 0, structSize);
            Marshal.FreeHGlobal(ptr);
            
            return array;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
     */

    /*
    //����Ʈ �迭�� String���� ��ȯ
    private string ByteToString(byte[] strByte)
    {
        string str = Encoding.UTF8.GetString(strByte); return str;
    }
    // String�� ����Ʈ �迭�� ��ȯ
    private byte[] StringToByte(string str)
    {
        byte[] StrByte = Encoding.UTF8.GetBytes(str); return StrByte;
    }


    //Byte->String
    public T ByteArrayToStruct<T>(byte[] buffer) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (size > buffer.Length)
        {
            throw new Exception();
        }

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(buffer, 0, ptr, size);
        T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);

        return obj;
    }
    //String->Byte
    public byte[] StructToByteArray(object obj)
    {
        int size = Marshal.SizeOf(obj);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    */
    void OnApplicationQuit()
    {
        // todo : ���ø����̼��� �����ϴ� ������ ó���� �ൿ��
        ChattingPacket.Type = 3;
        ChattingPacket.UserID = userID.text;
        ChattingPacket.ChattingMsg = "";
        SendPackect(ChattingPacket, "Chatting");

        if (socketLoginConnection != null)
        {
            socketLoginConnection.Close();

            if (socketWaitingConnection != null)
                socketWaitingConnection.Close();
        }
        if (socketChattingConnection != null)
        {
            ChattingPacket.Type = 3;
            ChattingPacket.UserID = userID.text;
            ChattingPacket.ChattingMsg = "";
            SendPackect(ChattingPacket, "Chatting");

            socketChattingConnection.Close();
            ChatText.text = "";
        }

        if (socketWaitingConnection != null || socketWaitingConnection.Client != null)
        {
            isReady.Type = 5;
            isReady.userID = user.ID;
            SendPackect(isReady, ISREADY);
            socketWaitingConnection.Close();
            userState.text = "�����";
        }
        
    }
}