
#pragma comment(lib, "ws2_32")
#pragma warning(disable : 4996)
#include <winsock2.h>
#include <stdlib.h>
#include <stdio.h>
#include <windows.h>
#include <mysql.h>


#define SERVERPORT 9090
#define BUFSIZE    512

#define MAX_PLAYER 10
#define NONE 0
#define LOGIN_req 1
#define LOGIN_success 2
#define LOGIN_fail 3
#define SIGNUP_req 4
#define SIGNUP_success 5
#define SIGNUP_fail 6

bool User[MAX_PLAYER];

int PlayerNumber = 0;

// 소켓 정보 저장을 위한 구조체

struct SOCKETINFO
{
	OVERLAPPED overlapped;
	SOCKET sock;
	char buf[BUFSIZE + 1];
	int recvbytes;
	int sendbytes;

	int curPlayer;
	WSABUF wsabuf;
};

// 작업자 스레드 함수
DWORD WINAPI WorkerThread(LPVOID arg);
// 오류 출력 함수
void err_quit(const char* msg);
void err_display(const char* msg);

int main(int argc, char* argv[])
{
	int retval;

	// 윈속 초기화
	WSADATA wsa;
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) return 1;

	// 입출력 완료 포트 생성
	HANDLE hcp = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
	if (hcp == NULL) return 1;

	// CPU 개수 확인
	SYSTEM_INFO si;
	GetSystemInfo(&si);

	// (CPU 개수 * 2)개의 작업자 스레드 생성
	HANDLE hThread;
	for (int i = 0; i < (int)si.dwNumberOfProcessors * 2; i++) {
		hThread = CreateThread(NULL, 0, WorkerThread, hcp, 0, NULL);
		if (hThread == NULL) return 1;
		CloseHandle(hThread);
	}

	// socket()
	SOCKET listen_sock = socket(AF_INET, SOCK_STREAM, 0);
	if (listen_sock == INVALID_SOCKET) err_quit("socket()");

	// bind()
	SOCKADDR_IN serveraddr;
	ZeroMemory(&serveraddr, sizeof(serveraddr));
	serveraddr.sin_family = AF_INET;
	serveraddr.sin_addr.s_addr = htonl(INADDR_ANY);
	serveraddr.sin_port = htons(SERVERPORT);
	retval = bind(listen_sock, (SOCKADDR*)&serveraddr, sizeof(serveraddr));
	if (retval == SOCKET_ERROR) err_quit("bind()");

	// listen()
	retval = listen(listen_sock, SOMAXCONN);
	if (retval == SOCKET_ERROR) err_quit("listen()");

	// 데이터 통신에 사용할 변수
	SOCKET client_sock;
	SOCKADDR_IN clientaddr;
	int addrlen;
	DWORD recvbytes, flags;

	printf("로그인 서버 Open!!\n");
	while (1) {
		// accept()
		addrlen = sizeof(clientaddr);
		client_sock = accept(listen_sock, (SOCKADDR*)&clientaddr, &addrlen);
		if (client_sock == INVALID_SOCKET) {
			err_display("accept()");
			break;
		}
		printf("[TCP 서버] 클라이언트 접속: IP 주소=%s, 포트 번호=%d\n",
			inet_ntoa(clientaddr.sin_addr), ntohs(clientaddr.sin_port));

		// 소켓과 입출력 완료 포트 연결
		CreateIoCompletionPort((HANDLE)client_sock, hcp, client_sock, 0);

		// 소켓 정보 구조체 할당
		SOCKETINFO* ptr = new SOCKETINFO;
		if (ptr == NULL) break;
		ZeroMemory(&ptr->overlapped, sizeof(ptr->overlapped));
		ptr->sock = client_sock;
		ptr->recvbytes = ptr->sendbytes = 0;
		ptr->wsabuf.buf = ptr->buf;
		ptr->wsabuf.len = BUFSIZE;
		ptr->curPlayer = 0;

		// 비동기 입출력 시작
		flags = 0;
		retval = WSARecv(client_sock, &ptr->wsabuf, 1, &recvbytes,
			&flags, &ptr->overlapped, NULL);
		if (retval == SOCKET_ERROR) {
			if (WSAGetLastError() != ERROR_IO_PENDING) {
				err_display("WSARecv()");
			}
			continue;
		}
	}

	// 윈속 종료
	WSACleanup();
	return 0;
}

// 작업자 스레드 함수
DWORD WINAPI WorkerThread(LPVOID arg)
{
	int retval;
	HANDLE hcp = (HANDLE)arg;


	while (1) {
		// 비동기 입출력 완료 기다리기
		DWORD cbTransferred;
		SOCKET client_sock;
		SOCKETINFO* ptr;

		int PacketType;
		char ID[10] = { '\0', };
		char PW[10] = { '\0', };
		char ERROR_MSG[20] = {'\0',};
		int PROFILE = 0;
		int WINNUM = 0;
		int LOSENUM = 0;


		retval = GetQueuedCompletionStatus(hcp, &cbTransferred,
			&client_sock, (LPOVERLAPPED*)&ptr, INFINITE);

		// 클라이언트 정보 얻기
		SOCKADDR_IN clientaddr;
		int addrlen = sizeof(clientaddr);
		getpeername(ptr->sock, (SOCKADDR*)&clientaddr, &addrlen);

		

		// 비동기 입출력 결과 확인
		if (retval == 0 || cbTransferred == 0) {
			if (retval == 0) {
				DWORD temp1, temp2;
				WSAGetOverlappedResult(ptr->sock, &ptr->overlapped,
					&temp1, FALSE, &temp2);
				err_display("WSAGetOverlappedResult()");
			}
			User[ptr->curPlayer] = false;
			PlayerNumber--;

			closesocket(ptr->sock);

			

			printf("[TCP 서버] 클라이언트 종료: IP 주소=%s, 포트 번호=%d\n",
				inet_ntoa(clientaddr.sin_addr), ntohs(clientaddr.sin_port));
			delete ptr;
			continue;
		}

		// 데이터 전송량 갱신
		if (ptr->recvbytes == 0) {
			ptr->recvbytes = cbTransferred;
			ptr->sendbytes = 0;	
			// 받은 데이터 출력
			PacketType = ptr->buf[0];
			for (int i = 0; i < 10; i++)
			{
				ID[i] = ptr->buf[4 + i];
			}
			for (int i = 0; i < 10; i++)
			{
				PW[i] = ptr->buf[14 + i];
			}
			PROFILE = ptr->buf[44];
			WINNUM = ptr->buf[48];
			LOSENUM = ptr->buf[52];

			printf("[TCP/%s:%d] Recv PacketType : %d    ID:%s PW:%s ProfileNum:%d W:%d L:%d\n", inet_ntoa(clientaddr.sin_addr),
				ntohs(clientaddr.sin_port), PacketType, ID, PW, PROFILE, WINNUM, LOSENUM);
		}
		else {
			ptr->sendbytes += cbTransferred;
		}

		if (ptr->recvbytes > ptr->sendbytes) {
			// 데이터 보내기
			ZeroMemory(&ptr->overlapped, sizeof(ptr->overlapped));
			
			//로그인 요구
			if (PacketType == 1)
			{
				//데이터베이스
				MYSQL* conn;
				MYSQL_RES* res;
				MYSQL_ROW row;

				const char* server = "localhost";
				const char* user = "root";
				const char* password = "dlghtjr";
				const char* database = "user_information";


				conn = mysql_init(NULL);
				
				if (!mysql_real_connect(conn, server, user, password, database, 8888, NULL, 0))
				{
					exit(1);
				}

				if (mysql_query(conn, "SELECT * FROM user"))
				{
					return 1;
				}
				res = mysql_use_result(conn);

				int count = 0;
				int IDcount = -1;
				int PWcount = -1;

				while ((row = mysql_fetch_row(res)) != NULL)
				{
					if (strcmp(ID, row[0]) == 0)
					{
						IDcount = count;
						printf("User : %d ID OK\n", IDcount);
						strcpy(ID, "ID:OK");
						PROFILE = atoi(row[2]);
						WINNUM = atoi(row[3]);
						LOSENUM = atoi(row[4]);
					}
					count++;
				}
				if (strcmp(ID, "ID:OK") != 0)
				{
					printf("User : Dontexist\n");
					strcpy(ID, "Dontexist"); // 아이디가 없는 에러
					PacketType = LOGIN_fail;
				}


				if (strcmp(ID, "ID:OK") == 0)
				{
					if (mysql_query(conn, "SELECT * FROM user"))
					{
						return 1;
					}
					res = mysql_use_result(conn);

					count = 0;
					while ((row = mysql_fetch_row(res)) != NULL)
					{
						if (count == IDcount) {
							if (strcmp(PW, row[1]) == 0)
							{
								PWcount = count;
								printf("User : %d PW OK\n", count);
								strcpy(PW, " PW:OK");
							}
							else
							{
								PacketType = LOGIN_fail;
								printf("User : %d PW NO\n", count);
								strcpy(PW, " PW:NO"); // 아이디는 있는데 패스워드가 틀린 에러

							}
						}
						count++;

						if (IDcount == PWcount)
						{
							ptr->curPlayer = IDcount;

							if (ptr->curPlayer < MAX_PLAYER && PlayerNumber < MAX_PLAYER && User[ptr->curPlayer] == false)
							{
								PlayerNumber++;
								User[ptr->curPlayer] = true;
								PacketType = LOGIN_success;
								break;
							}
							if (PlayerNumber > MAX_PLAYER && User[ptr->curPlayer] == false)
							{
								PacketType = LOGIN_fail;
								strcpy(ID, "정원초과 ");
								strcpy(PW, "");
								break;
							}
							if (User[ptr->curPlayer] == true) // 이미 접속중이라면
							{
								PacketType = LOGIN_fail;
								strcpy(ID, "Already "); // 이미 접속중이라서 중복 접속이 불가능하다는 에러
								strcpy(PW, "Connected");
								break;
							}
						}
					}
				}
				else
				{
					PacketType = LOGIN_fail;
					printf("User : PW NO\n"); //아이디가 없어서 패스워드도 없다는 에러
					strcpy(PW, " ID");
				}


				mysql_free_result(res);
				mysql_close(conn);

				strcat(ERROR_MSG, ID);
				strcat(ERROR_MSG, PW);

				ptr->buf[0] = PacketType;

				for (int i = 0; i < 10; i++)
				{
					ptr->buf[4 + i] = ID[i];
				}
				for (int i = 0; i < 10; i++)
				{
					ptr->buf[14 + i] = PW[i];
				}
				for (int i = 0; i < 20; i++)
				{
					ptr->buf[24 + i] = ERROR_MSG[i];
				}
				ptr->buf[44] = PROFILE;
				ptr->buf[48]= WINNUM;
				ptr->buf[52] = LOSENUM;

				strcpy(ptr->wsabuf.buf, ptr->buf);

				printf("[TCP/%s:%d] Send PacketType:%d  ErrorMsg:%s\n", inet_ntoa(clientaddr.sin_addr),
					ntohs(clientaddr.sin_port), PacketType, ERROR_MSG);
				printf("Total player number: %d\n", PlayerNumber);

				ptr->wsabuf.buf += ptr->sendbytes;
				ptr->wsabuf.len = ptr->recvbytes - ptr->sendbytes;

				DWORD sendbytes;
				retval = WSASend(ptr->sock, &ptr->wsabuf, 1,
					&sendbytes, 0, &ptr->overlapped, NULL);
				if (retval == SOCKET_ERROR) {
					if (WSAGetLastError() != WSA_IO_PENDING) {
						err_display("WSASend()");
					}
					continue;
				}
			}
			
			// 회원가입 요구
			if (PacketType == 4)
			{
				MYSQL* conn;
				MYSQL_RES* res;
				MYSQL_ROW row;

				const char* server = "localhost";
				const char* user = "root";
				const char* password = "dlghtjr";
				const char* database = "user_information";
				conn = mysql_init(NULL);

				if (!mysql_real_connect(conn, server, user, password, database, 8888, NULL, 0))
				{
					exit(1);
				}
				if (mysql_query(conn, "SELECT * FROM user"))
				{
					return 1;
				}
				res = mysql_use_result(conn);

				bool isExist = false;

				while ((row = mysql_fetch_row(res)) != NULL)
				{
					if (strcmp(ID, row[0]) == 0)
					{
						// 아이디가 같은게 있기 때문에 안된다고 반환해줍니다.
						isExist = true;
					}

					if (isExist == true)
					{
						PacketType = 6;
						strcpy(ERROR_MSG, "ID already exists");
						break;
					}
					
				}
				
				if (isExist == false)
				{
					// 아이디가 같은게 없으므로 ID와 PW를 추가해줍니다.
					PacketType = 5;
					char msg[100];
					sprintf(msg, "INSERT INTO user VALUES(\"%s\", \"%s\", %d, 0, 0)", ID, PW, PROFILE);
					if (mysql_query(conn, msg))
					{
						return 1;
					}
					strcpy(ERROR_MSG, "Sign up complete");
				}

				mysql_free_result(res);
				mysql_close(conn);

				ptr->buf[0] = PacketType;

				for (int i = 0; i < 10; i++)
				{
					ptr->buf[4 + i] = ID[i];
				}
				for (int i = 0; i < 10; i++)
				{
					ptr->buf[14 + i] = PW[i];
				}
				for (int i = 0; i < 20; i++)
				{
					ptr->buf[24 + i] = ERROR_MSG[i];
				}

				strcpy(ptr->wsabuf.buf, ptr->buf);

				printf("[TCP/%s:%d] Send PacketType:%d  ErrorMsg:%s\n", inet_ntoa(clientaddr.sin_addr),
					ntohs(clientaddr.sin_port), PacketType, ERROR_MSG);

				ptr->wsabuf.buf += ptr->sendbytes;
				ptr->wsabuf.len = ptr->recvbytes - ptr->sendbytes;

				DWORD sendbytes;
				retval = WSASend(ptr->sock, &ptr->wsabuf, 1,
					&sendbytes, 0, &ptr->overlapped, NULL);
				if (retval == SOCKET_ERROR) {
					if (WSAGetLastError() != WSA_IO_PENDING) {
						err_display("WSASend()");
					}
					continue;
				}
			}
		}
		else {
			ptr->recvbytes = 0;

			// 데이터 받기
			ZeroMemory(&ptr->overlapped, sizeof(ptr->overlapped));
			ptr->wsabuf.buf = ptr->buf;
			ptr->wsabuf.len = BUFSIZE;

			DWORD recvbytes;
			DWORD flags = 0;
			retval = WSARecv(ptr->sock, &ptr->wsabuf, 1,
				&recvbytes, &flags, &ptr->overlapped, NULL);
			if (retval == SOCKET_ERROR) {
				if (WSAGetLastError() != WSA_IO_PENDING) {
					err_display("WSARecv()");
				}
				continue;
			}
		}
	}

	return 0;
}

// 소켓 함수 오류 출력 후 종료
void err_quit(const char* msg)
{
	LPVOID lpMsgBuf;
	FormatMessage(
		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		NULL, WSAGetLastError(),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&lpMsgBuf, 0, NULL);
	MessageBox(NULL, (LPCTSTR)lpMsgBuf, (LPCWSTR)msg, MB_ICONERROR);
	LocalFree(lpMsgBuf);
	exit(1);
}

// 소켓 함수 오류 출력
void err_display(const char* msg)
{
	LPVOID lpMsgBuf;
	FormatMessage(
		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		NULL, WSAGetLastError(),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&lpMsgBuf, 0, NULL);
	printf("[%s] %s", msg, (char*)lpMsgBuf);
	LocalFree(lpMsgBuf);
}