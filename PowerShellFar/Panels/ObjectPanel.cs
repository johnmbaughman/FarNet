
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System.Collections.Generic;
using System.Management.Automation;

namespace PowerShellFar;

/// <summary>
/// .NET objects panel.
/// </summary>
public class ObjectPanel : FormatPanel
{
	/// <summary>
	/// Gets the panel explorer.
	/// </summary>
	public new ObjectExplorer Explorer => (ObjectExplorer)base.Explorer;

	/// <summary>
	/// New object panel with the object explorer.
	/// </summary>
	/// <param name="explorer">The panel explorer.</param>
	public ObjectPanel(ObjectExplorer explorer) : base(explorer)
	{
		CurrentLocation = "*";
		SortMode = PanelSortMode.Unsorted;
	}

	///
	public ObjectPanel() : this(new ObjectExplorer())
	{
	}

	/// <inheritdoc/>
	protected override string DefaultTitle => "Objects";

	/// <summary>
	/// Adds a single objects to the panel as it is.
	/// </summary>
	/// <param name="value">The object to add.</param>
	public void AddObject(object value)
	{
		if (value != null)
			Explorer.AddedValues.Add(PSObject.AsPSObject(value));
	}

	/// <summary>
	/// Adds objects to the panel.
	/// </summary>
	/// <param name="values">Objects represented by enumerable or a single object.</param>
	public void AddObjects(object values) { Explorer.AddObjects(values); }

	/// <summary>
	/// Exports objects to Clixml file.
	/// </summary>
	public override bool SaveData()
	{
		UI.ExportDialog.ExportClixml(CollectData(), StartDirectory);
		return true;
	}

	///
	internal override void HelpMenuInitItems(HelpMenuItems items, PanelMenuEventArgs e)
	{
		items.Save ??= new SetItem()
		{
			Text = "Export .clixml...",
			Click = delegate { SaveData(); }
		};

		base.HelpMenuInitItems(items, e);
	}

	/// <summary>
	/// Files data.
	/// </summary>
	List<object> CollectData()
	{
		var Files = Explorer.Cache;
		var r = new List<object>
		{
			Capacity = Files.Count
		};
		foreach (FarFile f in Files)
			if (f.Data != null)
				r.Add(f.Data);
		return r;
	}
}
