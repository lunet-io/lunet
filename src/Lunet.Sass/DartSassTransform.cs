// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using Lunet.Core;
using Lunet.Sass.DartSass;
using Zio;
using Zio.FileSystems;

namespace Lunet.Sass;

public class DartSassTransform
{
    public static string Minify(string content)
    {
        // We are using the dart compiler to minify the content
        // It's not ideal/efficient, but it is probably better than NUglify current limitations
        var dartSassCompiler = new DartSassCompiler();
        var result = dartSassCompiler.CompileString(content, configureOptions: options =>
        {
            options.Style = OutputStyle.Compressed;
        });
        return CleanContent(result.Css);
    }
    
    public static void Convert(ContentObject file, List<DirectoryEntry> includePaths, SiteObject site)
    {
        var contentType = file.ContentType;

        var content = file.GetOrLoadContent();

        var cacheDirectoryEntry = site.CacheMetaFileSystem.ResolvePath("/");

        var cacheDirectory = cacheDirectoryEntry.FileSystem.ConvertPathToInternal(cacheDirectoryEntry.Path);
        

        var resolvedIncludePaths = new List<string>();
        foreach (var includePath in includePaths)
        {
            var resolvedIncludePath = includePath.FileSystem.ResolvePath(includePath.Path);
            resolvedIncludePaths.Add(resolvedIncludePath.FileSystem.ConvertPathToInternal(resolvedIncludePath.Path));
        }

        var dartSassCompiler = new DartSassCompiler(cacheDirectory: cacheDirectory);
        var sourceFileSystem = file.SourceFile.FileSystem;
        DartSassResult result;

        if (sourceFileSystem is PhysicalFileSystem)
        {
            var resolvedFilePath = sourceFileSystem.ResolvePath(file.SourceFile.AbsolutePath);
            var resolvedFileInternalPath = resolvedFilePath.FileSystem.ConvertPathToInternal(resolvedFilePath.Path);
            result = dartSassCompiler.CompileFile(resolvedFileInternalPath, configureOptions: options =>
            {
                options.LoadPaths = resolvedIncludePaths;
            });
        }
        else
        {
            result = dartSassCompiler.CompileString(content, configureOptions: options =>
            {
                options.LoadPaths = resolvedIncludePaths;
            });
        }

        file.Content = CleanContent(result.Css);
        file.ChangeContentType(ContentType.Css);
    }

    private static string CleanContent(string? content)
    {
        // Check if content is starting with a UTF marker and remove it
        // Remove Unicode BOM U+FEFF if present
        if (content is not null && content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content.TrimStart('\uFEFF');
        }

        return content ?? string.Empty;
    }
}
