﻿
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using System.IO;

namespace PowerShellFar;

sealed class StreamOutputWriter(StreamWriter writer) : TextOutputWriter
{
	readonly StreamWriter _writer = writer;

	protected override void Append(string value)
	{
		_writer.Write(value);
	}

	protected override void AppendLine()
	{
		_writer.WriteLine();
	}

	protected override void AppendLine(string value)
	{
		_writer.WriteLine(value);
	}
}
