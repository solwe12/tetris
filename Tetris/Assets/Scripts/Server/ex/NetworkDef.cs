using UnityEngine;
using System.Collections;

// �̺�Ʈ ����.
public enum NetEventType
{
	Connect = 0,    // ���� �̺�Ʈ.
	Disconnect,     // ���� �̺�Ʈ.
	SendError,      // �۽� ����.
	ReceiveError,   // ���� ����.
}

// �̺�Ʈ ���.
public enum NetEventResult
{
	Failure = -1,   // ����.
	Success = 0,    // ����.
}

// �̺�Ʈ ���� ����.
public class NetEventState
{
	public NetEventType type;   // �̺�Ʈ Ÿ��.
	public NetEventResult result;   // �̺�Ʈ ���.
}
