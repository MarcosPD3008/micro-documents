namespace MicroDocuments.Application.Pagination;

public class PaginationResponse<T>
{
    public List<T> Content { get; set; } = new();
    public int Total { get; set; }
    public bool NextPage { get; set; }
}

