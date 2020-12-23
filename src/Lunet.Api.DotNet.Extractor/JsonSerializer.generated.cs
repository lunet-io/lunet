
using System;
namespace Lunet.Api.DotNet.Extractor
{
    public partial class JsonSerializer 
    {
        static JsonSerializer() 
        {
            _serializers.Add(typeof(Microsoft.DocAsCode.Metadata.ManagedReference.MetadataItem), SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_MetadataItem);
            _serializers.Add(typeof(Microsoft.DocAsCode.Metadata.ManagedReference.ReferenceItem), SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_ReferenceItem);
            _serializers.Add(typeof(Microsoft.DocAsCode.Metadata.ManagedReference.LinkItem), SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_LinkItem);
            _serializers.Add(typeof(Microsoft.DocAsCode.Metadata.ManagedReference.SyntaxDetail), SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_SyntaxDetail);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.AdditionalNotes), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_AdditionalNotes);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.ApiParameter), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ApiParameter);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.ArgumentInfo), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ArgumentInfo);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.AttributeInfo), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_AttributeInfo);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.ExceptionInfo), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ExceptionInfo);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.ItemViewModel), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ItemViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.LinkInfo), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_LinkInfo);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.NamedArgumentInfo), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_NamedArgumentInfo);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.PageViewModel), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_PageViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.ManagedReference.SyntaxDetailViewModel), SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_SyntaxDetailViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.Common.ReferenceViewModel), SerializeMicrosoft_DocAsCode_DataContracts_Common_ReferenceViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.Common.SourceDetail), SerializeMicrosoft_DocAsCode_DataContracts_Common_SourceDetail);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.Common.SpecViewModel), SerializeMicrosoft_DocAsCode_DataContracts_Common_SpecViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.Common.TocItemViewModel), SerializeMicrosoft_DocAsCode_DataContracts_Common_TocItemViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.DataContracts.Common.TocRootViewModel), SerializeMicrosoft_DocAsCode_DataContracts_Common_TocRootViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.Common.Git.GitDetail), SerializeMicrosoft_DocAsCode_Common_Git_GitDetail);
            _serializers.Add(typeof(Lunet.Api.DotNet.Extractor.AssemblyViewModel), SerializeLunet_Api_DotNet_Extractor_AssemblyViewModel);
            _serializers.Add(typeof(Microsoft.DocAsCode.Common.HtmlLogListener.ReportItem), SerializeMicrosoft_DocAsCode_Common_HtmlLogListener_ReportItem);
            _serializers.Add(typeof(Microsoft.DocAsCode.Common.ReportLogListener.ReportItem), SerializeMicrosoft_DocAsCode_Common_ReportLogListener_ReportItem);
        }
        private static void SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_MetadataItem(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Metadata.ManagedReference.MetadataItem)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("isEii", value.IsExplicitInterfaceImplementation);
            serializer.WriteJsonKeyValue("isExtensionMethod", value.IsExtensionMethod);
            serializer.WriteJsonKeyValue("id", value.Name);
            serializer.WriteJsonKeyValue("commentId", value.CommentId);
            serializer.WriteJsonKeyValue("language", value.Language);
            serializer.WriteJsonKeyValue("name", value.DisplayNames);
            serializer.WriteJsonKeyValue("nameWithType", value.DisplayNamesWithType);
            serializer.WriteJsonKeyValue("qualifiedName", value.DisplayQualifiedNames);
            serializer.WriteJsonKeyValue("parent", value.Parent);
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("assemblies", value.AssemblyNameList);
            serializer.WriteJsonKeyValue("namespace", value.NamespaceName);
            serializer.WriteJsonKeyValue("source", value.Source);
            serializer.WriteJsonKeyValue("documentation", value.Documentation);
            serializer.WriteJsonKeyValue("summary", value.Summary);
            serializer.WriteJsonKeyValue("remarks", value.Remarks);
            serializer.WriteJsonKeyValue("example", value.Examples);
            serializer.WriteJsonKeyValue("syntax", value.Syntax);
            serializer.WriteJsonKeyValue("overload", value.Overload);
            serializer.WriteJsonKeyValue("overridden", value.Overridden);
            serializer.WriteJsonKeyValue("exceptions", value.Exceptions);
            serializer.WriteJsonKeyValue("see", value.Sees);
            serializer.WriteJsonKeyValue("seealso", value.SeeAlsos);
            serializer.WriteJsonKeyValue("inheritance", value.Inheritance);
            serializer.WriteJsonKeyValue("derivedClasses", value.DerivedClasses);
            serializer.WriteJsonKeyValue("implements", value.Implements);
            serializer.WriteJsonKeyValue("inheritedMembers", value.InheritedMembers);
            serializer.WriteJsonKeyValue("extensionMethods", value.ExtensionMethods);
            serializer.WriteJsonKeyValue("attributes", value.Attributes);
            serializer.WriteJsonKeyValue("modifiers", value.Modifiers);
            serializer.WriteJsonKeyValue("items", value.Items);
            serializer.WriteJsonKeyValue("references", value.References);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_ReferenceItem(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Metadata.ManagedReference.ReferenceItem)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("name", value.Parts);
            serializer.WriteJsonKeyValue("isDefinition", value.IsDefinition);
            serializer.WriteJsonKeyValue("definition", value.Definition);
            serializer.WriteJsonKeyValue("parent", value.Parent);
            serializer.WriteJsonKeyValue("commentId", value.CommentId);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_LinkItem(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Metadata.ManagedReference.LinkItem)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("id", value.Name);
            serializer.WriteJsonKeyValue("name", value.DisplayName);
            serializer.WriteJsonKeyValue("nameWithType", value.DisplayNamesWithType);
            serializer.WriteJsonKeyValue("qualifiedName", value.DisplayQualifiedNames);
            serializer.WriteJsonKeyValue("isExternal", value.IsExternalPath);
            serializer.WriteJsonKeyValue("href", value.Href);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_Metadata_ManagedReference_SyntaxDetail(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Metadata.ManagedReference.SyntaxDetail)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("content", value.Content);
            serializer.WriteJsonKeyValue("parameters", value.Parameters);
            serializer.WriteJsonKeyValue("typeParameters", value.TypeParameters);
            serializer.WriteJsonKeyValue("return", value.Return);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_AdditionalNotes(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.AdditionalNotes)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("caller", value.Caller);
            serializer.WriteJsonKeyValue("implementer", value.Implementer);
            serializer.WriteJsonKeyValue("inheritor", value.Inheritor);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ApiParameter(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.ApiParameter)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("id", value.Name);
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("description", value.Description);
            serializer.WriteJsonKeyValue("attributes", value.Attributes);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ArgumentInfo(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.ArgumentInfo)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("value", value.Value);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_AttributeInfo(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.AttributeInfo)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("ctor", value.Constructor);
            serializer.WriteJsonKeyValue("arguments", value.Arguments);
            serializer.WriteJsonKeyValue("namedArguments", value.NamedArguments);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ExceptionInfo(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.ExceptionInfo)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("commentId", value.CommentId);
            serializer.WriteJsonKeyValue("description", value.Description);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_ItemViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.ItemViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("uid", value.Uid);
            serializer.WriteJsonKeyValue("commentId", value.CommentId);
            serializer.WriteJsonKeyValue("id", value.Id);
            serializer.WriteJsonKeyValue("isEii", value.IsExplicitInterfaceImplementation);
            serializer.WriteJsonKeyValue("isExtensionMethod", value.IsExtensionMethod);
            serializer.WriteJsonKeyValue("parent", value.Parent);
            serializer.WriteJsonKeyValue("children", value.Children);
            serializer.WriteJsonKeyValue("href", value.Href);
            serializer.WriteJsonKeyValue("langs", value.SupportedLanguages);
            serializer.WriteJsonKeyValue("name", value.Name);
            serializer.WriteJsonKeyValue("nameWithType", value.NameWithType);
            serializer.WriteJsonKeyValue("fullName", value.FullName);
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("source", value.Source);
            serializer.WriteJsonKeyValue("documentation", value.Documentation);
            serializer.WriteJsonKeyValue("assemblies", value.AssemblyNameList);
            serializer.WriteJsonKeyValue("namespace", value.NamespaceName);
            serializer.WriteJsonKeyValue("summary", value.Summary);
            serializer.WriteJsonKeyValue("additionalNotes", value.AdditionalNotes);
            serializer.WriteJsonKeyValue("remarks", value.Remarks);
            serializer.WriteJsonKeyValue("example", value.Examples);
            serializer.WriteJsonKeyValue("syntax", value.Syntax);
            serializer.WriteJsonKeyValue("overridden", value.Overridden);
            serializer.WriteJsonKeyValue("overload", value.Overload);
            serializer.WriteJsonKeyValue("exceptions", value.Exceptions);
            serializer.WriteJsonKeyValue("seealso", value.SeeAlsos);
            serializer.WriteJsonKeyValue("see", value.Sees);
            serializer.WriteJsonKeyValue("inheritance", value.Inheritance);
            serializer.WriteJsonKeyValue("derivedClasses", value.DerivedClasses);
            serializer.WriteJsonKeyValue("implements", value.Implements);
            serializer.WriteJsonKeyValue("inheritedMembers", value.InheritedMembers);
            serializer.WriteJsonKeyValue("extensionMethods", value.ExtensionMethods);
            serializer.WriteJsonKeyValue("conceptual", value.Conceptual);
            serializer.WriteJsonKeyValue("platform", value.Platform);
            serializer.WriteJsonKeyValue("attributes", value.Attributes);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_LinkInfo(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.LinkInfo)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("linkType", value.LinkType);
            serializer.WriteJsonKeyValue("linkId", value.LinkId);
            serializer.WriteJsonKeyValue("commentId", value.CommentId);
            serializer.WriteJsonKeyValue("altText", value.AltText);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_NamedArgumentInfo(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.NamedArgumentInfo)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("name", value.Name);
            serializer.WriteJsonKeyValue("type", value.Type);
            serializer.WriteJsonKeyValue("value", value.Value);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_PageViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.PageViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("items", value.Items);
            serializer.WriteJsonKeyValue("references", value.References);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_ManagedReference_SyntaxDetailViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.ManagedReference.SyntaxDetailViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("content", value.Content);
            serializer.WriteJsonKeyValue("parameters", value.Parameters);
            serializer.WriteJsonKeyValue("typeParameters", value.TypeParameters);
            serializer.WriteJsonKeyValue("return", value.Return);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_Common_ReferenceViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.Common.ReferenceViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("uid", value.Uid);
            serializer.WriteJsonKeyValue("commentId", value.CommentId);
            serializer.WriteJsonKeyValue("parent", value.Parent);
            serializer.WriteJsonKeyValue("definition", value.Definition);
            serializer.WriteJsonKeyValue("isExternal", value.IsExternal);
            serializer.WriteJsonKeyValue("href", value.Href);
            serializer.WriteJsonKeyValue("name", value.Name);
            serializer.WriteJsonKeyValue("nameWithType", value.NameWithType);
            serializer.WriteJsonKeyValue("fullName", value.FullName);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_Common_SourceDetail(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.Common.SourceDetail)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("remote", value.Remote);
            serializer.WriteJsonKeyValue("base", value.BasePath);
            serializer.WriteJsonKeyValue("id", value.Name);
            serializer.WriteJsonKeyValue("href", value.Href);
            serializer.WriteJsonKeyValue("path", value.Path);
            serializer.WriteJsonKeyValue("startLine", value.StartLine);
            serializer.WriteJsonKeyValue("endLine", value.EndLine);
            serializer.WriteJsonKeyValue("content", value.Content);
            serializer.WriteJsonKeyValue("isExternal", value.IsExternalPath);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_Common_SpecViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.Common.SpecViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("uid", value.Uid);
            serializer.WriteJsonKeyValue("name", value.Name);
            serializer.WriteJsonKeyValue("nameWithType", value.NameWithType);
            serializer.WriteJsonKeyValue("fullName", value.FullName);
            serializer.WriteJsonKeyValue("isExternal", value.IsExternal);
            serializer.WriteJsonKeyValue("href", value.Href);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_Common_TocItemViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.Common.TocItemViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("uid", value.Uid);
            serializer.WriteJsonKeyValue("name", value.Name);
            serializer.WriteJsonKeyValue("displayName", value.DisplayName);
            serializer.WriteJsonKeyValue("href", value.Href);
            serializer.WriteJsonKeyValue("originalHref", value.OriginalHref);
            serializer.WriteJsonKeyValue("tocHref", value.TocHref);
            serializer.WriteJsonKeyValue("originalTocHref", value.OriginalTocHref);
            serializer.WriteJsonKeyValue("topicHref", value.TopicHref);
            serializer.WriteJsonKeyValue("originalTopicHref", value.OriginalTopicHref);
            serializer.WriteJsonKeyValue("includedFrom", value.IncludedFrom);
            serializer.WriteJsonKeyValue("homepage", value.Homepage);
            serializer.WriteJsonKeyValue("originallHomepage", value.OriginalHomepage);
            serializer.WriteJsonKeyValue("homepageUid", value.HomepageUid);
            serializer.WriteJsonKeyValue("topicUid", value.TopicUid);
            serializer.WriteJsonKeyValue("items", value.Items);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_DataContracts_Common_TocRootViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.DataContracts.Common.TocRootViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("items", value.Items);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_Common_Git_GitDetail(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Common.Git.GitDetail)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("path", value.RelativePath);
            serializer.WriteJsonKeyValue("branch", value.RemoteBranch);
            serializer.WriteJsonKeyValue("repo", value.RemoteRepositoryUrl);
            serializer.EndObject();
        }
        private static void SerializeLunet_Api_DotNet_Extractor_AssemblyViewModel(JsonSerializer serializer, object valueObj)
        {
            var value = (Lunet.Api.DotNet.Extractor.AssemblyViewModel)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("name", value.Name);
            serializer.WriteJsonKeyValue("items", value.Items);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_Common_HtmlLogListener_ReportItem(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Common.HtmlLogListener.ReportItem)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("message", value.Message);
            serializer.WriteJsonKeyValue("source", value.Source);
            serializer.WriteJsonKeyValue("file", value.File);
            serializer.WriteJsonKeyValue("line", value.Line);
            serializer.WriteJsonKeyValue("date_time", value.DateTime);
            serializer.WriteJsonKeyValue("message_severity", value.Severity);
            serializer.EndObject();
        }
        private static void SerializeMicrosoft_DocAsCode_Common_ReportLogListener_ReportItem(JsonSerializer serializer, object valueObj)
        {
            var value = (Microsoft.DocAsCode.Common.ReportLogListener.ReportItem)valueObj;
            serializer.StartObject();
            serializer.WriteJsonKeyValue("message", value.Message);
            serializer.WriteJsonKeyValue("source", value.Source);
            serializer.WriteJsonKeyValue("file", value.File);
            serializer.WriteJsonKeyValue("line", value.Line);
            serializer.WriteJsonKeyValue("date_time", value.DateTime);
            serializer.WriteJsonKeyValue("message_severity", value.Severity);
            serializer.WriteJsonKeyValue("code", value.Code);
            serializer.WriteJsonKeyValue("correlation_id", value.CorrelationId);
            serializer.EndObject();
        }
    }
}
