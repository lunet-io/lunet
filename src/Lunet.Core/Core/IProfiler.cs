// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace Lunet.Core
{
    public interface IProfiler
    {
        void BeginEvent(string name, string data, ProfilerColor color);

        void EndEvent();
    }

    /// <summary>
    /// A color for the profiler.
    /// </summary>
    public readonly struct ProfilerColor : IEquatable<ProfilerColor>
    {
        /// <summary>
        /// The default color.
        /// </summary>
        public static readonly ProfilerColor Default = new ProfilerColor(0xFFFF_FFFF);

        /// <summary>
        /// Creates a new profiler color.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        public ProfilerColor(byte r, byte g, byte b)
        {
            Value = (uint)((r << 24) | (g << 16) | (b << 8) | 0xFF);
        }

        /// <summary>
        /// Creates a new profiler color.
        /// </summary>
        /// <param name="value">The color value.</param>
        public ProfilerColor(uint value)
        {
            Value = value;
        }

        /// <summary>
        /// The color value.
        /// </summary>
        public readonly uint Value;

        /// <inheritdoc/>
        public bool Equals(ProfilerColor other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ProfilerColor other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)Value;
        }

        public static bool operator ==(ProfilerColor left, ProfilerColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProfilerColor left, ProfilerColor right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"#{Value:X8}";
        }
    }


}