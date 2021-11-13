// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;

namespace Lunet.Helpers;

public static class ExceptionUtil
{
    public static string GetReason(this Exception exception)
    {
        var stringBuilder = new StringBuilder();
        bool requireSeparator = true;
        while (exception != null)
        {
            if (!requireSeparator)
            {
                stringBuilder.Append(".");
            }
            stringBuilder.Append(" ");
            var message = exception.Message;
            message = message.Trim();
            stringBuilder.Append(message);
            requireSeparator = message.EndsWith(".");
            exception = exception.InnerException;
        }
        return stringBuilder.ToString();
    }
}