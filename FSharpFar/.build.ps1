<#
.Synopsis
	Build script, https://github.com/nightroman/Invoke-Build
#>

param(
	$Configuration = (property Configuration Release),
	$FarHome = (property FarHome C:\Bin\Far\x64)
)

Set-StrictMode -Version 3
$ModuleName = 'FSharpFar'
$ModuleRoot = "$FarHome\FarNet\Modules\$ModuleName"
$Description = 'F# scripting and interactive services in Far Manager.'

task build meta, {
	exec { dotnet build "src\$ModuleName.sln" -c $Configuration "/p:FarHome=$FarHome" }
}

task publish {
	exec { dotnet publish "src\$ModuleName\$ModuleName.fsproj" -c $Configuration -o $ModuleRoot --no-build }

	$xml = [xml](Get-Content "src\$ModuleName\$ModuleName.fsproj")
	$node = $xml.SelectSingleNode('Project/ItemGroup/PackageReference[@Include="FSharp.Core"]')
	Copy-Item "$HOME\.nuget\packages\FSharp.Core\$($node.Version)\lib\netstandard2.1\FSharp.Core.xml" $ModuleRoot

	# used to be deleted, now missing: runtimes\unix
	Set-Location $ModuleRoot
	remove *.deps.json, cs, de, es, fr, it, ja, ko, pl, pt-BR, ru, tr, zh-Hans, zh-Hant
}

task clean {
	remove @(
		'z'
		'README.htm'
		"FarNet.$ModuleName.*.nupkg"
		"src\*\bin"
		"src\*\obj"
	)
}

task version {
	($script:Version = switch -regex -file History.txt {'^= (\d+\.\d+\.\d+) =$' {$matches[1]; break}})
}

task meta -Inputs .build.ps1, History.txt -Outputs src/Directory.Build.props -Jobs version, {
	Set-Content src/Directory.Build.props @"
<Project>
	<PropertyGroup>
		<Company>https://github.com/nightroman/FarNet</Company>
		<Copyright>Copyright (c) Roman Kuzmin</Copyright>
		<Description>$Description</Description>
		<Product>FarNet.FSharpFar</Product>
		<Version>$Version</Version>
	</PropertyGroup>
</Project>
"@
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

task package markdown, {
	remove z
	$toModule = mkdir "z\tools\FarHome\FarNet\Modules\$ModuleName"

	# module
	exec { robocopy $ModuleRoot $toModule /s /xf *.pdb } (0..2)
	equals 10 (Get-ChildItem $toModule -Recurse -File).Count

	# meta
	Copy-Item -Destination z @(
		'README.md'
		'..\Zoo\FarNetLogo.png'
	)

	# repo
	Copy-Item -Destination $toModule @(
		'README.htm'
		'History.txt'
		'..\LICENSE'
	)
}

task nuget package, version, {
	# test versions
	$dllPath = "$ModuleRoot\$ModuleName.dll"
	($dllVersion = (Get-Item $dllPath).VersionInfo.FileVersion.ToString())
	assert $dllVersion.StartsWith("$Version.") 'Versions mismatch.'

	# nuspec
	Set-Content z\Package.nuspec @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	<metadata>
		<id>FarNet.FSharpFar</id>
		<version>$Version</version>
		<authors>Roman Kuzmin</authors>
		<owners>Roman Kuzmin</owners>
		<projectUrl>https://github.com/nightroman/FarNet/tree/main/FSharpFar</projectUrl>
		<icon>FarNetLogo.png</icon>
		<readme>README.md</readme>
		<license type="expression">BSD-3-Clause</license>
		<description>$Description</description>
		<releaseNotes>https://github.com/nightroman/FarNet/blob/main/FSharpFar/History.txt</releaseNotes>
		<tags>FarManager FarNet Module FSharp</tags>
	</metadata>
</package>
"@
	# pack
	exec { NuGet pack z\Package.nuspec }
}

task test_testing {
	Start-Far "fs: exec: file=$env:FarNetCode\FSharpFar\samples\Testing\App1.fsx" -ReadOnly -Title Testing -Environment @{QuitFarAfterTests=1}
}

task test_tests {
	Start-Far "fs: exec: file=$env:FarNetCode\FSharpFar\tests\App1.fsx" -ReadOnly -Title Tests -Environment @{QuitFarAfterTests=1}
}

task test_tasks {
	Start-Far "ps: Test.far.ps1 * -Quit" $env:FarNetCode\FSharpFar\tests\PSF.test -ReadOnly -Title FSharpFar\PSF.test
}

task test_fsx {
	Invoke-Build Test src\fsx\.build.ps1
}

task test test_tasks, test_tests, test_testing, test_fsx

task . build, clean
