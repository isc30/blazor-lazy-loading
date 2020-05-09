namespace BlazorLazyLoading.LazyRoute.Internals
{
    internal class TypeRouteConstraint<T> : RouteConstraint
    {
        public delegate bool TryParseDelegate(string str, out T result);

        private readonly TryParseDelegate _parser;

        public TypeRouteConstraint(TryParseDelegate parser)
        {
            _parser = parser;
        }

        public override bool Match(string pathSegment, out object convertedValue)
        {
            if (_parser(pathSegment, out var result))
            {
                convertedValue = result!;
                return true;
            }
            else
            {
                convertedValue = null!;
                return false;
            }
        }
    }
}
