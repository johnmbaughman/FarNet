﻿using FarNet;
using System;
using System.Text.RegularExpressions;

namespace Drawer;

public sealed class Settings : ModuleSettings<Settings.Data>
{
	public const string CurrentWordGuid = "a9a6f877-e049-4438-a315-d5914b200988";
	public const string CurrentWordName = "Current word";
	public const string FixedColumnGuid = "efe9454e-0284-4047-ba74-a00685fe40a6";
	public const string FixedColumnName = "Fixed column";
	public const string TabsGuid = "ae160caa-6f5b-43f1-b94a-f2a4fa6ba000";
	public const string TabsName = "Tabs";

	public static Settings Default { get; } = new();

	public class Data : IValidate
	{
		public CurrentWord CurrentWord { get; set; } = new();
		public FixedColumn FixedColumn { get; set; } = new();
		public Tabs Tabs { get; set; } = new();

		public void Validate()
		{
			CurrentWord.Validate();
		}
	}

	public class CurrentWord
	{
		public XmlCData WordRegex { get; set; } = @"\w[-\w]*";

		public bool ExcludeCurrent { get; set; }

		public ConsoleColor ColorForeground { get; set; } = ConsoleColor.Black;

		public ConsoleColor ColorBackground { get; set; } = ConsoleColor.Gray;

		internal Regex WordRegex2 { get; private set; } = null!;

		internal void Validate()
		{
			if (string.IsNullOrWhiteSpace(WordRegex))
				throw new ModuleException("CurrentWord/WordRegex cannot be empty.");

			try { WordRegex2 = new Regex(WordRegex.Value.Trim()); }
			catch (Exception ex) { throw new ModuleException($"CurrentWord/WordRegex: {ex.Message}"); }
		}
	}

	public class FixedColumn
	{
		public int[] ColumnNumbers { get; set; } = [80, 120];

		public ConsoleColor ColorForeground { get; set; } = ConsoleColor.Black;

		public ConsoleColor ColorBackground { get; set; } = ConsoleColor.Gray;
	}

	public class Tabs
	{
		public ConsoleColor ColorBackground { get; set; } = ConsoleColor.Yellow;
	}
}
