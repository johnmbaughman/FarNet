﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet.Works;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FarNet;

public partial class Panel
{
	/// <summary>
	/// Called when files of another module panel have been changed.
	/// </summary>
	/// <param name="that">That panel.</param>
	/// <param name="args">.</param>
	/// <remarks>
	/// This panel may be updated if it contains data related to that panel.
	/// </remarks>
	public virtual void OnThatFileChanged(Panel that, EventArgs args)
	{
	}

	/// <summary>
	/// Called when files of this panel have been changed.
	/// </summary>
	/// <param name="args">.</param>
	/// <remarks>
	/// The base method calls <see cref="OnThatFileChanged"/> on another module panel, if any.
	/// </remarks>
	public virtual void OnThisFileChanged(EventArgs args)
	{
		if (TargetPanel is Panel that)
			that.OnThatFileChanged(this, args);
	}

	/// <summary>
	/// Called by <see cref="UIExplorerEntered"/>.
	/// </summary>
	public event EventHandler<ExplorerEnteredEventArgs>? ExplorerEntered;

	/// <summary>
	/// It is called when a new explorer has been attached after one of the explore methods.
	/// </summary>
	/// <param name="args">.</param>
	/// <remarks>
	/// The base method triggers the <see cref="ExplorerEntered"/> event.
	/// </remarks>
	public virtual void UIExplorerEntered(ExplorerEnteredEventArgs args)
	{
		ExplorerEntered?.Invoke(this, args);
	}

	///
	public static GetContentEventArgs? WorksExportExplorerFile(Explorer explorer, Panel panel, ExplorerModes mode, FarFile file, string fileName)
	{
		if (!explorer.CanGetContent)
			return null;

		// export file
		Log.Source.TraceInformation("ExportFile");
		var args = new GetContentEventArgs(mode, file, fileName);
		panel.UIGetContent(args);
		if (args.Result != JobResult.Done)
			return null;

		// no text or an actual file exists?
		if (args.UseText is null || !string.IsNullOrEmpty(args.UseFileName))
			return args;

		// export text
		var text = args.UseText as string;
		if (text is null)
		{
			if (args.UseText is IEnumerable collection)
			{
				// write collection
				using (var writer = new StreamWriter(fileName, false, Encoding.Unicode))
					foreach (var it in collection)
						writer.WriteLine(it);
				return args;
			}
			else
			{
				text = args.UseText.ToString();
			}
		}

		// write text, UTF8 just in case as the default for save as
		File.WriteAllText(fileName, text, Encoding.UTF8);
		args.CodePage = 65001;
		return args;
	}

	/// <summary>
	/// Copy/move action.
	/// </summary>
	/// <param name="move">Tells to move files.</param>
	/// <remarks>
	/// The source and target panel are module panels.
	/// The target panel explorer accepts the selected files.
	/// </remarks>
	public virtual void UICopyMove(bool move)
	{
		// target
		var that = TargetPanel;

		// commit
		if (that is null)
		{
			// can?
			if (!Explorer.CanExportFiles)
				return;

			// target?
			var native = Far.Api.Panel2!;
			if (native.IsPlugin || native.Kind != PanelKind.File)
				return;

			// args
			var argsExport = new ExportFilesEventArgs(ExplorerModes.None, GetSelectedFiles(), move, native.CurrentDirectory);
			if (argsExport.Files.Count == 0)
				return;

			// call
			UIExportFiles(argsExport);
			if (argsExport.Result == JobResult.Ignore)
				return;

			// show
			native.Update(true);
			native.Redraw();

			// complete
			UICopyMoveComplete(argsExport);
			return;
		}

		// can?
		if (!that.Explorer.CanAcceptFiles)
			return;

		// args
		var argsAccept = new AcceptFilesEventArgs(ExplorerModes.None, GetSelectedFiles(), move, Explorer);
		if (argsAccept.Files.Count == 0)
			return;

		// call
		that.UIAcceptFiles(argsAccept);
		if (argsAccept.Result == JobResult.Ignore)
			return;

		// the target may have new files, update, keep selection
		that.Post(argsAccept);
		that.Update(true);
		that.Redraw();

		// complete
		UICopyMoveComplete(argsAccept);
	}

	void UICopyMoveComplete(CopyFilesEventArgs args)
	{
		// info
		bool isIncomplete = args.Result == JobResult.Incomplete;
		bool isAllToStay = isIncomplete && args.FilesToStay.Count == 0;

		// Copy: do not update the source, files are the same
		if (!args.Move)
		{
			// keep it as it is
			if (isAllToStay || !SelectionExists)
				return;

			// drop selection
			UnselectAll();

			// recover
			if (isIncomplete)
				SelectFiles(args.FilesToStay, null);

			// show
			Redraw();
			return;
		}

		// Move: no need to delete or all to stay or cannot delete
		if (!args.ToDeleteFiles || isAllToStay || !Explorer.CanDeleteFiles)
		{
			// the source may have some files deleted, update, drop selection
			Update(false);

			// recover selection
			if (isIncomplete)
			{
				if (isAllToStay)
					SelectFiles(args.Files, null);
				else
					SelectFiles(args.FilesToStay, null);
			}

			// show
			Redraw();
			return;
		}

		// Move: delete is requested, delete the source files

		// exclude files to stay
		var filesToDelete = args.Files;
		if (isIncomplete)
		{
			var filesToDelete2 = new List<FarFile>(filesToDelete);
			foreach (var file in args.FilesToStay)
				filesToDelete2.Remove(file);
			filesToDelete = filesToDelete2;
		}

		// call
		var argsDelete = new DeleteFilesEventArgs(ExplorerModes.Silent, filesToDelete, false);
		UIDeleteWithRecover(argsDelete, false);
		if (isIncomplete)
			SelectFiles(args.FilesToStay, null);

		// show
		Redraw();
	}

	/// <summary>
	/// Creates a new file or directory.
	/// </summary>
	/// <remarks>
	/// It is normally called on [F7].
	/// It calls <see cref="UICreateFile"/> if the explorer supports it.
	/// <para>
	/// Current file after the operation is defined by <c>Post*</c> in the arguments.
	/// </para>
	/// </remarks>
	public void UICreate()
	{
		// can?
		if (!Explorer.CanCreateFile)
			return;

		// call
		var args = new CreateFileEventArgs(ExplorerModes.None);
		UICreateFile(args);
		if (args.Result != JobResult.Done)
			return;

		// post
		Post(args);

		// show
		Update(true);
		Redraw();
	}

	/// <summary>
	/// Delete action.
	/// </summary>
	/// <param name="force">The force mode flag.</param>
	public void UIDelete(bool force)
	{
		// can?
		if (!Explorer.CanDeleteFiles)
			return;

		// args
		var args = new DeleteFilesEventArgs(ExplorerModes.None, GetSelectedFiles(), force);
		if (args.Files.Count == 0)
			return;

		// call
		UIDeleteWithRecover(args, true);
	}

	/// <summary>
	/// Deletes files, heals selection.
	/// </summary>
	void UIDeleteWithRecover(DeleteFilesEventArgs args, bool redraw)
	{
		// call
		UIDeleteFiles(args);
		if (args.Result == JobResult.Ignore)
			return;

		// to recover
		bool recover = args.Result == JobResult.Incomplete && SelectionExists;

		// update, drop selection
		Update(false);

		// recover selection
		if (recover)
		{
			if (args.FilesToStay.Count > 0)
				SelectFiles(args.FilesToStay, null);
			else
				SelectFiles(args.Files, null);
		}

		// done
		if (redraw)
			Redraw();
	}

	/// <summary>
	/// Called before <see cref="UIEscape"/>.
	/// </summary>
	/// <remarks>
	/// If <see cref="PanelEventArgs.Ignore"/> = true then the core does nothing.
	/// Otherwise it calls <see cref="UIEscape"/> to close the panel.
	/// </remarks>
	public event EventHandler<KeyEventArgs>? Escaping;

	///
	public void WorksEscaping(KeyEventArgs e)
	{
		Escaping?.Invoke(this, e);
	}

	/// <summary>
	/// Called when [Esc] or [ShiftEsc] is pressed and the command line is empty.
	/// </summary>
	/// <param name="force">The force mode parameter.</param>
	/// <remarks>
	/// By default it closes the the panel itself or with all parent panels.
	/// The panel may override this method or use the <see cref="Escaping"/> event.
	/// </remarks>
	public void UIEscape(bool force)
	{
		if (!CanClose())
			return;

		if (force || _Parent is null)
		{
			// _090321_210416 We do not call Redraw(0, 0) to reset cursor to 0 any more.
			// See Mantis 1114: why it was needed. Now FarNet panels restore original state.

			// ask parents
			if (force)
			{
				for (var parent = _Parent; parent != null; parent = parent._Parent)
					if (!parent.CanClose())
						return;
			}

			// close
			_Panel.Close();
		}
		else
		{
			CloseChild();
		}
	}

	/// <summary>
	/// Opens the file in the editor.
	/// </summary>
	/// <param name="file">The file to edit.</param>
	/// <remarks>
	/// The default method calls <see cref="FarNet.Explorer.GetContent"/>  to get a temporary file to edit
	/// and <see cref="FarNet.Explorer.SetFile"/> to save changes when the editor closes.
	/// The explorer should have at least export implemented.
	/// </remarks>
	public virtual void UIEditFile(FarFile file)
	{
		ArgumentNullException.ThrowIfNull(file);

		// target file path
		// _201223_vc Avoid Far.Api.TempName(). I think it reuses names if files do not exist. But file history may exist unexpectedly.
		var temp = Kit.TempFileName(null);

		// export
		var xExportArgs = WorksExportExplorerFile(Explorer, this, ExplorerModes.Edit, file, temp);
		if (xExportArgs is null)
			return;

		// case: actual file exists
		if (!string.IsNullOrEmpty(xExportArgs.UseFileName))
		{
			var editorActual = Far.Api.CreateEditor();
			editorActual.FileName = xExportArgs.UseFileName;
			editorActual.Title = file.Name;
			if (!xExportArgs.CanSet)
				editorActual.IsLocked = true;
			if (xExportArgs.EditorOpened != null)
				editorActual.Opened += xExportArgs.EditorOpened;
			editorActual.Open();
			return;
		}

		// rename
		if (!string.IsNullOrEmpty(xExportArgs.UseFileExtension))
		{
			var temp2 = Path.ChangeExtension(temp, xExportArgs.UseFileExtension);
			File.Move(temp, temp2);
			temp = temp2;
		}

		// editor
		var editorTemp = Far.Api.CreateEditor();
		editorTemp.DisableHistory = true;
		editorTemp.FileName = temp;
		editorTemp.Title = file.Name;
		if (xExportArgs.CodePage != 0)
			editorTemp.CodePage = xExportArgs.CodePage;
		if (xExportArgs.EditorOpened != null)
			editorTemp.Opened += xExportArgs.EditorOpened;

		// future
		//! Not sure why but with used DisableHistory the file reopens with the last caret restored.
		//! If this is some Far feature and it changes then save and set the last caret manually.
		var timeError = DateTime.MinValue;
		if (xExportArgs.CanSet)
		{
			if (Explorer.CanSetText)
			{
				editorTemp.Saving += delegate
				{
					var xImportTextArgs = new SetTextEventArgs(ExplorerModes.Edit, file, editorTemp.GetText());
					Log.Source.TraceInformation("ImportText");
					try
					{
						timeError = DateTime.MinValue;
						UISetText(xImportTextArgs);
					}
					catch (Exception exn)
					{
						//! show first, then save error time
						Far.Api.ShowError("Cannot set text", exn);
						timeError = DateTime.UtcNow;
					}
				};
				editorTemp.Closed += delegate
				{
					//! if error was on saving without closing then on closing "much later" without new saving ignore error
					if (timeError != DateTime.MinValue && (DateTime.UtcNow - timeError).TotalSeconds > 1)
						timeError = DateTime.MinValue;
				};
			}
			else
			{
				editorTemp.Closed += delegate
				{
					if (editorTemp.TimeOfSave == DateTime.MinValue)
						return;

					var xImportFileArgs = new SetFileEventArgs(ExplorerModes.Edit, file, temp);
					Log.Source.TraceInformation("ImportFile");
					try
					{
						UISetFile(xImportFileArgs);
					}
					catch (Exception exn)
					{
						Far.Api.ShowError("Cannot set file", exn);
						timeError = DateTime.UtcNow;
					}
				};
			}
		}
		else
		{
			// lock, do nothing else
			editorTemp.IsLocked = true;
		}

		// loop while errors on saving
		async void Loop()
		{
			try
			{
				for (; ; )
				{
					timeError = DateTime.MinValue;

					await Tasks.Editor(editorTemp);

					if (timeError == DateTime.MinValue)
						break;
				}
			}
			finally
			{
				File.Delete(temp);
			}
		}

		Loop();
	}

	/// <summary>
	/// Opens the file in the viewer.
	/// </summary>
	/// <param name="file">The file to view.</param>
	/// <remarks>
	/// The default method calls <see cref="FarNet.Explorer.GetContent"/> to get a temporary file to view.
	/// The explorer should have it implemented.
	/// </remarks>
	public virtual void UIViewFile(FarFile file)
	{
		ArgumentNullException.ThrowIfNull(file);

		// target
		var temp = Far.Api.TempName();

		// export
		var xExportArgs = WorksExportExplorerFile(Explorer, this, ExplorerModes.View, file, temp);
		if (xExportArgs is null)
			return;

		// case: actual file exists
		if (!string.IsNullOrEmpty(xExportArgs.UseFileName))
		{
			var viewerActual = Far.Api.CreateViewer();
			viewerActual.FileName = xExportArgs.UseFileName;
			viewerActual.Title = file.Name;
			viewerActual.Open();
			return;
		}

		// viewer
		var viewerTemp = Far.Api.CreateViewer();
		viewerTemp.DeleteSource = DeleteSource.File;
		viewerTemp.DisableHistory = true;
		viewerTemp.FileName = temp;
		viewerTemp.Title = file.Name;
		viewerTemp.Switching = Switching.Enabled;
		if (xExportArgs.CodePage != 0)
			viewerTemp.CodePage = xExportArgs.CodePage;

		// open
		viewerTemp.Open();
	}

	/// <summary>
	/// Opens the file.
	/// </summary>
	/// <remarks>
	/// It is called for the current file when [Enter] is pressed.
	/// The base method just calls <see cref="FarNet.Explorer.OpenFile"/> if the explorer supports it.
	/// </remarks>
	/// <param name="file">The file to be opened.</param>
	public virtual void UIOpenFile(FarFile file)
	{
		ArgumentNullException.ThrowIfNull(file);

		if (!Explorer.CanOpenFile)
			return;

		var args = new OpenFileEventArgs(file);
		var explorer = UIOpenFile(args);
		explorer?.OpenPanelChild(this);
	}

	/// <summary>
	/// Clone action.
	/// </summary>
	/// <remarks>
	/// It is called for the current item when [ShiftF5] is pressed.
	/// It calls <see cref="UICloneFile"/> if the explorer supports it.
	/// <para>
	/// Current file after the operation is defined by <c>Post*</c> in the arguments.
	/// </para>
	/// </remarks>
	public void UIClone()
	{
		// can?
		if (!Explorer.CanCloneFile)
			return;

		// file
		var file = CurrentFile;
		if (file is null)
			return;

		// call
		var args = new CloneFileEventArgs(ExplorerModes.None, file);
		UICloneFile(args);
		if (args.Result != JobResult.Done)
			return;

		// post
		Post(args);

		// show
		Update(true);
		Redraw();
	}

	/// <summary>
	/// Rename action.
	/// </summary>
	/// <remarks>
	/// It is called for the current item when [ShiftF6] is pressed.
	/// It calls <see cref="UIRenameFile"/> if the explorer supports it.
	/// <para>
	/// Current file after the operation is defined by <c>Post*</c> in the arguments.
	/// </para>
	/// </remarks>
	public void UIRename()
	{
		// can?
		if (!Explorer.CanRenameFile)
			return;

		// file
		var file = CurrentFile;
		if (file is null)
			return;

		// call
		var args = new RenameFileEventArgs(ExplorerModes.None, file);
		UIRenameFile(args);
		if (args.Result != JobResult.Done)
			return;

		// post
		Post(args);

		// show
		Update(true);
		Redraw();
	}

	/// <summary>
	/// Tells to update and redraw the panel on timer events.
	/// </summary>
	/// <remarks>
	/// This is suitable for panels with frequently changed source data.
	/// Note, you should enable timer events by setting the timer interval.
	/// </remarks>
	/// <seealso cref="Timer"/>
	/// <seealso cref="TimerInterval"/>
	public bool IsTimerUpdate { get; set; }

	/// <include file='doc.xml' path='doc/Timer/*'/>
	/// <seealso cref="IsTimerUpdate"/>
	public event EventHandler? Timer;

	/// <include file='doc.xml' path='doc/TimerInterval/*'/>
	/// <seealso cref="IsTimerUpdate"/>
	public int TimerInterval { get; set; }

	/// <summary>
	/// Called periodically when idle.
	/// </summary>
	/// <remarks>
	/// It is used for panel updating and redrawing if data have changed.
	/// The base method triggers the <see cref="Timer"/> event.
	/// </remarks>
	public virtual void UITimer()
	{
		Timer?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called when a key is pressed.
	/// </summary>
	/// <remarks>
	/// Set <see cref="PanelEventArgs.Ignore"/> = true if the module processes the key itself.
	/// </remarks>
	public event EventHandler<KeyEventArgs>? KeyPressed;

	///
	public void WorksKeyPressed(KeyEventArgs e)
	{
		KeyPressed?.Invoke(this, e);
	}

	/// <summary>
	/// Called when a key is pressed after the <see cref="KeyPressed"/> event.
	/// </summary>
	/// <param name="key">The pressed key.</param>
	/// <returns>True if the key has been processed.</returns>
	public virtual bool UIKeyPressed(KeyInfo key)
	{
		switch (key.VirtualKeyCode)
		{
			case KeyCode.Enter:

				if (key.Is())
				{
					if (RealNames)
						break;

					var file = CurrentFile;
					if (file is null || file.IsDirectory)
						break;

					UIOpenFile(file);
					return true;
				}

				break;

			case KeyCode.F3:

				if (key.Is())
				{
					if (RealNames)
						break;

					var file = CurrentFile;
					if (file is null || file.IsDirectory)
						break;

					UIViewFile(file);
					return true;
				}

				break;

			case KeyCode.F4:

				if (key.Is())
				{
					if (RealNames)
						break;

					var file = CurrentFile;
					if (file is null || file.IsDirectory)
						break;

					UIEditFile(file);
					return true;
				}

				break;

			case KeyCode.F5:

				if (key.Is())
				{
					if (NeedDefaultCopy())
						return false;

					UICopyMove(false);
					return true;
				}

				if (key.IsShift()) //???? if (RealNames) ?
				{
					//! return true even if the file is dots
					UIClone();
					return true;
				}

				break;

			case KeyCode.F6:

				if (key.Is())
				{
					if (NeedDefaultCopy())
						return false;

					UICopyMove(true);
					return true;
				}

				if (key.IsShift()) //???? if (RealNames) ?
				{
					//! return true even if the file is dots
					UIRename();
					return true;
				}

				break;

			case KeyCode.F7:

				if (key.Is())
				{
					if (RealNames && RealNamesMakeDirectory)
						break;

					UICreate();
					return true;
				}

				break;

			case KeyCode.Delete:

				if (Far.Api.CommandLine.Length > 0)
					break;

				goto case KeyCode.F8;

			case KeyCode.F8:

				if (key.Is() || key.IsShift())
				{
					if (RealNames && RealNamesDeleteFiles)
						break;

					UIDelete(key.IsShift());
					return true;
				}

				break;

			//! index -1 ~ no files, e.g. page after the last
			case KeyCode.PageDown when key.Is() && CurrentIndex >= 0:
				{
					int currentIndex;
					FarFile? currentFile;
					if (PageLimit > 0 && (currentIndex = CurrentIndex) >= Files.Count - 1 && null != (currentFile = CurrentFile) && currentFile.Name != "..")
					{
						int topIndex = TopIndex;
						PageOffset += PageLimit;
						NeedsNewFiles = true;

						Update(false);
						Redraw(currentIndex, topIndex);
						return true;
					}
				}
				break;

			//! index -1 ~ no files, e.g. page after the last
			case KeyCode.PageUp when key.Is() && PageLimit > 0 && CurrentIndex <= 0:
				{
					PageOffset -= PageLimit;
					if (PageOffset < 0)
						PageOffset = 0;

					NeedsNewFiles = true;
					Update(false);
					Redraw(0, 0);
					return true;
				}
		}

		return false;
	}

	bool NeedDefaultCopy()
	{
		// target panel
		var panel2 = Far.Api.Panel2;

		// module panel
		if (panel2 is Panel)
			return false;

		// native plugin
		if (panel2!.IsPlugin)
			return true;

		// default if cannot export but can get content
		return !Explorer.CanExportFiles && Explorer.CanGetContent;
	}
}
