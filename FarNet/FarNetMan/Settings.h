
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

#pragma once
#include "Utils.h"

class Settings
{
	HANDLE _handle;

public:
	Settings(const GUID& guid, bool safe = false)
	{
		FarSettingsCreate settings = { sizeof(FarSettingsCreate), guid, INVALID_HANDLE_VALUE };
		if (Info.SettingsControl(INVALID_HANDLE_VALUE, SCTL_CREATE, PSL_ROAMING, &settings))
			_handle = settings.Handle;
		else if (safe)
			_handle = INVALID_HANDLE_VALUE;
		else
			throw gcnew InvalidOperationException(__FUNCTION__);
	}

	~Settings()
	{
		if (_handle != INVALID_HANDLE_VALUE)
			Info.SettingsControl(_handle, SCTL_FREE, 0, 0);
	}

	HANDLE Handle() const
	{
		return _handle;
	}

	int OpenSubKey(int root, const wchar_t* name)
	{
		FarSettingsValue value = { sizeof(value), (size_t)root, name };
		return (int)Info.SettingsControl(_handle, SCTL_OPENSUBKEY, 0, &value);
	}

	void Enum(int root, FarSettingsEnum& arg)
	{
		arg.Root = root;
		if (!Info.SettingsControl(_handle, SCTL_ENUM, 0, &arg))
			throw gcnew InvalidOperationException(__FUNCTION__);
	}

	bool Get(int root, String^ name, FarSettingsItem& arg)
	{
		PIN_NE(pin, name);
		arg.StructSize = sizeof(FarSettingsItem);
		arg.Root = root;
		arg.Name = pin;
		return Info.SettingsControl(_handle, SCTL_GET, 0, &arg) != 0;
	}

	//
	// see FarColorer
	//

	unsigned __int64 GetUint64(int root, const wchar_t* name, unsigned __int64 defaultValue)
	{
		FarSettingsItem item = { sizeof(FarSettingsItem), (size_t)root, name, FST_QWORD };
		if (Info.SettingsControl(_handle, SCTL_GET, 0, &item)) {
			return item.Number;
		}
		return defaultValue;
	}

	bool GetBool(int root, const wchar_t* name, bool defaultValue)
	{
		return GetUint64(root, name, defaultValue ? 1 : 0) != 0;
	}
};
