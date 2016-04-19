// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license. 
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Core
{
    /// <summary>
    /// Exception used to log unrecoverable errors.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class LunetException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LunetException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LunetException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LunetException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public LunetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}