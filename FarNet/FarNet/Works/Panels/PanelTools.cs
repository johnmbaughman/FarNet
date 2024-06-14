﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.IO;

namespace FarNet.Works;
#pragma warning disable 1591

public static class PanelTools
{
	public static void GoToPath(IPanel panel, string path)
	{
		ArgumentNullException.ThrowIfNull(path);

		//! can be null, e.g. for '\'
		var dir = Path.GetDirectoryName(path);
		if (dir == null && (path.StartsWith('\\') || path.StartsWith('/')))
			dir = "\\";

		if (!string.IsNullOrEmpty(dir))
		{
			panel.CurrentDirectory = dir;
			panel.Redraw();
		}

		var name = Path.GetFileName(path);
		if (name.Length > 0)
			panel.GoToName(name);
	}

	public static void ResizeColumn(Panel panel, bool right)
	{
		ArgumentNullException.ThrowIfNull(panel);

		var view = panel.ViewMode;
		var plan = panel.GetPlan(view) ?? panel.ViewPlan;
		if (plan.Columns!.Length != 2)
			return;

		int width = panel.Window.Width - 2;
		plan.Columns[0].Width = 0;
		if (plan.Columns[1].Width == 0)
			plan.Columns[1].Width = width / 2;

		int width2 = plan.Columns[1].Width;
		if (right)
		{
			--width2;
			if (width2 < 1)
				return;
		}
		else
		{
			++width2;
			if (width2 > width - 2)
				return;
		}

		plan.Columns[1].Width = width2;
		panel.SetPlan(view, plan);
		panel.Redraw();
	}

	public static void SwitchFullScreen(Panel panel)
	{
		ArgumentNullException.ThrowIfNull(panel);

		// get/make the plan
		var iViewMode = panel.ViewMode;
		var plan = panel.GetPlan(iViewMode) ?? panel.ViewPlan;

		// drop widths of text columns
		foreach (var c in plan.Columns!)
			if (c.Kind == "N" || c.Kind == "Z" || c.Kind == "O")
				c.Width = 0;

		// switch
		plan.IsFullScreen = !plan.IsFullScreen;

		// set
		panel.SetPlan(iViewMode, plan);
		panel.Redraw();
	}

	const string
		sPushPanel = "Push panel",
		sShelvePanel = "Shelve panel",
		sSwitchFullScreen = "Switch full screen",
		sResizeColum1 = "Decrease left column",
		sResizeColum2 = "Increase left column",
		sClose = "Close panel";

	public static void ShowPanelsMenu()
	{
		var menu = Far.Api.CreateMenu();
		menu.AutoAssignHotkeys = true;
		menu.HelpTopic = "panels-menu";
		menu.ShowAmpersands = true;
		menu.Title = "Panels";

		menu.AddKey(KeyCode.Delete);
		menu.AddKey(KeyCode.Spacebar);

		for (; ; menu.Items.Clear())
		{
			var panel = Far.Api.Panel!;
			Panel? module = null;

			// Push/Shelve
			if (panel.IsPlugin)
			{
				module = panel as Panel;
				if (module is not null)
				{
					menu.Add(sPushPanel);
					menu.Add(sResizeColum1);
					menu.Add(sResizeColum2);
					menu.Add(sSwitchFullScreen);
				}

				menu.Add(sClose);
			}
			else if (panel.Kind == PanelKind.File)
			{
				menu.Add(sShelvePanel);
			}

			// Pop/Unshelve
			if (ShelveInfo.Stack.Count > 0)
			{
				menu.Add("Pop/Unshelve").IsSeparator = true;

				foreach (var si in ShelveInfo.Stack)
					menu.Add(si.Title).Data = si;
			}

			if (!menu.Show())
				return;

			var mi = menu.Items[menu.Selected];

			// [Delete]:
			if (menu.Key.VirtualKeyCode == KeyCode.Delete)
			{
				// remove the shelved panel; do not remove module panels because of their shutdown bypassed
				var si = (ShelveInfo?)mi.Data;
				if (si!.CanRemove)
					ShelveInfo.Stack.Remove(si);

				continue;
			}

			// Push/Shelve
			if (mi.Text == sPushPanel || mi.Text == sShelvePanel)
			{
				panel.Push();
				return;
			}

			bool repeat = menu.Key.VirtualKeyCode == KeyCode.Spacebar;

			// Decrease/Increase column
			if (mi.Text == sResizeColum1 || mi.Text == sResizeColum2)
			{
				ResizeColumn(module!, mi.Text == sResizeColum2);
				if (repeat)
					continue;
				else
					return;
			}

			// Full screen
			if (mi.Text == sSwitchFullScreen)
			{
				SwitchFullScreen(module!);
				if (repeat)
					continue;
				else
					return;
			}

			// Close panel
			if (mi.Text == sClose)
			{
				// native plugin panel: go to the first item to work around "Far does not restore panel state",
				// this does not restore either but is still better than unexpected current item after exit.
				if (module is null)
				{
					panel.Redraw(0, 0);
					panel.Close();
				}
				else
				{
					module.UIEscape(true);
				}
				return;
			}

			// Pop/Unshelve
			var shelve = (ShelveInfo?)mi.Data;
			shelve!.Pop();
			return;
		}
	}
}
