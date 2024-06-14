
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using FarNet.Tools;
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace PowerShellFar;

/// <summary>
/// PowerShell interactive.
/// </summary>
class Interactive : InteractiveEditor
{
	FarUI? FarUI;
	FarHost? FarHost;
	Runspace? Runspace;
	PowerShell? PowerShell;
	bool _doneTabExpansion;

	static readonly HistoryLog _history = new(Entry.LocalData + "\\InteractiveHistory.log", Settings.Default.MaximumHistoryCount);

	static string GetFilePath()
	{
		return Path.Join(Path.GetTempPath(), DateTime.Now.ToString("_yyMMdd_HHmmss") + Word.InteractiveSuffix);
	}

	/// <summary>
	/// Creates an interactive.
	/// </summary>
	/// <remarks>
	/// With prompt may return null if a user cancels.
	/// </remarks>
	public static Interactive? Create(bool prompt)
	{
		int mode = 0;
		if (prompt)
		{
			IMenu menu = Far.Api.CreateMenu();
			menu.Title = "Open interactive";
			menu.Add("&1. Main session");
			menu.Add("&2. New local session");
			menu.Add("&3. New remote session");
			menu.HelpTopic = Entry.Instance.GetHelpTopic(HelpTopic.InteractiveMenu);

			if (!menu.Show())
				return null;

			mode = menu.Selected;
		}

		// editor
		var editor = Far.Api.CreateEditor();
		editor.FileName = GetFilePath();
		editor.CodePage = 65001;
		editor.DisableHistory = true;
		editor.Switching = Switching.Disabled;
		editor.DeleteSource = DeleteSource.File;

		// create interactive and attach it as the host to avoid conflicts
		Interactive interactive = new(editor, mode) { AutoSave = true };
		editor.Host = interactive;
		return interactive;
	}

	public Interactive(IEditor editor) : this(editor, 0) { }

	public Interactive(IEditor editor, int mode) : base(editor, _history, "<#<", ">#>", "<##>")
	{
		switch (mode)
		{
			case 0:
				OpenMainSession();
				break;
			case 1:
				OpenLocalSession();
				break;
			case 2:
				OpenRemoteSession();
				break;
		}
	}

	void CloseSession()
	{
		if (Runspace != null)
		{
			Runspace.Close();
			Runspace = null;
		}

		Editor.Title = Editor.FileName;
	}

	void EnsureHost()
	{
		if (FarHost is null)
		{
			Editor.Closed += delegate { CloseSession(); };
			Editor.CtrlCPressed += OnCtrlCPressed;
			FarUI = new FarUI();
			FarHost = new FarHost(FarUI);
		}
	}

	void RunspaceOpen()
	{
		Runspace!.Open();
	}

	void OpenMainSession()
	{
		Editor.Title = "PS main session " + Path.GetFileName(Editor.FileName);
	}

	void OpenLocalSession()
	{
		EnsureHost();

		Runspace = RunspaceFactory.CreateRunspace(FarHost, Runspace.DefaultRunspace.InitialSessionState);
		Runspace.ThreadOptions = PSThreadOptions.ReuseThread;
		RunspaceOpen();

		Editor.Title = "PS local session " + Path.GetFileName(Editor.FileName);

		InvokeProfile("Profile-Local.ps1", false);
	}

	void OpenRemoteSession()
	{
		UI.ConnectionDialog dialog = new("New remote interactive");
		if (!dialog.Show())
			return;

		string computerName = (dialog.ComputerName.Length == 0 || dialog.ComputerName == ".") ? "localhost" : dialog.ComputerName;
		PSCredential? credential = null;
		if (dialog.UserName.Length > 0)
		{
			credential = NativeMethods.PromptForCredential(null, null, dialog.UserName, string.Empty, PSCredentialTypes.Generic | PSCredentialTypes.Domain, PSCredentialUIOptions.Default);
			if (credential is null)
				return;
		}

		WSManConnectionInfo connectionInfo = new(false, computerName, 0, null, null, credential);

		EnsureHost();

		Runspace = RunspaceFactory.CreateRunspace(FarHost, connectionInfo);
		RunspaceOpen();

		Editor.Title = "PS " + computerName + " session " + Path.GetFileName(Editor.FileName);

		InvokeProfile("Profile-Remote.ps1", true);
	}

	void InvokeProfile(string fileName, bool remote)
	{
		var profile = Entry.RoamingData + "\\" + fileName;
		if (!File.Exists(profile))
			return;

		try
		{
			using var ps = PowerShell.Create();
			ps.Runspace = Runspace;
			if (remote)
				ps.AddScript(File.ReadAllText(profile), false);
			else
				ps.AddCommand(profile, false);
			ps.Invoke();
		}
		catch (RuntimeException ex)
		{
			Far.Api.Message(
				string.Format(null, "Error in {0}, see $Error for details. Message: {1}", fileName, ex.Message),
				Res.Me, MessageOptions.Warning | MessageOptions.LeftAligned);
		}
	}

	//! This method is sync and uses pipeline, that is why we must not null the pipeline async.
	void OnCtrlCPressed(object? sender, EventArgs e)
	{
		if (PowerShell != null && PowerShell.InvocationStateInfo.State == PSInvocationState.Running)
		{
			try
			{
				PowerShell.Stop();
			}
			catch
			{ }
		}
	}

	void InitTabExpansion()
	{
		if (!_doneTabExpansion)
		{
			_doneTabExpansion = true;
			EditorKit.InitTabExpansion(Runspace);
		}
	}

	/// <summary>
	/// Called on key in interactive.
	/// </summary>
	protected override bool KeyPressed(KeyInfo key)
	{
		ArgumentNullException.ThrowIfNull(key);

		// drop pipeline now, if any
		PowerShell = null;

		// current line
		var currentLine = Editor.Line;

		switch (key.VirtualKeyCode)
		{
			case KeyCode.Tab:
				{
					if (key.Is())
					{
						if (CommandArea() != null && EditorKit.NeedsTabExpansion(Editor))
						{
							InitTabExpansion();
							EditorKit.ExpandCode(currentLine, Runspace);
							Editor.Redraw();
							return true;
						}
					}
					break;
				}
			case KeyCode.F1:
				{
					if (key.IsShift())
					{
						Help.ShowHelpForContext();
						return true;
					}
					break;
				}
		}
		return base.KeyPressed(key);
	}

	protected override bool IsAsync => Runspace != null;

	protected override void Invoke(string code, InteractiveArea area)
	{
		if (Runspace is null)
		{
			EditorOutputWriter2 writer = new(Editor);
			A.Psf.Run(new RunArgs(code) { Writer = writer });
			return;
		}

		// begin editor
		FarUI!.PushWriter(new EditorOutputWriter3(Editor));

		// begin command
		PowerShell = PowerShell.Create();
		PowerShell.Runspace = Runspace;
		PowerShell.Commands.AddScript(code).AddCommand(A.OutHostCommand);
		Task.Run(() =>
		{
			try
			{
				PowerShell.Invoke();
			}
			catch (Exception ex)
			{
				using var ps = PowerShell.Create();
				ps.Runspace = Runspace;
				A.OutReason(ps, ex);
			}

			// complete output
			FarUI.PopWriter();
			EndInvoke();

			// kill
			PowerShell.Dispose();
		});
	}
}
