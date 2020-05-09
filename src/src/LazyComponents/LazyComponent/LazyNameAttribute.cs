using System;

namespace BlazorLazyLoading
{
    /// <summary>
    /// Specifies a LazyName for a Component that can be later used by &lt;Lazy&gt; Component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LazyNameAttribute : Attribute
    {
        /// <summary>
        /// LazyName
        /// </summary>
        public string ComponentName { get; }

        /// <inheritdoc/>
        public LazyNameAttribute(string componentName)
        {
            ComponentName = componentName;
        }
    }
}
