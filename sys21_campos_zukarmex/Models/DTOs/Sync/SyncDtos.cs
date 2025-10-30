using CommunityToolkit.Mvvm.ComponentModel;
namespace sys21_campos_zukarmex.Models.DTOs.Sync;

/// <summary>
/// DTO para el estado de sincronización
/// </summary>
public partial class SyncStatus : ObservableObject
{
    [ObservableProperty]
    private string catalogName = string.Empty;

    [ObservableProperty]
    private int progress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSuccess))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    private bool isCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSuccess))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    private string status = string.Empty;

    // Las propiedades calculadas se quedan exactamente igual.
    public bool IsSuccess => IsCompleted && !Status.Contains("Error");
    public bool IsError => IsCompleted && Status.Contains("Error");
}

/// <summary>
/// DTO para el resultado de sincronización
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// DTO para estadísticas de sincronización
/// </summary>
public class SyncStatistics
{
    public int AlmacenesCount { get; set; }
    public int ArticulosCount { get; set; }
    public int CamposCount { get; set; }
    public int EmpresasCount { get; set; }
    public int FamiliasCount { get; set; }
    public int InspectoresCount { get; set; }
    public int LotesCount { get; set; }
    public int MaquinariasCount { get; set; }
    public int RecetasCount { get; set; }
    public int SubFamiliasCount { get; set; }
    public int ZafrasCount { get; set; }
    public int PluviometrosCount { get; set; }
    public int CiclosCount { get; set; }
    public int LineasCount { get; set; }
    public int TotalRecords { get; set; }
    public DateTime LastSyncDate { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO para reporte de integridad de sincronización
/// </summary>
public class SyncIntegrityReport
{
    public DateTime VerificationDate { get; set; } = DateTime.Now;
    public int TotalTables { get; set; }
    public int SuccessfulSyncs { get; set; }
    public int EmptyTables { get; set; }
    public int ErroredTables { get; set; }
    public int TotalRecords { get; set; }
    public Dictionary<string, int> CatalogCounts { get; set; } = new();
    
    /// <summary>
    /// Indica si todas las tablas están sincronizadas correctamente
    /// </summary>
    public bool IsFullyIntegrated => ErroredTables == 0 && SuccessfulSyncs > 0;
    
    /// <summary>
    /// Porcentaje de tablas sincronizadas exitosamente
    /// </summary>
    public double SuccessPercentage => TotalTables > 0 ? (double)SuccessfulSyncs / TotalTables * 100 : 0;
    
    /// <summary>
    /// Obtener resumen textual del reporte
    /// </summary>
    public string GetSummary()
    {
        if (IsFullyIntegrated)
        {
            return $"✅ Integridad completa: {SuccessfulSyncs}/{TotalTables} tablas, {TotalRecords:N0} registros";
        }
        else if (ErroredTables > 0)
        {
            return $"❌ {ErroredTables} tablas con errores, {SuccessfulSyncs}/{TotalTables} exitosas";
        }
        else
        {
            return $"⚠️ {EmptyTables} tablas vacías, {SuccessfulSyncs}/{TotalTables} con datos";
        }
    }
}