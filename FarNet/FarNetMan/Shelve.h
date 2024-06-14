
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

#pragma once

namespace FarNet
{;
ref class Panel1;
ref class Panel2;

ref class ShelveInfoNative sealed : Works::ShelveInfo
{
public:
	static ShelveInfoNative^ CreateActiveInfo(bool modes);
public:
	ShelveInfoNative(Panel1^ panel, bool modes);
	virtual property bool CanRemove { bool get() override { return true; } }
	virtual property String^ Title { String^ get() override { return Path; } }
	virtual void PopWork(bool active) override;
public:
	property String^ Path;
	property String^ Current;
private:
	bool _modes;
	PanelSortMode _sortMode;
	PanelViewMode _viewMode;
};

ref class ShelveInfoModule sealed : Works::ShelveInfo
{
public:
	ShelveInfoModule(Panel2^ panel);
	property Panel2^ Panel { Panel2^ get() { return _panel; } }
	virtual property bool CanRemove { bool get() override { return false; } }
	virtual property String^ Title { String^ get() override; }
	virtual void PopWork(bool active) override;
private:
	Panel2^ _panel;
};
}
