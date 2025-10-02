namespace sys21_campos_zukarmex.Models.DTOs.Search;

/// <summary>
/// DTO para solicitudes de búsqueda y filtrado
/// </summary>
public class SearchRequest
{
    public string SearchTerm { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = string.Empty;
    public bool SortDescending { get; set; }
}

/// <summary>
/// DTO para respuestas de búsqueda con paginación
/// </summary>
public class SearchResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}