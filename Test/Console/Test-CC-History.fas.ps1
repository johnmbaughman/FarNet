<#
.Synopsis
	Command console history tests.
#>

### run 2 dummy commands, explore 2 history items and exit

job { $Psf.RunCommandConsole() }
keys 1 Enter
keys 2 Enter
job {
	Assert-Far $Far.Dialog[1].Text -eq ''
}
keys CtrlE
job {
	Assert-Far $Far.Dialog[1].Text -eq '2'
}
keys CtrlE
job {
	Assert-Far $Far.Dialog[1].Text -eq '1'
}
keys Esc # test clear prompt
job {
	Assert-Far -DialogTypeId ([PowerShellFar.Guids]::ReadCommandDialog)
	Assert-Far $Far.Dialog[1].Text -eq ''
}
job { $Psf.StopCommandConsole() }

### repeat and compare, do not exit

job { $Psf.RunCommandConsole() }
keys CtrlE
job {
	Assert-Far $Far.Dialog[1].Text -eq '2'
}
keys CtrlE
job {
	Assert-Far $Far.Dialog[1].Text -eq '1'
}

### run a dummy command, test reset navigation

keys Esc 1 Enter
job {
	Assert-Far $Far.Dialog[1].Text -eq ''
}
keys CtrlE
job {
	Assert-Far $Far.Dialog[1].Text -eq '1'
}
keys CtrlE
job {
	Assert-Far $Far.Dialog[1].Text -eq '2'
}
keys CtrlX
job {
	Assert-Far $Far.Dialog[1].Text -eq '1'
}
job { $Psf.StopCommandConsole() }
