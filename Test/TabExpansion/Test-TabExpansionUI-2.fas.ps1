﻿<#
.Synopsis
	_140112_150217

.Description
	Weird. Built-in TE gets nothing. Vanilla ISE has the same issue.  Thus, PSF
	at least should not fail with 'Index out range'. Instead it shows an empty
	list, similar to vanilla ISE.
#>

job {
	# open editor
	Open-FarEditor 'tmp.ps1'
}
job {
	Assert-Far -Editor

	#! new lines are relevant
	$Far.Editor.InsertText("`nit `"\pr`n")
	$Far.Editor.GoTo(7, 1)
}

#_090328_170110
macro @'
Plugin.Menu("10435532-9BB3-487B-A045-B0E6ECAAB6BC", "7DEF4106-570A-41AB-8ECB-40605339E6F7")
Keys"7"
'@
job {
	# TE dialog with a list box
	Assert-Far $Far.Dialog[1].GetType().Name -eq 'FarListBox'
}

keys Esc
job {
	Assert-Far -Editor
}

# exit editor
macro 'Keys"Esc n"'
