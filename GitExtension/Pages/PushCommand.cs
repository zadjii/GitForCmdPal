// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class PushCommand : InvokableCommand
{
    private readonly Repository _repo;
    private readonly Remote _remote;
    private readonly string _pushRefSpec;

    private PushCommand(Repository repo, Remote remote, string pushRefSpec)
    {
        _repo = repo;
        _remote = remote;
        _pushRefSpec = pushRefSpec;

        Name = "Push to upstream";
        Icon = Icons.Push;
        //_repo = repo;
    }

    public override ICommandResult Invoke()
    {
        try
        {
            // Push the current branch to its upstream branch.
            _repo.Network.Push(_remote, _pushRefSpec, new PushOptions());


            //Console.WriteLine($"Successfully pushed branch '{currentBranch.FriendlyName}' to upstream '{remote.Name}/{remoteBranchName}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during push: " + ex.Message);
        }

        return CommandResult.ShowToast(new ToastArgs()
        {
            Message = $"Successfully pushed branch to upstream '{_remote.Name}'",
            Result = CommandResult.KeepOpen(),
        });

    }

    public static ICommand? CreatePushForCurrentBrach(Repository repo)
    {
        // Get the current branch (HEAD)
        var currentBranch = repo.Head;
        if (currentBranch == null)
        {
            ExtensionHost.LogMessage("No current branch found.");
            return null;
        }

        // Check if the current branch has an upstream (tracked branch)
        if (currentBranch.TrackedBranch == null)
        {
            ExtensionHost.LogMessage("Current branch does not have an upstream configured.");
            return new PushListPage(repo, currentBranch);
        }

        // Retrieve the remote name from the upstream branch.
        var remoteName = currentBranch.TrackedBranch.RemoteName;
        var remote = repo.Network.Remotes[remoteName];
        if (remote == null)
        {
            ExtensionHost.LogMessage($"Remote '{remoteName}' not found.");
            return null;
        }

        // The canonical name of the tracked branch is typically in the form:
        // "refs/remotes/{remoteName}/{branchName}".
        // We need to convert this to the push reference "refs/heads/{branchName}".
        var trackedCanonical = currentBranch.TrackedBranch.CanonicalName;
        var prefix = $"refs/remotes/{remoteName}/";
        if (!trackedCanonical.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
        {
            ExtensionHost.LogMessage("Unexpected upstream branch canonical name format.");
            return null;
        }
        var remoteBranchName = trackedCanonical[prefix.Length..];
        var pushRefSpec = $"refs/heads/{remoteBranchName}";

        return new PushCommand(repo, remote, pushRefSpec);
    }
}


internal sealed partial class PushListPage : ListPage
{
    private readonly Repository _repo;
    private readonly Branch _branch;
    public PushListPage(Repository repo, Branch currentBranch)
    {
        _repo = repo;
        _branch = currentBranch;
        Title = $"Push {currentBranch.FriendlyName} to...";
        Name = "Push to...";
        Icon = Icons.Push;
    }

    public override IListItem[] GetItems()
    {
        return _repo.Network.Remotes
            .Select(r =>
            {
                var ci = new CommandItem(
                    title: r.Name,
                    subtitle: r.Url,
                    action: () =>
                    {
                        _repo.Network.Push(r, _branch.CanonicalName);
                    },
                    result: CommandResult.GoBack())
                {
                    Icon = Icons.Push,
                };
                var li = new ListItem(ci);
                return li;
            })
            .ToArray();
    }
}
