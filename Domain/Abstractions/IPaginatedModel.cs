namespace Domain.Abstractions;

public interface IPaginatedModel<T>
{
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int Page { get; set; }
    List<T> Data { get; set; }
}