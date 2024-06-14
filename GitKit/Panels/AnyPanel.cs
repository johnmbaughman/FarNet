﻿using FarNet;

namespace GitKit;

abstract class AnyPanel(Explorer explorer) : Panel(explorer)
{
	protected abstract string HelpTopic { get; }

	internal abstract void AddMenu(IMenu menu);

	protected (TData?, TData?) GetSelectedDataRange<TData>()
	{
		var files = GetSelectedFiles();
		if (files.Length >= 2)
			return ((TData?)files[0].Data, (TData?)files[^1].Data);

		var file1 = files.Length > 0 ? files[0] : null;
		var file2 = CurrentFile;

		if (ReferenceEquals(file1, file2))
			file1 = null;

		return ((TData?)file1?.Data, (TData?)file2?.Data);
	}

	void ShowHelp()
	{
		Host.Instance.ShowHelpTopic(HelpTopic);
	}

	void OpenMemberPanel()
	{
		var data = CurrentFile?.Data;
		if (data is not null)
		{
			Host.InvokeScript(
				"[PowerShellFar.MemberExplorer]::new($args[0]).CreatePanel().OpenChild($args[1])",
				[data, this]);
		}
	}

	public override bool UIKeyPressed(KeyInfo key)
	{
		switch (key.VirtualKeyCode)
		{
			case KeyCode.F1 when key.Is():
				ShowHelp();
				return true;

			case KeyCode.A when key.IsCtrl():
				OpenMemberPanel();
				return true;
		}

		return base.UIKeyPressed(key);
	}
}
