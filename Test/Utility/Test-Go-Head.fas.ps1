﻿
job {
	$Far.Panel.CurrentDirectory = 'C:\ROM'
	$Far.Panel.Redraw(0, $true)
	Assert-Far $Far.Panel.CurrentIndex -eq 0
}
job {
	Go-Head-
	$Data.Index = $Far.Panel.CurrentIndex
	Assert-Far @(
		$Data.Index -ne 0
		!$Far.Panel.CurrentFile.IsDirectory
	)
}
keys Up
job {
	Assert-Far $Far.Panel.CurrentFile.IsDirectory
}
job {
	Go-Head-
	Assert-Far $Far.Panel.CurrentIndex -eq $Data.Index
}
keys End
job {
	Go-Head-
	Assert-Far $Far.Panel.CurrentIndex -eq $Data.Index
}
