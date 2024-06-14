﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet.Forms;
using System;
using System.Text.RegularExpressions;

namespace FarNet;

/// <summary>
/// Abstract line in various text and line editors.
/// </summary>
/// <remarks>
/// It can be:
/// *) an item of <see cref="IEditor.Lines"/> or <see cref="IEditor.SelectedLines"/> in <see cref="IEditor"/>;
/// *) the command line <see cref="IFar.CommandLine"/>;
/// *) <see cref="IEditable.Line"/> of <see cref="IEdit"/>) or <see cref="IComboBox"/> in a dialog.
/// </remarks>
public abstract class ILine
{
	/// <summary>
	/// Gets the line index in the source editor.
	/// </summary>
	/// <remarks>
	/// It returns -1 for the editor caret line, the command line, and dialog edit lines.
	/// </remarks>
	public virtual int Index => -1;

	/// <summary>
	/// Gets or sets the line text.
	/// </summary>
	/// <seealso cref="ActiveText"/>
	/// <seealso cref="SelectedText"/>
	public abstract string Text { get; set; }

	/// <summary>
	/// Gets or sets (replaces) the selected text.
	/// </summary>
	/// <remarks>
	/// If there is no selection then <c>get</c> returns null, <c>set</c> throws.
	/// </remarks>
	/// <seealso cref="ActiveText"/>
	/// <seealso cref="Text"/>
	public abstract string SelectedText { get; set; }

	/// <summary>
	/// Gets or sets the caret position.
	/// </summary>
	/// <remarks>
	/// Returns -1 if it is an editor line and it is not current.
	/// Setting of a negative value moves the caret to the end.
	/// </remarks>
	public abstract int Caret { get; set; }

	/// <summary>
	/// Inserts text at the caret position.
	/// </summary>
	/// <param name="text">String to insert to the line.</param>
	/// <remarks>
	/// In the editor this method should not be used for the current line only.
	/// </remarks>
	public abstract void InsertText(string text);

	/// <summary>
	/// Selects the span of text in the current editor line, the command line, or the dialog line.
	/// </summary>
	/// <param name="startPosition">Start position.</param>
	/// <param name="endPosition">End position, not included into the span.</param>
	public abstract void SelectText(int startPosition, int endPosition);

	/// <summary>
	/// Turns selection off in the current editor line, the command line, or the dialog line.
	/// </summary>
	public abstract void UnselectText();

	/// <summary>
	/// Gets the text length.
	/// </summary>
	/// <remarks>
	/// Use it instead of more expensive <see cref="Text"/> in cases when just length is needed.
	/// </remarks>
	public abstract int Length { get; }

	/// <summary>
	/// Gets the parent window kind (<c>Editor</c>, <c>Panels</c>, <c>Dialog</c>).
	/// </summary>
	public abstract WindowKind WindowKind { get; }

	/// <summary>
	/// Gets the selection span.
	/// </summary>
	/// <remarks>
	/// If selection does not exists then returned position and length values are negative.
	/// </remarks>
	public abstract Span SelectionSpan { get; }

	/// <summary>
	/// Gets or sets <see cref="SelectedText"/> if any, otherwise gets or sets <see cref="Text"/>.
	/// </summary>
	public string ActiveText
	{
		get => SelectedText ?? Text;
		set
		{
			if (SelectionSpan.Length < 0)
				Text = value;
			else
				SelectedText = value;
		}
	}

	/// <summary>
	/// Returns the line text.
	/// </summary>
	public sealed override string ToString()
	{
		return Text;
	}

	/// <summary>
	/// Gets the match for the current caret position.
	/// </summary>
	/// <param name="regex">Regular expression that defines "words".</param>
	/// <returns>The found match or null if the caret is not at any "word".</returns>
	/// <remarks>
	/// This methods is useful for the common task of getting the current "word".
	/// In the editor it should be called on the current line only.
	/// "Words" to look for are defined by a regular expression.
	/// </remarks>
	public Match? MatchCaret(Regex regex)
	{
		ArgumentNullException.ThrowIfNull(regex);

		int caret = Caret;
		for (var match = regex.Match(Text); match.Success; match = match.NextMatch())
			if (caret <= match.Index + match.Length)
				return caret < match.Index ? null : match;

		return null;
	}

	/// <summary>
	/// Gets true if the text is read only.
	/// </summary>
	public virtual bool IsReadOnly => false;
}
