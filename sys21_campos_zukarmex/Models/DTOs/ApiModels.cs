using Newtonsoft.Json;

namespace sys21_campos_zukarmex.Models.DTOs;

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int IdEmpresa { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Session? Session { get; set; }
}

/// <summary>
/// DTO para mapear la respuesta de la API de empresas con nombres espec�ficos
/// </summary>
public class EmpresaApiDto
{
    /// <summary>
    /// ID de la empresa (mapea a Id en el modelo Empresa)
    /// </summary>
    [JsonProperty("idEmpresa")]
    public int IdEmpresa { get; set; }
    
    /// <summary>
    /// Nombre de la empresa (mapea a Nombre en el modelo Empresa)
    /// </summary>
    [JsonProperty("nombre")]
    public string Nombre { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si es empresa promotora (mapea a IsPromotora en el modelo Empresa)
    /// </summary>
    [JsonProperty("esPromotora")]
    public bool EsPromotora { get; set; }
    
    /// <summary>
    /// Convierte el DTO a modelo de dominio Empresa
    /// </summary>
    public Empresa ToEmpresa()
    {
        return new Empresa
        {
            Id = IdEmpresa,
            Nombre = Nombre,
            IsPromotora = EsPromotora
        };
    }
}

/// <summary>
/// Estructura de respuesta est�ndar de la API
/// </summary>
public class StandardApiResponse<T>
{
    /// <summary>
    /// Estado de la respuesta (c�digo de estado)
    /// </summary>
    public int Estado { get; set; }
    
    /// <summary>
    /// Lista de datos devueltos por la API
    /// </summary>
    public List<T> Datos { get; set; } = new();
    
    /// <summary>
    /// Total de datos devueltos
    /// </summary>
    public int TotalDatos { get; set; }
    
    /// <summary>
    /// Mensaje descriptivo de la respuesta
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si la operaci�n fue exitosa (estado 200)
    /// </summary>
    public bool Success => Estado == 200;
    
    /// <summary>
    /// Obtiene el primer elemento de los datos (para operaciones de un solo elemento)
    /// </summary>
    public T? FirstData => Datos != null && Datos.Count > 0 ? Datos[0] : default(T);
}

/// <summary>
/// Estructura de respuesta legacy (mantener para compatibilidad)
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<T>? DataList { get; set; }
}

public class SyncStatus
{
    public string CatalogName { get; set; } = string.Empty;
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class SyncStatistics
{
    public int AlmacenesCount { get; set; }
    public int ArticulosCount { get; set; }
    public int CamposCount { get; set; }
    public int CentrosCostoCount { get; set; }
    public int CiclosCount { get; set; }
    public int EmpresasCount { get; set; }
    public int FamiliasCount { get; set; }
    public int InspectoresCount { get; set; }
    public int LotesCount { get; set; }
    public int MaquinariasCount { get; set; }
    public int SubFamiliasCount { get; set; }
    public int TotalRecords { get; set; }
    public DateTime LastSyncDate { get; set; }
    public string? ErrorMessage { get; set; }
}

// DTOs for search and filtering
public class SearchRequest
{
    public string SearchTerm { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = string.Empty;
    public bool SortDescending { get; set; }
}

public class SearchResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// DTO for validation results
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

// DTOs for bulk operations
public class BulkOperationRequest<T>
{
    public List<T> Items { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "create", "update", "delete"
}

public class BulkOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

// Este archivo ha sido reorganizado en m�ltiples archivos especializados:
// - Models/DTOs/Authentication/AuthenticationDtos.cs
// - Models/DTOs/Api/ApiResponseDtos.cs  
// - Models/DTOs/Catalog/CatalogDtos.cs
// - Models/DTOs/Search/SearchDtos.cs
// - Models/DTOs/Sync/SyncDtos.cs
// - Models/DTOs/Validation/ValidationDtos.cs
// - Models/DTOs/Bulk/BulkOperationDtos.cs

// Archivo mantenido para compatibilidad, las clases ahora est�n en namespaces espec�ficos