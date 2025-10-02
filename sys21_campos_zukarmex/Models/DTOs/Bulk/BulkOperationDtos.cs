namespace sys21_campos_zukarmex.Models.DTOs.Bulk;

/// <summary>
/// DTO para solicitudes de operaciones en lote
/// </summary>
public class BulkOperationRequest<T>
{
    public List<T> Items { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "create", "update", "delete"
}

/// <summary>
/// DTO para respuestas de operaciones en lote
/// </summary>
public class BulkOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}