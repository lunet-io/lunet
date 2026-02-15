// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Metadata.ManagedReference;

namespace Lunet.Tests.Api.DotNet;

public class TestModernCSharpLanguageSupport
{
    private static readonly Lazy<MetadataItem> Metadata = new(() => ApiDotNetExtractorTestHelper.ExtractSingleFile(
        """
        #nullable enable
        namespace ModernFeatures;

        public record class RecordClass(int Value);

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
            public required string Name { get; init; }
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
            public void Accept(params System.ReadOnlySpan<int> values)
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
        """), LazyThreadSafetyMode.ExecutionAndPublication);

    [Test]
    public void TestRecordClassAndRecordStructSyntax()
    {
        var recordClass = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Class, "RecordClass");
        var recordStruct = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Struct, "ReadonlyRecordStruct");

        var classSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(recordClass);
        var structSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(recordStruct);

        StringAssert.Contains("record RecordClass(int Value)", classSyntax);
        StringAssert.Contains("readonly record struct ReadonlyRecordStruct(int Value)", structSyntax);
    }

    [Test]
    public void TestRefStructAndReadonlyMethodSyntax()
    {
        var refStruct = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Struct, "RefReader");
        var readonlyMethod = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, "RefReader.ReadonlyMethod");

        var structSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(refStruct);
        var methodSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(readonlyMethod);
        var methodModifiers = ApiDotNetExtractorTestHelper.GetCSharpModifiers(readonlyMethod);

        StringAssert.Contains("ref struct RefReader", structSyntax);
        StringAssert.Contains("readonly int ReadonlyMethod()", methodSyntax);
        CollectionAssert.Contains(methodModifiers, "readonly");
    }

    [Test]
    public void TestRequiredAndInitMembers()
    {
        var requiredProperty = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Property, "RequiredAndInit.Name");
        var requiredField = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Field, "RequiredAndInit.Code");

        var propertySyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(requiredProperty);
        var propertyModifiers = ApiDotNetExtractorTestHelper.GetCSharpModifiers(requiredProperty);
        var fieldSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(requiredField);
        var fieldModifiers = ApiDotNetExtractorTestHelper.GetCSharpModifiers(requiredField);

        StringAssert.Contains("required string Name", propertySyntax);
        StringAssert.Contains("init;", propertySyntax);
        CollectionAssert.Contains(propertyModifiers, "required");
        CollectionAssert.Contains(propertyModifiers, "init");

        StringAssert.Contains("required int Code", fieldSyntax);
        CollectionAssert.Contains(fieldModifiers, "required");
    }

    [Test]
    public void TestRefReadonlyAndScopedParameters()
    {
        var refReadonlyMethod = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, "RefFeatures.RefReadonlyReturn");
        var scopedMethod = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, "RefFeatures.ScopedIn");

        var refReadonlySyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(refReadonlyMethod);
        var scopedSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(scopedMethod);

        StringAssert.Contains("ref readonly int RefReadonlyReturn", refReadonlySyntax);
        StringAssert.Contains("(ref readonly int value)", refReadonlySyntax);
        StringAssert.Contains("(scoped in int value)", scopedSyntax);
    }

    [Test]
    public void TestNullableReferenceTypeSyntax()
    {
        var nullableProperty = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Property, "NullableApi`1.MaybeText");
        var nullableMethod = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, "NullableApi`1.Echo");
        var nullableType = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Class, "NullableApi`1");

        var propertySyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(nullableProperty);
        var methodSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(nullableMethod);
        var typeSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(nullableType);

        StringAssert.Contains("string? MaybeText", propertySyntax);
        StringAssert.Contains("TRef? Echo(TRef? value)", methodSyntax);
        StringAssert.Contains("where TRef : class?", typeSyntax);
    }

    [Test]
    public void TestPrimaryConstructorTypeSyntax()
    {
        var primaryCtorClass = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Class, "PrimaryCtorClass");
        var primaryCtorStruct = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Struct, "PrimaryCtorStruct");

        var classSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(primaryCtorClass);
        var structSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(primaryCtorStruct);

        StringAssert.Contains("class PrimaryCtorClass(string name)", classSyntax);
        StringAssert.Contains("readonly struct PrimaryCtorStruct(int value)", structSyntax);
    }

    [Test]
    public void TestNativeIntegerAndFunctionPointerSyntax()
    {
        var nativeField = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Field, "NativeAndFunctionPointers.NativeIntField");
        var nativeProperty = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Property, "NativeAndFunctionPointers.NativeUIntProperty");
        var callbackField = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Field, "NativeAndFunctionPointers.Callback");

        var fieldSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(nativeField);
        var propertySyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(nativeProperty);
        var callbackSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(callbackField);

        StringAssert.Contains("nint NativeIntField", fieldSyntax);
        StringAssert.Contains("nuint NativeUIntProperty", propertySyntax);
        StringAssert.Contains("delegate* unmanaged[Cdecl]<int, void> Callback", callbackSyntax);
    }

    [Test]
    public void TestStaticAbstractInterfaceMembers()
    {
        var createMethod = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, ".Create");
        var zeroProperty = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Property, ".Zero");

        var createSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(createMethod);
        var createModifiers = ApiDotNetExtractorTestHelper.GetCSharpModifiers(createMethod);
        var zeroSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(zeroProperty);
        var zeroModifiers = ApiDotNetExtractorTestHelper.GetCSharpModifiers(zeroProperty);

        StringAssert.Contains("static abstract", createSyntax);
        CollectionAssert.Contains(createModifiers, "static");
        CollectionAssert.Contains(createModifiers, "abstract");

        StringAssert.Contains("static abstract", zeroSyntax);
        CollectionAssert.Contains(zeroModifiers, "static");
        CollectionAssert.Contains(zeroModifiers, "abstract");
    }

    [Test]
    public void TestModernGenericConstraints()
    {
        var constrainedInterface = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Interface, "IConstraint");
        var syntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(constrainedInterface);

        StringAssert.Contains("where TUnmanaged : unmanaged", syntax);
        StringAssert.Contains("where TNotNull : notnull", syntax);
    }

    [Test]
    public void TestDefaultInterfaceMemberSyntax()
    {
        var method = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, "IDefaultInterfaceMember.Implemented");
        var syntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(method);

        StringAssert.Contains("void Implemented()", syntax);
        StringAssert.DoesNotContain("virtual", syntax);
    }

    [Test]
    public void TestCheckedAndUnsignedShiftOperators()
    {
        var checkedAdditionOperator = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Operator, "op_CheckedAddition");
        var unsignedRightShiftOperator = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Operator, "op_UnsignedRightShift");

        var checkedAdditionSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(checkedAdditionOperator);
        var unsignedRightShiftSyntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(unsignedRightShiftOperator);

        StringAssert.Contains("operator checked +", checkedAdditionSyntax);
        StringAssert.Contains("operator >>>", unsignedRightShiftSyntax);
    }

    [Test]
    public void TestAllowsRefStructConstraint()
    {
        var interfaceWithAllowsConstraint = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Interface, "IAllowsRefStruct");
        var syntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(interfaceWithAllowsConstraint);

        StringAssert.Contains("allows ref struct", syntax);
    }

    [Test]
    public void TestRefStructImplementsInterface()
    {
        var refStruct = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Struct, "RefStructImplementer");
        var syntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(refStruct);

        StringAssert.Contains("ref struct RefStructImplementer : IRefStructContract", syntax);
    }

    [Test]
    public void TestParamsCollectionSyntax()
    {
        var paramsMethod = ApiDotNetExtractorTestHelper.FindSingleByTypeAndUidContains(Metadata.Value, MemberType.Method, "ParamsCollectionFeature.Accept");
        var syntax = ApiDotNetExtractorTestHelper.GetCSharpSyntax(paramsMethod);

        StringAssert.Contains("params ReadOnlySpan<int> values", syntax);
    }

    [Test]
    public void TestCSharp14ExtensionMembersDoNotCrashExtractor()
    {
        Assert.DoesNotThrow(() => ApiDotNetExtractorTestHelper.ExtractSingleFile(
            """
            using System.Collections.Generic;

            namespace ModernFeatures;

            public static class EnumerableExtensions
            {
                extension<TSource>(IEnumerable<TSource> source)
                {
                    public bool IsEmpty => false;

                    public IEnumerable<TSource> Identity() => source;
                }
            }
            """));
    }

    [Test]
    public void TestCSharp14ExtensionMembersDoNotCrashIntermediateModel()
    {
        var metadata = ApiDotNetExtractorTestHelper.ExtractSingleFile(
            """
            using System.Collections.Generic;

            namespace ModernFeatures;

            public static class EnumerableExtensions
            {
                extension<TSource>(IEnumerable<TSource> source)
                {
                    public bool IsEmpty => false;

                    public IEnumerable<TSource> Identity() => source;
                }
            }
            """);

        var allMembers = new Dictionary<string, MetadataItem>();
        foreach (var ns in metadata.Items)
        {
            allMembers[ns.Name] = ns;
            if (ns.Items is null)
            {
                continue;
            }

            foreach (var member in ns.Items)
            {
                allMembers[member.Name] = member;
                if (member.Items is null)
                {
                    continue;
                }

                foreach (var nested in member.Items)
                {
                    allMembers[nested.Name] = nested;
                }
            }
        }

        Assert.DoesNotThrow(() =>
        {
            var model = YamlMetadataResolver.ResolveMetadata(allMembers, metadata.References, true);
            _ = model.Members.Select(member => member.ToPageViewModel()).ToList();
        });
    }
}
