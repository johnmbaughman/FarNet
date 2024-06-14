
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

namespace FarNet;

/// <summary>
/// Viewer frame info.
/// </summary>
public struct ViewFrame
{
	/// <param name="offset">See <see cref="Offset"/></param>
	/// <param name="column">See <see cref="Column"/></param>
	public ViewFrame(long offset, long column)
		: this()
	{
		Offset = offset;
		Column = column;
	}

	/// <summary>
	/// Offset in the file.
	/// </summary>
	public long Offset { get; set; }

	/// <summary>
	/// Leftmost visible column index.
	/// </summary>
	public long Column { get; set; }

	/// <include file='doc.xml' path='doc/OpEqual/*'/>
	public static bool operator ==(ViewFrame left, ViewFrame right)
	{
		return
			left.Offset == right.Offset &&
			left.Column == right.Column;
	}

	/// <include file='doc.xml' path='doc/OpNotEqual/*'/>
	public static bool operator !=(ViewFrame left, ViewFrame right)
	{
		return !(left == right);
	}

	/// <inheritdoc/>
	public override readonly bool Equals(object? obj)
	{
		return obj != null && obj.GetType() == typeof(ViewFrame) && this == (ViewFrame)obj;
	}

	/// <inheritdoc/>
	public override readonly int GetHashCode()
	{
		return (int)Offset | ((int)Column << 16);
	}

	/// <summary>
	/// Gets "({Offset}, {Column})".
	/// </summary>
	/// <returns>"({Offset}, {Column})"</returns>
	public override readonly string ToString()
	{
		return $"({Offset}, {Column})";
	}
}
