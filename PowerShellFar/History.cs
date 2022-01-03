﻿
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using FarNet.Tools;
using System;

namespace PowerShellFar
{
	static class History
	{
		static readonly HistoryLog _log = new(Entry.LocalData + "\\PowerShellFarHistory.log", Settings.Default.MaximumHistoryCount);
		internal static HistoryLog Log { get { return _log; } }
		/// <summary>
		/// Up/Down cache.
		/// </summary>
		static string[] navCache;
		/// <summary>
		/// Up/Down current index.
		/// </summary>
		static int navIndex { get; set; }
		/// <summary>
		/// Removes navigation data.
		/// </summary>
		public static void ResetNavigation()
		{
			navCache = null;
		}
		/// <summary>
		/// Gets history lines.
		/// </summary>
		public static string[] ReadLines()
		{
			return _log.ReadLines();
		}
		/// <summary>
		/// Add a new history line.
		/// </summary>
		public static void AddLine(string value)
		{
			_log.AddLine(value);
			ResetNavigation();
		}
		/// <summary>
		/// For Actor.
		/// </summary>
		public static void ShowHistory()
		{
			var m = new UI.CommandHistoryMenu(string.Empty);
			string code = m.Show();
			if (code == null)
				return;

			// insert to command lines
			switch (Far.Api.Window.Kind)
			{
				case WindowKind.Panels:
					Far.Api.CommandLine.Text = Entry.CommandInvoke1.Prefix + ": " + code;
					return;
				case WindowKind.Editor:
					var editor = Far.Api.Editor;
					if (!(editor.Host is Interactive))
						break;
					editor.GoToEnd(true);
					editor.InsertText(code);
					editor.Redraw();
					return;
				case WindowKind.Dialog:
					var dialog = Far.Api.Dialog;
					var typeId = dialog.TypeId;
					if (typeId != new Guid(Guids.ReadCommandDialog) && typeId != new Guid(Guids.InputDialog))
						break;
					var line = Far.Api.Line;
					if (line == null || line.IsReadOnly)
						break;
					line.Text = code;
					return;
			}

			InvokeInputCode(code);
		}
		static async void InvokeInputCode(string code)
		{
			var ui = new UI.InputDialog() { Title = Res.Me, History = Res.History, Prompt = new string[] { Res.InvokeCommands }, Text = code };
			code = await ui.ShowAsync();

			// invoke input
			if (!string.IsNullOrEmpty(code))
				await Tasks.Job(() => A.Psf.Act(code, null, true));
		}
		public static string GetNextCommand(bool up, string current)
		{
			string lastUsed = null;

			if (navCache == null)
			{
				lastUsed = current;
				navCache = ReadLines();
				navIndex = navCache.Length;
			}
			else if (navIndex >= 0 && navIndex < navCache.Length)
			{
				lastUsed = navCache[navIndex];
			}

			if (up)
			{
				for (; ; )
				{
					if (--navIndex < 0)
					{
						navIndex = -1;
						return string.Empty;
					}
					else
					{
						var command = navCache[navIndex];
						if (command != lastUsed)
							return command;
					}
				}
			}
			else
			{
				for (; ; )
				{
					if (++navIndex >= navCache.Length)
					{
						navIndex = navCache.Length;
						return string.Empty;
					}
					else
					{
						var command = navCache[navIndex];
						if (command != lastUsed)
							return command;
					}
				}
			}
		}
	}
}
