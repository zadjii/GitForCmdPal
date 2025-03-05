// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

public partial class LazyDetails : BaseObservable, IDetails
{
    public Lazy<string>? Title { get; set; }
    public Lazy<string>? Body { get; set; }
    public Lazy<IIconInfo>? HeroImage { get; set; }
    public Lazy<IDetailsElement[]>? Metadata { get; set; }

    private readonly string _defaultTitle;
    private readonly string _defaultBody;
    private readonly IIconInfo _defaultHeroImage;
    private readonly IDetailsElement[] _defaultMetadata;

    public LazyDetails(string title = "",
                       string body = "",
                       IIconInfo? heroImage = null,
                       IDetailsElement[]? metadata = null)
    {
        _defaultTitle = title;
        _defaultBody = body;
        _defaultHeroImage = heroImage ?? new IconInfo("");
        _defaultMetadata = metadata ?? [];
    }

    string IDetails.Title => Title != null ? Title.Value : _defaultTitle;
    string IDetails.Body => Body != null ? Body.Value : _defaultBody;
    IIconInfo IDetails.HeroImage => HeroImage != null ? HeroImage.Value : _defaultHeroImage;
    IDetailsElement[] IDetails.Metadata => Metadata != null ? Metadata.Value : _defaultMetadata;

}