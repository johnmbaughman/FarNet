
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

#pragma once

ref class Wrap
{
public:
	static WindowKind WindowGetKind();
	static int GetEndPalette();
};

class State
{
public:
	static bool GetPanelInfo;
};

template<class Type>
class SetState
{
public:
	SetState(Type& value1, Type value2) : _value(&value1), _saved(value1)
	{
		*_value = value2;
	}
	~SetState()
	{
		*_value = _saved;
	}
private:
	Type* _value;
	Type _saved;
};

class AutoEditorInfo : public EditorInfo
{
public:
	AutoEditorInfo(intptr_t editorId, bool safe = false);
	void Update();
private:
	void operator=(const AutoEditorInfo&) {}
};

#pragma push_macro("FCTL_GETPANELITEM")
#pragma push_macro("FCTL_GETSELECTEDPANELITEM")
#undef FCTL_GETPANELITEM
#undef FCTL_GETSELECTEDPANELITEM
enum FileType
{
	ShownFile = FCTL_GETPANELITEM,
	SelectedFile = FCTL_GETSELECTEDPANELITEM,
};
#pragma pop_macro("FCTL_GETPANELITEM")
#pragma pop_macro("FCTL_GETSELECTEDPANELITEM")

class AutoPluginPanelItem
{
public:
	AutoPluginPanelItem(HANDLE handle, int index, FileType type);
	~AutoPluginPanelItem();
	const PluginPanelItem& Get() const { return *m.Item; }
private:
	FarGetPluginPanelItem m;
	char mBuffer[1024];
	void operator=(const AutoPluginPanelItem&) {}
};

class AutoStopDialogRedraw
{
public:
	AutoStopDialogRedraw(HANDLE hDlg) : _hDlg(hDlg)
	{
		Info.SendDlgMessage(_hDlg, DM_ENABLEREDRAW, FALSE, 0);
	}
	~AutoStopDialogRedraw()
	{
		Info.SendDlgMessage(_hDlg, DM_ENABLEREDRAW, TRUE, 0);
	}
private:
	HANDLE _hDlg;
};

void GetPanelInfo(HANDLE handle, PanelInfo& info);
bool TryPanelInfo(HANDLE handle, PanelInfo& info);

String^ GetDialogControlText(HANDLE hDlg, int id, int start, int len);
