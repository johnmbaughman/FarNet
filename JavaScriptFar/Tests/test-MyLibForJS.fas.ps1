﻿
$dll = "$env:TEMP\MyLibForJS.dll"
if (!(Test-Path $dll)) {
	#? cannot load if compile in PSCore, use v5
	powershell -Command $env:FarNetCode\JavaScriptFar\Samples\extras\test-MyLibForJS.ps1
	if ($LASTEXITCODE) {throw}
}

run {
	$Far.InvokeCommand("js:@ $env:FarNetCode\JavaScriptFar\Samples\extras\test-MyLibForJS.js")
}
job {
	Assert-Far -Dialog
	Assert-Far $Far.Dialog[1].Text -eq 'done Job1, done Job2'

	$Far.Dialog.Close()
}
