﻿# Panel used to open with the current item 3 due to the empty "name".
# https://forum.farmanager.com/viewtopic.php?f=8&t=11965#p158120

job {
	$Explorer = New-Object PowerShellFar.ObjectExplorer -Property @{
		AsGetData = {
			[PSCustomObject]@{_id = 1; name = 'name1'}
			[PSCustomObject]@{_id = 2; name = 'name2'}
			[PSCustomObject]@{_id = 3}
		}
	}
	$Explorer.OpenPanel()
}
job {
	# current item is ".."
	Assert-Far -Plugin
	Assert-Far $(
		$Far.Panel.CurrentIndex -eq 0
		$Far.Panel.Files[0].Name -eq '..'
	)
}
keys Esc
