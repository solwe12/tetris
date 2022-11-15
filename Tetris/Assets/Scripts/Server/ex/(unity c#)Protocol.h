#pragma once

#pragma warning(disable : 4996)


//헤더 판별용 열거형
enum LoginPacket { NONE = 0, LOGIN_req, LOGIN_success, LOGIN_fail };


class PacketHeader
{
private:
	unsigned int PacketSize;
	LoginPacket Type;

public:
	PacketHeader() :
		Type(NONE), PacketSize(0)
	{
		//Type = NONE;
		//PacketSize = 0;
	}

	PacketHeader(LoginPacket _Type, unsigned int _PacketSize) :
		Type(_Type), PacketSize(_PacketSize)
	{

		//Type = _Type;
		//PacketSize = _PacketSize - sizeof(PacketHeader);

	}

	LoginPacket get_PacketHeader()
	{
		return Type;
	}

};
class Login_info : public PacketHeader
{
public:
	char ID_value[10];
	char PW_value[10];

public:
	Login_info() :ID_value(), PW_value(){}
	Login_info(string _ID, string _PW) :PacketHeader(LOGIN_req, sizeof(Login_info))
	{
		strcpy(ID_value, _ID.c_str());
		strcpy(PW_value, _PW.c_str());
	}

};
class Login_Success : public PacketHeader
{
private:
	int Userserial;

public:
	Login_Success():Userserial(0){}
	Login_Success(int _Userserial) :
		PacketHeader(LOGIN_success, sizeof(Login_Success)), Userserial(_Userserial)
	{
		//Userserial = _Userserial;
	}

	int get_Userserial()
	{
		return Userserial;
	}

};
class Login_Fail : public PacketHeader
{

public:
	Login_Fail() :PacketHeader(LOGIN_fail, sizeof(Login_Fail))
	{
	}

};