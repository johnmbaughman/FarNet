
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using System.Text;

namespace PowerShellFar.UI;

static class ChoiceMsg
{
	static void ShowHelpForChoices(Collection<ChoiceDescription> choices)
	{
		var sb = new StringBuilder();
		sb.AppendLine("Escape - more options, e.g. to halt the command.");
		foreach (ChoiceDescription choice in choices)
		{
			int a = choice.Label.IndexOf('&');
			if (a >= 0 && a + 1 < choice.Label.Length)
				sb.Append((choice.Label[a + 1].ToString()).ToUpperInvariant());
			else
				sb.Append(choice.Label);
			sb.Append(" - ");
			if (!string.IsNullOrEmpty(choice.HelpMessage))
				sb.AppendLine(choice.HelpMessage);
			else if (a >= 0)
				sb.AppendLine(choice.Label.Replace("&", string.Empty));
			else
				sb.AppendLine();
		}
		Far.Api.AnyViewer.ViewText(sb.ToString(), "Help", OpenMode.Modal);
	}

	static public int Show(string caption, string message, Collection<ChoiceDescription> choices)
	{
		// buttons
		string[] buttons = new string[choices.Count + 1];
		buttons[choices.Count] = "Help&?";
		for (int i = choices.Count; --i >= 0; )
			buttons[i] = choices[i].Label;

		// show
		for (; ; )
		{
			int answer = Far.Api.Message(message, caption, MessageOptions.LeftAligned, buttons);

			// [Esc]:
			if (answer < 0)
				return -1;

			// choise:
			if (answer < choices.Count)
				return answer;

			// help:
			ShowHelpForChoices(choices);
		}
	}
}
