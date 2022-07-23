﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;

namespace FarNet;

/// <summary>
/// Arguments of editor saving event.
/// </summary>
public sealed class EditorSavingEventArgs : EventArgs
{
	/// <param name="fileName">See <see cref="FileName"/></param>
	/// <param name="codePage">See <see cref="CodePage"/></param>
	public EditorSavingEventArgs(string fileName, int codePage)
	{
		FileName = fileName;
		CodePage = codePage;
	}

	/// <summary>
	/// Gets the file name being saved.
	/// </summary>
	public string FileName { get; }

	/// <summary>
	/// Gets the code page used on saving.
	/// </summary>
	public int CodePage { get; }
}
