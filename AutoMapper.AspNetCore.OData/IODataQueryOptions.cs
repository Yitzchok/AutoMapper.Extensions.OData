using System.Linq;
using Microsoft.OData.UriParser;

namespace AutoMapper.AspNet.OData
{
    public interface IODataQueryOptions
    {
        public int? Top { get; }
        public int? Skip { get; }
        bool? Count { get; }
        public IODataFilter Filter { get; }
        public OrderByClause OrderBy { get; }
        public SelectExpandClause SelectExpand { get; }

        void SetRequestTotalCount(long total);
        void SetRequestSelectExpandClause(SelectExpandClause selectExpandClause);
    }

    public interface IODataFilter
    {
        IQueryable ApplyTo(IQueryable query, IODataQuerySettings querySettings);
    }

    public interface IODataQuerySettings { }
}