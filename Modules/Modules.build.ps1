<#
.Synopsis
	Build script (https://github.com/nightroman/Invoke-Build)
#>

param(
	$FarHome = (property FarHome)
)

$Builds = Get-Item *\*.build.ps1

task testBuild {
	# build
	$FarNetModules = 'C:\TEMP\z'
	foreach($_ in $Builds) { Invoke-Build build $_ }

	# test
	assert (Test-Path $FarNetModules\Backslash\Backslash.dll)
	equals (Get-Item $FarNetModules\FarNet.Demo\*).Count 6
	assert (Test-Path $FarNetModules\IronPythonFar\IronPythonFar.dll)
	assert (Test-Path $FarNetModules\TryPanelCSharp\TryPanelCSharp.dll)

	# clean
	remove $FarNetModules
},
clean

task clean {
	foreach($_ in $Builds) { Invoke-Build clean $_ }
	remove *\obj
}

task . testBuild
