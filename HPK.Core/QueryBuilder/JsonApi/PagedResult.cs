using System.Collections.Generic;

namespace HPK.Core.JsonApi;

/// <summary>
/// A standardized paginated result container that integrates seamlessly with ApiResponseFilter.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; }
    public long TotalCount { get; set; }
    
    public PagedResult(IEnumerable<T> data, long totalCount)
    {
        Data = data;
        TotalCount = totalCount;
    }
}
