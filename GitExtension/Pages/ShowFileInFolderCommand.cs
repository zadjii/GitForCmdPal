// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

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
