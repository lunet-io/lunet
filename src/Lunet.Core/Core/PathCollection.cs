using System;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    /// <summary>
    /// Standard collection object with two attached method clear, add and remove.
    /// </summary>
    public class ScriptCollection : ScriptArray
    {
        public ScriptCollection()
        {
            ScriptObject.Import("clear", (Action)Clear);
            ScriptObject.Import("add", (Action<object>) AddItem);
            ScriptObject.Import("remove", (Action<object>) RemoveItem);
        }

        protected void RemoveItem(object item)
        {
            if (item == null) return;

            if (item is ScriptArray array)
            {
                foreach (var itemToAdd in array)
                {
                    RemoveItem(itemToAdd);
                }
            }
            else
            {
                ValidateRemoveItem(item);
                Remove(item);
            }
        }


        private void AddItem(object item)
        {
            if (item == null) return;

            if (item is ScriptArray array)
            {
                foreach (var itemToAdd in array)
                {
                    AddItem(itemToAdd);
                }
            }
            else
            {
                ValidateAddItem(item);
                Add(item);
            }
        }

        protected virtual void ValidateAddItem(object item)
        {
            ValidateItem(item);
        }

        protected virtual void ValidateRemoveItem(object item)
        {
            ValidateItem(item);
        }

        protected virtual void ValidateItem(object item)
        {

        }
    }


    public class PathCollection : ScriptCollection
    {
        public PathCollection()
        {
        }

        protected override void ValidateItem(object item)
        {
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
            }
            else
            { 

                throw new ArgumentException($"Invalid path. Expecting a string instead of `{item.GetType().FullName}`.", nameof(item));
            }
        }
    }
}