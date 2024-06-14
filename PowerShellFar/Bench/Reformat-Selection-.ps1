
<#
.Synopsis
	Reformats selected lines or the current line in the editor.
	Author: Roman Kuzmin

.Description
	Primary indent, prefix and secondary indent are taken from the first line
	and inserted into any result line. Primary indent is leading white spaces,
	prefix depends on the file type, secondary indent are white spaces after
	the prefix (tabs are replaced with spaces).

	Tabs in the primary indent are preserved, the parameter -TabSize is used
	only for actual text length calculation by -RightMargin.

.Parameter RightMargin
		Right margin. Default: $env:ReformatSelectionRightMargin or 79.
.Parameter TabSize
		Tab size for the line length calculation. Default: editor settings.
#>

param(
	[int]$RightMargin = $env:ReformatSelectionRightMargin,
	[int]$TabSize
)

$Editor = $Psf.Editor()
Assert-Far (!$Editor.IsLocked) -Message 'The editor is locked for changes.' -Title Reformat-Selection-.ps1

# get the prefix pattern by file type
$type = ''
switch -regex ([System.IO.Path]::GetExtension($Editor.FileName)) {
	'\.(?:txt|hlf)' { $pattern = '$'; break }
	'\.(?:ps1|psd1|psm1|pl|pls|py|pyw|pys|R|rb|rbw|ruby|rake|php\d?)$' { $pattern = '#+'; break }
	'\.(?:bat|cmd)$' { $pattern = '::+|rem\s'; break }
	'\.(?:text|md|markdown)$' { $pattern = ' {0,3}(?:>|(?:[*+\-:]|\d+\.)\s+)'; $type = 'md'; break }
	'\.(?:sql|lua)$' { $pattern = '--+'; break }
	'\.(?:vb|vbs|bas|vbp|frm|cls)$' { $pattern = "'+"; break }
	default { $pattern = '(?://+|;+)' }
}

# get the selected lines or the current line
$lines = $Editor.SelectedLines
if (!$lines.Count) {
	$line = $Editor.Line
	$line.SelectText(0, $line.Length)
	$lines = @($line)
}
if ($lines[0] -notmatch "^(\s*)($pattern)?(\s*)\S") {
	return
}

# default right margin
if ($RightMargin -le 0) { $RightMargin = 79 }

# default tab size
if ($TabSize -le 0) { $TabSize = $Editor.TabSize }

# indents, prefix and text length
$i1 = $matches[1]
$pr = $matches[2]
$i2 = $matches[3] -replace '\t', ' '
$pref = $i1 + $pr + $i2
$i1 = $i1 -replace '\t', (' ' * $TabSize)
$len = $RightMargin - $i1.Length - $pr.Length - $i2.Length

# join lines removing prefixes
$text = ''
foreach($line in $lines) {
	$s = $line.SelectedText.Trim()
	# remove the prefix
	if ($s.StartsWith($pr)) {
		$s = $s.Substring($pr.Length).TrimStart()
	}
	$text += $s + ' '
}

# split, format and insert
$first = $true
$text = [Regex]::Split($text, "(.{0,$len}(?:\s|$))") | .{process{ if ($_) {
	$pref + $_.TrimEnd()
	if ($first) {
		$first = $false
		if ($type -eq 'md') {
			if ($pref -match '^(\s*)((?:[*+\-:]|\d+\.)\s+)') {
				$pref = $matches[1] + (' ' * $matches[2].Length)
			}
			elseif ($pref -match '^(\s*)') {
				$pref = ' ' * $matches[1].Length
			}
		}
	}
}}}
if ($lines.Count -gt 1) {
	$text += ''
}

$Editor.BeginUndo()
$Editor.DeleteText()
$Editor.InsertText(($text -join "`r"))
$Editor.EndUndo()
