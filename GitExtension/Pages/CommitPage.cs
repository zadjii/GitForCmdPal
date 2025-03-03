// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class CommitPage : ContentPage
{
    private readonly Repository _repo;
    private readonly CommitForm _form;
    private readonly MarkdownContent _summary;
    internal CommitPage(Repository repo)
    {
        Name = "Commit...";
        Icon = Icons.Commit;
        _repo = repo;
        _form = new(_repo);

        var statusText = new StringBuilder();
        statusText.Append("# Status\n\n");
        var status = _repo.RetrieveStatus();
        foreach (var item in status.Staged)
        {
            statusText.Append(DisplayStatus(item));
            statusText.Append("\n\n");
        }
        _summary = new MarkdownContent() { Body = statusText.ToString() };
    }

    public override IContent[] GetContent() => [_form, _summary];

    internal static string DisplayStatus(StatusEntry file)
    {
        if ((file.State & FileStatus.RenamedInIndex) == FileStatus.RenamedInIndex ||
            (file.State & FileStatus.RenamedInWorkdir) == FileStatus.RenamedInWorkdir)
        {
            var oldFilePath = ((file.State & FileStatus.RenamedInIndex) != 0)
                ? file.HeadToIndexRenameDetails.OldFilePath
                : file.IndexToWorkDirRenameDetails.OldFilePath;

            return string.Format(CultureInfo.InvariantCulture, "{0}: {1} -> {2}", file.State, oldFilePath, file.FilePath);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", file.State, file.FilePath);
    }
}


internal sealed partial class CommitForm : FormContent
{
    private readonly Repository _repo;
    internal CommitForm(Repository repo)
    {
        _repo = repo;

        TemplateJson = """
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.5",
    "body": [
        {
            "type": "Input.Text",
            "style": "text",
            "id": "title",
            "label": "Title",
            "value": "",
            "placeholder": "Add a commit message",
            "isRequired": true,
            "errorMessage": "Message is required"
        },
        {
            "type": "Input.Text",
            "style": "text",
            "id": "body",
            "value": "",
            "isMultiline": true,
            "placeholder": "Add more details here",
            "label": "Path to repo"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Commit",
            "data": {
                "title": "title",
                "body": "body"
            }
        }
    ]
}
""";

    }
    public override ICommandResult SubmitForm(string inputs, string data)
    {
        var formInput = JsonNode.Parse(inputs);
        if (formInput == null)
        {
            return CommandResult.KeepOpen();
        }

        // get the name and url out of the values
        var title = formInput["title"]?.ToString() ?? string.Empty;
        var body = formInput["body"]?.ToString() ?? string.Empty;
        var fullMessage = string.IsNullOrEmpty(body) ? title : $"{title}\n\n{body.Replace('\r', '\n')}";

        // Build the signature from the repo's configuration (username/email)
        var signature = _repo.Config.BuildSignature(System.DateTimeOffset.Now);

        // Create the commit
        _ = _repo.Commit(fullMessage, signature, signature);

        return CommandResult.GoBack();
    }
}