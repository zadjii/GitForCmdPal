// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Storage.Streams;

namespace GitExtension;

internal sealed partial class GitExtensionPage : ListPage, System.IDisposable
{
    private readonly RepoData _repoData;
    private readonly Repository _repo;
    private readonly FileSystemWatcher _watcher;

    public GitExtensionPage(RepoData repo)
    {
        _repoData = repo;

        Icon = Icons.AppIcon;
        Name = "Open";

        var repoPath = _repoData.Path;

        _repo = new(repoPath);

        Title = $"{repo.Name} {BranchString(_repo)}";

        _watcher = new FileSystemWatcher
        {
            Path = repoPath,
            Filter = "*.*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        _watcher.Changed += OnRepoChanged;
        _watcher.Created += OnRepoChanged;
        _watcher.Deleted += OnRepoChanged;
        _watcher.Renamed += OnRepoChanged;

        _watcher.EnableRaisingEvents = true;
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
        CurrentBranch(repo) is Branch currentBranch ?
        $"on {currentBranch.FriendlyName}" :
        $"DETACHED at {repo.Head.Tip.Sha[..8]}";

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
        IsLoading = true;
        var status = _repo.RetrieveStatus();

        var items = status
            .Where(entry => entry.State is not FileStatus.Unaltered and not FileStatus.Ignored)
            .OrderBy(entry => entry.State)
            .Select(FileTolistItem)
            .Where(item => item != null)
            .Select(i => i!);

        List<ListItem> allCommands = [];

        var unstagedChanges = status.Where(IsUnstagedChange);
        var stagedChanges = status.Where(IsStagedChange);

        if (unstagedChanges.Any())
        {
            allCommands.Add(new(new AddAllCommand(_repo)));
        }
        if (stagedChanges.Any())
        {
            allCommands.Add(new(new CommitPage(_repo)) { Subtitle = $"{StagedSubtitle(status)} on {BranchString(_repo)}" });
            allCommands.Add(new(new UnstageAllCommand(_repo)));
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
                allCommands.Add(new(new PullCommand(_repo)) { Subtitle = $"{commitsToPull} behind" });
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

        allCommands.Add(new(new BranchListPage(_repo)) { Title = "Checkout...", Subtitle = "Switch branches" });

        allCommands.AddRange(items);

        IsLoading = false;
        return allCommands.ToArray();
    }

    internal static string StagedSubtitle(RepositoryStatus status)
    {
        return string.Format(CultureInfo.InvariantCulture,
                                "Added {0} Modified {1} Removed {2} ",
                                status.Added.Count(),
                                status.Staged.Count(),
                                status.Removed.Count());

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

    private ListItem? FileTolistItem(StatusEntry file)
    {
        List<Microsoft.CommandPalette.Extensions.Toolkit.Tag> tags = [];
        List<Microsoft.CommandPalette.Extensions.Toolkit.Command> commands = [];

        foreach (var state in GetFlags(file.State))
        {
            try
            {

                var tag = Statics.StateTags.TryGetValue(state, out var value) ? value : Statics.UhOhTag;
                tags.Add(tag);

                Command? command = state switch
                {
                    FileStatus.NewInIndex => new UnstageCommand(_repo, file),
                    FileStatus.ModifiedInIndex => new UnstageCommand(_repo, file),
                    FileStatus.DeletedFromIndex => new UnstageCommand(_repo, file),
                    FileStatus.RenamedInIndex => new UnstageCommand(_repo, file),
                    FileStatus.TypeChangeInIndex => new UnstageCommand(_repo, file),

                    FileStatus.NewInWorkdir => new StageCommand(_repo, file),
                    FileStatus.ModifiedInWorkdir => new StageCommand(_repo, file),
                    FileStatus.DeletedFromWorkdir => new StageCommand(_repo, file),
                    FileStatus.TypeChangeInWorkdir => new StageCommand(_repo, file),
                    FileStatus.RenamedInWorkdir => new StageCommand(_repo, file),

                    _ => null,
                };

                if (command != null)
                {
                    commands.Add(command);
                }
            }
            catch { }

        }

        try
        {

            var state = file.State;

            var title = file.FilePath;

            var subtitle = state switch
            {
                FileStatus.NewInIndex => "Staged",
                FileStatus.ModifiedInIndex => "Staged",
                FileStatus.DeletedFromIndex => "Staged",
                FileStatus.RenamedInIndex => "Staged",
                FileStatus.TypeChangeInIndex => "Staged",

                _ => string.Empty,
            };

            if (file.IndexToWorkDirRenameDetails is RenameDetails rename)
            {
                subtitle += $": {rename.OldFilePath} -> {rename.NewFilePath}";
            }
            else if (file.HeadToIndexRenameDetails is RenameDetails rename2)
            {
                subtitle += $": {rename2.OldFilePath} -> {rename2.NewFilePath}";
            }

            var fullPath = Path.Combine(_repo.Info.WorkingDirectory, file.FilePath);
            fullPath = Path.GetFullPath(fullPath);

            var defaultCommand = commands.FirstOrDefault(new NoOpCommand());
            var remaining = commands.Skip(1);

            var li = new ListItem(defaultCommand)
            {
                Title = title,
                Subtitle = subtitle,
                Tags = tags.ToArray(),
                // Icon = GetFileIcon(file),
                MoreCommands = [
                    ..remaining.Select(c => new CommandContextItem(c)),
                    new CommandContextItem(new OpenUrlCommand(fullPath)) { Title = "Open file" },
                    new CommandContextItem(new CopyTextCommand(fullPath)) { Title = "Copy full path" },
                    new CommandContextItem(new CopyTextCommand(file.FilePath)) { Title = "Copy relative path" },
                    new CommandContextItem(new ShowFileInFolderCommand(fullPath)) { Title = "Open in explorer" },
                ],
            };

            _ = Task.Run(() => { li.Icon = GetFileIcon(file); });
            return li;
        }
        catch (Exception e)
        {
            ExtensionHost.LogMessage(e.ToString());
        }
        return null;
    }

    public void Dispose() => throw new System.NotImplementedException();


    private static IEnumerable<FileStatus> GetFlags(FileStatus input)
    {
        foreach (var value in Enum.GetValues<FileStatus>())
        {
            if (value == FileStatus.Unaltered)
            {
                continue;
            }

            if (input.HasFlag(value))
            {
                yield return value;
            }
        }
    }

    private void OnRepoChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Convert the absolute path to a relative path based on the working directory.
            var relativePath = Path.GetRelativePath(_repo.Info.WorkingDirectory, e.FullPath);

            // Check if the file is ignored.
            if (_repo.Ignore.IsPathIgnored(relativePath))
            {
                // Skip processing if the file is ignored.
                return;
            }
        }
        catch { }

        Title = $"{_repoData.Name} {BranchString(_repo)}";
        RaiseItemsChanged(-1);

    }

    private IconInfo? GetFileIcon(StatusEntry file)
    {

        if (IsStagedChange(file))
        {
            return Icons.StagedFile;
        }

        var path = Path.Combine(_repo.Info.WorkingDirectory, file.FilePath);
        path = Path.GetFullPath(path);
        var t = ThumbnailHelper.GetThumbnail(path);
        t.ConfigureAwait(false);
        var stream = t.Result;
        if (stream != null)
        {
            var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
            var icon = new IconInfo(data, data);
            return icon;
        }
        return null;
    }
}

internal sealed partial class StageCommand : InvokableCommand
{
    private readonly Repository _repo;
    private readonly StatusEntry _file;
    internal StageCommand(Repository repo, StatusEntry file)
    {
        Name = "Stage";
        Icon = Icons.Add;
        _repo = repo;
        _file = file;
    }
    public override ICommandResult Invoke()
    {
        Commands.Stage(_repo, _file.FilePath);

        return CommandResult.KeepOpen();
    }
}

internal sealed partial class UnstageCommand : InvokableCommand
{
    private readonly Repository _repo;
    private readonly StatusEntry _file;
    internal UnstageCommand(Repository repo, StatusEntry file)
    {
        Name = "Unstage";
        _repo = repo;
        _file = file;
    }
    public override ICommandResult Invoke()
    {
        Commands.Unstage(_repo, _file.FilePath);

        return CommandResult.KeepOpen();
    }
}

internal sealed partial class AddAllCommand : InvokableCommand
{
    private readonly Repository _repo;
    internal AddAllCommand(Repository repo)
    {
        Name = "Add all";
        Icon = Icons.AddAll;
        _repo = repo;
    }
    public override ICommandResult Invoke()
    {
        Commands.Stage(_repo, "*");

        return CommandResult.KeepOpen();
    }
}
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

// Stolen from toolkit 0.0.9
public partial class ShowFileInFolderCommand : InvokableCommand
{
    private readonly string _path;
    private static readonly IconInfo Ico = new("\uE838");

    public CommandResult Result { get; set; } = CommandResult.GoHome();

    internal ShowFileInFolderCommand(string path)
    {
        _path = path;
        Name = "Show in folder";
        Icon = Ico;
    }

    public override CommandResult Invoke()
    {
        if (File.Exists(_path))
        {
            try
            {
                var argument = "/select, \"" + _path + "\"";
                Process.Start("explorer.exe", argument);
            }
            catch (System.Exception)
            {
            }
        }

        return Result;
    }
}
