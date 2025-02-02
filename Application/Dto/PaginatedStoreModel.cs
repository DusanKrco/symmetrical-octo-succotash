using Domain.Abstractions;

namespace Application.Dto;

public class PaginatedStoreModel<T> : IPaginatedModel<T>
{
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public List<T> Data { get; set; } = new();
}