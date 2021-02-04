using System.Linq;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.OData.UriParser;

namespace AutoMapper.AspNet.OData
{
    public class AspNetODataQueryOptions<TModel> : IODataQueryOptions
    {
        private readonly ODataQueryOptions<TModel> options;

        public AspNetODataQueryOptions(ODataQueryOptions<TModel> options)
        {
            this.options = options;

            if (options.Filter != null)
                Filter = new AspNetODataFilter(options.Filter);
        }

        public int? Top => options.Top?.Value;
        public int? Skip => options.Skip?.Value;
        public bool? Count => options.Count?.Value;
        public IODataFilter Filter { get; }
        public OrderByClause OrderBy => options.OrderBy?.OrderByClause;
        public SelectExpandClause SelectExpand => options.SelectExpand?.SelectExpandClause;

        public void SetRequestSelectExpandClause(SelectExpandClause selectExpandClause) =>
            options.Request.ODataFeature().SelectExpandClause = selectExpandClause;

        public void SetRequestTotalCount(long total) =>
            options.Request.ODataFeature().TotalCount = total;
    }

    public class AspNetODataFilter : IODataFilter
    {
        private readonly FilterQueryOption filterQueryOption;

        public AspNetODataFilter(FilterQueryOption filterQueryOption)
        {
            this.filterQueryOption = filterQueryOption;
        }

        public IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings) =>
            filterQueryOption.ApplyTo(query, querySettings);

        public IQueryable ApplyTo(IQueryable query, IODataQuerySettings querySettings)
        {
            var odataQuerySettings = (AspNetODataQuerySettings)querySettings;
            return filterQueryOption?.ApplyTo(query, odataQuerySettings.QuerySettings);
        }
    }

    public class AspNetODataQuerySettings : IODataQuerySettings
    {
        public ODataQuerySettings QuerySettings { get; set; }
        public HandleNullPropagationOption HandleNullPropagation { get; set; }
    }
}