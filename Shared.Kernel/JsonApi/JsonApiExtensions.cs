using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Shared.Kernel.JsonApi;

public static class JsonApiExtensions
{
    /// <summary>
    /// Applies JSON:API specification query options (Filter, Sort, Include, Pagination) to an IQueryable.
    /// </summary>
    public static async Task<PagedResult<T>> ApplyJsonApiAsync<T>(this IQueryable<T> query, JsonApiQueryOptions options) where T : class
    {
        // 1. Includes (?include=author,comments.author)
        if (!string.IsNullOrWhiteSpace(options.Include))
        {
            var includes = options.Include.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var include in includes)
            {
                // Convert snake_case or camelCase to PascalCase if needed, but EF Core handles exact casing best.
                // Assuming standard JSON API convention maps to navigation properties.
                var navProperty = include.Trim().Replace(".", "."); // Nested paths work directly in EF (e.g. Author.Comments)
                query = query.Include(navProperty);
            }
        }

        // 2. Filtering (?filter[name]=john&filter[status]=active)
        if (options.Filter != null && options.Filter.Any())
        {
            foreach (var filter in options.Filter)
            {
                var field = ToPascalCase(filter.Key.Trim());
                var value = filter.Value?.Trim();
                
                if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(value)) continue;

                // Simple exact match or string contains check
                bool isString = typeof(T).GetProperty(field, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.PropertyType == typeof(string);
                
                if (isString)
                {
                    query = query.Where($"{field}.Contains(@0)", value);
                }
                else
                {
                    query = query.Where($"{field} == @0", value);
                }
            }
        }

        // 3. Sorting (?sort=-created_at,title)
        if (!string.IsNullOrWhiteSpace(options.Sort))
        {
            var sortFields = options.Sort.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            var orderByStrings = sortFields.Select(f => 
            {
                var field = f.Trim();
                if (field.StartsWith("-"))
                {
                    return $"{ToPascalCase(field.Substring(1))} descending";
                }
                return ToPascalCase(field);
            });
            
            var sortExpression = string.Join(", ", orderByStrings);
            query = query.OrderBy(sortExpression);
        }

        // 4. Pagination
        var totalCount = await query.CountAsync();
        var pageNumber = options.GetPageNumber();
        var pageSize = options.GetPageSize();

        var pagedData = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(pagedData, totalCount);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        var words = input.Split(new[] { '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join("", words);
    }
}
