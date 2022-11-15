using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;


public class Client : MonoBehaviour
{

	//
	// ���� ���� ����.
	//

	// ������ ����.
	private Socket m_listener = null;

	// Ŭ���̾�Ʈ ���ӿ� ����.
	private Socket m_socket = null;

	// �۽� ����.
	private PacketQueue m_sendQueue;

	// ���� ����.
	private PacketQueue m_recvQueue;

	// ���� �÷���.	
	private bool m_isServer = false;

	// ���� �÷���.
	private bool m_isConnected = false;

	//
	// �̺�Ʈ ���� ��� ����.
	//

	// �̺�Ʈ �˸� ��������Ʈ.
	public delegate void EventHandler(NetEventState state);

	private EventHandler m_handler;

	//
	// ������ ���� ��� ����.
	//

	// ������ ���� �÷���.
	protected bool m_threadLoop = false;

	protected Thread m_thread = null;

	private static int s_mtu = 1400;


	// Use this for initialization
	void Start()
	{

		// �ۼ��� ���۸� ����ϴ�.
		m_sendQueue = new PacketQueue();
		m_recvQueue = new PacketQueue();
	}

	// Update is called once per frame
	void Update()
	{
	}

	// ��� ����.
	public bool StartServer(int port, int connectionNum)
	{
		Debug.Log("StartServer called.!");

		// ������ ������ �����մϴ�.
		try
		{
			// ������ �����մϴ�.
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			// ����� ��Ʈ ��ȣ�� �Ҵ��մϴ�.
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			// ����մϴ�.
			m_listener.Listen(connectionNum);
		}
		catch
		{
			Debug.Log("StartServer fail");
			return false;
		}

		m_isServer = true;

		return LaunchThread();
	}

	// ��� ����.
	public void StopServer()
	{
		m_threadLoop = false;
		if (m_thread != null)
		{
			m_thread.Join();
			m_thread = null;
		}

		Disconnect();

		if (m_listener != null)
		{
			m_listener.Close();
			m_listener = null;
		}

		m_isServer = false;

		Debug.Log("Server stopped.");
	}


	// ����.
	public bool Connect(string address, int port)
	{
		Debug.Log("TransportTCP connect called.");

		if (m_listener != null)
		{
			return false;
		}

		bool ret = false;
		try
		{
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_socket.NoDelay = true;
			m_socket.Connect(address, port);
			ret = LaunchThread();
		}
		catch
		{
			m_socket = null;
		}

		if (ret == true)
		{
			m_isConnected = true;
			Debug.Log("Connection success.");
		}
		else
		{
			m_isConnected = false;
			Debug.Log("Connect fail");
		}

		if (m_handler != null)
		{
			// ���� ����� �˸��ϴ�.
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (m_isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
			m_handler(state);
			Debug.Log("event handler called");
		}

		return m_isConnected;
	}

	// ����.
	public void Disconnect()
	{
		m_isConnected = false;

		if (m_socket != null)
		{
			// ���� �ݱ�.
			m_socket.Shutdown(SocketShutdown.Both);
			m_socket.Close();
			m_socket = null;

			// ���� ���Ḧ �˸��ϴ�.
			if (m_handler != null)
			{
				NetEventState state = new NetEventState();
				state.type = NetEventType.Disconnect;
				state.result = NetEventResult.Success;
				m_handler(state);
			}
		}

	}

	// �۽�ó��.
	public int Send(byte[] data, int size)
	{
		if (m_sendQueue == null)
		{
			return 0;
		}

		return m_sendQueue.Enqueue(data, size);
	}

	// ����ó��.
	public int Receive(ref byte[] buffer, int size)
	{
		if (m_recvQueue == null)
		{
			return 0;
		}

		return m_recvQueue.Dequeue(ref buffer, size);
	}

	// �̺�Ʈ �Լ� ���.
	public void RegisterEventHandler(EventHandler handler)
	{
		m_handler += handler;
	}

	// �̺�Ʈ ���� �Լ� ����.
	public void UnregisterEventHandler(EventHandler handler)
	{
		m_handler -= handler;
	}

	// ������ ���� �Լ�.
	bool LaunchThread()
	{
		try
		{
			// Dispatch�� ������ ����.
			m_threadLoop = true;
			m_thread = new Thread(new ThreadStart(Dispatch));
			m_thread.Start();
		}
		catch
		{
			Debug.Log("Cannot launch thread.");
			return false;
		}

		return true;
	}

	// ������ ���� �ۼ��� ó��.
	public void Dispatch()
	{
		Debug.Log("Dispatch thread started.");

		while (m_threadLoop)
		{
			// Ŭ���̾�Ʈ�κ����� ������ ��ٸ��ϴ�.
			AcceptClient();

			// Ŭ���̾�Ʈ���� �ۼ����� ó���մϴ�.
			if (m_socket != null && m_isConnected == true)
			{

				// �۽�ó��.
				DispatchSend();

				// ����ó��.
				DispatchReceive();
			}

			Thread.Sleep(5);
		}

		Debug.Log("Dispatch thread ended.");
	}

	// Ŭ���̾�Ʈ�� ����.
	void AcceptClient()
	{
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead))
		{
			// Ŭ���̾�Ʈ�κ��� ���ӵǾ����ϴ�.
			m_socket = m_listener.Accept();
			m_isConnected = true;
			if (m_handler != null)
			{
				NetEventState state = new NetEventState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				m_handler(state);
			}
			Debug.Log("Connected from client.");
		}
	}

	// ������ �� �۽�ó��.
	void DispatchSend()
	{
		try
		{
			// �۽�ó��.
			if (m_socket.Poll(0, SelectMode.SelectWrite))
			{
				byte[] buffer = new byte[s_mtu];

				int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				while (sendSize > 0)
				{
					m_socket.Send(buffer, sendSize, SocketFlags.None);
					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		}
		catch
		{
			return;
		}
	}

	// ������ �� ����ó��.
	void DispatchReceive()
	{
		// ����ó��.
		try
		{
			while (m_socket.Poll(0, SelectMode.SelectRead))
			{
				byte[] buffer = new byte[s_mtu];

				int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
				if (recvSize == 0)
				{
					// ����.
					Debug.Log("Disconnect recv from client.");
					Disconnect();
				}
				else if (recvSize > 0)
				{
					m_recvQueue.Enqueue(buffer, recvSize);
				}
			}
		}
		catch
		{
			return;
		}
	}

	// �������� Ȯ��.
	public bool IsServer()
	{
		return m_isServer;
	}

	// ���� Ȯ��.
	public bool IsConnected()
	{
		return m_isConnected;
	}

}
