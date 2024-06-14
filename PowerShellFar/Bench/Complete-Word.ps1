<#
.Synopsis
	Completes the current word in editors.
	Author: Roman Kuzmin

.Description
	The script completes the current word in editors, command line, dialog
	edit controls. Candidate words are taken from the current editor text,
	command history, or dialog history respectively.

	Words are grouped by the preceding symbol and sorted. The first group
	candidates are usually used more frequently, at least in source code.
#>

# get edit line
$Line = $Far.Line
Assert-Far ($Line -and !$Line.IsReadOnly) -Message 'Missing or read only edit line.' -Title 'Complete word'

# current word
$match = $Line.MatchCaret('\w[-\w]*')
if (!$match) {
	return
}

$text = $Line.Text
$word = $text.Substring($match.Index, $Line.Caret - $match.Index)
if (!$word) {
	return
}

$skip = $match.Value
$pref = if ($match.Index) {[string]$text[$match.Index - 1]}

# collect matching words in editor or history
$words = @{}
$re = [regex]::new("(^|\W)($word[-\w]+)", 'IgnoreCase')

filter CollectWords
{
	for($m = $re.Match($_); $m.Success; $m = $m.NextMatch()) {
		$w = $m.Groups[2].Value
		if ($w -eq $skip) {
			continue
		}

		$p = $m.Groups[1].Value
		if ($words.Contains($w)) {
			if ($p -eq $pref) {
				$words[$w] = $pref
			}
		}
		elseif ($p -eq $pref) {
			$words.Add($w, $pref)
		}
		else {
			$words.Add($w, $null)
		}
	}
}

# cases: source
switch($Line.WindowKind) {
	Editor {
		$Editor = $Far.Editor
		$Editor.Lines | CollectWords
		if ($Editor.FileName -like '*.interactive.ps1') {
			$Psf.GetHistory(0) | CollectWords
		}
	}
	Dialog {
		$control = $Far.Dialog.Focused
		if ($control.History) {
			$Far.History.Dialog($control.History) | .{process{ $_.Name }} | CollectWords
		}
	}
	default {
		$Far.History.Command() | .{process{ $_.Name }} | CollectWords
	}
}

if ($words.get_Count() -eq 0) {
	return
}

# select a word
if ($words.get_Count() -eq 1) {
	$w = @($words.get_Keys())[0]
}
else {
	# select from list
	$cursor = $Far.UI.WindowCursor
	$w = .{
		$words.GetEnumerator() | .{process{ if ($_.Value) { $_.Key } }} | Sort-Object
		$words.GetEnumerator() | .{process{ if (!$_.Value) { $_.Key } }} | Sort-Object
	} |
	Out-FarList -Popup -IncrementalOptions 'Prefix' -Incremental "$word*" -X $cursor.X -Y $cursor.Y
	if (!$w) {
		return
	}
}

# complete by the selected word
$Line.InsertText($w.Substring($word.Length))
if ($Line.WindowKind -eq 'Editor') {
	$Editor.Redraw()
}
