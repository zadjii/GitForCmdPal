// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

public partial class GitExtensionCommandsProvider : CommandProvider
{
    private readonly List<ICommandItem> _repoCommands = [];
    private readonly AddRepoPage _addRepoPage = new();

    private AppState _state = new();

    public GitExtensionCommandsProvider()
    {
        DisplayName = "Git for CmdPal";
        Icon = Icons.AppIcon;

        LoadReposFromFile();
    }

    public override ICommandItem[] TopLevelCommands()
    {
        var addRepoItem = new ListItem(_addRepoPage) { Title = "Add a git repo" };
        return [.. _repoCommands, addRepoItem];
    }

    private void LoadReposFromFile()
    {
        try
        {
            var jsonFile = StateJsonPath();
            if (File.Exists(jsonFile))
            {
                _state = AppState.ReadFromFile(jsonFile);
            }
        }
        catch (Exception)
        {

        }

        _repoCommands.Clear();

        var hasVsCode = IsVSCodeInstalled();

        foreach (var repo in _state.Repos)
        {
            var page = new GitExtensionPage(repo);
            List<CommandContextItem> contextItems = [];
            var openExplorer = new OpenUrlCommand(repo.Path) { Icon = Icons.ExplorerIcon, Name = "Open path" };
            contextItems.Add(new CommandContextItem(openExplorer));
            if (hasVsCode)
            {
                var openCodeCommand = new CommandContextItem(new OpenInCodeCommand(repo.Path));
                contextItems.Add(openCodeCommand);
            }
            var ci = new CommandItem(page)
            {
                Title = repo.Name,
                Subtitle = repo.Path,
                Icon = page.Icon,
                MoreCommands = [.. contextItems],
            };
            _repoCommands.Add(ci);
        }
    }
    internal static string StateJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("zadjii.GitExtension");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return System.IO.Path.Combine(directory, "state.json");
    }

    private static bool IsVSCodeInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "code",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            var output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            process?.WaitForExit();
            return !string.IsNullOrWhiteSpace(output);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

internal sealed partial class OpenInCodeCommand : InvokableCommand
{
    private readonly string _path;
    internal OpenInCodeCommand(string path)
    {
        _path = path;
        Name = "Open in VsCode";
        Icon = Icons.VsCodeIcon;
    }
    public override ICommandResult Invoke()
    {
        Process.Start("code", _path);
        return CommandResult.Dismiss();
    }
}

public sealed class AppState
{
    public List<RepoData> Repos { get; set; } = [];

    public static AppState ReadFromFile(string path)
    {
        var data = new AppState();

        // if the file exists, load it and append the new item
        if (File.Exists(path))
        {
            var jsonStringReading = File.ReadAllText(path);

            if (!string.IsNullOrEmpty(jsonStringReading))
            {
                data = JsonSerializer.Deserialize<AppState>(jsonStringReading, JsonSerializationContext.Default.AppState) ?? new AppState();
            }
        }

        return data;
    }

    public static void WriteToFile(AppState data)
    {
        var jsonString = JsonSerializer.Serialize(data, JsonSerializationContext.Default.AppState);

        File.WriteAllText(GitExtensionCommandsProvider.StateJsonPath(), jsonString);
    }
}

public sealed class RepoData
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

}

[JsonSerializable(typeof(AppState))]
[JsonSerializable(typeof(RepoData))]
[JsonSerializable(typeof(string))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true, WriteIndented = true, IncludeFields = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Just used here")]
public partial class JsonSerializationContext : JsonSerializerContext
{
}
