
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using System.Management.Automation;

namespace PowerShellFar.Commands;

sealed class StartFarJobCommand : BaseCmdlet
{
	public StartFarJobCommand()
	{
		KeepSeconds = int.MaxValue;
	}

	[Parameter(Position = 0, Mandatory = true)]
	public JobCommand Command { get; set; } = null!;

	[Parameter(Position = 1)]
	public PSObject? Parameters { get; set; }

	[Parameter]
	public string? Name { get; set; }

	[Parameter]
	public SwitchParameter Output { get; set; }

	[Parameter]
	public SwitchParameter Return { get; set; }

	[Parameter]
	public SwitchParameter Hidden { get; set; }

	[Parameter]
	public int KeepSeconds { get; set; }

	protected override void BeginProcessing()
	{
		if (Hidden)
		{
			Output = Return = false;
			KeepSeconds = 0;
		}
		else if (Return)
		{
			Output = true;
			KeepSeconds = int.MaxValue;
		}
		else if (Output)
		{
			KeepSeconds = int.MaxValue;
		}

		// new
		var job = new Job(
			Command,
			Parameters?.BaseObject,
			Name,
			!Hidden && !Output,
			KeepSeconds);

		// start
		if (!Return)
			job.StartJob();

		// write
		if (Output)
			WriteObject(job);
	}
}
