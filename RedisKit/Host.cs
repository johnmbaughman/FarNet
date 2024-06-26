﻿using FarNet;

namespace RedisKit;

public class Host : ModuleHost
{
	internal const string MyName = "RedisKit";
	internal static class Param
	{
		public const string
			Key = "key",
			Mask = "mask",
			Redis = "redis";
	}

	public static Host Instance { get; private set; } = null!;

	public Host()
	{
		Instance = this;
	}
}
