# Start-FarTask samples

`Start-FarTask` starts task scripts with jobs and macros.

To view help, use:

    vps: help -full Start-FarTask

There are two kind of scripts:

- Scripts `*.far.ps1` are normal scripts. They work with FarNet as usual and
  then they call `Start-FarTask` with the task script and prepared data.

- Scripts `*.fas.ps1` are task scripts. They are invoked by `Start-FarTask`,
  for example by the association `ps: Start-FarTask (Get-FarPath) #`. Such
  scripts work with FarNet using jobs and macros.

[Basics.fas.ps1]: Basics.fas.ps1
[NestedTasks.fas.ps1]: NestedTasks.fas.ps1
[DialogNonModalInput.fas.ps1]: DialogNonModalInput.fas.ps1
[InputEditorMessage.fas.ps1]: InputEditorMessage.fas.ps1

Task scripts may call other tasks and consume their results, if any. Example:
[NestedTasks.fas.ps1] calls [DialogNonModalInput.fas.ps1] and uses its result
as the parameter for the next task [InputEditorMessage.fas.ps1].

[KeysAndMacro.fas.ps1]: KeysAndMacro.fas.ps1
[Test-Dialog.fas.ps1]: Test-Dialog.fas.ps1

Apart from practical async applications, `Start-FarTask` is suitable for tests
implemented as task scripts. For example, the script [KeysAndMacro.fas.ps1] is
test like: it does not require user interaction and it checks for the expected
results. Other tests: [Basics.fas.ps1] and [Test-Dialog.fas.ps1].

## Tips and tricks

### Share data between tasks

Use `[FarNet.User]::Data`, the concurrent dictionary, for sharing global data
between different tasks.

See [ConsoleGitStatus.far.ps1](ConsoleGitStatus.far.ps1), it uses the global
data in order to run and stop the only task instance.

### Output task messages to console

Tasks running in the background may output their messages to the console in
order to notify about the progress, error, completion. This way may be less
intrusive than, say, showing message boxes. Use the `ps:` jobs. They output
to the console, as if they are called from the command line.

See [ConsoleGitStatus.far.ps1](ConsoleGitStatus.far.ps1), it prints some git
info when the current panel directory changes.

### Beware of unexpected current paths

Keep in mind, there are several different current paths in tasks and jobs and
they all may be out of sync. Interesting paths:

- Far Manager process current directory, `%FARHOME%`, it normally does not change
- Far Manager internal current directory, it depends on the active panel path
- Task current location, normally it is the start panel path, it may change
- Job current location, it is the session current location, it may change

See [CurrentLocations.fas.ps1](Case/CurrentLocations.fas.ps1), it shows how
these paths change and may be same or different depending on actions.

### Expose all task or job variables

If a task or job creates many variables to be shared, then instead of saving
them in `$Data` individually you may expose all variables:

```powershell
$Data.Var = $ExecutionContext.SessionState.PSVariable
```

Then later get variable values as `$Data.Var.GetValue('myVar')`.
See [Case/PSVariable.fas.ps1](Case/PSVariable.fas.ps1)
