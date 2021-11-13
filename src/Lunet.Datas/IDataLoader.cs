// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;
using Zio;

namespace Lunet.Datas;

public interface IDataLoader
{
    bool CanHandle(string fileExtension);

    object Load(FileEntry file);
}