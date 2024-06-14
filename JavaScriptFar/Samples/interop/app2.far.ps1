﻿<#
.Synopsis
	Runs JavaScript scripts using JavaScriptFar interop.
#>

# step 1: get the function to run scripts
$EvaluateDocument = $Far.GetModuleInterop('JavaScriptFar', 'EvaluateDocument', $null)

### step 2: run scripts with parameters
$user = $EvaluateDocument.Invoke("$PSScriptRoot\input.js", $null)
if ($user) {
	$null = $EvaluateDocument.Invoke("$PSScriptRoot\message.js", @{user = $user})
}
