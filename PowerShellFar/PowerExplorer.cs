﻿
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PowerShellFar;

/// <summary>
/// Explorer designed for PowerShell scripts.
/// </summary>
/// <inheritdoc/>
public class PowerExplorer(Guid typeId) : Explorer(typeId)
{
	/// <summary>
	/// Gets the collection ready to use as the file cache, if needed.
	/// </summary>
	public IList<FarFile> Cache
	{
		get => _Cache;
		internal set => _Cache = value ?? new List<FarFile>();
	}
	IList<FarFile> _Cache = new List<FarFile>();

	/// <summary>
	/// Gets or sets the user data object.
	/// </summary>
	/// <remarks>
	/// Normally it should be set on creation to describe the assigned explorer location,
	/// so that other explorer methods can use this information. There is no much sense
	/// to change these data later (note: each explorer deals with one fixed location).
	/// But it is fine to cache files in here and refresh them when needed.
	/// </remarks>
	public PSObject? Data { get; set; }

	/// <summary>
	/// <see cref="Explorer.GetFiles"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual IList<FarFile> DoGetFiles(GetFilesEventArgs args) => Cache;

	/// <summary>
	/// <see cref="Explorer.GetFiles"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ExplorerEventArgs"/>.
	/// <para>
	/// The script outputs files or nothing. In the latter case the predefined <see cref="Cache"/> list is used.
	/// </para>
	/// </remarks>
	public ScriptBlock? AsGetFiles { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override IEnumerable<FarFile> GetFiles(GetFilesEventArgs args)
	{
		if (AsGetFiles is null)
			return DoGetFiles(args);

		// nothing, use the predefined file list
		var output = AsGetFiles.Invoke(this, args);
		if (output.Count == 0)
			return Cache;

		// convert the output to files
		var result = new List<FarFile>();
		foreach (var it in output)
		{
			FarFile file = (FarFile)LanguagePrimitives.ConvertTo(it, typeof(FarFile), null);
			if (file != null)
				result.Add(file);
		}

		return result;
	}

	/// <summary>
	/// <see cref="Explorer.ExploreDirectory"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual Explorer? DoExploreDirectory(ExploreDirectoryEventArgs args) => null;

	/// <summary>
	/// <see cref="Explorer.ExploreDirectory"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ExploreDirectoryEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsExploreDirectory { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override Explorer? ExploreDirectory(ExploreDirectoryEventArgs args)
	{
		if (AsExploreDirectory is null)
			return DoExploreDirectory(args);
		else
			return InvokeExplorerScript(AsExploreDirectory, args);
	}

	/// <summary>
	/// <see cref="Explorer.ExploreLocation"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual Explorer? DoExploreLocation(ExploreLocationEventArgs args) => null;

	/// <summary>
	/// <see cref="Explorer.ExploreLocation"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ExploreLocationEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsExploreLocation { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override Explorer? ExploreLocation(ExploreLocationEventArgs args)
	{
		if (AsExploreLocation is null)
			return DoExploreLocation(args);
		else
			return InvokeExplorerScript(AsExploreLocation, args);
	}

	/// <summary>
	/// <see cref="Explorer.ExploreParent"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual Explorer? DoExploreParent(ExploreParentEventArgs args) => null;

	/// <summary>
	/// <see cref="Explorer.ExploreParent"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ExplorerEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsExploreParent { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override Explorer? ExploreParent(ExploreParentEventArgs args)
	{
		if (AsExploreParent is null)
			return DoExploreParent(args);
		else
			return InvokeExplorerScript(AsExploreParent, args);
	}

	/// <summary>
	/// <see cref="Explorer.ExploreRoot"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual Explorer? DoExploreRoot(ExploreRootEventArgs args) => null;

	/// <summary>
	/// <see cref="Explorer.ExploreRoot"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ExplorerEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsExploreRoot { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override Explorer? ExploreRoot(ExploreRootEventArgs args)
	{
		if (AsExploreRoot is null)
			return DoExploreRoot(args);
		else
			return InvokeExplorerScript(AsExploreRoot, args);
	}

	/// <summary>
	/// <see cref="Explorer.GetContent"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoGetContent(GetContentEventArgs args) => base.GetContent(args);

	/// <summary>
	/// <see cref="Explorer.GetContent"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="GetContentEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsGetContent { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void GetContent(GetContentEventArgs args)
	{
		if (AsGetContent is null)
			DoGetContent(args);
		else
			AsGetContent.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.SetFile"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoSetFile(SetFileEventArgs args) => base.SetFile(args);

	/// <summary>
	/// <see cref="Explorer.SetFile"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="SetFileEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsSetFile
	{
		get => _AsSetFile;
		set
		{
			_AsSetFile = value;
			if (value != null)
				Functions |= ExplorerFunctions.SetFile;
		}
	}
	ScriptBlock? _AsSetFile;

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void SetFile(SetFileEventArgs args)
	{
		if (AsSetFile is null)
			DoSetFile(args);
		else
			AsSetFile.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.SetText"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoSetText(SetTextEventArgs args) => base.SetText(args);

	/// <summary>
	/// <see cref="Explorer.SetText"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="SetTextEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsSetText
	{
		get => _AsSetText;
		set
		{
			_AsSetText = value;
			if (value != null)
				Functions |= ExplorerFunctions.SetText;
		}
	}
	ScriptBlock? _AsSetText;

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void SetText(SetTextEventArgs args)
	{
		if (AsSetText is null)
			DoSetText(args);
		else
			AsSetText.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.CloneFile"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoCloneFile(CloneFileEventArgs args) => base.CloneFile(args);

	/// <summary>
	/// <see cref="Explorer.CloneFile"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="CloneFileEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsCloneFile { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void CloneFile(CloneFileEventArgs args)
	{
		if (AsCloneFile is null)
			DoCloneFile(args);
		else
			AsCloneFile.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.CreateFile"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoCreateFile(CreateFileEventArgs args) => base.CreateFile(args);

	/// <summary>
	/// <see cref="Explorer.CreateFile"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="CreateFileEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsCreateFile { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void CreateFile(CreateFileEventArgs args)
	{
		if (AsCreateFile is null)
			DoCreateFile(args);
		else
			AsCreateFile.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.OpenFile"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual Explorer? DoOpenFile(OpenFileEventArgs args) => base.OpenFile(args);

	/// <summary>
	/// <see cref="Explorer.OpenFile"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="OpenFileEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsOpenFile { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override Explorer? OpenFile(OpenFileEventArgs args)
	{
		if (AsOpenFile is null)
			return DoOpenFile(args);
		else
			return InvokeExplorerScript(AsOpenFile, args);
	}

	/// <summary>
	/// <see cref="Explorer.RenameFile"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoRenameFile(RenameFileEventArgs args) => base.RenameFile(args);

	/// <summary>
	/// <see cref="Explorer.RenameFile"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="RenameFileEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsRenameFile { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void RenameFile(RenameFileEventArgs args)
	{
		if (AsRenameFile is null)
			DoRenameFile(args);
		else
			AsRenameFile.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.AcceptFiles"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoAcceptFiles(AcceptFilesEventArgs args) => base.AcceptFiles(args);

	/// <summary>
	/// <see cref="Explorer.AcceptFiles"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="AcceptFilesEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsAcceptFiles { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void AcceptFiles(AcceptFilesEventArgs args)
	{
		if (AsAcceptFiles is null)
			DoAcceptFiles(args);
		else
			AsAcceptFiles.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.ImportFiles"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoImportFiles(ImportFilesEventArgs args) => base.ImportFiles(args);

	/// <summary>
	/// <see cref="Explorer.ImportFiles"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ImportFilesEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsImportFiles { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void ImportFiles(ImportFilesEventArgs args)
	{
		if (AsImportFiles is null)
			DoImportFiles(args);
		else
			AsImportFiles.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.ExportFiles"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoExportFiles(ExportFilesEventArgs args) => base.ExportFiles(args);

	/// <summary>
	/// <see cref="Explorer.ExportFiles"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="ExportFilesEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsExportFiles { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void ExportFiles(ExportFilesEventArgs args)
	{
		if (AsExportFiles is null)
			DoExportFiles(args);
		else
			AsExportFiles.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.DeleteFiles"/> worker.
	/// </summary>
	/// <param name="args">.</param>
	public virtual void DoDeleteFiles(DeleteFilesEventArgs args) => base.DeleteFiles(args);

	/// <summary>
	/// <see cref="Explorer.DeleteFiles"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="DeleteFilesEventArgs"/>.
	/// </remarks>
	public ScriptBlock? AsDeleteFiles { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="args">.</param>
	public sealed override void DeleteFiles(DeleteFilesEventArgs args)
	{
		if (AsDeleteFiles is null)
			DoDeleteFiles(args);
		else
			AsDeleteFiles.InvokeReturnAsIs(this, args);
	}

	/// <summary>
	/// <see cref="Explorer.CreatePanel"/> worker.
	/// </summary>
	public virtual Panel DoCreatePanel() => base.CreatePanel();

	/// <summary>
	/// <see cref="Explorer.CreatePanel"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer.
	/// </remarks>
	public ScriptBlock? AsCreatePanel { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	public sealed override Panel CreatePanel()
	{
		if (AsCreatePanel is null)
			return DoCreatePanel();
		else
			return (Panel)LanguagePrimitives.ConvertTo(AsCreatePanel.InvokeReturnAsIs(this), typeof(Panel), null);
	}

	/// <summary>
	/// <see cref="Explorer.EnterPanel"/> worker.
	/// </summary>
	/// <param name="panel">The panel.</param>
	public virtual void DoEnterPanel(Panel panel) => base.EnterPanel(panel);

	/// <summary>
	/// <see cref="Explorer.EnterPanel"/> worker.
	/// </summary>
	/// <remarks>
	/// Arguments: 0: this explorer, 1: <see cref="Panel"/> to be updated.
	/// </remarks>
	public ScriptBlock? AsEnterPanel { get; set; }

	/// <include file='doc.xml' path='doc/ScriptFork/*'/>
	/// <param name="panel">The panel.</param>
	public sealed override void EnterPanel(Panel panel)
	{
		if (AsEnterPanel is null)
			DoEnterPanel(panel);
		else
			AsEnterPanel.InvokeReturnAsIs(this, panel);
	}

	internal Explorer? InvokeExplorerScript(ScriptBlock script, ExplorerEventArgs args)
	{
		var data = script.Invoke(this, args);
		if (data.Count == 0)
			return null;

		return (Explorer)LanguagePrimitives.ConvertTo(data[0], typeof(Explorer), null);
	}
}
