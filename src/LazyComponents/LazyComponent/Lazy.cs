using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Newtonsoft.Json.Linq;

namespace BlazorLazyLoading
{
    public class Lazy : ComponentBase
    {
        [Parameter]
        public string Name { get; set; } = null!;

        [Parameter]
        public Type? Fallback { get; set; } = null;

        [Inject]
        public IAssemblyLoader AssemblyLoader { get; set; } = null!;

        [Inject]
        public IManifestRepository ManifestRepository { get; set; } = null!;

        public Type? Type { get; protected set; } = null;

        public ComponentBase? Instance { get; private set; } = null;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync().ConfigureAwait(false);

            // dirty stuff, enough for V1
            var manifests = (await ManifestRepository.GetAllAsync().ConfigureAwait(false))
                .Where(m => m.ManifestSections.ContainsKey("Components"))
                .Select(m => new
                {
                    Manifest = m,
                    Matches = m.ManifestSections["Components"]
                                .Children<JObject>()
                                .Select(o => o.Value<string?>("Name"))
                                .NotNull()
                                .Where(n => n == Name)
                                .ToList(),
                })
                .Where(i => i.Matches.Any());

            var match = manifests.FirstOrDefault();

            if (match == null)
            {
                return;
            }

            if (match.Matches.Count > 1)
            {
                throw new NotSupportedException($"Multiple matches for Component with name '{Name}'");
            }

            await Task.Delay(1000);

            Assembly? componentAssembly = await AssemblyLoader
                .LoadAssemblyByNameAsync(new AssemblyName
                {
                    Name = match.Manifest.ModuleName,
                    Version = null,
                })
                .ConfigureAwait(false);

            Type = componentAssembly?.GetType(match.Matches.First());
            ShouldRender();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Type == null)
            {
                BuildFallbackComponent(builder);
                return;
            }

            builder.OpenComponent(0, Type);
            builder.AddComponentReferenceCapture(1, (componentRef) => Instance = (ComponentBase)componentRef);
            builder.CloseComponent();
        }

        private void BuildFallbackComponent(RenderTreeBuilder builder)
        {
            if (Fallback != null)
            {
                builder.OpenComponent(0, Fallback);
                builder.CloseComponent();

                return;
            }

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "bll-loading");
            builder.AddContent(2, "Loading...");
            builder.CloseElement();
        }
    }

    public static class X
    {
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            return source.Where(i => i != null).Cast<T>();
        }
    }
}
