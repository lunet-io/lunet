// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class CSYamlModelGenerator : SimpleYamlModelGenerator
    {
        #region Fields
        private static readonly Regex BracesRegex = new Regex(@"\s*\{(;|\s)*\}\s*$", RegexOptions.Compiled);
        private static readonly SymbolDisplayFormat CSharpTypeDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        #endregion

        public CSYamlModelGenerator() : base(SyntaxLanguage.CSharp)
        {
        }

        #region Overrides

        public override void DefaultVisit(ISymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            item.DisplayNames[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
            item.DisplayNamesWithType[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.WithType | NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
            item.DisplayQualifiedNames[SyntaxLanguage.CSharp] = NameVisitorCreator.GetCSharp(NameOptions.Qualified | NameOptions.WithGenericParameter | NameOptions.WithParameter).GetName(symbol);
        }

        public override void GenerateNamedType(INamedTypeSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateNamedType(symbol, item, adapter);

            var modifiers = new List<string>();
            var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
            if (visiblity != null)
            {
                modifiers.Add(visiblity);
            }
            if (symbol.TypeKind == TypeKind.Class)
            {
                if (symbol.IsStatic)
                {
                    modifiers.Add("static");
                }
                else if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            else if (symbol.TypeKind == TypeKind.Struct)
            {
                if (symbol.IsRefLikeType)
                {
                    modifiers.Add("ref");
                }
                if (symbol.IsReadOnly)
                {
                    modifiers.Add("readonly");
                }
            }
            switch (symbol.TypeKind)
            {
                case TypeKind.Module:
                case TypeKind.Class:
                    if (symbol.IsRecord)
                    {
                        modifiers.Add("record");
                    }
                    else
                    {
                        modifiers.Add("class");
                    }
                    break;
                case TypeKind.Delegate:
                    modifiers.Add("delegate");
                    break;
                case TypeKind.Enum:
                    modifiers.Add("enum");
                    break;
                case TypeKind.Interface:
                    modifiers.Add("interface");
                    break;
                case TypeKind.Struct:
                    if (symbol.IsRecord)
                    {
                        modifiers.Add("record");
                    }
                    modifiers.Add("struct");
                    break;
                default:
                    break;
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        public override void GenerateMethod(IMethodSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateMethod(symbol, item, adapter);

            var modifiers = new List<string>();
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
                if (visiblity != null)
                {
                    modifiers.Add(visiblity);
                }
            }
            if (symbol.IsStatic)
            {
                modifiers.Add("static");
            }
            if (symbol.IsReadOnly)
            {
                modifiers.Add("readonly");
            }
            if (symbol.IsAbstract && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                modifiers.Add("abstract");
            }
            if (symbol.IsOverride)
            {
                modifiers.Add("override");
            }
            if (symbol.IsVirtual && symbol.IsSealed)
            {
            }
            else if (symbol.IsVirtual && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                modifiers.Add("virtual");
            }
            else if (symbol.IsSealed)
            {
                modifiers.Add("sealed");
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        public override void GenerateField(IFieldSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateField(symbol, item, adapter);

            var modifiers = new List<string>();
            var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
            if (visiblity != null)
            {
                modifiers.Add(visiblity);
            }
            if (symbol.IsConst)
            {
                modifiers.Add("const");
            }
            else if (symbol.IsStatic)
            {
                modifiers.Add("static");
            }
            if (symbol.IsReadOnly)
            {
                modifiers.Add("readonly");
            }
            if (symbol.IsRequired)
            {
                modifiers.Add("required");
            }
            if (symbol.IsVolatile)
            {
                modifiers.Add("volatile");
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        public override void GenerateProperty(IPropertySymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateProperty(symbol, item, adapter);

            var modifiers = new List<string>();
            var propertyVisiblity = GetVisiblity(symbol.DeclaredAccessibility);
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                if (propertyVisiblity != null)
                {
                    modifiers.Add(propertyVisiblity);
                }
                if (symbol.IsStatic)
                {
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("virtual");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            else
            {
                if (symbol.IsStatic)
                {
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract && symbol.IsStatic)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsVirtual && symbol.IsStatic)
                {
                    modifiers.Add("virtual");
                }
            }
            if (symbol.IsRequired)
            {
                modifiers.Add("required");
            }
            if (symbol.GetMethod != null)
            {
                var getMethodVisiblity = GetVisiblity(symbol.GetMethod.DeclaredAccessibility);
                if (propertyVisiblity != null && getMethodVisiblity == null)
                {
                }
                else if (getMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{getMethodVisiblity} get");
                }
                else
                {
                    modifiers.Add("get");
                }
            }
            if (symbol.SetMethod != null)
            {
                var setMethodVisiblity = GetVisiblity(symbol.SetMethod.DeclaredAccessibility);
                var setKeyword = symbol.SetMethod.IsInitOnly ? "init" : "set";
                if (propertyVisiblity != null && setMethodVisiblity == null)
                {
                }
                else if (setMethodVisiblity != propertyVisiblity)
                {
                    modifiers.Add($"{setMethodVisiblity} {setKeyword}");
                }
                else
                {
                    modifiers.Add(setKeyword);
                }
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        public override void GenerateEvent(IEventSymbol symbol, MetadataItem item, SymbolVisitorAdapter adapter)
        {
            base.GenerateEvent(symbol, item, adapter);

            var modifiers = new List<string>();
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                var visiblity = GetVisiblity(symbol.DeclaredAccessibility);
                if (visiblity != null)
                {
                    modifiers.Add(visiblity);
                }
                if (symbol.IsStatic)
                {
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsOverride)
                {
                    modifiers.Add("override");
                }
                if (symbol.IsVirtual && symbol.IsSealed)
                {
                }
                else if (symbol.IsVirtual)
                {
                    modifiers.Add("virtual");
                }
                else if (symbol.IsSealed)
                {
                    modifiers.Add("sealed");
                }
            }
            else
            {
                if (symbol.IsStatic)
                {
                    modifiers.Add("static");
                }
                if (symbol.IsAbstract && symbol.IsStatic)
                {
                    modifiers.Add("abstract");
                }
                if (symbol.IsVirtual && symbol.IsStatic)
                {
                    modifiers.Add("virtual");
                }
            }
            item.Modifiers[SyntaxLanguage.CSharp] = modifiers;
        }

        protected override string GetSyntaxContent(MemberType typeKind, ISymbol symbol, SymbolVisitorAdapter adapter)
        {
            switch (typeKind)
            {
                case MemberType.Class:
                    return GetClassSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Enum:
                    return GetEnumSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Interface:
                    return GetInterfaceSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Struct:
                    return GetStructSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Delegate:
                    return GetDelegateSyntax((INamedTypeSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Method:
                    return GetMethodSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Operator:
                    return GetOperatorSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Constructor:
                    return GetConstructorSyntax((IMethodSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Field:
                    return GetFieldSyntax((IFieldSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Event:
                    return GetEventSyntax((IEventSymbol)symbol, adapter.FilterVisitor);
                case MemberType.Property:
                    return GetPropertySyntax((IPropertySymbol)symbol, adapter.FilterVisitor);
                default:
                    return null;
            }
        }

        protected override void GenerateReference(ISymbol symbol, ReferenceItem reference, SymbolVisitorAdapter adapter, bool asOverload)
        {
            symbol.Accept(new CSReferenceItemVisitor(reference, asOverload));
        }

        #endregion

        #region Private methods

        #region Syntax

        private string GetClassSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            ReplaceRecordKeywordIfNeeded(
                symbol,
                AppendAllowsRefStructConstraints(
                    RemoveBraces(
                        SyntaxFactory.ClassDeclaration(
                        GetAttributes(symbol, filterVisitor),
                        SyntaxFactory.TokenList(GetTypeModifiers(symbol)),
                        SyntaxFactory.Identifier(symbol.Name),
                        GetTypeParameters(symbol),
                        GetPrimaryConstructorParameters(symbol, filterVisitor),
                        GetBaseTypeList(symbol),
                        SyntaxFactory.List(GetTypeParameterConstraints(symbol)),
                        new SyntaxList<MemberDeclarationSyntax>()
                        ).NormalizeWhitespace().ToString()
                    ),
                    symbol.TypeParameters));

        private string GetEnumSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            RemoveBraces(
                SyntaxFactory.EnumDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetTypeModifiers(symbol)),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetEnumBaseTypeList(symbol),
                    new SeparatedSyntaxList<EnumMemberDeclarationSyntax>()
                )
                .NormalizeWhitespace()
                .ToString()
            );

        private string GetInterfaceSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            AppendAllowsRefStructConstraints(
                RemoveBraces(
                    SyntaxFactory.InterfaceDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetTypeModifiers(symbol)
                    ),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetTypeParameters(symbol),
                    GetBaseTypeList(symbol),
                    SyntaxFactory.List(
                        GetTypeParameterConstraints(symbol)
                    ),
                    new SyntaxList<MemberDeclarationSyntax>()
                    ).NormalizeWhitespace().ToString()
                ),
                symbol.TypeParameters);

        private string GetStructSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            ReplaceRecordKeywordIfNeeded(
                symbol,
                AppendAllowsRefStructConstraints(
                    RemoveBraces(
                        SyntaxFactory.StructDeclaration(
                        GetAttributes(symbol, filterVisitor),
                        SyntaxFactory.TokenList(
                            GetTypeModifiers(symbol)
                        ),
                        SyntaxFactory.Identifier(symbol.Name),
                        GetTypeParameters(symbol),
                        GetPrimaryConstructorParameters(symbol, filterVisitor),
                        GetBaseTypeList(symbol),
                        SyntaxFactory.List(
                            GetTypeParameterConstraints(symbol)
                        ),
                        new SyntaxList<MemberDeclarationSyntax>()
                        ).NormalizeWhitespace().ToString()
                    ),
                    symbol.TypeParameters));

        private string GetDelegateSyntax(INamedTypeSymbol symbol, IFilterVisitor filterVisitor) =>
            AppendAllowsRefStructConstraints(
                SyntaxFactory.DelegateDeclaration(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetTypeModifiers(symbol)
                ),
                GetTypeSyntax(symbol.DelegateInvokeMethod.ReturnType, symbol.DelegateInvokeMethod.ReturnNullableAnnotation),
                SyntaxFactory.Identifier(symbol.Name),
                GetTypeParameters(symbol),
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        from p in symbol.DelegateInvokeMethod.Parameters
                        select GetParameter(p, filterVisitor)
                    )
                ),
                SyntaxFactory.List(
                    GetTypeParameterConstraints(symbol)
                )
                ).NormalizeWhitespace().ToString(),
                symbol.TypeParameters);

        private string GetMethodSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            ExplicitInterfaceSpecifierSyntax eii = null;
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(symbol, filterVisitor)));
            }
            return AppendAllowsRefStructConstraints(
                SyntaxFactory.MethodDeclaration(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                GetMethodReturnTypeSyntax(symbol),
                eii,
                SyntaxFactory.Identifier(
                    GetMemberName(symbol, filterVisitor)
                ),
                GetTypeParameters(symbol),
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        symbol.Parameters.Select((p, i) => GetParameter(p, filterVisitor, i == 0 && symbol.IsExtensionMethod))
                    )
                ),
                SyntaxFactory.List(
                    GetTypeParameterConstraints(symbol)
                ),
                null,
                null
                ).NormalizeWhitespace().ToString(),
                symbol.TypeParameters);
        }

        private string GetOperatorSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            var operatorToken = GetOperatorToken(symbol.Name);
            var isCheckedOperator = IsCheckedOperator(symbol.Name);
            if (operatorToken == null)
            {
                return "Not supported in c#";
            }
            else if (operatorToken.Value.Kind() == SyntaxKind.ImplicitKeyword || operatorToken.Value.Kind() == SyntaxKind.ExplicitKeyword)
            {
                var syntax = SyntaxFactory.ConversionOperatorDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetMemberModifiers(symbol)
                    ),
                    operatorToken.Value,
                    GetMethodReturnTypeSyntax(symbol),
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(
                            from p in symbol.Parameters
                            select GetParameter(p, filterVisitor)
                        )
                    ),
                    null,
                    null
                ).NormalizeWhitespace().ToString();

                return isCheckedOperator ? InsertCheckedKeywordInOperatorSyntax(syntax) : syntax;
            }
            else
            {
                var syntax = SyntaxFactory.OperatorDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetMemberModifiers(symbol)
                    ),
                    GetMethodReturnTypeSyntax(symbol),
                    operatorToken.Value,
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(
                            from p in symbol.Parameters
                            select GetParameter(p, filterVisitor)
                        )
                    ),
                    null,
                    null
                ).NormalizeWhitespace().ToString();

                return isCheckedOperator ? InsertCheckedKeywordInOperatorSyntax(syntax) : syntax;
            }
        }

        private string GetConstructorSyntax(IMethodSymbol symbol, IFilterVisitor filterVisitor) =>
            SyntaxFactory.ConstructorDeclaration(
                GetAttributes(symbol, filterVisitor),
                SyntaxFactory.TokenList(
                    GetMemberModifiers(symbol)
                ),
                SyntaxFactory.Identifier(symbol.ContainingType.Name),
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        from p in symbol.Parameters
                        select GetParameter(p, filterVisitor)
                    )
                ),
                null,
                (BlockSyntax)null
            ).NormalizeWhitespace().ToString();

        private string GetFieldSyntax(IFieldSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                return SyntaxFactory.EnumMemberDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.Identifier(symbol.Name),
                    GetDefaultValueClause(symbol)
                ).NormalizeWhitespace().ToString();
            }
            else
            {
                return SyntaxFactory.FieldDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(
                        GetMemberModifiers(symbol)
                    ),
                    SyntaxFactory.VariableDeclaration(
                        GetFieldTypeSyntax(symbol),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(symbol.Name),
                                null,
                                GetDefaultValueClause(symbol)
                            )
                        )
                    )
                ).NormalizeWhitespace().ToString().TrimEnd(';');
            }
        }

        private string GetEventSyntax(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            ExplicitInterfaceSpecifierSyntax eii = null;
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(symbol, filterVisitor)));
            }
            return RemoveBraces(
                SyntaxFactory.EventDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    SyntaxFactory.Token(SyntaxKind.EventKeyword),
                    GetTypeSyntax(symbol.Type, symbol.NullableAnnotation),
                    eii,
                    SyntaxFactory.Identifier(GetMemberName(symbol, filterVisitor)),
                    SyntaxFactory.AccessorList()
                ).NormalizeWhitespace().ToString()
            );
        }

        private string GetPropertySyntax(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            string result;
            ExplicitInterfaceSpecifierSyntax eii = null;
            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                eii = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(GetEiiContainerTypeName(symbol, filterVisitor)));
            }
            if (symbol.IsIndexer)
            {
                result = SyntaxFactory.IndexerDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    GetTypeSyntax(symbol.Type, symbol.NullableAnnotation),
                    eii,
                    SyntaxFactory.BracketedParameterList(
                        SyntaxFactory.SeparatedList(
                            from p in symbol.Parameters
                            select GetParameter(p, filterVisitor))),
                    SyntaxFactory.AccessorList(SyntaxFactory.List(GetPropertyAccessors(symbol, filterVisitor))))
                    .NormalizeWhitespace()
                    .ToString();
            }
            else
            {
                result = SyntaxFactory.PropertyDeclaration(
                    GetAttributes(symbol, filterVisitor),
                    SyntaxFactory.TokenList(GetMemberModifiers(symbol)),
                    GetTypeSyntax(symbol.Type, symbol.NullableAnnotation),
                    eii,
                    SyntaxFactory.Identifier(GetMemberName(symbol, filterVisitor)),
                    SyntaxFactory.AccessorList(SyntaxFactory.List(GetPropertyAccessors(symbol, filterVisitor))))
                    .NormalizeWhitespace()
                    .ToString();
            }
            result = Regex.Replace(result, @"\s*\{\s*get;\s*set;\s*}\s*$", " { get; set; }");
            result = Regex.Replace(result, @"\s*\{\s*get;\s*}\s*$", " { get; }");
            result = Regex.Replace(result, @"\s*\{\s*set;\s*}\s*$", " { set; }");
            result = Regex.Replace(result, @"\s*\{\s*get;\s*protected set;\s*}\s*$", " { get; protected set; }");
            result = Regex.Replace(result, @"\s*\{\s*protected get;\s*set;\s*}\s*$", " { protected get; set; }");
            return result;
        }

        #endregion

        private static SyntaxList<AttributeListSyntax> GetAttributes(ISymbol symbol, IFilterVisitor filterVisitor, bool inOneLine = false)
        {
            var attrs = symbol.GetAttributes();
            if (attrs.Length > 0)
            {
                var attrList = (from attr in attrs
                                where !(attr.AttributeClass is IErrorTypeSymbol)
                                where attr?.AttributeConstructor != null
                                where filterVisitor.CanVisitAttribute(attr.AttributeConstructor)
                                select GetAttributeSyntax(attr)).ToList();
                if (attrList.Count > 0)
                {
                    if (inOneLine)
                    {
                        return SyntaxFactory.SingletonList(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SeparatedList(attrList)));
                    }
                    return SyntaxFactory.List(
                        from attr in attrList
                        select SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(attr)));
                }
            }
            return new SyntaxList<AttributeListSyntax>();
        }

        private static AttributeSyntax GetAttributeSyntax(AttributeData attr)
        {
            var attrTypeName = NameVisitorCreator.GetCSharp(NameOptions.None).GetName(attr.AttributeClass);
            if (attrTypeName.EndsWith(nameof(Attribute), StringComparison.Ordinal))
            {
                attrTypeName = attrTypeName.Remove(attrTypeName.Length - nameof(Attribute).Length);
            }
            if (attr.ConstructorArguments.Length == 0 && attr.NamedArguments.Length == 0)
            {
                return SyntaxFactory.Attribute(SyntaxFactory.ParseName(attrTypeName));
            }
            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(attrTypeName),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList(
                        (from item in attr.ConstructorArguments
                         select GetLiteralExpression(item) into expr
                         where expr != null
                         select SyntaxFactory.AttributeArgument(expr)
                        ).Concat(
                            from item in attr.NamedArguments
                            let expr = GetLiteralExpression(item.Value)
                            where expr != null
                            select SyntaxFactory.AttributeArgument(
                                SyntaxFactory.NameEquals(
                                    SyntaxFactory.IdentifierName(item.Key)
                                ),
                                null,
                                expr
                            )
                        )
                    )
                )
            );
        }

        private static string GetMemberName(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetMemberName(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetMemberName(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            string name = symbol.Name;
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return symbol.Name;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return symbol.ExplicitInterfaceImplementations[i].Name;
                }
            }
            Debug.Fail("Should not be here!");
            return symbol.Name;
        }

        private static string GetEiiContainerTypeName(IMethodSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static string GetEiiContainerTypeName(IEventSymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static string GetEiiContainerTypeName(IPropertySymbol symbol, IFilterVisitor filterVisitor)
        {
            if (symbol.ExplicitInterfaceImplementations.Length == 0)
            {
                return null;
            }
            for (int i = 0; i < symbol.ExplicitInterfaceImplementations.Length; i++)
            {
                if (filterVisitor.CanVisitApi(symbol.ExplicitInterfaceImplementations[i]))
                {
                    return NameVisitorCreator.GetCSharp(NameOptions.UseAlias | NameOptions.WithGenericParameter).GetName(symbol.ExplicitInterfaceImplementations[i].ContainingType);
                }
            }
            Debug.Fail("Should not be here!");
            return null;
        }

        private static ParameterSyntax GetParameter(IParameterSymbol p, IFilterVisitor filterVisitor, bool isThisParameter = false)
        {
            return SyntaxFactory.Parameter(
                GetAttributes(p, filterVisitor, true),
                SyntaxFactory.TokenList(GetParameterModifiers(p, isThisParameter)),
                GetTypeSyntax(p.Type, p.NullableAnnotation),
                SyntaxFactory.Identifier(p.Name),
                GetDefaultValueClause(p));
        }

        private static IEnumerable<SyntaxToken> GetParameterModifiers(IParameterSymbol parameter, bool isThisParameter)
        {
            if (isThisParameter)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ThisKeyword);
            }
            if (parameter.ScopedKind != ScopedKind.None)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ScopedKeyword);
            }
            switch (parameter.RefKind)
            {
                case RefKind.None:
                    break;
                case RefKind.Ref:
                    yield return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                    break;
                case RefKind.Out:
                    yield return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                    break;
                case RefKind.In:
                    yield return SyntaxFactory.Token(SyntaxKind.InKeyword);
                    break;
                case RefKind.RefReadOnlyParameter:
                    yield return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                    yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
                    break;
                default:
                    break;
            }
            if (parameter.IsParams)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ParamsKeyword);
            }
        }

        private static EqualsValueClauseSyntax GetDefaultValueClause(IParameterSymbol symbol)
        {
            if (symbol.HasExplicitDefaultValue)
            {
                return GetDefaultValueClauseCore(symbol.ExplicitDefaultValue, symbol.Type);
            }
            return null;
        }

        private static EqualsValueClauseSyntax GetDefaultValueClause(IFieldSymbol symbol)
        {
            if (symbol.IsConst)
            {
                if (symbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    return GetDefaultValueClauseCore(symbol.ConstantValue, ((INamedTypeSymbol)symbol.Type).EnumUnderlyingType);
                }
                return GetDefaultValueClauseCore(symbol.ConstantValue, symbol.Type);
            }
            return null;
        }

        private static EqualsValueClauseSyntax GetDefaultValueClauseCore(object value, ITypeSymbol type)
        {
            var expr = GetLiteralExpression(value, type);
            if (expr != null)
            {
                return SyntaxFactory.EqualsValueClause(expr);
            }
            return null;
        }

        private static ExpressionSyntax GetLiteralExpression(TypedConstant constant)
        {
            if (constant.Type.TypeKind == TypeKind.Array)
            {
                if (constant.Values == null)
                {
                    return GetLiteralExpression(null, constant.Type);
                }
                var items = (from value in constant.Values
                             select GetLiteralExpression(value)).ToList();
                if (items.TrueForAll(x => x != null))
                {
                    return SyntaxFactory.ArrayCreationExpression(
                        (ArrayTypeSyntax)GetTypeSyntax(constant.Type),
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ArrayInitializerExpression,
                            SyntaxFactory.SeparatedList(
                                from value in constant.Values
                                select GetLiteralExpression(value))));
                }
                return SyntaxFactory.ArrayCreationExpression(
                    (ArrayTypeSyntax)GetTypeSyntax(constant.Type));
            }

            var expr = GetLiteralExpression(constant.Value, constant.Type);
            if (expr == null)
            {
                return null;
            }

            switch (constant.Type.SpecialType)
            {
                case SpecialType.System_SByte:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.SByteKeyword)),
                        expr);
                case SpecialType.System_Byte:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ByteKeyword)),
                        expr);
                case SpecialType.System_Int16:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.ShortKeyword)),
                        expr);
                case SpecialType.System_UInt16:
                    return SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.UShortKeyword)),
                        expr);
                default:
                    return expr;
            }
        }

        private static ExpressionSyntax GetLiteralExpression(object value, ITypeSymbol type)
        {
            if (value == null)
            {
                if (type.IsValueType)
                {
                    return SyntaxFactory.DefaultExpression(GetTypeSyntax(type));
                }
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NullLiteralExpression,
                    SyntaxFactory.Token(SyntaxKind.NullKeyword));
            }
            var result = GetLiteralExpressionCore(value, type);
            if (result != null)
            {
                return result;
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                var namedType = (INamedTypeSymbol)type;
                var enumType = GetTypeSyntax(namedType);
                var isFlags = namedType.GetAttributes().Any(attr => attr.AttributeClass.GetDocumentationCommentId() == "T:System.FlagsAttribute");

                var pairs = from member in namedType.GetMembers().OfType<IFieldSymbol>()
                            where member.IsConst && member.HasConstantValue
                            select new { member.Name, member.ConstantValue };
                if (isFlags)
                {
                    var exprs = (from pair in pairs
                                 where HasFlag(namedType.EnumUnderlyingType, value, pair.ConstantValue)
                                 select SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     enumType,
                                     SyntaxFactory.IdentifierName(pair.Name))).ToList();
                    if (exprs.Count > 0)
                    {
                        return exprs.Aggregate<ExpressionSyntax>((x, y) =>
                            SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, x, y));
                    }
                }
                else
                {
                    var expr = (from pair in pairs
                                where object.Equals(value, pair.ConstantValue)
                                select SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    enumType,
                                    SyntaxFactory.IdentifierName(pair.Name))).FirstOrDefault();
                    if (expr != null)
                    {
                        return expr;
                    }
                }
                return SyntaxFactory.CastExpression(
                    enumType,
                    GetLiteralExpressionCore(
                        value,
                        namedType.EnumUnderlyingType));
            }
            if (value is ITypeSymbol)
            {
                return SyntaxFactory.TypeOfExpression(
                    GetTypeSyntax((ITypeSymbol)value));
            }
            Debug.Fail("Unknown default value!");
            return null;
        }

        private static bool HasFlag(ITypeSymbol type, object value, object constantValue)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_SByte:
                    {
                        var v = (sbyte)value;
                        var cv = (sbyte)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Byte:
                    {
                        var v = (byte)value;
                        var cv = (byte)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Int16:
                    {
                        var v = (short)value;
                        var cv = (short)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_UInt16:
                    {
                        var v = (ushort)value;
                        var cv = (ushort)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Int32:
                    {
                        var v = (int)value;
                        var cv = (int)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_UInt32:
                    {
                        var v = (uint)value;
                        var cv = (uint)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_Int64:
                    {
                        var v = (long)value;
                        var cv = (long)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                case SpecialType.System_UInt64:
                    {
                        var v = (ulong)value;
                        var cv = (ulong)constantValue;
                        if (cv == 0)
                        {
                            return v == 0;
                        }
                        return (v & cv) == cv;
                    }
                default:
                    return false;
            }
        }

        public static ExpressionSyntax GetLiteralExpressionCore(object value, ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxFactory.LiteralExpression(
                        (bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                case SpecialType.System_Char:
                    var ch = (char)value;
                    var category = char.GetUnicodeCategory(ch);
                    switch (category)
                    {
                        case System.Globalization.UnicodeCategory.Surrogate:
                            return SyntaxFactory.LiteralExpression(
                                SyntaxKind.CharacterLiteralExpression,
                                SyntaxFactory.Literal("'\\u" + ((int)ch).ToString("X4") + "'", ch));
                        default:
                            return SyntaxFactory.LiteralExpression(
                                SyntaxKind.CharacterLiteralExpression,
                                SyntaxFactory.Literal((char)value));
                    }
                case SpecialType.System_SByte:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((sbyte)value));
                case SpecialType.System_Byte:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((byte)value));
                case SpecialType.System_Int16:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((short)value));
                case SpecialType.System_UInt16:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((ushort)value));
                case SpecialType.System_Int32:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((int)value));
                case SpecialType.System_UInt32:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((uint)value));
                case SpecialType.System_Int64:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((long)value));
                case SpecialType.System_UInt64:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((ulong)value));
                case SpecialType.System_Decimal:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((decimal)value));
                case SpecialType.System_Single:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((float)value));
                case SpecialType.System_Double:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal((double)value));
                case SpecialType.System_String:
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal((string)value));
                default:
                    return null;
            }
        }

        private static IEnumerable<TypeParameterConstraintClauseSyntax> GetTypeParameterConstraints(INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                yield break;
            }
            foreach (ITypeParameterSymbol ta in symbol.TypeArguments)
            {
                if (ta.HasConstructorConstraint || ta.HasReferenceTypeConstraint || ta.HasValueTypeConstraint || ta.HasUnmanagedTypeConstraint || ta.HasNotNullConstraint || ta.ConstraintTypes.Length > 0)
                {
                    yield return SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(ta.Name), SyntaxFactory.SeparatedList(GetTypeParameterConstraint(ta)));
                }
            }
        }

        private static IEnumerable<TypeParameterConstraintClauseSyntax> GetTypeParameterConstraints(IMethodSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                yield break;
            }
            foreach (ITypeParameterSymbol ta in symbol.TypeArguments)
            {
                if (ta.HasConstructorConstraint || ta.HasReferenceTypeConstraint || ta.HasValueTypeConstraint || ta.HasUnmanagedTypeConstraint || ta.HasNotNullConstraint || ta.ConstraintTypes.Length > 0)
                {
                    yield return SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(ta.Name), SyntaxFactory.SeparatedList(GetTypeParameterConstraint(ta)));
                }
            }
        }

        private static IEnumerable<TypeParameterConstraintSyntax> GetTypeParameterConstraint(ITypeParameterSymbol symbol)
        {
            if (symbol.HasUnmanagedTypeConstraint)
            {
                yield return SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName("unmanaged"));
            }
            if (symbol.HasNotNullConstraint)
            {
                yield return SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName("notnull"));
            }
            if (symbol.HasReferenceTypeConstraint)
            {
                if (symbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                {
                    yield return SyntaxFactory.ClassOrStructConstraint(
                        SyntaxKind.ClassConstraint,
                        SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                        SyntaxFactory.Token(SyntaxKind.QuestionToken));
                }
                else
                {
                    yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);
                }
            }
            if (symbol.HasValueTypeConstraint)
            {
                yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint);
            }
            if (symbol.ConstraintTypes.Length > 0)
            {
                var nullableAnnotations = symbol.ConstraintNullableAnnotations;
                for (int i = 0; i < symbol.ConstraintTypes.Length; i++)
                {
                    var constraintType = symbol.ConstraintTypes[i];
                    if (i < nullableAnnotations.Length)
                    {
                        constraintType = constraintType.WithNullableAnnotation(nullableAnnotations[i]);
                    }
                    yield return SyntaxFactory.TypeConstraint(GetTypeSyntax(constraintType));
                }
            }
            if (symbol.HasConstructorConstraint)
            {
                yield return SyntaxFactory.ConstructorConstraint();
            }
        }

        private BaseListSyntax GetBaseTypeList(INamedTypeSymbol symbol)
        {
            IReadOnlyList<INamedTypeSymbol> baseTypeList;
            if (symbol.TypeKind != TypeKind.Class || symbol.BaseType == null || symbol.BaseType.GetDocumentationCommentId() == "T:System.Object")
            {
                baseTypeList = symbol.AllInterfaces;
            }
            else
            {
                baseTypeList = new[] { symbol.BaseType }.Concat(symbol.AllInterfaces).ToList();
            }
            if (baseTypeList.Count == 0)
            {
                return null;
            }
            return SyntaxFactory.BaseList(
                SyntaxFactory.SeparatedList<BaseTypeSyntax>(
                    from t in baseTypeList
                    select SyntaxFactory.SimpleBaseType(GetTypeSyntax(t))));
        }

        private BaseListSyntax GetEnumBaseTypeList(INamedTypeSymbol symbol)
        {
            var underlyingType = symbol.EnumUnderlyingType;
            if (underlyingType.GetDocumentationCommentId() == "T:System.Int32")
            {
                return null;
            }
            return SyntaxFactory.BaseList(
                SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                    SyntaxFactory.SimpleBaseType(
                        GetTypeSyntax(underlyingType))));
        }

        private static TypeParameterListSyntax GetTypeParameters(INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(
                    from ITypeParameterSymbol t in symbol.TypeArguments
                    select SyntaxFactory.TypeParameter(
                        new SyntaxList<AttributeListSyntax>(),
                        GetVarianceToken(t),
                        SyntaxFactory.Identifier(t.Name))));
        }

        private static TypeParameterListSyntax GetTypeParameters(IMethodSymbol symbol)
        {
            if (symbol.TypeArguments.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(
                    from ITypeParameterSymbol t in symbol.TypeArguments
                    select SyntaxFactory.TypeParameter(
                        new SyntaxList<AttributeListSyntax>(),
                        GetVarianceToken(t),
                        SyntaxFactory.Identifier(t.Name))));
        }

        private static SyntaxToken GetVarianceToken(ITypeParameterSymbol t)
        {
            if (t.Variance == VarianceKind.In)
                return SyntaxFactory.Token(SyntaxKind.InKeyword);
            if (t.Variance == VarianceKind.Out)
                return SyntaxFactory.Token(SyntaxKind.OutKeyword);
            return new SyntaxToken();
        }

        private static IEnumerable<SyntaxToken> GetTypeModifiers(INamedTypeSymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.Public:
                    yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                default:
                    break;
            }
            if (symbol.TypeKind == TypeKind.Class)
            {
                if (symbol.IsStatic)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
                }
                else
                {
                    if (symbol.IsAbstract)
                    {
                        yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
                    }
                    if (symbol.IsSealed)
                    {
                        yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
                    }
                }
            }
            else if (symbol.TypeKind == TypeKind.Struct)
            {
                if (symbol.IsRefLikeType)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                }
                if (symbol.IsReadOnly)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
                }
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IMethodSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            }
            if (symbol.IsReadOnly)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
            }
            if (symbol.IsAbstract && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsVirtual && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IEventSymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                    case Accessibility.Private:
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            }
            if (symbol.IsAbstract && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsVirtual && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IPropertySymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                        break;
                    case Accessibility.Public:
                        yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                        break;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                    case Accessibility.Private:
                    default:
                        break;
                }
            }
            if (symbol.IsStatic)
            {
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            }
            if (symbol.IsRequired)
            {
                yield return SyntaxFactory.Token(SyntaxKind.RequiredKeyword);
            }
            if (symbol.IsAbstract && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                yield return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            }
            if (symbol.IsVirtual && (symbol.ContainingType.TypeKind != TypeKind.Interface || symbol.IsStatic))
            {
                yield return SyntaxFactory.Token(SyntaxKind.VirtualKeyword);
            }
            if (symbol.IsOverride)
            {
                yield return SyntaxFactory.Token(SyntaxKind.OverrideKeyword);
            }
            if (symbol.IsSealed)
            {
                yield return SyntaxFactory.Token(SyntaxKind.SealedKeyword);
            }
        }

        private static IEnumerable<SyntaxToken> GetMemberModifiers(IFieldSymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.Public:
                    yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                case Accessibility.Private:
                default:
                    break;
            }
            if (symbol.IsConst)
            {
                yield return SyntaxFactory.Token(SyntaxKind.ConstKeyword);
            }
            else
            {
                if (symbol.IsStatic)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
                }
                if (symbol.IsReadOnly)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
                }
                if (symbol.IsRequired)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.RequiredKeyword);
                }
                if (symbol.IsVolatile)
                {
                    yield return SyntaxFactory.Token(SyntaxKind.VolatileKeyword);
                }
            }
        }

        private static SyntaxToken? GetOperatorToken(string operatorName)
        {
            switch (GetUncheckedOperatorName(operatorName))
            {
                // unary
                case "op_UnaryPlus": return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "op_UnaryNegation": return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "op_LogicalNot": return SyntaxFactory.Token(SyntaxKind.ExclamationToken);
                case "op_OnesComplement": return SyntaxFactory.Token(SyntaxKind.TildeToken);
                case "op_Increment": return SyntaxFactory.Token(SyntaxKind.PlusPlusToken);
                case "op_Decrement": return SyntaxFactory.Token(SyntaxKind.MinusMinusToken);
                case "op_True": return SyntaxFactory.Token(SyntaxKind.TrueKeyword);
                case "op_False": return SyntaxFactory.Token(SyntaxKind.FalseKeyword);
                // binary
                case "op_Addition": return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "op_Subtraction": return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "op_Multiply": return SyntaxFactory.Token(SyntaxKind.AsteriskToken);
                case "op_Division": return SyntaxFactory.Token(SyntaxKind.SlashToken);
                case "op_Modulus": return SyntaxFactory.Token(SyntaxKind.PercentToken);
                case "op_BitwiseAnd": return SyntaxFactory.Token(SyntaxKind.AmpersandToken);
                case "op_BitwiseOr": return SyntaxFactory.Token(SyntaxKind.BarToken);
                case "op_ExclusiveOr": return SyntaxFactory.Token(SyntaxKind.CaretToken);
                case "op_RightShift": return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanToken);
                case "op_LeftShift": return SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken);
                case "op_UnsignedRightShift": return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
                case "op_AdditionAssignment": return SyntaxFactory.Token(SyntaxKind.PlusEqualsToken);
                case "op_SubtractionAssignment": return SyntaxFactory.Token(SyntaxKind.MinusEqualsToken);
                case "op_MultiplicationAssignment": return SyntaxFactory.Token(SyntaxKind.AsteriskEqualsToken);
                case "op_DivisionAssignment": return SyntaxFactory.Token(SyntaxKind.SlashEqualsToken);
                case "op_ModulusAssignment": return SyntaxFactory.Token(SyntaxKind.PercentEqualsToken);
                case "op_BitwiseAndAssignment": return SyntaxFactory.Token(SyntaxKind.AmpersandEqualsToken);
                case "op_BitwiseOrAssignment": return SyntaxFactory.Token(SyntaxKind.BarEqualsToken);
                case "op_ExclusiveOrAssignment": return SyntaxFactory.Token(SyntaxKind.CaretEqualsToken);
                case "op_LeftShiftAssignment": return SyntaxFactory.Token(SyntaxKind.LessThanLessThanEqualsToken);
                case "op_RightShiftAssignment": return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanEqualsToken);
                case "op_UnsignedRightShiftAssignment": return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken);
                // comparision
                case "op_Equality": return SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken);
                case "op_Inequality": return SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken);
                case "op_GreaterThan": return SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
                case "op_LessThan": return SyntaxFactory.Token(SyntaxKind.LessThanToken);
                case "op_GreaterThanOrEqual": return SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken);
                case "op_LessThanOrEqual": return SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken);
                // conversion
                case "op_Implicit": return SyntaxFactory.Token(SyntaxKind.ImplicitKeyword);
                case "op_Explicit": return SyntaxFactory.Token(SyntaxKind.ExplicitKeyword);
                // not supported:
                //case "op_Assign":
                default: return null;
            }
        }

        private static bool IsCheckedOperator(string operatorName)
        {
            return operatorName.StartsWith("op_Checked", StringComparison.Ordinal);
        }

        private static string GetUncheckedOperatorName(string operatorName)
        {
            if (!IsCheckedOperator(operatorName))
            {
                return operatorName;
            }

            return "op_" + operatorName.Substring("op_Checked".Length);
        }

        private static IEnumerable<AccessorDeclarationSyntax> GetPropertyAccessors(IPropertySymbol propertySymbol, IFilterVisitor filterVisitor)
        {
            var getAccessor = GetPropertyAccessorCore(propertySymbol, propertySymbol.GetMethod, SyntaxKind.GetAccessorDeclaration, SyntaxKind.GetKeyword, filterVisitor);
            if (getAccessor != null)
            {
                yield return getAccessor;
            }
            var setAccessor = GetPropertyAccessorCore(propertySymbol, propertySymbol.SetMethod, SyntaxKind.SetAccessorDeclaration, SyntaxKind.SetKeyword, filterVisitor);
            if (setAccessor != null)
            {
                yield return setAccessor;
            }
        }

        private static AccessorDeclarationSyntax GetPropertyAccessorCore(
            IPropertySymbol propertySymbol, IMethodSymbol methodSymbol,
            SyntaxKind kind, SyntaxKind keyword, IFilterVisitor filterVisitor)
        {
            if (methodSymbol == null)
            {
                return null;
            }
            switch (methodSymbol.DeclaredAccessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    if (propertySymbol.DeclaredAccessibility == Accessibility.Protected ||
                        propertySymbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                    {
                        return SyntaxFactory.AccessorDeclaration(kind,
                            GetAttributes(methodSymbol, filterVisitor),
                            new SyntaxTokenList(),
                            SyntaxFactory.Token(GetAccessorKeyword(methodSymbol, keyword)),
                            (BlockSyntax)null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                    else
                    {
                        return SyntaxFactory.AccessorDeclaration(
                            kind,
                            GetAttributes(methodSymbol, filterVisitor),
                            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)),
                            SyntaxFactory.Token(GetAccessorKeyword(methodSymbol, keyword)),
                            (BlockSyntax)null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                case Accessibility.Public:
                    return SyntaxFactory.AccessorDeclaration(kind,
                        GetAttributes(methodSymbol, filterVisitor),
                        new SyntaxTokenList(),
                        SyntaxFactory.Token(GetAccessorKeyword(methodSymbol, keyword)),
                        (BlockSyntax)null,
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                default:
                    if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
                    {
                        return SyntaxFactory.AccessorDeclaration(kind,
                            GetAttributes(methodSymbol, filterVisitor),
                            new SyntaxTokenList(),
                            SyntaxFactory.Token(GetAccessorKeyword(methodSymbol, keyword)),
                            (BlockSyntax)null,
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                    return null;
            }
        }

        private static string RemoveBraces(string text)
        {
            return BracesRegex.Replace(text, string.Empty);
        }

        private static string ReplaceRecordKeywordIfNeeded(INamedTypeSymbol symbol, string text)
        {
            if (!symbol.IsRecord)
            {
                return text;
            }

            return symbol.TypeKind switch
            {
                TypeKind.Class => new Regex(@"\bclass\b", RegexOptions.Compiled).Replace(text, "record", 1),
                TypeKind.Struct => new Regex(@"\bstruct\b", RegexOptions.Compiled).Replace(text, "record struct", 1),
                _ => text
            };
        }

        private static ParameterListSyntax GetPrimaryConstructorParameters(INamedTypeSymbol symbol, IFilterVisitor filterVisitor)
        {
            var primaryConstructor = symbol.InstanceConstructors.FirstOrDefault(
                constructor => !constructor.IsImplicitlyDeclared
                               && constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax));
            if (primaryConstructor == null || primaryConstructor.Parameters.Length == 0)
            {
                return null;
            }

            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    primaryConstructor.Parameters.Select(parameter => GetParameter(parameter, filterVisitor))));
        }

        private static string AppendAllowsRefStructConstraints(string declaration, IEnumerable<ITypeParameterSymbol> typeParameters)
        {
            var result = declaration;
            foreach (var typeParameter in typeParameters)
            {
                if (!typeParameter.AllowsRefLikeType)
                {
                    continue;
                }

                result = AppendAllowsRefStructConstraint(result, typeParameter.Name);
            }

            return result;
        }

        private static string AppendAllowsRefStructConstraint(string declaration, string typeParameterName)
        {
            var whereFragment = $"where {typeParameterName} :";
            var whereIndex = declaration.IndexOf(whereFragment, StringComparison.Ordinal);
            if (whereIndex >= 0)
            {
                var clauseStart = whereIndex + whereFragment.Length;
                var nextWhereIndex = declaration.IndexOf(" where ", clauseStart, StringComparison.Ordinal);
                var clauseEnd = nextWhereIndex >= 0
                    ? nextWhereIndex
                    : declaration.EndsWith(";", StringComparison.Ordinal) ? declaration.Length - 1 : declaration.Length;

                return declaration.Insert(clauseEnd, ", allows ref struct");
            }

            var constraintClause = $" where {typeParameterName} : allows ref struct";
            if (declaration.EndsWith(";", StringComparison.Ordinal))
            {
                return declaration.Insert(declaration.Length - 1, constraintClause);
            }

            return declaration + constraintClause;
        }

        private static string InsertCheckedKeywordInOperatorSyntax(string syntax)
        {
            var operatorKeyword = "operator ";
            var operatorKeywordIndex = syntax.IndexOf(operatorKeyword, StringComparison.Ordinal);
            if (operatorKeywordIndex < 0)
            {
                return syntax;
            }

            return syntax.Insert(operatorKeywordIndex + operatorKeyword.Length, "checked ");
        }

        private static TypeSyntax GetMethodReturnTypeSyntax(IMethodSymbol symbol)
        {
            var returnType = symbol.ReturnType.WithNullableAnnotation(symbol.ReturnNullableAnnotation);
            var returnTypeName = GetTypeDisplayName(returnType);
            if (symbol.ReturnsByRefReadonly)
            {
                return SyntaxFactory.ParseTypeName($"ref readonly {returnTypeName}");
            }

            if (symbol.ReturnsByRef)
            {
                return SyntaxFactory.ParseTypeName($"ref {returnTypeName}");
            }

            return SyntaxFactory.ParseTypeName(returnTypeName);
        }

        private static SyntaxKind GetAccessorKeyword(IMethodSymbol methodSymbol, SyntaxKind fallbackKeyword)
        {
            if (fallbackKeyword == SyntaxKind.SetKeyword && methodSymbol.IsInitOnly)
            {
                return SyntaxKind.InitKeyword;
            }

            return fallbackKeyword;
        }

        private static TypeSyntax GetFieldTypeSyntax(IFieldSymbol symbol)
        {
            var fieldTypeName = GetTypeDisplayName(symbol.Type.WithNullableAnnotation(symbol.NullableAnnotation));
            if (symbol.RefKind == RefKind.Ref)
            {
                return SyntaxFactory.ParseTypeName($"ref {fieldTypeName}");
            }

            if (symbol.RefKind == RefKind.RefReadOnly)
            {
                return SyntaxFactory.ParseTypeName($"ref readonly {fieldTypeName}");
            }

            return SyntaxFactory.ParseTypeName(fieldTypeName);
        }

        private static TypeSyntax GetTypeSyntax(ITypeSymbol type, NullableAnnotation nullableAnnotation)
        {
            return GetTypeSyntax(type.WithNullableAnnotation(nullableAnnotation));
        }

        private static TypeSyntax GetTypeSyntax(ITypeSymbol type)
        {
            var name = GetTypeDisplayName(type);
            return SyntaxFactory.ParseTypeName(name);
        }

        private static string GetTypeDisplayName(ITypeSymbol type)
        {
            return type.ToDisplayString(CSharpTypeDisplayFormat);
        }

        private static string GetVisiblity(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return "protected";
                case Accessibility.Public:
                    return "public";
                default:
                    return null;
            }
        }

        #endregion
    }
}
