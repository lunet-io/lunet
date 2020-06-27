using System;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    public class PathCollection : ScriptArray
    {
        public PathCollection()
        {
            ScriptObject.Import("add", (Action<object>)AddItem);
            ScriptObject.Import("remove", (Action<object>)RemoveItem);
        }

        private void RemoveItem(object item)
        {
            if (item == null) return;

            if (item is string str)
            {
                if (!UPath.TryParse(str, out var path))
                {
                    throw new ArgumentException($"Invalid path `{str}`. The path is malformed.", nameof(item));
                }

                if (!path.IsAbsolute)
                {
                    throw new ArgumentException($"Invalid path `{str}`. Expecting an absolute path.", nameof(item));
                }

                Remove(item);
            }
            else if (item is ScriptArray array)
            {
                foreach (var itemToAdd in array)
                {
                    RemoveItem(itemToAdd);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid path. Expecting a string instead of `{item.GetType().FullName}`.", nameof(item));
            }
        }

        private void AddItem(object item)
        {
            if (item == null) return;

            if (item is string str)
            {
                if (!UPath.TryParse(str, out var path))
                {
                    throw new ArgumentException($"Invalid path `{str}`. The path is malformed.", nameof(item));
                }

                if (!path.IsAbsolute)
                {
                    throw new ArgumentException($"Invalid path `{str}`. Expecting an absolute path.", nameof(item));
                }

                this.Add(item);
            }
            else if (item is ScriptArray array)
            {
                foreach (var itemToAdd in array)
                {
                    AddItem(itemToAdd);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid path. Expecting a string instead of `{item.GetType().FullName}`.", nameof(item));
            }
        }
    }
}