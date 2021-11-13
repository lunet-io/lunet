// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using DotNet.Globbing;
using Lunet.Core;
using Scriban.Runtime;
using Zio;

namespace Lunet.Attributes;

public class AttributesGlobber : DynamicObject<AttributesObject>
{
    public AttributesGlobber(AttributesObject parent, string pattern, bool match, ScriptObject setters) : base(parent)
    {
        Match = match;
        Pattern = pattern;
        Setters = setters;
    }

    public bool Match
    {
        get => GetSafeValue<bool>("match"); 
        set => this["match"] = value;
    }

    public string Pattern
    {
        get => GetSafeValue<string>("pattern");
        set => this["pattern"] = value;
    }

    public Glob Glob { get; set; }

    public ScriptObject Setters
    {
        get => GetSafeValue<ScriptObject>("setters");
        set => this["setters"] = value;
    }
}

public class AttributesObject : DynamicCollection<AttributesGlobber, AttributesObject>
{
    public AttributesObject()
    {
        this.Import("clear", (Action)Clear);
        this.Import("unmatch", (Func<string, ScriptObject, AttributesGlobber>)UnMatch);
        this.Import("match", (Func<string, ScriptObject, AttributesGlobber>)Match);

        // Add a default match for blog posts files
        Match("/**/[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]*.*", new ScriptObject()
        {
            {"url", "/:section/:year/:month/:day/:slug:output_ext"}
        });
    }

    public AttributesGlobber Match(string pattern, ScriptObject setters)
    {
        return Match(pattern, true, setters);
    }

    public AttributesGlobber UnMatch(string pattern, ScriptObject setters)
    {
        return Match(pattern, false, setters);
    }

    private AttributesGlobber Match(string pattern, bool match, ScriptObject setters)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        if (setters == null) throw new ArgumentNullException(nameof(setters));

        Glob glob;
        try
        {
            glob = Glob.Parse(pattern);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid pattern. {ex.Message}");

        }

        var globber = new AttributesGlobber(this, pattern, match, setters)
        {
            Glob = glob
        };

        Add(globber);
        return globber;
    }
        
    internal void ProcessAttributesForPath(UPath path, ref ScriptObject obj)
    {
        foreach (var globber in this)
        {
            if (globber.Match == globber.Glob.IsMatch(path.FullName))
            {
                obj ??= new ScriptObject();
                globber.Setters?.CopyTo(obj);
            }
        }
    }
}