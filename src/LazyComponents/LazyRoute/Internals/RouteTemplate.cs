using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BlazorLazyLoading.LazyRoute.Internals
{
    [DebuggerDisplay("{TemplateText}")]
    internal class RouteTemplate
    {
        public RouteTemplate(string templateText, TemplateSegment[] segments)
        {
            TemplateText = templateText;
            Segments = segments;
            OptionalSegmentsCount = segments.Count(template => template.IsOptional);
        }

        public string TemplateText { get; }

        public TemplateSegment[] Segments { get; }

        public int OptionalSegmentsCount { get; }
    }
}
