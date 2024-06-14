<#
.Synopsis
	Build script, https://github.com/nightroman/Invoke-Build
#>

param(
	$FarHome = (property FarHome C:\Bin\Far\x64),
	$Configuration = (property Configuration Release)
)

Set-StrictMode -Version 3
$ModuleName = 'EditorKit'
$ModuleRoot = "$FarHome\FarNet\Modules\$ModuleName"
$Description = 'Editor configuration. FarNet module for Far Manager.'

task build meta, {
	exec { dotnet build "$ModuleName.csproj" "/p:FarHome=$FarHome" "/p:Configuration=$Configuration" }
}

task publish {
	exec { dotnet publish "$ModuleName.csproj" -c $Configuration -o $ModuleRoot --no-build }
}

task clean {
	remove z, bin, obj, README.htm, Directory.Build.props, "*$ModuleName.*.nupkg"
}

task markdown {
	assert (Test-Path $env:MarkdownCss)
	exec { pandoc.exe @(
		'README.md'
		'--output=README.htm'
		'--from=gfm'
		'--embed-resources'
		'--standalone'
		"--css=$env:MarkdownCss"
		"--metadata=pagetitle=$ModuleName"
	)}
}

task version {
	($script:Version = switch -regex -file History.txt {'^= (\d+\.\d+\.\d+) =$' {$matches[1]; break}})
}

task meta -Inputs .build.ps1, History.txt -Outputs Directory.Build.props -Jobs version, {
	Set-Content Directory.Build.props @"
<Project>
	<PropertyGroup>
		<Company>https://github.com/nightroman/FarNet</Company>
		<Copyright>Copyright (c) Roman Kuzmin</Copyright>
		<Description>$Description</Description>
		<Product>FarNet.$ModuleName</Product>
		<Version>$Version</Version>
		<FileVersion>$Version</FileVersion>
		<AssemblyVersion>$Version</AssemblyVersion>
	</PropertyGroup>
</Project>
"@
}

task package markdown, {
	remove z
	$toModule = mkdir "z\tools\FarHome\FarNet\Modules\$ModuleName"

	# module
	exec { robocopy $ModuleRoot $toModule /s /xf *.pdb } (0..2)
	equals 4 (Get-ChildItem $toModule -Recurse -File).Count

	# meta
	Copy-Item -Destination z @(
		'README.md'
		'..\Zoo\FarNetLogo.png'
	)

	# module
	Copy-Item -Destination $toModule @(
		'README.htm'
		'History.txt'
		'..\LICENSE'
	)
}

task nuget package, version, {
	# test versions
	$dllPath = "$FarHome\FarNet\Modules\$ModuleName\$ModuleName.dll"
	($dllVersion = (Get-Item $dllPath).VersionInfo.FileVersion.ToString())
	equals $dllVersion $Version

	# nuspec
	Set-Content z\Package.nuspec @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	<metadata>
		<id>FarNet.$ModuleName</id>
		<version>$Version</version>
		<authors>Roman Kuzmin</authors>
		<owners>Roman Kuzmin</owners>
		<projectUrl>https://github.com/nightroman/FarNet/tree/main/$ModuleName</projectUrl>
		<icon>FarNetLogo.png</icon>
		<readme>README.md</readme>
		<license type="expression">BSD-3-Clause</license>
		<description>$Description</description>
		<releaseNotes>https://github.com/nightroman/FarNet/blob/main/$ModuleName/History.txt</releaseNotes>
		<tags>FarManager FarNet Module EditorConfig</tags>
	</metadata>
</package>
"@
	# pack
	exec { NuGet pack z\Package.nuspec }
}

task . build, clean
