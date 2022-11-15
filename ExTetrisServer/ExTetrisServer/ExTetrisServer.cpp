
#include <stdio.h>
#include <stdlib.h>
#include <process.h>
#include <winsock2.h>
#include <windows.h>
#include <ws2tcpip.h>
#include <list>


#pragma comment(lib, "ws2_32.lib")
#pragma warning(disable : 4996)
#define BUF_SIZE 100
#define READ 3
#define WRITE 5

int const NAME_SIZE = 10;

typedef struct         

{
    SOCKET hClntSock;
    SOCKADDR_IN clntAdr;
    int userNum;
}PER_HANDLE_DATA, * LPPER_HANDLE_DATA;

typedef struct // buffer info
{
    OVERLAPPED overlapped;
    WSABUF wsaBuf;
    char buffer[BUF_SIZE];
    int rwMode;            

}PER_IO_DATA, * LPPER_IO_DATA;

typedef struct
{
    char id[NAME_SIZE];
    unsigned int level;

}USER_INFO, * PUSER_INFO;       

std::list<LPPER_HANDLE_DATA> UserList;
int userNum=0;
DWORD WINAPI ThreadMain(LPVOID CompletionPortIO);
void err_display(const char* msg);
CRITICAL_SECTION cs;       


int main()
{
    WSADATA wsaData;
    HANDLE hComPort;
    SYSTEM_INFO sysInfo;
    LPPER_IO_DATA ioInfo;
    LPPER_HANDLE_DATA handleInfo;

    SOCKET hServSock;
    SOCKADDR_IN servAdr;
    int recvBytes, i, flags = 0;

    if (0 != WSAStartup(MAKEWORD(2, 2), &wsaData))
    {
        printf("WSAStartup() error!");
        exit(1);
    }

    hComPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    GetSystemInfo(&sysInfo);

    for (i = 0; i < (int)sysInfo.dwNumberOfProcessors; ++i)
    {
        
        _beginthreadex(NULL, 0, (_beginthreadex_proc_type)ThreadMain, (LPVOID)hComPort, 0, NULL);
    }

    hServSock = WSASocketW(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
    memset(&servAdr, 0, sizeof(servAdr));
    servAdr.sin_family = AF_INET;
    inet_pton(AF_INET, "127.0.0.1", &(servAdr.sin_addr));
    servAdr.sin_port = htons(atoi("7777"));

    bind(hServSock, (SOCKADDR*)&servAdr, sizeof(servAdr));
    listen(hServSock, 5);

    InitializeCriticalSection(&cs);

    while (1)
    {
        SOCKET hClntSock;
        SOCKADDR_IN clntAdr;
        int addrLen = sizeof(clntAdr);

        hClntSock = accept(hServSock, (SOCKADDR*)&clntAdr, &addrLen);

        handleInfo = (LPPER_HANDLE_DATA)malloc(sizeof(PER_HANDLE_DATA));       
        handleInfo->hClntSock = hClntSock;                                     
        memcpy(&(handleInfo->clntAdr), &clntAdr, addrLen);
        userNum++;
        handleInfo->userNum = userNum;

        EnterCriticalSection(&cs);
        UserList.push_back(handleInfo);
        LeaveCriticalSection(&cs);

        printf("[TCP 서버] 클라이언트 접속: IP 주소=%s, 포트 번호=%d\n",
            inet_ntoa(clntAdr.sin_addr), ntohs(clntAdr.sin_port));

        CreateIoCompletionPort((HANDLE)hClntSock, hComPort, (DWORD)handleInfo, 0); 

        ioInfo = (LPPER_IO_DATA)malloc(sizeof(PER_IO_DATA));                       
        memset(&(ioInfo->overlapped), 0, sizeof(OVERLAPPED));                   
        ioInfo->wsaBuf.len = BUF_SIZE;
        ioInfo->wsaBuf.buf = ioInfo->buffer;
        ioInfo->rwMode = READ;

        WSARecv(handleInfo->hClntSock, &(ioInfo->wsaBuf), 1, (LPDWORD)&recvBytes, (LPDWORD)&flags, &(ioInfo->overlapped), NULL);   
    }

    DeleteCriticalSection(&cs);

    return 0;
}

DWORD WINAPI ThreadMain(LPVOID pComPort)
{
    HANDLE hComPort = (HANDLE)pComPort;
    
    while (1)
    {
        SOCKET sock;
        DWORD bytesTrans;
        LPPER_HANDLE_DATA   handleInfo;
        LPPER_IO_DATA       ioInfo;
        DWORD flags = 0;
        char msg[BUF_SIZE];

        int retval;

        retval = GetQueuedCompletionStatus(hComPort, &bytesTrans, (LPDWORD)&handleInfo, (LPOVERLAPPED*)&ioInfo, INFINITE);
        sock = handleInfo->hClntSock;

        if (retval == 0 || 0 == bytesTrans)
        {
            if (retval == 0) {
                DWORD temp1, temp2;
                WSAGetOverlappedResult(handleInfo->hClntSock, &ioInfo->overlapped,
                    &temp1, FALSE, &temp2);
                err_display("WSAGetOverlappedResult()");
            }

            EnterCriticalSection(&cs);
            std::list<LPPER_HANDLE_DATA>::iterator iter;
            for (iter = UserList.begin(); iter != UserList.end(); ++iter)
            {
                if (sock == (*iter)->hClntSock)
                {
                    UserList.erase(iter);
                    printf("%d번째 유저 지움\n", (*iter)->userNum);
                    userNum--;
                    printf("총 유저 : %d\n", userNum);
                    break;
                }
            }
            LeaveCriticalSection(&cs);
            closesocket(sock);

            printf("[TCP 서버] 클라이언트 종료: IP 주소=%s, 포트 번호=%d\n",
                inet_ntoa(handleInfo->clntAdr.sin_addr), ntohs(handleInfo->clntAdr.sin_port));
            free(handleInfo);
            free(ioInfo);
            continue;
        }

        if (READ == ioInfo->rwMode)
        {
            puts("message received!");

            

            memset(msg, 0, BUF_SIZE);
            memcpy(msg, ioInfo->buffer, BUF_SIZE);
            free(ioInfo);
            
                int count = -1;
                EnterCriticalSection(&cs);
                std::list<LPPER_HANDLE_DATA>::iterator iter;
                for (iter = UserList.begin(); iter != UserList.end(); ++iter)
                {
                    count++;
                    if ((*iter)->hClntSock == handleInfo->hClntSock) {
                        break;
                    }
                }
                if (count % 2 == 0)
                {
                    iter = UserList.begin();
                    for (int i = 0; i < count + 1; i++)
                    {
                        ++iter;
                    }
                    ioInfo = (LPPER_IO_DATA)malloc(sizeof(PER_IO_DATA));
                    memset(&(ioInfo->overlapped), 0, sizeof(OVERLAPPED));
                    ioInfo->wsaBuf.buf = msg;
                    ioInfo->wsaBuf.len = bytesTrans;
                    ioInfo->rwMode = WRITE;
                    WSASend((*iter)->hClntSock, &(ioInfo->wsaBuf), 1, NULL, 0, &(ioInfo->overlapped), NULL);
                }
                else
                {
                    iter = UserList.begin();
                    for (int i = 0; i < count - 1; i++)
                    {
                        ++iter;
                    }
                    ioInfo = (LPPER_IO_DATA)malloc(sizeof(PER_IO_DATA));
                    memset(&(ioInfo->overlapped), 0, sizeof(OVERLAPPED));
                    ioInfo->wsaBuf.buf = msg;
                    ioInfo->wsaBuf.len = bytesTrans;
                    ioInfo->rwMode = WRITE;
                    WSASend((*iter)->hClntSock, &(ioInfo->wsaBuf), 1, NULL, 0, &(ioInfo->overlapped), NULL);
                }




                LeaveCriticalSection(&cs);

                ioInfo = (LPPER_IO_DATA)malloc(sizeof(PER_IO_DATA));
                memset(&(ioInfo->overlapped), 0, sizeof(OVERLAPPED));
                ioInfo->wsaBuf.len = BUF_SIZE;
                ioInfo->wsaBuf.buf = ioInfo->buffer;
                ioInfo->rwMode = READ;
                WSARecv(sock, &(ioInfo->wsaBuf), 1, NULL, &flags, &(ioInfo->overlapped), NULL);
            
        }
        else
        {
            puts("message sent!");
            free(ioInfo);
        }
    }

    return 0;
}

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