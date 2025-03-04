// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class PullCommand : InvokableCommand
{
    private readonly Repository _repo;
    internal PullCommand(Repository repo)
    {
        Name = "Pull";
        Icon = Icons.Pull;
        _repo = repo;
    }
    public override ICommandResult Invoke()
    {
        var options = new LibGit2Sharp.PullOptions
        {
            FetchOptions = new FetchOptions()
        };
        var signature = _repo.Config.BuildSignature(System.DateTimeOffset.Now);

        Commands.Pull(_repo, signature, options);

        return CommandResult.KeepOpen();
    }
}
