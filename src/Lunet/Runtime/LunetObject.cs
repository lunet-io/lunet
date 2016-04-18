using Scriban.Runtime;

namespace Lunet.Runtime
{
    public abstract class LunetObject : ScriptObject
    {
        public T GetSafe<T>(string name)
        {
            var obj = this[name];
            // If value is null, the property does no exist, 
            // so we can safely return immediately with the default value
            if (obj == null)
            {
                return default(T);
            }
            if (!(obj is T))
            {
                obj = default(T);
                this[name] = obj;
            }
            return (T)obj;
        }
    }
}