using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public RenderFragment? ChildContent { get; set; } = null;

        [Parameter]
        public Func<Lazy, Task>? OnBeforeLoadAsync { get; set; } = null;

        [Parameter]
        public Action<Lazy>? OnAfterLoad { get; set; } = null;

        public Type? Type { get; protected set; } = null;

        public ComponentBase? Instance { get; private set; } = null;

        [Inject]
        private IAssemblyLoader _assemblyLoader { get; set; } = null!;

        [Inject]
        private IManifestRepository _manifestRepository { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync().ConfigureAwait(false);

            var allManifests = await _manifestRepository.GetAllAsync().ConfigureAwait(false);

            var manifests = allManifests
                .Where(m => m.ManifestSections.ContainsKey("Components"))
                .DistinctBy(m => m.ModuleName)
                .SelectMany(m => m.ManifestSections["Components"]
                    .Children<JObject>()
                    .Select(o => new
                    {
                        TypeFullName = o.Value<string?>("TypeFullName"),
                        Name = o.Value<string?>("Name"),
                    })
                    .Where(i => !string.IsNullOrWhiteSpace(i.TypeFullName))
                    .Select(o => new
                    {
                        TypeFullName = o.TypeFullName!,
                        Name = o.Name,
                    })
                    .Select(n => new
                    {
                        Match = n,
                        Score = n.TypeFullName == Name
                            ? 3
                            : n.Name == Name
                                ? 2
                                : n.TypeFullName.EndsWith(Name)
                                    ? 1
                                    : 0,
                        Manifest = m,
                    }));

            var bestMatches = manifests
                .GroupBy(i => i.Score)
                .OrderByDescending(i => i.Key)
                .FirstOrDefault()
                ?.ToList();

            if (bestMatches == null || !bestMatches.Any())
            {
                Debug.WriteLine($"Unable to find lazy component '{Name}'");
                return;
            }

            if (bestMatches.Count > 1)
            {
                throw new NotSupportedException($"Multiple matches for Component with name '{Name}': '{string.Join(";", bestMatches.Select(m => m.Match.TypeFullName))}'");
            }

            var bestMatch = bestMatches.First();

            Assembly? componentAssembly = await _assemblyLoader
                .LoadAssemblyByNameAsync(new AssemblyName
                {
                    Name = bestMatch.Manifest.ModuleName,
                    Version = null,
                })
                .ConfigureAwait(false);

            Type = componentAssembly?.GetType(bestMatch.Match.TypeFullName);

            if (OnBeforeLoadAsync != null)
            {
                await OnBeforeLoadAsync(this);
            }

            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Type == null)
            {
                BuildFallbackComponent(builder);
                return;
            }

            builder.OpenComponent(0, Type);
            builder.AddComponentReferenceCapture(1, (componentRef) =>
            {
                Instance = (ComponentBase)componentRef;
                OnAfterLoad?.Invoke(this);
            });
            builder.CloseComponent();
        }

        private void BuildFallbackComponent(RenderTreeBuilder builder)
        {
            if (ChildContent != null)
            {
                ChildContent.Invoke(builder);
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

        public static IEnumerable<T> DistinctBy<T, TOut>(
            this IEnumerable<T> source,
            Func<T, TOut> selector)
        {
            return source
                .GroupBy(m => selector(m))
                .Select(g => g.First());
        }
    }
}
