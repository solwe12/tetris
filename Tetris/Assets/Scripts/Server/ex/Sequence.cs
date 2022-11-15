using UnityEngine;
using System.Collections;
using System.Net;


public class Sequence : MonoBehaviour
{

	private Mode m_mode;

	private string serverAddress;

	private HostType hostType;

	private const int m_port = 50765;

	private Client m_transport = null;

	private int m_counter = 0;

	//public GUITexture bgTexture;
	//public GUITexture pushTexture;

	private static float WINDOW_WIDTH = 640.0f;
	private static float WINDOW_HEIGHT = 480.0f;

	enum Mode
	{
		SelectHost = 0,
		Connection,
		Game,
		Disconnection,
		Error,
	};

	enum HostType
	{
		None = 0,
		Server,
		Client,
	};


	void Awake()
	{
		m_mode = Mode.SelectHost;
		hostType = HostType.None;
		serverAddress = "";

		// Network Ŭ������ ������Ʈ ���.
		GameObject obj = new GameObject("Network");
		m_transport = obj.AddComponent<Client>();
		DontDestroyOnLoad(obj);

		// ȣ��Ʈ���� �����ɴϴ�.
		string hostname = Dns.GetHostName();
		// ȣ��Ʈ���� IP�ּҸ� �����ɴϴ�.
		IPAddress[] adrList = Dns.GetHostAddresses(hostname);
		serverAddress = adrList[0].ToString();
	}

	void Update()
	{

		switch (m_mode)
		{
			case Mode.SelectHost:
				OnUpdateSelectHost();
				break;

			case Mode.Connection:
				OnUpdateConnection();
				break;

			case Mode.Game:
				OnUpdateGame();
				break;

			case Mode.Disconnection:
				OnUpdateDisconnection();
				break;
		}

		++m_counter;
	}

	//
	void OnGUI()
	{
		switch (m_mode)
		{
			case Mode.SelectHost:
				OnGUISelectHost();
				break;

			case Mode.Connection:
				OnGUIConnection();
				break;

			case Mode.Game:
				break;

			case Mode.Disconnection:
				break;

			case Mode.Error:
				OnGUICError();
				break;
		}
	}


	// Sever �Ǵ� Client ����ȭ��
	void OnUpdateSelectHost()
	{

		switch (hostType)
		{
			case HostType.Server:
				{
					bool ret = m_transport.StartServer(m_port, 1);
					m_mode = ret ? Mode.Connection : Mode.Error;
				}
				break;

			case HostType.Client:
				{
					bool ret = m_transport.Connect(serverAddress, m_port);
					m_mode = ret ? Mode.Connection : Mode.Error;
				}
				break;

			default:
				break;
		}
	}

	void OnUpdateConnection()
	{
		if (m_transport.IsConnected() == true)
		{
			m_mode = Mode.Game;

			//GameObject game = GameObject.Find("TicTacToe");
			//game.GetComponent<TicTacToe>().GameStart();
		}
	}

	void OnUpdateGame()
	{
		//GameObject game = GameObject.Find("TicTacToe");

		//if (game.GetComponent<TicTacToe>().IsGameOver() == true)
		//{
//			m_mode = Mode.Disconnection;
		//}
	}


	void OnUpdateDisconnection()
	{
		switch (hostType)
		{
			case HostType.Server:
				m_transport.StopServer();
				break;

			case HostType.Client:
				m_transport.Disconnect();
				break;

			default:
				break;
		}

		m_mode = Mode.SelectHost;
		hostType = HostType.None;
		//serverAddress = "";
		// ȣ��Ʈ���� �����ɴϴ�.
		string hostname = Dns.GetHostName();
		// ȣ��Ʈ���� IP �ּҸ� �����ɴϴ�.
		IPAddress[] adrList = Dns.GetHostAddresses(hostname);
		serverAddress = adrList[0].ToString();
	}


	void OnGUISelectHost()
	{
		// ��� ǥ��.
		DrawBg(true);

		if (GUI.Button(new Rect(20, 290, 150, 20), "���� ��븦 ��ٸ��ϴ�"))
		{
			hostType = HostType.Server;
		}

		// Ŭ���̾�Ʈ�� �������� �� ������ ���� �ּҸ� �Է��մϴ�.
		if (GUI.Button(new Rect(20, 380, 150, 20), "���� ���� �����մϴ�"))
		{
			hostType = HostType.Client;
		}

		Rect labelRect = new Rect(20, 410, 200, 30);
		GUIStyle style = new GUIStyle();
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = Color.white;
		GUI.Label(labelRect, "���� IP �ּ�", style);
		labelRect.y -= 2;
		style.fontStyle = FontStyle.Normal;
		style.normal.textColor = Color.black;
		GUI.Label(labelRect, "���� IP �ּ�", style);

		serverAddress = GUI.TextField(new Rect(20, 430, 200, 20), serverAddress);
	}


	void OnGUIConnection()
	{
		// ��� ǥ��.
		DrawBg(false);

		// Ŭ���̾�Ʈ�� �������� �� ������ ���� �ּҸ� �Է��մϴ�.
		GUI.Button(new Rect(84, 335, 160, 20), "���� ��븦 ��ٸ��ϴ�");
	}

	void OnGUICError()
	{
		// ��� ǥ��.
		DrawBg(false);

		float px = Screen.width * 0.5f - 150.0f;
		float py = Screen.height * 0.5f;

		if (GUI.Button(new Rect(px, py, 300, 80), "������ �� �����ϴ�.\n\n��ư�� ��������"))
		{
			m_mode = Mode.SelectHost;
			hostType = HostType.None;
		}
	}

	// ��� ǥ��.
	void DrawBg(bool blink)
	{
		// ����� �׸��ϴ�.
		Rect bgRect = new Rect(Screen.width / 2 - WINDOW_WIDTH * 0.5f,
							 Screen.height / 2 - WINDOW_HEIGHT * 0.5f,
							 WINDOW_WIDTH,
							 WINDOW_HEIGHT);
		//Graphics.DrawTexture(bgRect, bgTexture.texture);

		// ��ư�� ��������.
		if (blink && m_counter % 120 > 20)
		{
			Rect pushRect = new Rect(84, 335, 220, 25);
			//Graphics.DrawTexture(pushRect, pushTexture.texture);
		}
	}
}
