// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class UnstageAllCommand : InvokableCommand
{
    private readonly Repository _repo;
    internal UnstageAllCommand(Repository repo)
    {
        Name = "Unstage all";
        Icon = Icons.AddAll;
        _repo = repo;
    }
    public override ICommandResult Invoke()
    {
        Commands.Unstage(_repo, "*");

        return CommandResult.KeepOpen();
    }
}
