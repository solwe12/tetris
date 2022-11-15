
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

typedef struct 
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
    servAdr.sin_port = htons(atoi("9002"));

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

            EnterCriticalSection(&cs);                      
            std::list<LPPER_HANDLE_DATA>::iterator iter;
            for (iter = UserList.begin(); iter != UserList.end(); ++iter)
            {
                
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
