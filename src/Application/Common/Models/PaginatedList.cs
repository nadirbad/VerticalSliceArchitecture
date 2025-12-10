using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

namespace VerticalSliceArchitecture.Application.Common.Models;

public class PaginatedList<T>
{
    public List<T> Items { get; init; }
    public int PageNumber { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }

    [JsonConstructor]
    public PaginatedList(List<T> items, int pageNumber, int totalPages, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        TotalPages = totalPages;
        TotalCount = totalCount;
    }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PaginatedList<T> Create(List<T> items, int count, int pageNumber, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        return new PaginatedList<T>(items, pageNumber, totalPages, count);
    }

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return Create(items, count, pageNumber, pageSize);
    }
}