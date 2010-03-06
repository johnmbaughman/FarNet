/*
FarNet plugin for Far Manager
Copyright (c) 2005 FarNet Team
*/

using System;
using System.Globalization;

namespace FarNet
{
	/// <summary>
	/// Infrastructure. Internal use only.
	/// </summary>
	public static class Invariant
	{
		///
		public static string Format(string formatting, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, formatting, args);
		}
	}
}