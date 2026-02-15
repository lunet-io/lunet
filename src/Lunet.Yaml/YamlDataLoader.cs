// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using Lunet.Datas;
using Zio;

namespace Lunet.Yaml;

public class YamlDataLoader : IDataLoader
{
    public bool CanHandle(string fileExtension)
    {
        var fileExt = fileExtension.ToLowerInvariant();
        return fileExt == ".yml" || fileExt == ".yaml";
    }

    public object? Load(FileEntry file)
    {
        var text = file.ReadAllText();
        return YamlUtil.FromText(text);
    }
}
