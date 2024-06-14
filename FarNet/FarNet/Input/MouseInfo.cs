
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

namespace FarNet;

/// <summary>
/// Mouse event information.
/// </summary>
/// <param name="where">Position.</param>
/// <param name="action">Action.</param>
/// <param name="buttons">Buttons.</param>
/// <param name="controls">Control keys.</param>
/// <param name="value">Wheel value.</param>
public sealed class MouseInfo(Point where, MouseAction action, MouseButtons buttons, ControlKeyStates controls, int value) : KeyBase(controls)
{
	Point _Where = where;

	/// <summary>
	/// Mouse positon.
	/// </summary>
	public Point Where => _Where;

	/// <summary>
	/// Action.
	/// </summary>
	public MouseAction Action { get; } = action;

	/// <summary>
	/// Buttons.
	/// </summary>
	public MouseButtons Buttons { get; } = buttons;

	/// <summary>
	/// Wheel value.
	/// </summary>
	/// <remarks>
	/// It is positive or negative depending on the wheel direction.
	/// The value is normally 120*X but it depends on the mouse driver.
	/// </remarks>
	public int Value { get; } = value;

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		return obj is MouseInfo that &&
			Action == that.Action &&
			Buttons == that.Buttons &&
			ControlKeyState == that.ControlKeyState &&
			_Where == that._Where;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		uint num = (uint)Action + ((uint)Buttons << 8) + ((uint)ControlKeyState << 16);
		return num.GetHashCode() ^ _Where.GetHashCode();
	}

	/// <summary>
	/// Returns the string "{0} {1} ({2}) ({3})", Where, Action, Buttons, ControlKeyState.
	/// </summary>
	public override string ToString()
	{
		return string.Format(null, "{0} {1} ({2}) ({3})", Where, Action, Buttons, ControlKeyState);
	}
}
