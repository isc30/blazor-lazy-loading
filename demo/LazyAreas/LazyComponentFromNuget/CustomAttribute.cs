using System;

[assembly: BlazorLazyLoadingModule()]

[AttributeUsage(AttributeTargets.Assembly)]
public class BlazorLazyLoadingModuleAttribute : Attribute
{
}
