﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FarNet.Tools;

/// <summary>
/// XPath input helper.
/// </summary>
public partial class XPathInput
{
	[GeneratedRegex(@"^declare\s+variable\s+\$(\w+)\s+(.*)")]
	private static partial Regex RegexDeclareVariable();

	[GeneratedRegex(@"^external[;\s]*$")]
	private static partial Regex RegexExternal();

	[GeneratedRegex(@"^:=\s*(.*?)[;\s]*$")]
	private static partial Regex RegexValue();

	/// <summary>
	/// Gets the XPath expression.
	/// </summary>
	public string Expression { get; }

	/// <summary>
	/// Gets the XPath variables.
	/// </summary>
	public Dictionary<string, object> Variables { get; }

	/// <summary>
	/// Parses the XPath file.
	/// </summary>
	public static XPathInput ParseFile(string path)
	{
		return Parse(File.ReadAllLines(path, Encoding.Default));
	}

	/// <summary>
	/// Parses the XPath text.
	/// </summary>
	public static XPathInput ParseText(string text)
	{
		return Parse(Works.Kit.SplitLines(text));
	}

	XPathInput(string expression, Dictionary<string, object> variables)
	{
		Expression = expression;
		Variables = variables;
	}

	static XPathInput Parse(string[] lines)
	{
		var variables = new Dictionary<string, object>();

		var regex1 = RegexDeclareVariable();
		var regex2 = RegexExternal();
		var regex3 = RegexValue();
		int i;
		bool comment = false;
		for (i = 0; i < lines.Length; ++i)
		{
			var line = lines[i].Trim();

		repeat:

			// empty line
			if (line.Length == 0)
				continue;

			// comment
			if (comment)
			{
				int index = line.IndexOf(":)", StringComparison.Ordinal);
				if (index < 0)
					continue;
				
				line = line[(index + 2)..].Trim();
				comment = false;
				goto repeat;
			}
			else if (line.StartsWith("(:", StringComparison.Ordinal))
			{
				line = line[2..];
				comment = true;
				goto repeat;
			}

			var match = regex1.Match(line);
			if (!match.Success)
				break;

			var name = match.Groups[1].Value;
			var text = match.Groups[2].Value;
			match = regex2.Match(text);
			if (match.Success)
			{
				// prompt
				text = Far.Api.Input("Variable: " + name, "XPathVariable", "Input variable");
				if (text == null)
				{
					variables.Add(name, string.Empty);
					continue;
				}

				if (double.TryParse(text, out double adouble))
					variables.Add(name, adouble);
				else
					variables.Add(name, text);
				continue;
			}

			match = regex3.Match(text);
			if (!match.Success)
				throw new InvalidOperationException("declare variable: expected 'external' or ':='");

			text = match.Groups[1].Value;
			if (text.StartsWith('\'') && text.EndsWith('\'') ||
				text.StartsWith('"') && text.EndsWith('"'))
			{
				variables.Add(name, text[1..^1]);
			}
			else
			{
				if (!double.TryParse(text, out double adouble))
					throw new InvalidOperationException("Not supported variable value.");
				variables.Add(name, adouble);
			}
		}

		var expression = string.Join(Environment.NewLine, lines, i, lines.Length - i);
		return new XPathInput(expression, variables);
	}
}
