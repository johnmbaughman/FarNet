
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

namespace FarNet.Tools;

/// <summary>
/// History next line navigator.
/// </summary>
/// <param name="lines">The history lines.</param>
/// <param name="start">The start line. It is returned when navigation steps after the last line.</param>
public class HistoryNext(string[] lines, string start)
{
	readonly string[] _lines = lines;
	readonly string _start = start;
	int _index = lines.Length;

	/// <summary>
	/// Gets the next line from history different from the current.
	/// </summary>
	/// <param name="up">Tells to step up or down the lines.</param>
	/// <param name="current">The current line to be filtered out on stepping.</param>
	/// <returns>The next suitable line or start/empty line on stepping out of lines.</returns>
	public string GetNext(bool up, string current)
	{
		if (up)
		{
			while (--_index >= 0)
			{
				var line = _lines[_index];
				if (line != current)
					return line;
			}
			_index = -1;
			return string.Empty;
		}
		else
		{
			while (++_index < _lines.Length)
			{
				var line = _lines[_index];
				if (line != current)
					return line;
			}
			_index = _lines.Length;
			return _start;
		}
	}
}
