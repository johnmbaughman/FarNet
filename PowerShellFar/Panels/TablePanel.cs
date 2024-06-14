
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;

namespace PowerShellFar;

/// <summary>
/// Abstract table panel.
/// </summary>
public abstract class TablePanel : AnyPanel
{
	/// <summary>
	/// Gets the panel explorer.
	/// </summary>
	public new TableExplorer Explorer => (TableExplorer)base.Explorer;

	internal TablePanel(TableExplorer explorer) : base(explorer)
	{
	}

	/// <include file='doc.xml' path='doc/Columns/*'/>
	public object[]? Columns
	{
		get => Explorer.Columns;
		set => Explorer.Columns = value;
	}

	/// <summary>
	/// Regular expression pattern of members to be excluded in a child <see cref="ListPanel"/>.
	/// </summary>
	public string? ExcludeMemberPattern { get; set; }

	/// <summary>
	/// Regular expression pattern of members to be hidden in a child <see cref="ListPanel"/>.
	/// </summary>
	public string? HideMemberPattern { get; set; }

	internal abstract string HelpMenuTextOpenFileMembers { get; }

	///
	internal override void HelpMenuInitItems(HelpMenuItems items, PanelMenuEventArgs e)
	{
		items.OpenFileMembers ??= new SetItem()
		{
			Text = HelpMenuTextOpenFileMembers,
			Click = delegate { UIOpenFileMembers(); }
		};

		items.ApplyCommand ??= new SetItem()
		{
			Text = Res.UIApply,
			Click = delegate { UIApply(); }
		};

		base.HelpMenuInitItems(items, e);
	}

	internal override void UIApply() => A.InvokePipelineForEach(SelectedItems!);
}
