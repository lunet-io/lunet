// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Lunet.Api.DotNet;
using Lunet.Core;

namespace Lunet.Tests.Api.DotNet;

public class TestApiDotNetSlugGenerator
{
    [Test]
    public void TestBuildSlugKeepsShortUidSlug()
    {
        const string uid = "ApiE2E.RecordClass";

        var slug = ApiDotNetSlugGenerator.BuildSlug(uid, ApiDotNetSlugGenerator.DefaultMaxLength);

        Assert.AreEqual(UidHelper.Handleize(uid), slug);
    }

    [Test]
    public void TestBuildSlugShortensLongUidWithStableHash()
    {
        var uid = "ApiE2E." + new string('X', 500);

        var slug1 = ApiDotNetSlugGenerator.BuildSlug(uid, 64);
        var slug2 = ApiDotNetSlugGenerator.BuildSlug(uid, 64);

        Assert.AreEqual(slug1, slug2);
        Assert.LessOrEqual(slug1.Length, 64);
        StringAssert.Contains("-", slug1);
    }

    [Test]
    public void TestBuildSlugAvoidsWindowsReservedNames()
    {
        const string uid = "con";

        var slug = ApiDotNetSlugGenerator.BuildSlug(uid, ApiDotNetSlugGenerator.DefaultMaxLength);

        Assert.IsFalse(string.Equals("con", slug, StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void TestResolverDisambiguatesCollidingSlugs()
    {
        var resolver = new ApiDotNetSlugResolver(ApiDotNetSlugGenerator.DefaultMaxLength);

        var slugA = resolver.GetSlug("ApiE2E.Type+A");
        var slugB = resolver.GetSlug("ApiE2E.Type A");

        Assert.AreNotEqual(slugA, slugB);
        Assert.AreEqual(slugA, resolver.GetSlug("ApiE2E.Type+A"));
        Assert.AreEqual(slugB, resolver.GetSlug("ApiE2E.Type A"));
    }
}
