// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Lunet.Core;
using Lunet.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Lunet.Tests.Api.DotNet;

[NonParallelizable]
public class TestApiDotNetEndToEndModernCSharp
{
    private static readonly (string Type, string UidContains, string SyntaxFragment)[] ExpectedFeatureSyntax =
    [
        ("Class", "ApiE2E.RecordClass", "record RecordClass(int Value)"),
        ("Struct", "ReadonlyRecordStruct", "readonly record struct ReadonlyRecordStruct(int Value)"),
        ("Property", "RequiredAndInit.Name", "required string Name { get; init; }"),
        ("Method", "RefFeatures.RefReadonlyReturn", "ref readonly int RefReadonlyReturn(ref readonly int value)"),
        ("Method", "RefFeatures.ScopedIn", "ScopedIn(scoped in int value)"),
        ("Method", "NullableApi`1.Echo", "TRef? Echo(TRef? value)"),
        ("Class", "PrimaryCtorClass", "class PrimaryCtorClass(string name)"),
        ("Struct", "PrimaryCtorStruct", "readonly struct PrimaryCtorStruct(int value)"),
        ("Field", "NativeAndFunctionPointers.NativeIntField", "nint NativeIntField"),
        ("Field", "NativeAndFunctionPointers.Callback", "delegate* unmanaged[Cdecl]<int, void> Callback"),
        ("Method", "IStaticMath`1.Create", "static abstract TSelf Create()"),
        ("Method", "IDefaultInterfaceMember.Implemented", "void Implemented()"),
        ("Operator", "op_CheckedAddition", "operator checked +"),
        ("Operator", "op_UnsignedRightShift", "operator >>>"),
        ("Interface", "IAllowsRefStruct", "allows ref struct"),
        ("Method", "ParamsCollectionFeature.Accept", "params ReadOnlySpan<int> values"),
    ];

    private PhysicalLunetAppTestContext _context = null!;
    private JsonDocument _intermediateModel = null!;
    private Dictionary<string, JsonElement> _itemsByUid = null!;
    private string _outputRoot = null!;

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        _context = new PhysicalLunetAppTestContext();
        WriteTestSiteAndProject(_context);

        var siteDirectory = _context.GetAbsolutePath("site");
        var exitCode = await _context.RunAsync($"--input-dir={siteDirectory}", "build", "--dev");
        Assert.AreEqual(0, exitCode, "lunet build should succeed for API integration site.");

        var cachePath = _context.GetAbsolutePath("site/.lunet/build/cache/api/dotnet/ApiE2ESample.api.json");
        Assert.IsTrue(File.Exists(cachePath), $"Expected API cache file not found at `{cachePath}`.");
        _intermediateModel = JsonDocument.Parse(File.ReadAllText(cachePath));
        _itemsByUid = BuildItemIndex(_intermediateModel);
        _outputRoot = _context.GetAbsolutePath("site/.lunet/build/www");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _intermediateModel?.Dispose();
        _context?.Dispose();
    }

    [Test]
    public void TestBuildGeneratesApiRootNamespaceAndMemberPages()
    {
        Assert.IsTrue(File.Exists(Path.Combine(_outputRoot, "api", "index.html")));
        Assert.IsTrue(File.Exists(Path.Combine(_outputRoot, "api", UidHelper.Handleize("ApiE2E"), "index.html")));
        Assert.IsTrue(File.Exists(Path.Combine(_outputRoot, "api", UidHelper.Handleize("ApiE2E.RecordClass"), "index.html")));
    }

    [Test]
    public void TestIntermediateModelContainsModernLanguageFeatureSyntax()
    {
        foreach (var (type, uidContains, syntaxFragment) in ExpectedFeatureSyntax)
        {
            var item = FindSingleItem(type, uidContains);
            var syntax = GetSyntaxContent(item);
            StringAssert.Contains(syntaxFragment, syntax, $"Expected `{syntaxFragment}` in intermediate syntax for `{uidContains}`.");
        }
    }

    [Test]
    public void TestRenderedPagesContainModernLanguageFeatureSyntax()
    {
        foreach (var (type, uidContains, syntaxFragment) in ExpectedFeatureSyntax)
        {
            var item = FindSingleItem(type, uidContains);
            var uid = item.GetProperty("uid").GetString();
            Assert.That(uid, Is.Not.Null.And.Not.Empty);
            var html = ReadRenderedHtmlByUid(uid!);
            StringAssert.Contains(syntaxFragment, html, $"Expected `{syntaxFragment}` in rendered page for `{uidContains}`.");
        }
    }

    [Test]
    public void TestRenderedPagesContainExtensionAndExplicitInterfaceSections()
    {
        var recordClassPage = ReadRenderedHtmlByUid("ApiE2E.RecordClass");
        StringAssert.Contains("Extensions", recordClassPage);
        StringAssert.Contains("DoubleValue(RecordClass)", recordClassPage);

        var explicitImplementerPage = ReadRenderedHtmlByUid("ApiE2E.ExplicitImplementer");
        StringAssert.Contains("Explicit Interface Implementation Methods", explicitImplementerPage);
        StringAssert.Contains("Execute()", explicitImplementerPage);
    }

    [Test]
    public void TestCSharp14ExtensionMembersDoNotBreakModelOrPages()
    {
        Assert.IsFalse(_itemsByUid.Values.Any(item => item.GetProperty("type").GetString() == "Default"));

        var extensionMembersType = FindSingleItem("Class", "ApiE2E.CSharp14ExtensionMembers");
        var syntax = GetSyntaxContent(extensionMembersType);
        StringAssert.Contains("static class CSharp14ExtensionMembers", syntax);

        var extensionMembersPage = ReadRenderedHtmlByUid("ApiE2E.CSharp14ExtensionMembers");
        StringAssert.Contains("CSharp14ExtensionMembers Class", extensionMembersPage);
    }

    [Test]
    public void TestApiIndexAndNamespacePagesContainExpectedStructure()
    {
        var apiIndexPath = Path.Combine(_outputRoot, "api", "index.html");
        var apiIndex = WebUtility.HtmlDecode(File.ReadAllText(apiIndexPath));
        StringAssert.Contains("Namespaces", apiIndex);
        StringAssert.Contains("ApiE2E", apiIndex);

        var namespacePage = ReadRenderedHtmlByUid("ApiE2E");
        StringAssert.Contains("ApiE2E Namespace", namespacePage);
        StringAssert.Contains("Classes", namespacePage);
        StringAssert.Contains("Structs", namespacePage);
        StringAssert.Contains("Interfaces", namespacePage);
        StringAssert.Contains("CSharp14ExtensionMembers", namespacePage);
    }

    [Test]
    public void TestConfiguredReferencedAssemblyGeneratesExternalApiPages()
    {
        const string uid = "NuGet.Versioning.NuGetVersion";

        Assert.IsTrue(_itemsByUid.TryGetValue(uid, out var nuGetVersionItem), $"Expected referenced assembly uid `{uid}` in API model.");
        Assert.AreEqual("Class", nuGetVersionItem.GetProperty("type").GetString());

        var syntax = GetSyntaxContent(nuGetVersionItem);
        StringAssert.Contains("class NuGetVersion", syntax);

        if (nuGetVersionItem.TryGetProperty("summary", out var summaryProperty))
        {
            var summary = summaryProperty.GetString();
            Assert.That(summary, Is.Not.Null.And.Not.Empty, "Expected XML documentation summary for referenced package type.");
        }

        var page = ReadRenderedHtmlByUid(uid);
        StringAssert.Contains("NuGetVersion Class", page);
    }

    [Test]
    public void TestGeneratedApiPagesExposeMenuHierarchy()
    {
        var apiRootPage = ReadRenderedApiRootHtml();
        StringAssert.Contains("API Reference", apiRootPage);
        StringAssert.Contains("ApiE2E", apiRootPage);
        StringAssert.Contains("RecordClass", apiRootPage);

        var recordClassPage = ReadRenderedHtmlByUid("ApiE2E.RecordClass");
        StringAssert.Contains("breadcrumb", recordClassPage);
        StringAssert.Contains("API Reference", recordClassPage);
        StringAssert.Contains("ApiE2E", recordClassPage);

        var namespacePage = ReadRenderedHtmlByUid("ApiE2E");
        StringAssert.Contains("bi-diagram-3", namespacePage);

        var refFeaturesPage = ReadRenderedHtmlByUid("ApiE2E.RefFeatures");
        StringAssert.Contains("Methods", refFeaturesPage);
        StringAssert.Contains("RefReadonlyReturn", refFeaturesPage);
        StringAssert.Contains("bi-gear", refFeaturesPage);
    }

    [Test]
    public void TestTypeMemberGroupMenuLinksTargetMemberSections()
    {
        var typeUid = FindTypeUidWithMemberKinds("Constructor", "Property");
        var typePage = ReadRenderedHtmlByUid(typeUid);
        var typeUrl = $"/api/{UidHelper.Handleize(typeUid)}/";

        StringAssert.Contains($"href='{typeUrl}#constructors'", typePage);
        StringAssert.Contains($"href='{typeUrl}#properties'", typePage);
    }

    [Test]
    public void TestApiTablesRenderAsHtmlWithoutEscapedTableCells()
    {
        var namespacePage = ReadRenderedHtmlByUid("ApiE2E");
        StringAssert.DoesNotContain("&lt;td&gt;", namespacePage);
        StringAssert.DoesNotContain("<pre><code>    &lt;td&gt;", namespacePage);
        StringAssert.Contains("table table-striped table-hover table-sm api-dotnet-members-table", namespacePage);
    }

    [Test]
    public void TestSearchIndexesGeneratedApiPages()
    {
        var searchDbPath = Path.Combine(_outputRoot, "js", "lunet-search.sqlite");
        Assert.IsTrue(File.Exists(searchDbPath), $"Expected search database not found at `{searchDbPath}`.");

        Assert.Greater(
            ExecuteScalar(searchDbPath, "SELECT COUNT(*) FROM pages WHERE url LIKE '/api/%';"),
            0,
            "Expected generated API pages to be indexed in search database.");

        var recordClassUrl = $"/api/{UidHelper.Handleize("ApiE2E.RecordClass")}/";
        Assert.AreEqual(
            1,
            ExecuteScalar(searchDbPath, "SELECT COUNT(*) FROM pages WHERE url = $url;", ("$url", recordClassUrl)),
            $"Expected API member url `{recordClassUrl}` to be indexed in search database.");
    }

    [Test]
    public void TestNamespaceMarkdownDocumentationIsMerged()
    {
        Assert.IsTrue(_itemsByUid.TryGetValue("ApiE2E", out var namespaceItem), "Expected namespace uid `ApiE2E` in API model.");
        var summary = namespaceItem.GetProperty("summary").GetString() ?? string.Empty;
        var remarks = namespaceItem.GetProperty("remarks").GetString() ?? string.Empty;

        StringAssert.Contains("Extra ApiE2E namespace summary from markdown.", summary);
        StringAssert.Contains("Extra ApiE2E namespace remarks from markdown.", remarks);

        var namespacePage = ReadRenderedHtmlByUid("ApiE2E");
        StringAssert.Contains("Extra ApiE2E namespace summary from markdown.", namespacePage);
        StringAssert.Contains("Extra ApiE2E namespace remarks from markdown.", namespacePage);
    }

    private static void WriteTestSiteAndProject(PhysicalLunetAppTestContext context)
    {
        context.WriteAllText(
            "site/config.scriban",
            """
            baseurl = "https://example.test"
            title = "API End-to-End"

            # The end-to-end API tests assert on the rendered menu HTML.
            # Disable async menu partials so the sidebar is inlined and testable.
            with menu
              async_load_threshold = 0
            end

            with search
              enable = true
              engine = "sqlite"
            end

            with api.dotnet
              title = "API End-to-End"
              path = "/api"
              properties = { TargetFramework: "net10.0" }
              projects = [
                {
                  name: "ApiE2ESample",
                  path: "../src/ApiE2ESample/ApiE2ESample.csproj",
                  references: ["NuGet.Versioning"]
                }
              ]
            end
            """);

        context.WriteAllText(
            "site/readme.md",
            """
            ---
            title: Home
            ---

            # Home
            """);

        context.WriteAllText(
            "site/menu.yml",
            """
            home:
              - { path: readme.md, title: Home }
              - { path: api/readme.md, title: API Reference, folder: true }
            """);

        context.WriteAllText(
            "site/.lunet/layouts/_default.sbn-html",
            """
            <div class="test-layout">
            {{ if page.menu_item != null; page.menu_item.breadcrumb; end }}
            {{ if page.menu != null; page.menu.render { kind: "menu", depth: 6 }; end }}
            {{ content }}
            </div>
            """);

        context.WriteAllText(
            "src/ApiE2ESample/ApiE2ESample.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>preview</LangVersion>
                <Nullable>enable</Nullable>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="NuGet.Versioning" Version="7.3.0" />
              </ItemGroup>
            </Project>
            """);

        context.WriteAllText(
            "src/ApiE2ESample/ModernFeatures.cs",
            """
            #nullable enable
            using System;

            namespace ApiE2E;

            /// <summary>A record class.</summary>
            public record class RecordClass(int Value);

            /// <summary>A readonly record struct.</summary>
            public readonly record struct ReadonlyRecordStruct(int Value);

            public ref struct RefReader
            {
                public readonly int ReadonlyMethod() => 42;
            }

            public class NullableApi<TRef>
                where TRef : class?
            {
                public string? MaybeText { get; set; }

                public TRef? Echo(TRef? value) => value;
            }

            public class RequiredAndInit
            {
                public required string Name { get; init; } = string.Empty;
                public required int Code;
            }

            public class RefFeatures
            {
                public ref readonly int RefReadonlyReturn(ref readonly int value) => ref value;

                public void ScopedIn(scoped in int value)
                {
                }
            }

            public class PrimaryCtorClass(string name)
            {
                public string Name => name;
            }

            public readonly struct PrimaryCtorStruct(int value)
            {
                public int Value => value;
            }

            public unsafe class NativeAndFunctionPointers
            {
                public nint NativeIntField;
                public nuint NativeUIntProperty { get; set; }
                public delegate* unmanaged[Cdecl]<int, void> Callback;
            }

            public interface IStaticMath<TSelf>
                where TSelf : IStaticMath<TSelf>
            {
                static abstract TSelf operator +(TSelf left, TSelf right);
                static abstract TSelf Create();
                static abstract TSelf Zero { get; }
            }

            public interface IConstraint<TUnmanaged, TNotNull>
                where TUnmanaged : unmanaged
                where TNotNull : notnull
            {
            }

            public interface IDefaultInterfaceMember
            {
                void Implemented() { }
            }

            public interface IRefStructContract
            {
                void Use();
            }

            public ref struct RefStructImplementer : IRefStructContract
            {
                public void Use()
                {
                }
            }

            public class ParamsCollectionFeature
            {
                public void Accept(params ReadOnlySpan<int> values)
                {
                }
            }

            public readonly struct CheckedOperators
            {
                public static CheckedOperators operator +(CheckedOperators left, CheckedOperators right) => left;
                public static CheckedOperators operator checked +(CheckedOperators left, CheckedOperators right) => left;
                public static CheckedOperators operator >>>(CheckedOperators value, int count) => value;
            }

            public interface IAllowsRefStruct<TValue>
                where TValue : allows ref struct
            {
            }

            public interface IExplicitContract
            {
                void Execute();
            }

            public class ExplicitImplementer : IExplicitContract
            {
                void IExplicitContract.Execute()
                {
                }
            }

            public static class RecordClassExtensions
            {
                public static int DoubleValue(this RecordClass value) => value.Value * 2;
            }

            public static class CSharp14ExtensionMembers
            {
                extension(RecordClass value)
                {
                    public int TripleValue() => value.Value * 3;
                }
            }
            """);

        context.WriteAllText(
            "src/ApiE2ESample/apidocs/ApiE2E.md",
            """
            ---
            uid: ApiE2E
            ---

            # Summary
            Extra ApiE2E namespace summary from markdown.

            # Remarks
            Extra ApiE2E namespace remarks from markdown.
            """);
    }

    private static Dictionary<string, JsonElement> BuildItemIndex(JsonDocument apiModel)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var module in apiModel.RootElement.GetProperty("items").EnumerateArray())
        {
            if (!module.TryGetProperty("items", out var items))
            {
                continue;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("uid", out var uidProperty))
                {
                    continue;
                }

                var uid = uidProperty.GetString();
                if (string.IsNullOrEmpty(uid))
                {
                    continue;
                }

                map[uid] = item;
            }
        }

        return map;
    }

    private JsonElement FindSingleItem(string type, string uidContains)
    {
        var matches = _itemsByUid
            .Values
            .Where(item => item.GetProperty("type").GetString() == type
                           && item.GetProperty("uid").GetString()?.Contains(uidContains, StringComparison.Ordinal) == true)
            .ToList();

        var exactMatches = matches
            .Where(item => string.Equals(item.GetProperty("uid").GetString(), uidContains, StringComparison.Ordinal))
            .ToList();
        if (exactMatches.Count > 0)
        {
            matches = exactMatches;
        }

        Assert.AreEqual(1, matches.Count, $"Expected a single item of type `{type}` containing `{uidContains}`, but found {matches.Count}.");
        return matches[0];
    }

    private static string GetSyntaxContent(JsonElement item)
    {
        if (!item.TryGetProperty("syntax", out var syntax))
        {
            Assert.Fail($"Missing syntax.content for uid `{item.GetProperty("uid").GetString()}`.");
        }

        if (!syntax.TryGetProperty("content", out var content))
        {
            Assert.Fail($"Missing syntax.content for uid `{item.GetProperty("uid").GetString()}`.");
        }

        return content.GetString() ?? string.Empty;
    }

    private string FindTypeUidWithMemberKinds(params string[] expectedKinds)
    {
        foreach (var item in _itemsByUid.Values)
        {
            if (!IsTypeDeclaration(item))
            {
                continue;
            }

            var childKinds = GetChildKinds(item);
            if (expectedKinds.All(kind => childKinds.Contains(kind)))
            {
                return item.GetProperty("uid").GetString()!;
            }
        }

        Assert.Fail($"Unable to find a type uid containing member kinds: {string.Join(", ", expectedKinds)}.");
        return string.Empty;
    }

    private HashSet<string> GetChildKinds(JsonElement item)
    {
        var childKinds = new HashSet<string>(StringComparer.Ordinal);
        if (!item.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
        {
            return childKinds;
        }

        foreach (var childUidEntry in children.EnumerateArray())
        {
            var childUid = childUidEntry.GetString();
            if (string.IsNullOrWhiteSpace(childUid) || !_itemsByUid.TryGetValue(childUid, out var childItem))
            {
                continue;
            }

            if (childItem.TryGetProperty("type", out var childTypeProperty))
            {
                var childType = childTypeProperty.GetString();
                if (!string.IsNullOrWhiteSpace(childType))
                {
                    childKinds.Add(childType);
                }
            }
        }

        return childKinds;
    }

    private static bool IsTypeDeclaration(JsonElement item)
    {
        if (!item.TryGetProperty("type", out var typeProperty))
        {
            return false;
        }

        return typeProperty.GetString() is "Class" or "Struct" or "Interface" or "Enum" or "Delegate";
    }

    private string ReadRenderedHtmlByUid(string uid)
    {
        var path = Path.Combine(_outputRoot, "api", UidHelper.Handleize(uid), "index.html");
        Assert.IsTrue(File.Exists(path), $"Expected rendered API page not found at `{path}`.");
        return WebUtility.HtmlDecode(File.ReadAllText(path));
    }

    private string ReadRenderedApiRootHtml()
    {
        var path = Path.Combine(_outputRoot, "api", "index.html");
        Assert.IsTrue(File.Exists(path), $"Expected rendered API root page not found at `{path}`.");
        return WebUtility.HtmlDecode(File.ReadAllText(path));
    }

    private static int ExecuteScalar(string sqlitePath, string sql, params (string Name, string Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={sqlitePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }
}
