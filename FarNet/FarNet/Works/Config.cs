﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FarNet.Works;
#pragma warning disable 1591

// Check for empty lists, not nulls.
// Use strings for value types, so that null means default.
public class Config : ModuleSettings<Config.Data>
{
	public static Config Default { get; } = new Config();

	Config() : base(Far.Api.GetFolderPath(SpecialFolder.RoamingData) + @"\FarNet\FarNet.xml")
	{
	}

	public class Data
	{
		[XmlElement(ElementName = "Module")]
		public List<Module> Modules { get; set; } = [];

		public Module? GetModule(string name)
		{
			return Modules.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		}

		public void SetModule(Module value)
		{
			RemoveModule(value.Name!);
			Modules.Add(value);
		}

		public void RemoveModule(string name)
		{
			Modules.RemoveAll(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		}
	}

	public class Module
	{
		[XmlAttribute]
		public string? Name { get; set; }

		[XmlAttribute]
		public string? Culture { get; set; }

		[XmlElement(ElementName = "Command")]
		public List<Command> Commands { get; set; } = [];

		[XmlElement(ElementName = "Drawer")]
		public List<Drawer> Drawers { get; set; } = [];

		[XmlElement(ElementName = "Editor")]
		public List<Editor> Editors { get; set; } = [];

		[XmlElement(ElementName = "Tool")]
		public List<Tool> Tools { get; set; } = [];

		public bool IsDefault()
		{
			return
				Commands.Count == 0 &&
				Drawers.Count == 0 &&
				Editors.Count == 0 &&
				Tools.Count == 0 &&
				Culture is null;
		}

		public Command? GetCommand(Guid id)
		{
			return Commands.Find(x => x.Id == id);
		}

		public void SetCommand(Guid id, Command? value)
		{
			Commands.RemoveAll(x => x.Id == id);
			if (value != null)
				Commands.Add(value);
		}

		public Drawer? GetDrawer(Guid id)
		{
			return Drawers.Find(x => x.Id == id);
		}

		public void SetDrawer(Guid id, Drawer? value)
		{
			Drawers.RemoveAll(x => x.Id == id);
			if (value != null)
				Drawers.Add(value);
		}

		public Editor? GetEditor(Guid id)
		{
			return Editors.Find(x => x.Id == id);
		}

		public void SetEditor(Guid id, Editor? value)
		{
			Editors.RemoveAll(x => x.Id == id);
			if (value != null)
				Editors.Add(value);
		}

		public Tool? GetTool(Guid id)
		{
			return Tools.Find(x => x.Id == id);
		}

		public void SetTool(Guid id, Tool? value)
		{
			Tools.RemoveAll(x => x.Id == id);
			if (value != null)
				Tools.Add(value);
		}
	}

	public class Command
	{
		[XmlAttribute]
		public Guid Id { get; set; }

		[XmlAttribute]
		public string? Prefix { get; set; }
	}

	public class Drawer
	{
		[XmlAttribute]
		public Guid Id { get; set; }

		[XmlAttribute]
		public string? Mask { get; set; }

		[XmlAttribute]
		public string? Priority { get; set; }
	}

	public class Editor
	{
		[XmlAttribute]
		public Guid Id { get; set; }

		[XmlAttribute]
		public string? Mask { get; set; }
	}

	public class Tool
	{
		[XmlAttribute]
		public Guid Id { get; set; }

		[XmlAttribute]
		public string? Options { get; set; }
	}
}
