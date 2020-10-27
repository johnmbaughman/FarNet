<#
.Synopsis
	Build script (https://github.com/nightroman/Invoke-Build)
#>

[CmdletBinding()]
param(
	$FarHome = (property FarHome C:\Bin\Far\x64),
	$Configuration = (property Configuration Release),
	$FarNetModules = (property FarNetModules $FarHome\FarNet\Modules)
)

$ModuleName = 'EditorKit'
$ProjectName = "$ModuleName.csproj"

task build {
	exec {& (Resolve-MSBuild) $ProjectName /p:FarHome=$FarHome /p:Configuration=$Configuration /p:FarNetModules=$FarNetModules}
}

task clean {
	remove bin, obj
}

task . build, clean
