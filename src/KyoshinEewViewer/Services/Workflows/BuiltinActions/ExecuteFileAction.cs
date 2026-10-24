using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Scriban;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace KyoshinEewViewer.Services.Workflows.BuiltinActions;

public partial class ExecuteFileAction : WorkflowAction
{
	private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
	{
		Converters =
		{
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
		},
	};

	[JsonIgnore]
	public override Control DisplayControl => new ExecuteFileActionControl() { DataContext = this };

	[ObservableProperty]
	public partial string FilePath { get; set; } = "";

	[ObservableProperty]
	public partial string WorkingDirectory { get; set; } = "";

	[ObservableProperty]
	public partial bool UseShellExecute { get; set; } = true;

	[ObservableProperty]
	public partial string Arguments { get; set; } = "";

	[ObservableProperty]
	public partial bool WaitToEnd { get; set; } = true;

	[JsonIgnore]
	[ObservableProperty]
	public partial string LatestExecuteResult { get; set; } = "";

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
	public async override Task ExecuteAsync(WorkflowEvent content)
	{
		if (string.IsNullOrWhiteSpace(FilePath))
			return;

		// テンプレートをレンダリング
		var renderedFilePath = (await Template.Parse(FilePath).RenderAsync(content, m => m.Name)).Trim().Replace("\n", "");
		var renderedArguments = (await Template.Parse(Arguments).RenderAsync(content, m => m.Name)).Trim();
		var renderedWorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory)
			? ""
			: (await Template.Parse(WorkingDirectory).RenderAsync(content, m => m.Name)).Trim().Replace("\n", "");

		var info = CreateProcessStartInfo(renderedFilePath, renderedArguments, renderedWorkingDirectory);
		info.EnvironmentVariables["KEVI_EVENT_DATA"] = JsonSerializer.Serialize(content, JsonSerializerOptions);

		var task = Task.Run(async () =>
		{
			try
			{
				var sw = Stopwatch.StartNew();
				using var process = Process.Start(info);
				if (process == null)
				{
					LatestExecuteResult = "プロセスの起動に失敗しました";
					return;
				}
				LatestExecuteResult = "実行中...";
				using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
				await process.WaitForExitAsync(cts.Token);
				LatestExecuteResult = $"終了コード: {process.ExitCode}\n実行時間:{sw.ElapsedMilliseconds}ms";
			}
			catch (OperationCanceledException)
			{
				LatestExecuteResult = "プロセスの実行がタイムアウト(2分)しました。";
			}
			catch (Exception ex)
			{
				LatestExecuteResult = "実行中に例外が発生しました。\n" + ex;
			}
		});

		if (WaitToEnd)
			await task;
	}

	private ProcessStartInfo CreateProcessStartInfo(string filePath, string arguments, string workingDirectory)
	{
		if (UseShellExecute)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return new ProcessStartInfo(filePath, arguments)
				{
					WorkingDirectory = workingDirectory,
					UseShellExecute = true,
				};
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var psi = new ProcessStartInfo("xdg-open")
				{
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				psi.ArgumentList.Add(filePath);
				if (!string.IsNullOrWhiteSpace(arguments))
					psi.ArgumentList.Add(arguments);
				return psi;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				var psi = new ProcessStartInfo("open")
				{
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				psi.ArgumentList.Add(filePath);
				if (!string.IsNullOrWhiteSpace(arguments))
					psi.ArgumentList.Add(arguments);
				return psi;
			}
		}

		return new ProcessStartInfo(filePath, arguments)
		{
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
	}
}
