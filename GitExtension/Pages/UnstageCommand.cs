// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

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
