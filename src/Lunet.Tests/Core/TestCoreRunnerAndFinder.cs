// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Tests.Infrastructure;

namespace Lunet.Tests.Core;

public class TestCoreRunnerAndFinder
{
    [Test]
    public async Task TestSiteRunnerReturnsZeroWhenNoCommandRunnerIsRegistered()
    {
        using var context = new SiteTestContext();
        using var runner = new SiteRunner(context.Configuration);

        var exitCode = await runner.RunAsync();

        Assert.AreEqual(0, exitCode);
    }

    [Test]
    public async Task TestSiteRunnerRepeatsWhileCommandReturnsContinue()
    {
        using var context = new SiteTestContext();
        var commandRunner = new ContinueThenExitCommandRunner();
        context.Configuration.CommandRunners.Add(commandRunner);
        using var runner = new SiteRunner(context.Configuration);

        var exitCode = await runner.RunAsync();

        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(2, commandRunner.RunCount);
    }

    [Test]
    public async Task TestSiteRunnerReturnsErrorCodeWhenCommandFails()
    {
        using var context = new SiteTestContext();
        context.Configuration.CommandRunners.Add(new ExitWithErrorCommandRunner());
        using var runner = new SiteRunner(context.Configuration);

        var exitCode = await runner.RunAsync();

        Assert.AreEqual(1, exitCode);
    }

    [Test]
    public void TestSiteRunnerRegistersAndDisposesServices()
    {
        using var context = new SiteTestContext();
        using var runner = new SiteRunner(context.Configuration);
        var service = new TestSiteService();

        runner.RegisterService(service);
        runner.RegisterService(service);

        Assert.AreEqual(1, runner.Services.Count);
        runner.Dispose();
        Assert.IsTrue(service.IsDisposed);
    }

    [Test]
    public void TestPageFinderResolvesUidAndRelativeLinks()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";

        var destinationPage = context.CreateDynamicContentObject("/docs/a/", path: "/docs/a.md");
        destinationPage.ContentType = ContentType.Html;
        destinationPage.Url = "/docs/a/";
        destinationPage.UrlWithoutBasePath = "/docs/a/";
        destinationPage.Uid = "Docs.A";
        destinationPage.Title = "Page A";

        var sourcePage = context.CreateDynamicContentObject("/blog/post/", path: "/blog/post.md");
        sourcePage.ContentType = ContentType.Html;
        sourcePage.Url = "/blog/post/";
        sourcePage.UrlWithoutBasePath = "/blog/post/";

        context.Site.Pages.Add(destinationPage);
        context.Site.Pages.Add(sourcePage);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);

        Assert.IsTrue(context.Site.Content.Finder.TryFindByUid("Docs.A", out var foundByUid));
        Assert.AreSame(destinationPage, foundByUid);
        Assert.AreEqual("https://example.com/docs/a", context.Site.Content.Finder.UrlRef(sourcePage, "../docs/a.md"));
        Assert.AreEqual("/docs/a", context.Site.Content.Finder.UrlRelRef(sourcePage, "../docs/a.md"));
    }

    [Test]
    public void TestPageFinderSupportsExternalUidAndExtraContentMapping()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";

        var page = context.CreateDynamicContentObject("/docs/internal/", path: "/docs/internal.md");
        page.ContentType = ContentType.Html;
        page.Url = "/docs/internal/";
        page.UrlWithoutBasePath = "/docs/internal/";
        page.Uid = "Docs.Internal";
        page.Title = "Internal";
        context.Site.Pages.Add(page);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);

        context.Site.Content.Finder.RegisterExtraContent(new ExtraContent
        {
            Uid = "Alias.Internal",
            DefinitionUid = "Docs.Internal",
            Name = "Alias",
            FullName = "Alias.FullName"
        });

        Assert.IsTrue(context.Site.Content.Finder.TryFindByUid("Alias.Internal", out var found));
        Assert.AreSame(page, found);

        Assert.IsTrue(context.Site.Content.Finder.TryGetExternalUid("System.String", out var name, out var fullName, out var url));
        Assert.AreEqual("System.String", name);
        Assert.AreEqual("System.String", fullName);
        Assert.IsNotNull(url);
        StringAssert.Contains("docs.microsoft.com", url!);
    }

    [Test]
    public void TestPageFinderResolvesUrlEncodedXrefUid()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";

        var genericPage = context.CreateDynamicContentObject("/api/binding/", path: "/api/binding.md");
        genericPage.ContentType = ContentType.Html;
        genericPage.Url = "/api/binding/";
        genericPage.UrlWithoutBasePath = "/api/binding/";
        genericPage.Uid = "XenoAtom.Terminal.UI.Binding`1";
        genericPage.Title = "Binding<T>";

        var sourcePage = context.CreateDynamicContentObject("/docs/readme/", path: "/docs/readme.md");
        sourcePage.ContentType = ContentType.Html;
        sourcePage.Url = "/docs/readme/";
        sourcePage.UrlWithoutBasePath = "/docs/readme/";

        context.Site.Pages.Add(genericPage);
        context.Site.Pages.Add(sourcePage);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);

        Assert.IsTrue(context.Site.Content.Finder.TryFindByUid("XenoAtom.Terminal.UI.Binding%601", out var foundByEncodedUid));
        Assert.AreSame(genericPage, foundByEncodedUid);
        Assert.AreEqual("/api/binding/", context.Site.Content.Finder.UrlRelRef(sourcePage, "xref:XenoAtom.Terminal.UI.Binding%601"));
    }

    [Test]
    public void TestPageFinderKeepsFragmentOnlyLinksRelativeToCurrentPage()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";

        var sourcePage = context.CreateDynamicContentObject("/folder/myfile/", path: "/folder/myfile.md");
        sourcePage.ContentType = ContentType.Html;
        sourcePage.Url = "/folder/myfile/";
        sourcePage.UrlWithoutBasePath = "/folder/myfile/";
        context.Site.Pages.Add(sourcePage);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);

        Assert.AreEqual("#myanchor", context.Site.Content.Finder.UrlRelRef(sourcePage, "#myanchor"));
        Assert.AreEqual("?q=1#myanchor", context.Site.Content.Finder.UrlRelRef(sourcePage, "?q=1#myanchor"));
    }

    [Test]
    public void TestPageFinderResolvesFileAndReadmeLinksWithFragments()
    {
        using var context = new SiteTestContext();
        context.Site.BaseUrl = "https://example.com";

        var sourcePage = context.CreateDynamicContentObject("/folder/myfile/", path: "/folder/myfile.md");
        sourcePage.ContentType = ContentType.Html;
        sourcePage.Url = "/folder/myfile/";
        sourcePage.UrlWithoutBasePath = "/folder/myfile/";

        var siblingPage = context.CreateDynamicContentObject("/folder/afile/", path: "/folder/afile.md");
        siblingPage.ContentType = ContentType.Html;
        siblingPage.Url = "/folder/afile/";
        siblingPage.UrlWithoutBasePath = "/folder/afile/";

        var readmePage = context.CreateDynamicContentObject("/docs/", path: "/docs/readme.md");
        readmePage.ContentType = ContentType.Html;
        readmePage.Url = "/docs/";
        readmePage.UrlWithoutBasePath = "/docs/";

        context.Site.Pages.Add(sourcePage);
        context.Site.Pages.Add(siblingPage);
        context.Site.Pages.Add(readmePage);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);

        Assert.AreEqual("/folder/afile#section", context.Site.Content.Finder.UrlRelRef(sourcePage, "afile.md#section"));
        Assert.AreEqual("/docs#overview", context.Site.Content.Finder.UrlRelRef(sourcePage, "../docs/readme.md#overview"));
    }

    [Test]
    public void TestPageFinderNormalizesEncodedExternalGenericUid()
    {
        using var context = new SiteTestContext();

        Assert.IsTrue(context.Site.Content.Finder.TryGetExternalUid("System.Collections.Generic.List%601", out var name, out var fullName, out var url));
        Assert.AreEqual("System.Collections.Generic.List`1", name);
        Assert.AreEqual("System.Collections.Generic.List`1", fullName);
        Assert.IsNotNull(url);
        StringAssert.Contains("system.collections.generic.list-1", url!.ToLowerInvariant());
    }

    [Test]
    public void TestPageFinderLogsErrorsForDuplicateUid()
    {
        using var context = new SiteTestContext();
        var first = context.CreateDynamicContentObject("/docs/a/", path: "/docs/a.md");
        first.ContentType = ContentType.Html;
        first.Url = "/docs/a/";
        first.UrlWithoutBasePath = "/docs/a/";
        first.Uid = "Duplicate.Uid";

        var second = context.CreateDynamicContentObject("/docs/b/", path: "/docs/b.md");
        second.ContentType = ContentType.Html;
        second.Url = "/docs/b/";
        second.UrlWithoutBasePath = "/docs/b/";
        second.Uid = "Duplicate.Uid";

        context.Site.Pages.Add(first);
        context.Site.Pages.Add(second);
        context.Site.Content.Finder.Process(ProcessingStage.AfterLoadingContent);

        Assert.IsTrue(context.Configuration.LoggerFactory.HasErrors);
    }

    private sealed class ContinueThenExitCommandRunner : ISiteCommandRunner
    {
        public int RunCount { get; private set; }

        public Task<RunnerResult> RunAsync(SiteRunner runner, CancellationToken cancellationToken)
        {
            RunCount++;
            return Task.FromResult(RunCount == 1 ? RunnerResult.Continue : RunnerResult.Exit);
        }
    }

    private sealed class ExitWithErrorCommandRunner : ISiteCommandRunner
    {
        public Task<RunnerResult> RunAsync(SiteRunner runner, CancellationToken cancellationToken)
        {
            return Task.FromResult(RunnerResult.ExitWithError);
        }
    }

    private sealed class TestSiteService : ISiteService
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
