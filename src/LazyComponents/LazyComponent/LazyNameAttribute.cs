using System;

namespace BlazorLazyLoading
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LazyNameAttribute : Attribute
    {
        public string ComponentName { get; }

        public LazyNameAttribute(string componentName)
        {
            ComponentName = componentName;
        }
    }
}
