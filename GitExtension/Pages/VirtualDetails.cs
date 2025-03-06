// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

public partial class VirtualDetails : BaseObservable, IDetails
{
    public virtual IIconInfo HeroImage
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(nameof(HeroImage));
        }
    }

    public virtual string Title
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    public virtual string Body
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Body));
        }
    }

    public virtual IDetailsElement[] Metadata
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Metadata));
        }
    }

    public VirtualDetails(string title = "",
                          string body = "",
                          IIconInfo? heroImage = null,
                          IDetailsElement[]? metadata = null)
    {
        Title = title;
        Body = body;
        HeroImage = heroImage ?? new IconInfo("");
        Metadata = metadata ?? [];
    }
}
