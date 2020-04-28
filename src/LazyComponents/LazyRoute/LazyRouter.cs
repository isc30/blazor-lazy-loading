using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlazorLazyLoading.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

using IComponent = Microsoft.AspNetCore.Components.IComponent;
using RouteContext = BlazorLazyLoading.LazyRoute.Internals.RouteContext;
using RouteTable = BlazorLazyLoading.LazyRoute.Internals.RouteTable;
using RouteTableFactory = BlazorLazyLoading.LazyRoute.Internals.RouteTableFactory;

namespace BlazorLazyLoading
{
    /// <summary>
    /// A component that supplies route data corresponding to the current navigation state.
    /// </summary>
    public class LazyRouter : IComponent, IHandleAfterRender, IDisposable
    {

        [Inject] private IManifestRepository _manifestRepository { get; set; } = null!;

        [Inject] private IAssemblyLoader _assemblyLoader { get; set; } = null!;

        [Parameter] public RenderFragment<string?> Loading { get; set; } = null!;

        private bool isFirstRender = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static readonly char[] _queryOrHashStartChar = new[] { '?', '#' };
        static readonly ReadOnlyDictionary<string, object> _emptyParametersDictionary
            = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        RenderHandle _renderHandle;
        string _baseUri = null!;
        string _locationAbsolute = null!;
        bool _navigationInterceptionEnabled;
        ILogger<LazyRouter> _logger = null!;

        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        [Inject] private INavigationInterception NavigationInterception { get; set; } = null!;

        [Inject] private ILoggerFactory LoggerFactory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the assembly that should be searched for components matching the URI.
        /// </summary>
        [Parameter] public Assembly AppAssembly { get; set; } = null!;

        /// <summary>
        /// Gets or sets a collection of additional assemblies that should be searched for components
        /// that can match URIs.
        /// </summary>
        [Parameter] public IEnumerable<Assembly> AdditionalAssemblies { get; set; } = null!;

        /// <summary>
        /// Gets or sets the content to display when no match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment NotFound { get; set; } = null!;

        /// <summary>
        /// Gets or sets the content to display when a match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment<RouteData> Found { get; set; } = null!;

        private RouteTable Routes { get; set; } = null!;

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _logger = LoggerFactory.CreateLogger<LazyRouter>();
            _renderHandle = renderHandle;
            _baseUri = NavigationManager.BaseUri;
            _locationAbsolute = NavigationManager.Uri;

            NavigationManager.LocationChanged += OnLocationChanged;
        }

        /// <inheritdoc />
        public async Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (AppAssembly == null)
            {
                throw new InvalidOperationException($"The {nameof(LazyRouter)} component requires a value for the parameter {nameof(AppAssembly)}.");
            }

            // Found content is mandatory, because even though we could use something like <RouteView ...> as a
            // reasonable default, if it's not declared explicitly in the template then people will have no way
            // to discover how to customize this (e.g., to add authorization).
            if (Found == null)
            {
                throw new InvalidOperationException($"The {nameof(LazyRouter)} component requires a value for the parameter {nameof(Found)}.");
            }

            // Loading content is mandatory, because even though we could use something like <RouteView ...> as a
            // reasonable default, if it's not declared explicitly in the template then people will have no way
            // to discover how to customize this (e.g., to add authorization).
            if (Loading == null)
            {
                throw new InvalidOperationException($"The {nameof(LazyRouter)} component requires a value for the parameter {nameof(Loading)}.");
            }

            // NotFound content is mandatory, because even though we could display a default message like "Not found",
            // it has to be specified explicitly so that it can also be wrapped in a specific layout
            if (NotFound == null)
            {
                throw new InvalidOperationException($"The {nameof(LazyRouter)} component requires a value for the parameter {nameof(NotFound)}.");
            }

            await PerformInitialRouting();
            await RefreshLazyRouteTableAsync();
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        private static string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            _locationAbsolute = args.Location;
            if (_renderHandle.IsInitialized && Routes != null)
            {
                _ = PerformNavigationAsync(args.IsNavigationIntercepted); // from render thread
            }
        }

        Task IHandleAfterRender.OnAfterRenderAsync()
        {
            if (!_navigationInterceptionEnabled)
            {
                _navigationInterceptionEnabled = true;
                return NavigationInterception.EnableNavigationInterceptionAsync();
            }

            return Task.CompletedTask;
        }

        /// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task PerformModuleLoadAsync(string moduleName)
        {
            var assemblyName = new AssemblyName
            {
                Name = moduleName,
                Version = null,
            };

            var lazyPageAssembly = _assemblyLoader.GetLoadedAssemblyByName(assemblyName);

            if (lazyPageAssembly == null)
            {
                Render(Loading(moduleName)); // show loading RenderFragment

                lazyPageAssembly = await _assemblyLoader
                    .LoadAssemblyByNameAsync(new AssemblyName
                    {
                        Name = moduleName,
                        Version = null,
                    })
                    .ConfigureAwait(true); // continue in render thread
            }

            if (lazyPageAssembly == null)
            {
                throw new InvalidOperationException("Page cannot load if assembly failed to load");
            }

            AdditionalAssemblies = AdditionalAssemblies == null
                ? new[] { lazyPageAssembly }
                : AdditionalAssemblies.Concat(new[] { lazyPageAssembly });

            await RefreshLazyRouteTableAsync();
        }

        private Task PerformInitialRouting()
        {
            var assemblies = AdditionalAssemblies == null
                ? new[] { AppAssembly }
                : new[] { AppAssembly }.Concat(AdditionalAssemblies);

            Routes = RouteTableFactory.Create(assemblies);

            return PerformNavigationAsync(isNavigationIntercepted: false); // from render thread
        }

        private async Task RefreshLazyRouteTableAsync()
        {
            var createLazyRoutesTask = GetLazyRoutes(); // continue in render thread

            var assemblies = AdditionalAssemblies == null
                ? new[] { AppAssembly }
                : new[] { AppAssembly }.Concat(AdditionalAssemblies);

            var assemblyRoutes = RouteTableFactory.Create(assemblies);
            var lazyRoutes = await createLazyRoutesTask;

            Routes = new RouteTable(assemblyRoutes.Routes.Concat(lazyRoutes.Routes).ToArray());

            await PerformNavigationAsync(isNavigationIntercepted: false); // from render thread
        }

        private async Task<RouteTable> GetLazyRoutes()
        {
            var manifests = (await _manifestRepository.GetAllAsync().ConfigureAwait(false))
                .Where(m => m.ManifestSections.ContainsKey("Routes"))
                .DistinctBy(m => m.ModuleName)
                .SelectMany(m => m.ManifestSections["Routes"]
                    .Children<JObject>()
                    .Select(o => new
                    {
                        TypeFullName = o.Value<string?>("TypeFullName"),
                        Route = o.Value<string?>("Route"),
                    })
                    .Where(i => !string.IsNullOrWhiteSpace(i.TypeFullName) && !string.IsNullOrWhiteSpace(i.Route))
                    .Select(o => new
                    {
                        TypeFullName = o.TypeFullName!,
                        Route = o.Route!,
                    })
                    .GroupBy(i => i.TypeFullName)
                    .Select(n => new
                    {
                        Match = n,
                        Manifest = m,
                    }));

            var manifestDictionary = manifests.ToDictionary(
                i => (object)new LazyRouteHandler(i.Manifest.ModuleName, i.Match.Key),
                i => i.Match.Select(m => m.Route).ToArray());

            return RouteTableFactory.Create(manifestDictionary);
        }

        private Task PerformNavigationAsync(bool isNavigationIntercepted)
        {
            var locationPath = NavigationManager.ToBaseRelativePath(_locationAbsolute);
            locationPath = StringUntilAny(locationPath, _queryOrHashStartChar);
            var context = new RouteContext(locationPath);
            Routes.Route(context);

            if (context.Handler != null)
            {
                switch (context.Handler)
                {
                    // default implementation: the route handler is a Type
                    case Type typeHandler:
                    {
                        if (!typeof(IComponent).IsAssignableFrom(typeHandler))
                        {
                            throw new InvalidOperationException($"The type {typeHandler.FullName} " +
                                $"does not implement {typeof(IComponent).FullName}.");
                        }

                        var routeData = new RouteData(
                            typeHandler,
                            context.Parameters ?? _emptyParametersDictionary);

                        Render(Found(routeData));
                        return Task.CompletedTask;
                    }

                    // lazy implementation: a module needs to be loaded
                    case LazyRouteHandler lazyHandler:
                    {
                        return PerformModuleLoadAsync(lazyHandler.ModuleName); // from render thread
                    }
                }
            }

            if (!isNavigationIntercepted)
            {
                Render(isFirstRender
                    ? Loading(null)
                    : NotFound);

                return Task.CompletedTask;
            }

            NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);

            return Task.CompletedTask;
        }

        private void Render(RenderFragment fragment)
        {
            _renderHandle.Render(fragment);
            isFirstRender = false;
        }
    }

    internal class LazyRouteHandler
    {
        public LazyRouteHandler(string moduleName, string typeFullName)
        {
            ModuleName = moduleName;
            TypeFullName = typeFullName;
        }

        public string ModuleName { get; set; }

        public string TypeFullName { get; set; }
    }
}
