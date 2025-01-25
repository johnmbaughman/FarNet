﻿using FarNet;
using RedisKit.Panels;

namespace RedisKit.Commands;

sealed class KeysCommand : BaseCommand
{
	readonly string? _mask;

	public KeysCommand(CommandParameters parameters) : base(parameters)
	{
		_mask = parameters.GetString(Param.Mask);
	}

	public override void Invoke()
	{
		new KeysExplorer(Database, _mask)
			.CreatePanel()
			.Open();
	}
}
