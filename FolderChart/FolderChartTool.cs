﻿
// FarNet module FolderChart
// Copyright (c) Roman Kuzmin

using System;
using System.IO;
using System.Linq;
using System.Threading;
using FarNet;

namespace FolderChart
{
	[System.Runtime.InteropServices.Guid("192d8c20-ab65-4807-9654-9387881dd70b")]
	[ModuleTool(Name = "FolderChart", Options = ModuleToolOptions.Panels)]
	public class FolderChartTool : ModuleTool
	{
		const int HIDDEN_FACTOR = 100;
		const int MAX_ITEM_COUNT = 40;

		static void Message(string body)
		{
			Far.Api.Message(body, "Forder Chart", MessageOptions.LeftAligned);
		}

		public override void Invoke(object sender, ModuleToolEventArgs e)
		{
			var panel = Far.Api.Panel;
			if (panel.IsPlugin || panel.Kind != PanelKind.File)
				return;

			var path = panel.CurrentDirectory;

			var run = new SizeRun();
			if (!run.Run(Directory.GetDirectories(path), Directory.GetFiles(path)))
				return;

			var sorted = run.Result.OrderBy(x => x.Size).ToList();
			if (sorted.Count == 0)
				return;

			var totalSize = run.Result.Sum(x => x.Size);
			var title = Kit.FormatSize(totalSize, path);

			var maxSizeToShow = sorted[sorted.Count - 1].Size / HIDDEN_FACTOR;
			long sumHiddenSizes = 0;
			int index = 0;
			for (; index < sorted.Count; ++index)
			{
				if (sorted[index].Size < maxSizeToShow || sorted.Count - index > MAX_ITEM_COUNT)
					sumHiddenSizes += sorted[index].Size;
				else
					break;
			}
			if (index > 0)
				sorted.RemoveRange(0, index);
			if (sumHiddenSizes > 0)
				sorted.Insert(0, new FolderItem() { Name = string.Empty, Size = sumHiddenSizes });

			var errors = run.GetErrors();
			if (errors.Length > 0)
				title = string.Format("{0} ~ Cannot read: {1}", title, errors.Length);

			// handle clicks
			Action<string> action = (result) =>
			{
				Far.Api.PostJob(() =>
				{
					var path2 = Path.Combine(path, result);
					if (Directory.Exists(path2))
					{
						panel.CurrentDirectory = path2;
					}
					else if (File.Exists(path2))
					{
						panel.GoToName(result, false);
					}
					panel.Redraw();
				});
			};

			// start non-blocking form
			var thread = new Thread(() =>
			{
				FolderChartForm.Show(title, sorted, action);
			});
			thread.Start();
		}
	}
}
