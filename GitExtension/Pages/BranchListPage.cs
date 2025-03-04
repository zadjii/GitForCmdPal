// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json.Nodes;
using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class BranchListPage : ListPage
{
    private readonly Repository _repo;
    private readonly bool _allBranches;

    public BranchListPage(Repository repo, bool all = false)
    {
        _repo = repo;
        _allBranches = all;
        Name = "Branches";
        Icon = Icons.Checkout;
    }

    public override IListItem[] GetItems()
    {
        var branches = _repo.Branches
            .Where(b => !b.IsRemote || _allBranches)
            .Select(BranchToListItem);

        return [new ListItem(new NewBranchPage(_repo)), .. branches];
    }

    private ListItem BranchToListItem(Branch b)
    {
        var isCurrent = b.IsCurrentRepositoryHead;

        var ci = new CommandItem(
            title: b.FriendlyName,
            subtitle: b.UpstreamBranchCanonicalName,
            name: isCurrent ? "Current" : "Checkout",
            action: () =>
            {
                Commands.Checkout(_repo, b);
            },
            result: CommandResult.GoBack())
        {
            Icon = isCurrent ? Icons.CurrentBranch : Icons.Checkout,
        };


        var li = new ListItem(ci);
        return li;

    }
}


internal sealed partial class NewBranchPage : ContentPage
{
    private readonly Repository _repo;
    private readonly NewBranchForm _form;
    internal NewBranchPage(Repository repo)
    {
        Name = "New branch...";
        Icon = Icons.Add; //Icons.CreateBranch;
        _repo = repo;
        _form = new(_repo);
    }

    public override IContent[] GetContent() => [_form];
}


internal sealed partial class NewBranchForm : FormContent
{
    private readonly Repository _repo;
    internal NewBranchForm(Repository repo)
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
            "id": "branch",
            "label": "Branch name",
            "value": "",
            "placeholder": "branchName",
            "isRequired": true,
            "errorMessage": "Message is required"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Checkout new branch",
            "data": {
                "branch": "branch",
                "verb": "checkout"
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
        var branch = formInput["branch"]?.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(branch))
        {
            var b = _repo.CreateBranch(branch);
            Commands.Checkout(_repo, b);
        }

        return CommandResult.GoBack();
    }
}