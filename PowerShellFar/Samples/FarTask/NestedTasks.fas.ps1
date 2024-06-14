<#
.Synopsis
	How to combine task scripts.

.Description
	Jobs call `Start-FarTask ... -AsTask` and return tasks.
	The job runner awaits tasks and outputs their results.
#>

# Run DialogNonModalInput.fas.ps1 and keep the result.
$text = job {
	Start-FarTask $PSScriptRoot\DialogNonModalInput.fas.ps1 -AsTask
}

# Run InputEditorMessage.fas.ps1 using the result.
job -Arguments $text {
	Start-FarTask $PSScriptRoot\InputEditorMessage.fas.ps1 -Text $args[0] -AsTask
}
