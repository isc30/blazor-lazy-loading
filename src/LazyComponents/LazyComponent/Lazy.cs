using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using BlazorLazyLoading.Extensions;
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
        public bool Required { get; set; } = false;

        [Parameter]
        public RenderFragment? Loading { get; set; } = null;

        [Parameter]
        public RenderFragment? Error { get; set; } = null;

        [Parameter]
        public Func<Lazy, Task>? OnBeforeLoadAsync { get; set; } = null;

        [Parameter]
        public Action<Lazy>? OnAfterLoad { get; set; } = null;

        public Type? Type { get; protected set; } = null;

        public IComponent? Instance { get; private set; } = null;

        [Inject]
        private IAssemblyLoader _assemblyLoader { get; set; } = null!;

        [Inject]
        private IManifestRepository _manifestRepository { get; set; } = null!;

        private RenderFragment? _currentFallbackBuilder = null;

        protected override void OnInitialized()
        {
            _currentFallbackBuilder = Loading;
            base.OnInitialized(); // trigger initial render
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync().ConfigureAwait(false);

            if (OnBeforeLoadAsync != null)
            {
                await OnBeforeLoadAsync(this);
            }

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
                .Where(i => i.Score > 0)
                .GroupBy(i => i.Score)
                .OrderByDescending(i => i.Key)
                .FirstOrDefault()
                ?.ToList();

            if (bestMatches == null || !bestMatches.Any())
            {
                DisplayErrorView();
                ThrowIfRequired($"Unable to find lazy component '{Name}'. Required: {(Required ? "true" : "false")}");
                return;
            }

            if (bestMatches.Count > 1)
            {
                DisplayErrorView();
                ThrowIfRequired($"Multiple matches for Component with name '{Name}': '{string.Join(";", bestMatches.Select(m => m.Match.TypeFullName))}'");
                return;
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

            if (Type == null)
            {
                DisplayErrorView(false);
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
                Instance = (IComponent)componentRef;
                OnAfterLoad?.Invoke(this);
            });
            builder.CloseComponent();
        }

        private void BuildFallbackComponent(RenderTreeBuilder builder)
        {
            _currentFallbackBuilder?.Invoke(builder);
        }

        private void DisplayErrorView(bool render = true)
        {
            _currentFallbackBuilder = Error;
            if (render) StateHasChanged();
        }

        private void ThrowIfRequired(string errorMessage)
        {
            if (Required)
            {
                throw new NotSupportedException(errorMessage);
            }
            else
            {
                Debug.WriteLine(errorMessage);
            }
        }
    }
}
