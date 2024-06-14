﻿<#
.Notes
	New OK: if ($Regex -isnot [regex]) {$Regex = "$Regex"}
	Old KO: if ($Regex -is [string]) {...} elseif ($Regex -isnot [regex]) {ERROR}
	KO because `Search-Regex 12.9` ~ ERROR (neither string nor regex).
#>

job {
	### Whole word option

	#! from script
	function WholeWord($_) {
		"(?(?=\w)\b)$_(?(?<=\w)\b)"
	}

	Assert-Far @(
		'='  -match    (WholeWord '=')
		'-=' -match    (WholeWord '=')
		'=-' -match    (WholeWord '=')
		'b=' -match    (WholeWord '=')
		'=b' -match    (WholeWord '=')
	)

	Assert-Far @(
		'a'  -match    (WholeWord 'a')
		'-a' -match    (WholeWord 'a')
		'a-' -match    (WholeWord 'a')
		'ba' -notmatch (WholeWord 'a')
		'ab' -notmatch (WholeWord 'a')
	)

	Assert-Far @(
		'-a'  -match    (WholeWord '-a')
		'--a' -match    (WholeWord '-a')
		'-a-' -match    (WholeWord '-a')
		'b-a' -match    (WholeWord '-a')
		'-ab' -notmatch (WholeWord '-a')
	)

	Assert-Far @(
		'a-'  -match    (WholeWord 'a-')
		'-a-' -match    (WholeWord 'a-')
		'a--' -match    (WholeWord 'a-')
		'a-b' -match    (WholeWord 'a-')
		'ba-' -notmatch (WholeWord 'a-')
	)
}

job {
	### Invalid input

	$r = try {<##> Search-Regex.ps1 -miss miss} catch {$_}
	Assert-Far "$r" -eq 'Invalid arguments: -miss miss'
	Assert-Far $r.InvocationInfo.Line.Contains('<##>')

	$r = try {<##> Search-Regex.ps1 it} catch {$_}
	Assert-Far "$r" -eq 'There is no input to search in.'
	Assert-Far $r.InvocationInfo.Line.Contains('<##>')

	$r = try {'bar' | <##> Search-Regex.ps1 Assert miss} catch {$_}
	Assert-Far ("$r" -like 'Cannot convert value "miss" to type "SearchRegexOptions"*IgnoreCase*SimpleMatch*WholeWord*')
	Assert-Far $r.InvocationInfo.Line.Contains('<##>')

	$global:Error.Clear()
}
