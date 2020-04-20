using System;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorLazyLoading
{
    public sealed class Lazy : ComponentBase
    {
        [Parameter]
        public Type Type { get; set; } = null!;

        [Inject]
        public IAssemblyLoader AssemblyLoader { get; set; } = null!;

        public ComponentBase? Instance { get; private set; } = null;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync().ConfigureAwait(false);

            //Assembly? testAssembly = await AssemblyLoader
            //    .LoadAssemblyByNameAsync(assemblyName)
            //    .ConfigureAwait(false);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Type == null)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "loading");
                builder.CloseElement();

                return;
            }

            builder.OpenComponent(0, Type);
            builder.AddComponentReferenceCapture(1, (componentRef) => Instance = (ComponentBase)componentRef);
            builder.CloseComponent();
        }
    }
}
