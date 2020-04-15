using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlazorLazyLoading
{
    public class LoadDLLInfo : Task
    {
        [Required]
        public string EntryPoint { get; set; } = null!;

        public override bool Execute()
        {
            Log.LogError("HELLO FROM TASK! {0}", EntryPoint);

            return true;
        }
    }
}
