// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Reflection;
using Lunet.Core;
using Lunet.Helpers;
using Scriban.Runtime;
using Zio;

namespace Lunet.Tests.Core;

public class TestCoreUtilities
{
    [Test]
    public void TestPathUtilNormalizeHelpers()
    {
        Assert.AreEqual(".md", PathUtil.NormalizeExtension("md"));
        Assert.AreEqual(".md", PathUtil.NormalizeExtension(".md"));
        Assert.AreEqual("/docs/posts/", PathUtil.NormalizeUrl("docs\\posts", true));
        Assert.AreEqual("/docs/posts", PathUtil.NormalizeUrl("docs\\posts", false));
        Assert.AreEqual("docs/posts/", PathUtil.NormalizeRelativePath("\\docs\\posts", true));
        Assert.AreEqual("docs/posts", PathUtil.NormalizeRelativePath("/docs/posts", false));
    }

    [Test]
    public void TestGlobCollectionMatchesAndRootBehavior()
    {
        var globs = new GlobCollection();
        globs.Add("/docs/**/*.md");

        Assert.IsFalse(globs.IsMatch(UPath.Root));
        Assert.IsTrue(globs.IsMatch("/docs/getting-started/index.md"));
        Assert.IsFalse(globs.IsMatch("/docs/getting-started/index.txt"));
        Assert.Throws<ArgumentNullException>(() => globs.Add(null!));
    }

    [Test]
    public void TestPathCollectionValidationAndArrayExpansion()
    {
        var collection = new PathCollection();
        InvokeAddItem(collection, "/docs");
        InvokeAddItem(collection, new ScriptArray { "/blog", "/about" });

        Assert.AreEqual(3, collection.Count);

        var invalidPathEx = Assert.Throws<TargetInvocationException>(() => InvokeAddItem(collection, "relative/path"));
        Assert.IsNotNull(invalidPathEx);
        Assert.IsNotNull(invalidPathEx!.InnerException);
        StringAssert.Contains("absolute path", invalidPathEx.InnerException!.Message);

        var invalidTypeEx = Assert.Throws<TargetInvocationException>(() => InvokeAddItem(collection, 42));
        Assert.IsNotNull(invalidTypeEx);
        Assert.IsNotNull(invalidTypeEx!.InnerException);
        StringAssert.Contains("Expecting a string", invalidTypeEx.InnerException!.Message);
    }

    [Test]
    public void TestDynamicObjectCopyToWithReadOnly()
    {
        var from = new ScriptObject();
        from.SetValue("readonly", 1, true);
        from.SetValue("writable", 2, false);

        var to = new ScriptObject();
        from.CopyToWithReadOnly(to);

        Assert.AreEqual(1, to["readonly"]);
        Assert.AreEqual(2, to["writable"]);
        Assert.IsFalse(to.CanWrite("readonly"));
        Assert.IsTrue(to.CanWrite("writable"));
    }

    [Test]
    public void TestOrderedListInsertFindAndReplaceOperations()
    {
        var list = new OrderedList<IOrderedItem> { new OrderedItemA(), new OrderedItemB(), new OrderedItemC() };

        Assert.IsTrue(list.InsertBefore<OrderedItemB>(new OrderedItemD()));
        Assert.IsTrue(list.InsertAfter<OrderedItemA>(new OrderedItemE()));
        Assert.IsTrue(list.Contains<OrderedItemB>());
        Assert.NotNull(list.Find<OrderedItemB>());
        Assert.NotNull(list.FindExact<OrderedItemB>());
        Assert.IsTrue(list.ReplacyBy<OrderedItemC>(new OrderedItemF()));

        Assert.IsInstanceOf<OrderedItemA>(list[0]);
        Assert.IsInstanceOf<OrderedItemE>(list[1]);
        Assert.IsInstanceOf<OrderedItemD>(list[2]);
        Assert.IsInstanceOf<OrderedItemB>(list[3]);
        Assert.IsInstanceOf<OrderedItemF>(list[4]);
    }

    [Test]
    public void TestOrderedListFindExactDiffersFromFind()
    {
        var list = new OrderedList<IOrderedItem> { new OrderedItemBDerived() };

        Assert.NotNull(list.Find<OrderedItemB>());
        Assert.Null(list.FindExact<OrderedItemB>());
    }

    [Test]
    public void TestContentLayoutTypesAndListDetection()
    {
        var layoutTypes = new ContentLayoutTypes();
        layoutTypes.AddListType("archives");

        Assert.AreEqual(ContentLayoutTypes.SingleWeight, layoutTypes.GetSafeValue<int>(ContentLayoutTypes.Single));
        Assert.AreEqual(ContentLayoutTypes.ListWeight, layoutTypes.GetSafeValue<int>(ContentLayoutTypes.List));
        Assert.AreEqual(ContentLayoutTypes.ListWeight, layoutTypes.GetSafeValue<int>("archives"));
        Assert.IsTrue(ContentPlugin.IsListLayout("posts"));
        Assert.IsTrue(ContentPlugin.IsListLayout("mylist"));
        Assert.IsFalse(ContentPlugin.IsListLayout("post"));
    }

    private static void InvokeAddItem(ScriptCollection collection, object item)
    {
        var method = typeof(ScriptCollection).GetMethod("AddItem", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null)
        {
            throw new InvalidOperationException("Unable to access ScriptCollection.AddItem");
        }

        method.Invoke(collection, [item]);
    }

    private interface IOrderedItem
    {
    }

    private sealed class OrderedItemA : IOrderedItem
    {
    }

    private class OrderedItemB : IOrderedItem
    {
    }

    private sealed class OrderedItemBDerived : OrderedItemB
    {
    }

    private sealed class OrderedItemC : IOrderedItem
    {
    }

    private sealed class OrderedItemD : IOrderedItem
    {
    }

    private sealed class OrderedItemE : IOrderedItem
    {
    }

    private sealed class OrderedItemF : IOrderedItem
    {
    }
}
