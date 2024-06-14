﻿
// FarNet module CopyColor
// Copyright (c) Roman Kuzmin

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FarNet.CopyColor;

[ModuleTool(Name = ModuleName, Options = ModuleToolOptions.Editor, Id = "e9a7fa32-15e0-4f19-b004-9148df346794")]
public class TheTool : ModuleTool
{
	const string ModuleName = "CopyColor";
	readonly static Guid Colorer = new("d2f36b62-a470-418d-83a3-ed7a3710e5b5");
	readonly static string[] Colors = ["#000000", "#000080", "#008000", "#008080", "#800000", "#800080", "#808000", "#c0c0c0", "#808080", "#0000ff", "#00ff00", "#00ffff", "#ff0000", "#ff00ff", "#ffff00", "#ffffff",];

	static string EncodeHtml(string html)
	{
		return html.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}

	public override void Invoke(object sender, ModuleToolEventArgs e)
	{
		var editor = Far.Api.Editor;

		int iLine1, iLine2;
		if (editor.SelectionExists)
		{
			var rect = editor.SelectionPlace;
			iLine1 = rect.First.Y;
			iLine2 = rect.Last.Y;
			if (rect.Last.X < 0)
				--iLine2;
		}
		else
		{
			iLine1 = editor.Caret.Y;
			iLine2 = iLine1;
		}

		var linetexts = new List<string>();
		var linespans = new List<EditorColorInfo[]>();
		var bgcount = new int[16];

		var allColors = new List<EditorColorInfo>();
		for (int line = iLine1; line <= iLine2; ++line)
		{
			var text = editor[line].Text;
			linetexts.Add(text);

			var colors = new EditorColorInfo[text.Length];
			linespans.Add(colors);

			editor.GetColors(line, allColors);
			var colorerColors = allColors.Where(x => x.Owner == Colorer).ToList();
			int min = colorerColors.Count > 0 ? colorerColors.Min(x => x.Start) : 1;
			int max = colorerColors.Count > 0 ? colorerColors.Max(x => x.End) : -1;
			if (min > 0 || max < text.Length)
			{
				Far.Api.Message(@"
Cannot copy: part of the selected test has no colors.
Try to scroll the text. Long lines are not supported.
", ModuleName);
				return;
			}

			foreach (var span in colorerColors)
			{
				for (int ch = span.Start; ch < span.End; ++ch)
				{
					++bgcount[(int)span.Background];
					if (ch < text.Length)
						colors[ch] = span;
				}
			}
		}

		int bgindex = Array.IndexOf(bgcount, bgcount.Max());
		var bgcolor = (ConsoleColor)bgindex;

		var sbText = new StringBuilder();
		var sbHtml = new StringBuilder();
		sbHtml.AppendFormat(@"<div style=""background-color:{0};""><pre>", Colors[bgindex]);
		sbHtml.AppendLine();

		for (int line = 0; line < linetexts.Count; ++line)
		{
			var text = linetexts[line];
			var colors = linespans[line];

			sbText.AppendLine(text);

			for (int start = 0; start < text.Length; )
			{
				var color = colors[start];

				int end = start + 1;
				while (end < text.Length && colors[end].Background == color.Background && colors[end].Foreground == color.Foreground)
					++end;

				var html = EncodeHtml(text[start..end]);
				if (color.Background == bgcolor)
					sbHtml.AppendFormat(@"<span style=""color:{0};"">{1}</span>",
						Colors[(int)color.Foreground], html);
				else
					sbHtml.AppendFormat(@"<span style=""color:{0}; background-color:{1};"">{2}</span>",
						Colors[(int)color.Foreground], Colors[(int)color.Background], html);

				start = end;
			}

			sbHtml.AppendLine();
		}

		sbHtml.AppendLine("</pre></div>");

		ClipboardHelper.CopyToClipboard(sbHtml.ToString(), sbText.ToString());
	}
}
