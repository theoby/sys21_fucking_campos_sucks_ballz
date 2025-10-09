using System.Text.Json.Serialization;

namespace sys21_campos_zukarmex.Models.DTOs.Api;

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
/// Estructura de respuesta especial para operaciones de vales donde datos es un string
/// </summary>
public class ApiResponseVale
{
    /// <summary>
    /// Estado de la respuesta (c�digo de estado)
    /// </summary>
    public int Estado { get; set; }
    
    /// <summary>
    /// Datos como string (en lugar de lista)
    /// </summary>
    public string Datos { get; set; } = string.Empty;
    
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
public class ValePendienteApiResponse
{
    public int Id { get; set; }
    public string Predio { get; set; } = string.Empty;
    public string Almacen { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } 
    public string Concepto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public bool Estatus { get; set; }    
    public bool Autorizado { get; set; }
}

public class DetalleApiResponse
{

}

public class RatCaptureApiResponse
{
    [Newtonsoft.Json.JsonProperty("estado")]
    public int Estado { get; set; }

    [Newtonsoft.Json.JsonProperty("datos")]
    public bool Datos { get; set; }

    [Newtonsoft.Json.JsonProperty("totalDatos")]
    public int TotalDatos { get; set; }

    [Newtonsoft.Json.JsonProperty("mensaje")]
    public string Mensaje { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public bool Success => Estado == 200 && Datos == true;
}

public class IrrigationEntryApiResponse
{
    [Newtonsoft.Json.JsonProperty("estado")]
    public int Estado { get; set; }

    [Newtonsoft.Json.JsonProperty("datos")]
    public bool Datos { get; set; }

    [Newtonsoft.Json.JsonProperty("totalDatos")]
    public int TotalDatos { get; set; }

    [Newtonsoft.Json.JsonProperty("mensaje")]
    public string Mensaje { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public bool Success => Estado == 200 && Datos == true;
}
public class RodenticideApiResponse
{
    [Newtonsoft.Json.JsonProperty("estado")]
    public int Estado { get; set; }

    [Newtonsoft.Json.JsonProperty("datos")]
    public bool Datos { get; set; }

    [Newtonsoft.Json.JsonProperty("totalDatos")]
    public int TotalDatos { get; set; }

    [Newtonsoft.Json.JsonProperty("mensaje")]
    public string Mensaje { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public bool Success => Estado == 200 && Datos == true;
}
public class DamageAssessmentApiResponse
{
    [Newtonsoft.Json.JsonProperty("estado")]
    public int Estado { get; set; }

    [Newtonsoft.Json.JsonProperty("datos")]
    public bool Datos { get; set; }

    [Newtonsoft.Json.JsonProperty("totalDatos")]
    public int TotalDatos { get; set; }

    [Newtonsoft.Json.JsonProperty("mensaje")]
    public string Mensaje { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public bool Success => Estado == 200 && Datos == true;
}


