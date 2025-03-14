// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class GitExtensionPage : ListPage, System.IDisposable
{
    private readonly RepoData _repoData;
    private readonly Repository? _repo;

    private readonly StatusPage _statusPage;
    private readonly AddAllCommand _addAllCommand;
    private readonly CommitPage _commitPage;
    private readonly UnstageAllCommand _unstageAllCommand;
    private readonly PullCommand _pullCommand;
    //private PushCommand? _pushCommand;
    private readonly BranchListPage _branchListPage;

    private readonly ListItem _statusItem;
    private readonly ListItem _addAllItem;
    private readonly ListItem _commitItem;
    private readonly ListItem _unstageAllItem;
    private readonly ListItem _pullItem;
    //private ListItem? _pushItem;
    private readonly ListItem _branchListItem;

    public GitExtensionPage(RepoData repo)
    {
        _repoData = repo;

        Icon = Icons.AppIcon;
        Name = "Open";
        ShowDetails = true;

        var repoPath = _repoData.Path;

        try
        {
            _repo = new(repoPath);
        }
        catch (LibGit2SharpException)
        {

        }
        if (_repo == null)
        {

            var emptyCommands = GitExtensionCommandsProvider.ContextMenuForRepo(_repoData);

            EmptyContent = new CommandItem()
            {
                Title = "No git repo found",
                Subtitle = "Run `git init` in this directory to create a repo",
                Icon = Icons.AppIcon,
                Command = emptyCommands.First().Command,
                MoreCommands = emptyCommands.Skip(1).ToArray()
            };
            return;
        }

        Title = $"{_repoData.Name} {(_repo != null ? BranchString(_repo) : string.Empty)}";

        //_watcher.EnableRaisingEvents = true;
        _statusPage = new StatusPage(_repoData, _repo);
        _addAllCommand = new AddAllCommand(_repo);
        _commitPage = new CommitPage(_repo);
        _unstageAllCommand = new(_repo);
        _pullCommand = new PullCommand(_repo);
        //_pushCommand = null;
        _branchListPage = new BranchListPage(_repo);

        _statusItem = new(_statusPage) { Title = "Status" };
        _addAllItem = new(_addAllCommand);
        _commitItem = new(_commitPage);
        _unstageAllItem = new(_unstageAllCommand);
        _pullItem = new(_pullCommand);
        //_pushItem = new(_pushCommand);
        _branchListItem = new(_branchListPage) { Title = "Checkout...", Subtitle = "Switch branches" };

        _statusPage.StatusChanged += (s, e) => OnRepoChanged();
        _statusPage.FileSystemChanged += (s, e) => IsLoading = true;

    }

    internal static Branch? CurrentBranch(Repository repo)
    {
        foreach (var b in repo.Branches.Where(b => !b.IsRemote))
        {
            if (b.IsCurrentRepositoryHead)
            {
                return b;
            }
        }
        return null;
    }

    internal static string BranchString(Repository repo) =>
        repo.Head.FriendlyName;
    //CurrentBranch(repo) is Branch currentBranch ?
    //$"on {currentBranch.FriendlyName}" :
    //$"DETACHED at {repo.Head.Tip.Sha[..8]}";

    internal static string RepoStatusDisplayString(RepositoryStatus status)
    {
        return string.Format(CultureInfo.InvariantCulture,
                                "+{0} ~{1} -{2} | +{3} ~{4} -{5} | i{6}",
                                status.Added.Count(),
                                status.Staged.Count(),
                                status.Removed.Count(),
                                status.Untracked.Count(),
                                status.Modified.Count(),
                                status.Missing.Count(),
                                status.Ignored.Count());

    }

    public override IListItem[] GetItems()
    {
        if (_repo == null)
        {
            return [];
        }

        IsLoading = true;
        var status = _repo.RetrieveStatus();

        // Get the HEAD commit's tree to compare against the working directory.
        //var headTree = _repo.Head.Tip.Tree;

        var items = status
            .Where(entry => entry.State is not FileStatus.Unaltered and not FileStatus.Ignored)
            //.OrderBy(entry => entry.State)
            //.Select(file => FileTolistItem(file, patch))
            .Where(item => item != null)
            .Select(i => i!);

        List<ListItem> allCommands = [];

        _statusItem.Subtitle = $"{StagedSubtitle(status)} {BranchString(_repo)}";
        _statusItem.Tags = StatusTags(status).ToArray();
        allCommands.Add(_statusItem);

        var unstagedChanges = status.Where(IsUnstagedChange);
        var stagedChanges = status.Where(IsStagedChange);

        if (unstagedChanges.Any())
        {
            allCommands.Add(_addAllItem);
        }
        if (stagedChanges.Any())
        {
            allCommands.Add(_commitItem);
            allCommands.Add(_unstageAllItem);
        }


        if (_repo.Head is Branch currentBranch)
        {
            // Retrieve tracking details
            var trackingDetails = currentBranch.TrackingDetails;

            // trackingDetails will be 0,0 even if there's no upstream
            // 'BehindBy' indicates commits present in upstream (to pull)
            // 'AheadBy' indicates commits present locally (to push)
            var commitsToPull = trackingDetails.BehindBy ?? 0;
            var commitsToPush = trackingDetails.AheadBy ?? 0;

            if (commitsToPull > 0)
            {
                _pullItem.Subtitle = $"{commitsToPull} behind";
                allCommands.Add(_pullItem);
            }
            if (currentBranch.TrackedBranch == null || commitsToPush > 0)
            {
                var pushCommand = PushCommand.CreatePushForCurrentBrach(_repo);
                if (pushCommand != null)
                {
                    allCommands.Add(new(pushCommand) { Subtitle = commitsToPush > 0 ? $"{commitsToPush} ahead" : string.Empty });
                }
            }
        }

        allCommands.Add(_branchListItem);

        //allCommands.AddRange((IEnumerable<ListItem>)items);

        IsLoading = false;
        return allCommands.ToArray();
    }

    internal static string StagedSubtitle(RepositoryStatus status)
    {
        return string.Format(CultureInfo.InvariantCulture,
                                "Added {0}, Modified {1}, Removed {2} ",
                                status.Added.Count(),
                                status.Staged.Count(),
                                status.Removed.Count());

    }
    internal static List<Microsoft.CommandPalette.Extensions.Toolkit.Tag> StatusTags(RepositoryStatus status)
    {
        var tags = new List<Microsoft.CommandPalette.Extensions.Toolkit.Tag>();

        var added = status.Added.Count();
        var staged = status.Staged.Count();
        var removed = status.Removed.Count();
        var untracked = status.Untracked.Count();
        var modified = status.Modified.Count();
        var missing = status.Missing.Count();

        var addedTag = new Microsoft.CommandPalette.Extensions.Toolkit.Tag($"+{added}") { ToolTip = "Added", Background = Statics.StateColors[FileStatus.NewInIndex] };
        var stagedTag = new Microsoft.CommandPalette.Extensions.Toolkit.Tag($"~{staged}") { ToolTip = "Staged", Background = Statics.StateColors[FileStatus.ModifiedInIndex] };
        var removedTag = new Microsoft.CommandPalette.Extensions.Toolkit.Tag($"-{removed}") { ToolTip = "Removed", Background = Statics.StateColors[FileStatus.DeletedFromIndex] };
        var untrackedTag = new Microsoft.CommandPalette.Extensions.Toolkit.Tag($"+{untracked}") { ToolTip = "Untracked", Foreground = Statics.StateColors[FileStatus.NewInWorkdir] };
        var modifiedTag = new Microsoft.CommandPalette.Extensions.Toolkit.Tag($"~{modified}") { ToolTip = "Modified", Foreground = Statics.StateColors[FileStatus.ModifiedInWorkdir] };
        var missingTag = new Microsoft.CommandPalette.Extensions.Toolkit.Tag($"-{missing}") { ToolTip = "Missing", Foreground = Statics.StateColors[FileStatus.DeletedFromWorkdir] };

        if (added > 0)
        {
            tags.Add(addedTag);
        }
        if (staged > 0)
        {
            tags.Add(stagedTag);
        }
        if (removed > 0)
        {
            tags.Add(removedTag);
        }
        if (untracked > 0)
        {
            tags.Add(untrackedTag);
        }
        if (modified > 0)
        {
            tags.Add(modifiedTag);
        }
        if (missing > 0)
        {
            tags.Add(missingTag);
        }
        return tags;
    }

    private static bool IsUnstagedChange(StatusEntry file)
    {
        var state = file.State;
        return
            state.HasFlag(FileStatus.NewInWorkdir) ||
            state.HasFlag(FileStatus.ModifiedInWorkdir) ||
            state.HasFlag(FileStatus.DeletedFromWorkdir) ||
            state.HasFlag(FileStatus.TypeChangeInWorkdir) ||
            state.HasFlag(FileStatus.RenamedInWorkdir);
    }
    private static bool IsStagedChange(StatusEntry file)
    {
        var state = file.State;
        return
            state.HasFlag(FileStatus.NewInIndex) ||
            state.HasFlag(FileStatus.ModifiedInIndex) ||
            state.HasFlag(FileStatus.DeletedFromIndex) ||
            state.HasFlag(FileStatus.RenamedInIndex) ||
            state.HasFlag(FileStatus.TypeChangeInIndex);
    }

    // TODO I'm sure this isn't right
    public void Dispose() =>
        _repo?.Dispose();

    private void OnRepoChanged()
    {
        IsLoading = true;
        Title = $"{_repoData.Name} {(_repo != null ? BranchString(_repo) : string.Empty)}";
        RaiseItemsChanged(-1);
    }
}

