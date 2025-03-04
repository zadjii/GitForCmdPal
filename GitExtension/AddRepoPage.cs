// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Nodes;
using GitExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;

namespace GitExtension;

internal sealed partial class AddRepoPage : ContentPage
{
    private readonly AddRepoForm _addRepo;

    public AddRepoPage()
    {
        _addRepo = new AddRepoForm();
        Name = "Open";
        Icon = Icons.AddRepoIcon;
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