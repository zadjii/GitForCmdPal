// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GitExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;

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
        foreach (var repo in _state.Repos)
        {
            var page = new GitExtensionPage(repo);
            var li = new ListItem(page) { Title = repo.Name, Subtitle = repo.Path };
            _repoCommands.Add(li);
        }
    }
    internal static string StateJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("zadjii.GitExtension");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return System.IO.Path.Combine(directory, "state.json");
    }
}


internal sealed partial class AddRepoPage : ContentPage
{
    private readonly AddRepoForm _addRepo;

    public AddRepoPage()
    {
        _addRepo = new AddRepoForm();
        Name = "Open";
        Icon = Icons.AddNewRepo;
    }
    internal event TypedEventHandler<object, object?>? AddedCommand
    {
        add => _addRepo.AddedCommand += value;
        remove => _addRepo.AddedCommand -= value;
    }
    public override IContent[] GetContent() => [_addRepo];
}

internal sealed partial class AddRepoForm : FormContent
{
    internal event TypedEventHandler<object, object?>? AddedCommand;

    public AddRepoForm(string name = "", string url = "")
    {
        TemplateJson = $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.5",
    "body": [
        {
            "type": "Input.Text",
            "style": "text",
            "id": "name",
            "label": "Name",
            "value": {{JsonSerializer.Serialize(name, JsonSerializationContext.Default.String)}},
            "isRequired": true,
            "errorMessage": "Name is required"
        },
        {
            "type": "Input.Text",
            "style": "text",
            "id": "path",
            "value": {{JsonSerializer.Serialize(url, JsonSerializationContext.Default.String)}},
            "label": "Path to repo",
            "isRequired": true,
            "errorMessage": "File path is required"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Save",
            "data": {
                "name": "name",
                "path": "path"
            }
        }
    ]
}
""";
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload);
        if (formInput == null)
        {
            return CommandResult.GoHome();
        }

        // get the name and url out of the values
        var formName = formInput["name"] ?? string.Empty;
        var formPath = formInput["path"] ?? string.Empty;


        var formData = new RepoData()
        {
            Name = formName.ToString(),
            Path = formPath.ToString(),
        };

        // Construct a new json blob with the name and url
        var jsonPath = GitExtensionCommandsProvider.StateJsonPath();
        var state = AppState.ReadFromFile(jsonPath);
        state.Repos.Add(formData);
        AppState.WriteToFile(state);

        AddedCommand?.Invoke(this, null);
        return CommandResult.GoHome();
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
