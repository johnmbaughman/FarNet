
<#
.Synopsis
	TabExpansion2 argument completers.
	Author: Roman Kuzmin

.Description
	The script should be in the path. It is invoked on the first call of
	TabExpansion2 in order to register various completers. The script
	TabExpansion2.ps1 is built in PowerShellFar but it is universal.

	This profile reflects preferences of the author. Use it as the sample for
	your completers. Multiple profiles *ArgumentCompleters.ps1 are supported.
#>

### FarHost completers
if ($Host.Name -ceq 'FarHost') {
	### Find-FarFile - names from the active panel
	Register-ArgumentCompleter -CommandName Find-FarFile -ParameterName Name -ScriptBlock {
		param($commandName, $parameterName, $wordToComplete, $commandAst, $boundParameters)
		@(foreach($_ in $Far.Panel.ShownFiles) {$_.Name}) -like "$wordToComplete*"
	}
	### Out-FarPanel - properties + column info template
	Register-ArgumentCompleter -CommandName Out-FarPanel -ParameterName Columns -ScriptBlock {
		param($commandName, $parameterName, $wordToComplete, $commandAst, $boundParameters)

		# properties
		if (($ast = $commandAst.Parent) -is [System.Management.Automation.Language.PipelineAst] -and $ast.PipelineElements.Count -eq 2) {
			try {
				(Invoke-Expression $ast.PipelineElements[0] | Get-Member $wordToComplete* -MemberType Properties).Name | Sort-Object -Unique
			}
			catch {}
		}

		# column info template
		$$ = "@{e=''; n=''; k=''; w=0; a=''}"
		New-Object System.Management.Automation.CompletionResult $$, '@{...}', 'ParameterValue', $$
	}
}

### Parameter ComputerName for all cmdlets
Register-ArgumentCompleter -ParameterName ComputerName -ScriptBlock {
	# add this machine first
	$name = $env:COMPUTERNAME
	New-Object System.Management.Automation.CompletionResult $name, $name, 'ParameterValue', $name

	# add others from the list
	foreach($_ in $env:pc_master, $env:pc_slave) {
		if ($_ -and $_ -ne $name) {
			New-Object System.Management.Automation.CompletionResult $_, $_, 'ParameterValue', $_
		}
	}
}

### Complete x in "git x"
Register-ArgumentCompleter -CommandName git -Native -ScriptBlock {
	param($wordToComplete, $commandAst)

	# if (!word)
	# git x [Tab] ~ ast -ne "git" -- skip it
	# git [Tab] ~ ast -eq "git" -- get all commands
	$code = "$commandAst"
	if (!$wordToComplete -and $code -ne 'git') {return}
	if ($wordToComplete -notmatch '^\w?[\w\-]*$') {return}
	if ($code -notmatch "^git\s*$wordToComplete$") {return}

	$wild = "$wordToComplete*"
	$(foreach($line in git help -a) {
		if ($line -match '^  \S') {
			foreach($token in $line -split '\s+') {
				if ($token -and $token -like $wild) {
					$token
				}
			}
		}
	}) | Sort-Object
}

### Add result processors
$TabExpansionOptions.ResultProcessors += {
	### WORD=[Tab] completions from TabExpansion.txt
	param($result, $ast, $tokens, $positionOfCursor, $options)

	# default
	if ($result.CompletionMatches) {return}

	# WORD=?
	if ("$ast".Substring($result.ReplacementIndex, $result.ReplacementLength) -notmatch '(^.*)=$') {return}
	$body = [regex]::Escape($matches[1])
	$head = "^$body"

	# completions from TabExpansion.txt in the TabExpansion2 script directory
	$path = [System.IO.Path]::GetDirectoryName((Get-Item Function:TabExpansion2).ScriptBlock.File)
	$lines = @(Get-Content -LiteralPath $path\TabExpansion.txt)
	$lines -match $body | Sort-Object {$_ -notmatch $head}, {$_} | .{process{
		if ($Host.Name -cne 'FarHost') {$_ = $_.Replace('#', '')}
		$result.CompletionMatches.Add($_)
	}}
},{
	### WORD#[Tab] completions from history
	param($result, $ast, $tokens, $positionOfCursor, $options)

	# default
	if ($result.CompletionMatches) {return}

	# WORD#?
	if ("$ast".Substring($result.ReplacementIndex, $result.ReplacementLength) -notmatch '(^.*)#$') {return}
	$body = [regex]::Escape($matches[1])

	$_ = [System.Collections.ArrayList](@(Get-History -Count 9999) -match $body)
	$_.Reverse()
	$_ | .{process{
		$result.CompletionMatches.Add((
			New-Object System.Management.Automation.CompletionResult $_, $_, 'History', $_
		))
	}}
},{
	### Complete an alias as definition and remove itself
	param($result, $ast, $tokens, $positionOfCursor, $options)

	$token = foreach($_ in $tokens) {if ($_.Extent.EndOffset -eq $positionOfCursor.Offset) {$_; break}}
	if (!$token -or $token.TokenFlags -ne 'CommandName') {return}

	# aliases
	$name = "$token"
	$aliases = @(Get-Alias $name -ErrorAction Ignore)
	if ($aliases.Count -ne 1) {return}

	# remove itself
	for($i = $result.CompletionMatches.Count; --$i -ge 0) {
		if ($result.CompletionMatches[$i].CompletionText -eq $name) {
			$result.CompletionMatches.RemoveAt($i)
			break
		}
	}

	# insert first
	$result.CompletionMatches.Insert(0, $(
		$$ = $aliases[0].Definition
		New-Object System.Management.Automation.CompletionResult $$, $$, 'Command', $$
	))
},{
	### Complete variable $*var
	param($result, $ast, $tokens, $positionOfCursor, $options)

	$token = foreach($_ in $tokens) {if ($_.Extent.EndOffset -eq $positionOfCursor.Offset) {$_; break}}
	if (!$token -or $token -notmatch '^\$(\*.*)') {return}

	foreach($_ in Get-Variable "$($matches[1])*") {
		$result.CompletionMatches.Add($(
			$$ = "`$$($_.Name)"
			New-Object System.Management.Automation.CompletionResult $$, $$, 'Variable', $$
		))
	}
}

### Add input processors
$TabExpansionOptions.InputProcessors += {
	### Complete [Type/Namespace[Tab]
	# Expands one piece at a time, e.g. [System. | [System.Data. | [System.Data.CommandType]
	# If pattern in "[pattern" contains wildcard characters all types are searched for the match.
	param($ast, $tokens, $positionOfCursor, $options)

	$token = foreach($_ in $tokens) {if ($_.Extent.EndOffset -eq $positionOfCursor.Offset) {$_; break}}
	if (!$token -or ($token.TokenFlags -ne 'TypeName' -and $token.TokenFlags -ne 'CommandName')) {return}

	$line = $positionOfCursor.Line.Substring(0, $positionOfCursor.ColumnNumber - 1)
	if ($line -notmatch '\[([\w.*?]+)$') {return}
	$pattern = $matches[1]

	# fake
	function TabExpansion($line, $lastWord) { GetTabExpansionType $pattern $lastWord.Substring(0, $lastWord.Length - $pattern.Length) }
	$result = [System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $positionOfCursor, $null)

	# amend
	for($i = $result.CompletionMatches.Count; --$i -ge 0) {
		$text = $result.CompletionMatches[$i].CompletionText
		if ($text -match '\b(\w+([.,\[\]])+)$') {
			$type = if ($matches[2] -ceq '.') {'Namespace'} else {'Type'}
			$result.CompletionMatches[$i] = New-Object System.Management.Automation.CompletionResult $text, "[$($matches[1])", $type, $text
		}
	}

	$result
},{
	### Complete in comments: help tags or one line code
	param($ast, $tokens, $positionOfCursor, $options)

	$token = foreach($_ in $tokens) {if ($_.Extent.EndOffset -ge $positionOfCursor.Offset) {$_; break}}
	if (!$token -or $token.Kind -ne 'Comment') {return}

	$line = $positionOfCursor.Line
	$caret = $positionOfCursor.ColumnNumber - 1
	$offset = $positionOfCursor.Offset - $caret

	# help tags
	if ($line -match '^(\s*#*\s*)(\.\w*)' -and $caret -eq $Matches[0].Length) {
		$results = @(
			'.Synopsis'
			'.Description'
			'.Parameter'
			'.Inputs'
			'.Outputs'
			'.Notes'
			'.Example'
			'.Link'
			'.Component'
			'.Role'
			'.Functionality'
			'.ForwardHelpTargetName'
			'.ForwardHelpCategory'
			'.RemoteHelpRunspace'
			'.ExternalHelp'
		) -like "$($Matches[2])*"
		function TabExpansion {$results}
		return [System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $positionOfCursor, $null)
	}

	# one line code
	if ($token.Extent.StartOffset -gt $offset) {
		$line = $line.Substring($token.Extent.StartOffset - $offset)
		$offset = $token.Extent.StartOffset
	}
	if ($line -match '^(\s*(?:<#)?#*\s*)(.+)') {
		$inputScript = ''.PadRight($offset + $Matches[1].Length) + $Matches[2]
		TabExpansion2 $inputScript $positionOfCursor.Offset $options
	}
}

<#
.Synopsis
	Gets types and namespaces for completers.
#>
function global:GetTabExpansionType($pattern, $prefix)
{
	$suffix = if ($prefix) {']'} else {''}

	# wildcard type
	if ([System.Management.Automation.WildcardPattern]::ContainsWildcardCharacters($pattern)) {
		.{ foreach($assembly in [System.AppDomain]::CurrentDomain.GetAssemblies()) {
			try {
				foreach($_ in $assembly.GetExportedTypes()) {
					if ($_.FullName -like $pattern) {
						"$prefix$($_.FullName)$suffix"
					}
				}
			}
			catch { $Error.RemoveAt(0) }
		}} | Sort-Object
		return
	}

	# patterns
	$escaped = [regex]::Escape($pattern)
	$re1 = [regex]"(?i)^($escaped[^.]*)"
	$re2 = [regex]"(?i)^($escaped[^.``]*)(?:``(\d+))?$"
	if (!$pattern.StartsWith('System.', 'OrdinalIgnoreCase')) {
		$re1 = $re1, [regex]"(?i)^System\.($escaped[^.]*)"
		$re2 = $re2, [regex]"(?i)^System\.($escaped[^.``]*)(?:``(\d+))?$"
	}

	# namespaces and types
	$1 = @{}
	$2 = [System.Collections.ArrayList]@()
	foreach($assembly in [System.AppDomain]::CurrentDomain.GetAssemblies()) {
		try { $types = $assembly.GetExportedTypes() }
		catch { $Error.RemoveAt(0); continue }
		$n = [System.Collections.Generic.HashSet[object]]@(foreach($_ in $types) {$_.Namespace})
		foreach($r in $re1) {
			foreach($_ in $n) {
				if ($_ -match $r) {
					$1["$prefix$($matches[1])."] = $null
				}
			}
		}
		foreach($r in $re2) {
			foreach($_ in $types) {
				if ($_.FullName -match $r) {
					if ($matches[2]) {
						$null = $2.Add("$prefix$($matches[1])[$(''.PadRight(([int]$matches[2] - 1), ','))]$suffix")
					}
					else {
						$null = $2.Add("$prefix$($matches[1])$suffix")
					}
				}
			}
		}
	}
	$1.Keys | Sort-Object
	$2 | Sort-Object
}