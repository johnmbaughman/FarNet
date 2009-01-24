/*
FAR.NET plugin for Far Manager
Copyright (c) 2005-2009 FAR.NET Team
*/

#pragma once
class CStr;

namespace FarNet
{;
ref class Message
{
internal:
	static int Show(String^ body, String^ header, MessageOptions options, array<String^>^ buttons, String^ helpTopic);
private:
	bool Show();
	int ShowDialog(int width);
	CStr* CreateBlock(int& outNbItems);
	static void FormatMessageLines(List<String^>^ lines, String^ message, int width, int height);
private:
	int _flags;
	int _selected;
	String^ _header;
	String^ _helpTopic;
	List<String^> _body;
	array<String^>^ _buttons;
};
}
