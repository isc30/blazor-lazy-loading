using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Renders a Component (IComponent) from a Lazy Module based on it's 'LazyName' or 'TypeFullName'.
    /// </summary>
    public class Lazy : ComponentBase
    {
        /// <summary>
        /// <br>Specifies the Component Name. This can be the 'LazyName' or the TypeFullName.</br>
        /// <br>'LazyName' can be set using [LazyName] attribute on a external Component inside a module.</br>
        /// </summary>
        [Parameter] public string Name { get; set; } = null!;

        /// <summary>
        /// Specifies the list of parameters that will be passed to the Lazy Component.
        /// </summary>
        [Parameter] public IEnumerable<KeyValuePair<string, object>> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// <br>Specifies if the Component is required (throws exceptions if load fails) or can error gracefully.</br>
        /// <br>default: false</br>
        /// </summary>
        [Parameter] public bool Required { get; set; } = false;

        /// <summary>
        /// Specifies a custom 'Loading' view.
        /// </summary>
        [Parameter] public RenderFragment? Loading { get; set; } = null;

        /// <summary>
        /// Specifies a custom 'Error' view.
        /// </summary>
        [Parameter] public RenderFragment? Error { get; set; } = null;

        /// <summary>
        /// <br>This callback will be awaited before trying to resolve the Component from the manifests.</br>
        /// <br>Useful for delaying a Component render and debugging with Task.Delay.</br>
        /// </summary>
        [Parameter] public Func<Lazy, Task>? OnBeforeLoadAsync { get; set; } = null;

        /// <summary>
        /// This callback will be invoked after resolving and rendering the Lazy Component.
        /// </summary>
        [Parameter] public Action<Lazy>? OnAfterLoad { get; set; } = null;

        /// <summary>
        /// Not recommended to use. Specifies a Module Name directly to avoid reading the manifests and uses Name as TypeFullName.
        /// </summary>
        [Parameter] public string? ModuleName { get; set; } = null;

        /// <summary>
        /// Exposes the resolved Type for the Lazy Component. Can be accessed from 'OnAfterLoad'.
        /// </summary>
        public Type? Type { get; protected set; } = null;

        /// <summary>
        /// Exposes the Instance the Lazy Component. Can be accessed from 'OnAfterLoad'.
        /// </summary>
        public IComponent? Instance { get; private set; } = null;

        [Inject]
        private IAssemblyLoader _assemblyLoader { get; set; } = null!;

        [Inject]
        private IManifestRepository _manifestRepository { get; set; } = null!;

        private RenderFragment? _currentFallbackBuilder = null;

        /// <inheritdoc/>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (Name == null)
            {
                throw new InvalidOperationException($"The {nameof(Lazy)} component requires a value for the parameter {nameof(Name)}.");
            }
        }

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _currentFallbackBuilder = Loading;
            base.OnInitialized(); // trigger initial render
        }

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync().ConfigureAwait(false);

                if (OnBeforeLoadAsync != null)
                {
                    await OnBeforeLoadAsync(this);
                }

                string typeFullName = Name;

                if (ModuleName == null)
                {
                    var moduleInfo = await ResolveModuleAndType().ConfigureAwait(false);

                    if (moduleInfo == null)
                    {
                        DisplayErrorView(false);
                        return;
                    }

                    (ModuleName, typeFullName) = moduleInfo.Value;
                }

                Assembly? componentAssembly = await _assemblyLoader
                    .LoadAssemblyByNameAsync(new AssemblyName
                    {
                        Name = ModuleName,
                        Version = null,
                    })
                    .ConfigureAwait(false);

                Type = componentAssembly?.GetType(typeFullName);

                if (Type == null)
                {
                    ThrowIfRequired($"Unable to load lazy component '{Name}'. Component type '{typeFullName}' not found in module '{ModuleName}'");
                    DisplayErrorView(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                DisplayErrorView(false);
                ThrowIfRequired(ex.Message); // re-throw if Required is true
            }
            finally
            {
                StateHasChanged(); // always re-render after load
            }
        }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Type == null)
            {
                BuildFallbackComponent(builder);
                return;
            }

            builder.OpenComponent(0, Type);
            builder.AddMultipleAttributes(0, Parameters);
            builder.AddComponentReferenceCapture(1, (componentRef) =>
            {
                Instance = (IComponent)componentRef;
                OnAfterLoad?.Invoke(this);
            });
            builder.CloseComponent();
        }

        private async Task<(string, string)?> ResolveModuleAndType()
        {
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
                ThrowIfRequired($"Unable to find lazy component '{Name}'. Required: {(Required ? "true" : "false")}");
                return null;
            }

            if (bestMatches.Count > 1)
            {
                ThrowIfRequired($"Multiple matches for Component with name '{Name}': '{string.Join(";", bestMatches.Select(m => m.Match.TypeFullName))}'");
                return null;
            }

            var bestMatch = bestMatches.First();

            return (bestMatch.Manifest.ModuleName, bestMatch.Match.TypeFullName);
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
