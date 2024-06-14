
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System;
using System.Globalization;
using System.Management.Automation.Host;
using System.Threading;

namespace PowerShellFar;

/// <summary>
/// PowerShellFar host implementation.
/// </summary>
class FarHost : PSHost
{
	// This instance ID.
	static readonly Guid _InstanceId = Guid.NewGuid();

	// Original current culture.
	readonly CultureInfo _CurrentCulture = Thread.CurrentThread.CurrentCulture;

	// User interface object.
	readonly PSHostUserInterface _UI;

	// Nested prompt editor.
	IEditor? _nested;

	/// <summary>
	/// Construct an instance of this PSHost implementation.
	/// Keep a reference to the hosting application object.
	/// </summary>
	/// <param name="ui">Host UI.</param>
	internal FarHost(PSHostUserInterface ui)
	{
		_UI = ui;
	}

	#region PSHost
	/// <summary>
	/// The host name: FarHost
	/// </summary>
	public override string Name => "FarHost";

	/// <summary>
	/// Gets the current culture to use.
	/// </summary>
	public override CultureInfo CurrentCulture => _CurrentCulture;

	/// <summary>
	/// Gets the current UI culture to use.
	/// </summary>
	public override CultureInfo CurrentUICulture => A.Psf.Manager.CurrentUICulture;

	/// <summary>
	/// Gets the GUID generated once.
	/// </summary>
	public override Guid InstanceId => _InstanceId;

	/// <summary>
	/// Gets the UI instance.
	/// </summary>
	public override PSHostUserInterface UI => _UI;

	/// <summary>
	/// Gets the assembly version.
	/// </summary>
	public override Version Version => typeof(Actor).Assembly.GetName().Version!;

	/// <summary>
	/// Instructs the host to interrupt the currently running pipeline and start a new nested input loop.
	/// An input loop is the cycle of prompt, input, and execute.
	/// </summary>
	public override void EnterNestedPrompt()
	{
		// push the last
		IEditor? keepNested = _nested;

		try
		{
			//! Far used to crash: Test-CallStack-.ps1 \ suspend \ type exit + enter
			//! This exception from Open() was removed, so don't try\catch all in here.
			//! SVN tag 4.2.26
			var console = Interactive.Create(false)!;
			_nested = console.Editor;

			// Enter the modal editor. There are two ways to exit.
			// 1) User exits the editor ([Esc]/[F10]). _nested should be this editor, not null.
			// But PowerShell nested prompt is not yet exited, call 'exit', it triggers
			// ExitNestedPrompt(), it sets _nested to null.
			// 2) User types 'exit' in the editor. Then ExitNestedPrompt() is called first,
			// it sets _nested to null and closes the editor. Control gets here with null
			// _nested, so we do nothing but restoring the very first _nested.
			_nested.Open(OpenMode.Modal);

			// If _nested is not null then a user has closed the editor via UI, not by 'exit'.
			// Thus, we have to exit the nested prompt. IsRunning check is added for V3 CTP2.
			// It works fine in V2, too. Meaning: if there is no running pipeline (stepper)
			// then there is nothing to exit, so do not exit. Exit nothing hangs in V3 CTP2.
			if (_nested != null && A.Psf.IsRunning)
			{
				using var ps = A.Psf.NewPowerShell();
				ps.AddScript("exit").Invoke();
			}
		}
		finally
		{
			// pop the last
			_nested = keepNested;
		}
	}

	/// <summary>
	/// Instructs the host to exit the currently running input loop.
	/// </summary>
	public override void ExitNestedPrompt()
	{
		if (_nested != null)
		{
			var nested = _nested;
			_nested = null;
			if (nested.IsOpened)
				nested.Close();
		}
	}

	/// <summary>
	/// Called before an external application process is started.
	/// It is used to save state that the child process may alter
	/// so the parent can restore that state when the child exits.
	/// </summary>
	public override void NotifyBeginApplication()
	{
		if (!s_ignoreApplications)
			Far.Api.UI.ShowUserScreen();
	}

	/// <summary>
	/// Called after an external application process finishes.
	/// It is used to restore state that the child process may have altered.
	/// </summary>
	public override void NotifyEndApplication()
	{
		if (!s_ignoreApplications)
			Far.Api.UI.SaveUserScreen();
	}

	/// <summary>
	/// Indicates to the host that an exit has been requested.
	/// It passes the exit code that the host should use when exiting the process.
	/// </summary>
	public override void SetShouldExit(int exitCode)
	{
	}
	#endregion

	#region IgnoreApplications
	static bool s_ignoreApplications;

	/// <summary>
	/// Use this object with `using` to disable/enable Notify*Application().
	/// </summary>
	internal sealed class IgnoreApplications : IDisposable
	{
		readonly bool _old = s_ignoreApplications;

		public IgnoreApplications()
		{
			s_ignoreApplications = true;
		}

		public void Dispose()
		{
			s_ignoreApplications = _old;
		}
	}
	#endregion
}
