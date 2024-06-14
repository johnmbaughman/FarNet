
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PowerShellFar;

/// <summary>
/// Base class of PowerShellFar panels.
/// </summary>
/// <remarks>
/// <para>
/// Terminology (both for names and documentation):
/// "files" (<c>FarFile</c>) are elements of <see cref="Panel"/>;
/// "items" (<c>PSObject</c>) are <see cref="FarFile.Data"/> attached to "files".
/// Note that null and ".." items are not processed (e.g. by <see cref="ShownItems"/>
/// and <see cref="SelectedItems"/>).
/// </para>
/// </remarks>
public partial class AnyPanel : Panel
{
	/// <summary>
	/// New panel with the explorer.
	/// </summary>
	/// <param name="explorer">The panel explorer.</param>
	public AnyPanel(Explorer explorer) : base(explorer)
	{
		// settings
		DotsMode = PanelDotsMode.Dots;

		// start mode
		ViewMode = PanelViewMode.AlternativeFull;
	}

	static PSObject? ConvertFileToItem(FarFile? file)
	{
		if (file is null || file.Data is null)
			return null;

		return PSObject.AsPSObject(file.Data);
	}

	/// <summary>
	/// Gets all items. See <see cref="AnyPanel"/> remarks.
	/// </summary>
	/// <remarks>
	/// Items are returned according to the current panel filter and sort order.
	/// <para>
	/// WARNING: it is <c>IEnumerable</c>, not a list or an array.
	/// </para>
	/// <example>
	/// OK:
	/// <code>
	/// foreach($item in $panel.ShownItems) { ... }
	/// $panel.ShownItems | ...
	/// </code>
	/// ERROR:
	/// <code>
	/// $panel.ShownItems.Count
	/// $panel.ShownItems[$index]
	/// </code>
	/// OK: use operator @() when you really want an array:
	/// <code>
	/// $items = @($panel.ShownItems)
	/// $items.Count
	/// $items[$index]
	/// </code>
	/// </example>
	/// </remarks>
	public IEnumerable<PSObject?> ShownItems
	{
		get
		{
			foreach (FarFile file in GetFiles())
				yield return ConvertFileToItem(file);
		}
	}

	/// <summary>
	/// Gets selected items. See <see cref="AnyPanel"/> and <see cref="ShownItems"/> remarks.
	/// </summary>
	public IList<PSObject?> SelectedItems => GetSelectedFiles().Select(x => ConvertFileToItem(x)).ToList();

	/// <summary>
	/// Gets the current item. It can be null (e.g. if ".." is the current file).
	/// </summary>
	public PSObject? CurrentItem => ConvertFileToItem(CurrentFile);

	/// <summary>
	/// Tells to treat the items as not directories even if they have a directory flag.
	/// </summary>
	internal bool IgnoreDirectoryFlag { get; set; } // _090810_180151

	/// <inheritdoc/>
	/// <remarks>
	/// Prepares for opening this panel as a child panel.
	/// </remarks>
	protected override bool CanOpenAsChild(Panel parent)
	{
		ArgumentNullException.ThrowIfNull(parent);

		if (Lookup is null)
			return true;

		// lookup: try to post the current
		if (this is TablePanel)
		{
			// assume parent Description = child Name
			PostName(parent.CurrentFile!.Description);
		}

		return true;
	}

	/// <summary>
	/// Shows help.
	/// </summary>
	internal virtual void ShowHelpForPanel()
	{
		Entry.Instance.ShowHelpTopic(HelpTopic.PowerPanel);
	}

	/// <summary>
	/// Shows help menu (e.g. called on [F1]).
	/// </summary>
	/// <seealso cref="MenuCreating"/>
	public void ShowMenu()
	{
		IMenu menu = HelpMenuCreate();
		if (menu.Show())
		{
			if (menu.Key.VirtualKeyCode == KeyCode.F1)
				ShowHelpForPanel();
		}
	}

	/// <summary>Apply command.</summary>
	internal virtual void UIApply()
	{
	}

	/// <summary>Attributes action.</summary>
	internal virtual void UIAttributes()
	{
	}

	internal virtual bool UICopyMoveCan(bool move) //?????
	{
		return !move && TargetPanel is ObjectPanel;
	}

	/// <summary>
	/// Shows help or the panel menu.
	/// </summary>
	internal void UIHelp() => ShowMenu();

	/// <summary>Mode action.</summary>
	internal virtual void UIMode()
	{
	}

	///
	internal void UIOpenFileMembers()
	{
		var file = CurrentFile;
		if (file != null)
			OpenFileMembers(file);
	}

	/// <summary>
	/// Updates Far data and redraws.
	/// </summary>
	internal void UpdateRedraw(bool keepSelection)
	{
		Update(keepSelection);
		Redraw();
	}

	/// <summary>
	/// Updates Far data and redraws with positions.
	/// </summary>
	internal void UpdateRedraw(bool keepSelection, int current, int top)
	{
		Update(keepSelection);
		Redraw(current, top);
	}

	/// <summary>
	/// Sets a handler called on [Enter] and makes this panel lookup.
	/// </summary>
	/// <remarks>
	/// If it is set than [Enter] triggers this handler and closes the panel.
	/// This is normally used for selecting an item from a table.
	/// The task of this handler is to use this selected item.
	/// <para>
	/// Lookup panels are usually derived from <see cref="TablePanel"/>.
	/// Panels derived from <see cref="ListPanel"/> also can be lookup but this scenario is not tested.
	/// </para>
	/// </remarks>
	/// <seealso cref="MemberPanel.CreateDataLookup"/>
	public ScriptHandler<OpenFileEventArgs>? Lookup { get; set; }

	/// <summary>
	/// Opens the file member panel.
	/// </summary>
	/// <remarks>
	/// The base method propagates lookup openers.
	/// </remarks>
	internal virtual MemberPanel? OpenFileMembers(FarFile file)
	{
		//??? _090610_071700, + $panel.SetOpen({ @ Test-Panel-Tree-.ps1
		object target = file.Data ?? file;

		var panel = new MemberPanel(new MemberExplorer(target))
		{
			_LookupOpeners = _LookupOpeners
		};

		if (Far.Api.Panel is TablePanel tablePanel)
		{
			if (!string.IsNullOrEmpty(tablePanel.ExcludeMemberPattern))
				panel.Explorer.ExcludeMemberPattern = tablePanel.ExcludeMemberPattern;

			if (!string.IsNullOrEmpty(tablePanel.HideMemberPattern))
				panel.Explorer.HideMemberPattern = tablePanel.HideMemberPattern;
		}

		//! use null as parent: this panel can be not open now
		panel.OpenChild(null);
		return panel;
	}

	/// <summary>
	/// The last user action.
	/// </summary>
	internal UserAction UserWants { get; set; }

	/// <include file='doc.xml' path='doc/AddLookup/*'/>
	public void AddLookup(string name, object handler)
	{
		_LookupOpeners ??= [];
		_LookupOpeners.Add(name, new ScriptHandler<OpenFileEventArgs>(handler));
	}

	/// <summary>
	/// Adds name/handler pairs to the lookup collection.
	/// </summary>
	/// <param name="lookups">The dictionary with name/handler pairs.</param>
	public void AddLookup(IDictionary lookups)
	{
		if (lookups != null)
			foreach (DictionaryEntry it in lookups)
				AddLookup(it.Key.ToString()!, it.Value!);
	}

	internal Dictionary<string, ScriptHandler<OpenFileEventArgs>>? _LookupOpeners;

	/// <summary>
	/// Creates or gets existing menu.
	/// </summary>
	IMenu HelpMenuCreate()
	{
		// create
		IMenu r = Far.Api.CreateMenu();
		r.AutoAssignHotkeys = true;
		r.Sender = this;
		r.Title = "Help menu";
		r.AddKey(KeyCode.F1);

		// args
		var e = new PanelMenuEventArgs(r, CurrentFile, SelectedFiles);

		// event
		MenuCreating?.Invoke(this, e);

		// init items
		var items = new HelpMenuItems();
		HelpMenuInitItems(items, e);

		// add main items
		if (items.OpenFile != null) r.Items.Add(items.OpenFile);
		if (items.OpenFileMembers != null) r.Items.Add(items.OpenFileMembers);
		if (items.OpenFileAttributes != null) r.Items.Add(items.OpenFileAttributes);
		if (items.ApplyCommand != null) r.Items.Add(items.ApplyCommand);
		if (items.Copy != null) r.Items.Add(items.Copy);
		if (items.CopyHere != null) r.Items.Add(items.CopyHere);
		if (items.Move != null) r.Items.Add(items.Move);
		if (items.Rename != null) r.Items.Add(items.Rename);
		if (items.Create != null) r.Items.Add(items.Create);
		if (items.Delete != null) r.Items.Add(items.Delete);
		if (items.Save != null) r.Items.Add(items.Save);
		if (items.Exit != null) r.Items.Add(items.Exit);
		if (items.Help != null) r.Items.Add(items.Help);

		return r;
	}

	/// <summary>
	/// Derived should add its items, then call base.
	/// </summary>
	internal virtual void HelpMenuInitItems(HelpMenuItems items, PanelMenuEventArgs e)
	{
		items.Exit ??= new SetItem()
		{
			Text = "E&xit panel",
			Click = delegate { Close(); }
		};

		items.Help ??= new SetItem()
		{
			Text = "Help (F1)",
			Click = delegate { ShowHelpForPanel(); }
		};
	}

	/// <summary>
	/// Called when the panel help menu is just created (e.g. on [F1]).
	/// </summary>
	/// <remarks>
	/// You may add your menu items to the menu. The sender of the event is this panel (<c>$this</c>).
	/// </remarks>
	/// <seealso cref="ShowMenu"/>
	public event EventHandler<PanelMenuEventArgs>? MenuCreating;

	/// <inheritdoc/>
	public override bool UIKeyPressed(KeyInfo key)
	{
		ArgumentNullException.ThrowIfNull(key);

		UserWants = UserAction.None;
		try
		{
			switch (key.VirtualKeyCode)
			{
				case KeyCode.Enter:

					if (key.Is())
					{
						var file = CurrentFile;
						if (file is null)
							break;

						UserWants = UserAction.Enter;

						if (file.IsDirectory && !IgnoreDirectoryFlag)
							break;

						UIOpenFile(file);
						return true;
					}

					if (key.IsShift())
					{
						UIAttributes();
						return true;
					}

					break;

				case KeyCode.F1:

					if (key.Is())
					{
						UIHelp();
						return true;
					}

					break;

				case KeyCode.F3:

					if (key.Is())
					{
						if (CurrentFile is null)
						{
							UIViewAll();
							return true;
						}
						break;
					}

					if (key.IsShift())
					{
						ShowMenu();
						return true;
					}

					break;

				case KeyCode.PageDown:

					if (key.IsCtrl())
					{
						UIOpenFileMembers();
						return true;
					}

					break;

				case KeyCode.A:

					if (key.IsCtrl())
					{
						UIAttributes();
						return true;
					}

					break;

				case KeyCode.G:

					if (key.IsCtrl())
					{
						UIApply();
						return true;
					}

					break;

				case KeyCode.M:

					if (key.IsCtrlShift())
					{
						UIMode();
						return true;
					}

					break;

				case KeyCode.S:

					//! Mantis#2635 Ignore if auto-completion menu is opened
					if (key.IsCtrl() && Far.Api.Window.Kind != WindowKind.Menu)
					{
						SaveData();
						return true;
					}

					break;
			}

			// base
			return base.UIKeyPressed(key);
		}
		finally
		{
			UserWants = UserAction.None;
		}
	}

	/// <inheritdoc/>
	/// <remarks>
	/// The method prompts for the new file name and then calls the base method.
	/// </remarks>
	public override void UICloneFile(CloneFileEventArgs args)
	{
		ArgumentNullException.ThrowIfNull(args);

		// prompt
		IInputBox input = Far.Api.CreateInputBox();
		input.EmptyEnabled = true;
		input.Title = "Copy";
		input.Prompt = "New name";
		input.History = "Copy";
		input.Text = args.File.Name;
		if (!input.Show())
		{
			args.Result = JobResult.Ignore;
			return;
		}

		// new name
		args.Parameter = input.Text;

		// base
		base.UICloneFile(args);
	}

	/// <inheritdoc/>
	public override void UIRenameFile(RenameFileEventArgs args)
	{
		ArgumentNullException.ThrowIfNull(args);

		// prompt
		IInputBox input = Far.Api.CreateInputBox();
		input.EmptyEnabled = true;
		input.Title = "Rename";
		input.Prompt = "New name";
		input.History = "Copy";
		input.Text = args.File.Name;
		if (!input.Show() || input.Text == args.File.Name)
		{
			args.Result = JobResult.Ignore;
			return;
		}

		// set new name and post it
		args.Parameter = input.Text;
		args.PostName = input.Text;

		// base
		base.UIRenameFile(args);
	}
}
