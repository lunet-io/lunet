// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Core;

public abstract class HtmlElement : DynamicObject<ScriptObject>
{
    protected HtmlElement(ScriptObject parent) : base(parent)
    {
    }
}

public class HtmlPage : HtmlElement
{
    public HtmlPage(ScriptObject parent) : base(parent)
    {
        parent.SetValue(SiteVariables.Html, this, true);
        Head = new HtmlHead(this);
        SetValue("head", Head, true);
    }

    public HtmlHead Head { get; }
}

public class HtmlHead : HtmlElement
{
    public HtmlHead(ScriptObject parent) : base(parent)
    {
        Metas = new ScriptCollection();
        Includes = new ScriptCollection();

        SetValue("metas", Metas, true);
        SetValue("includes", Includes, true);

        Metas.Add(@"<meta charset=""utf-8"">");
        Metas.Add(@"<meta name=""viewport"" content=""width=device-width, initial-scale=1"">");
        Metas.Add(@"<meta name=""generator"" content=""lunet {{lunet.version}}"">");
    }

    public ScriptCollection Metas { get; }

    public object Title
    {
        get => GetSafeValue<object>("title");
        set => SetValue("title", value);
    }

    public ScriptCollection Includes { get; }
}