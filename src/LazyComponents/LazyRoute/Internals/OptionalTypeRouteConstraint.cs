namespace BlazorLazyLoading.LazyRoute.Internals
{
    internal class OptionalTypeRouteConstraint<T> : RouteConstraint
    {
        public delegate bool TryParseDelegate(string str, out T result);

        private readonly TryParseDelegate _parser;

        public OptionalTypeRouteConstraint(TryParseDelegate parser)
        {
            _parser = parser;
        }

        public override bool Match(string pathSegment, out object convertedValue)
        {
            // Unset values are set to null in the Parameters object created in
            // the RouteContext. To match this pattern, unset optional parmeters
            // are converted to null.
            if (string.IsNullOrEmpty(pathSegment))
            {
                convertedValue = null!;
                return true;
            }

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
