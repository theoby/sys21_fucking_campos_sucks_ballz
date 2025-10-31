using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Models.DTOs.Authentication;


/// <summary>
/// DTO para la solicitud de login
/// </summary>
public class LoginRequest
{
    [JsonProperty("usuario")]
    public string Usuario { get; set; } = string.Empty;
    [JsonProperty("contraseña")]
    public string Password { get; set; } = string.Empty;
    [JsonProperty("idEmpresa")]
    public int IdEmpresa { get; set; }
    [JsonProperty("idApp")]
    public int IdApp { get; set; }
}

/// <summary>
/// DTO para la respuesta de login
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Session? Session { get; set; }
}

public class LoginApiResponse
{
    [JsonProperty("estado")]
    public int Estado { get; set; }

    [JsonProperty("datos")]
    public LoginData? Datos { get; set; }

    [JsonProperty("mensaje")]
    public string? Mensaje { get; set; }
}

public class LoginData
{
    [JsonProperty("usuario")]
    public UserData? Usuario { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;

    [JsonProperty("expirationDate")]
    public DateTime ExpirationDate { get; set; }
}

public class UserData
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("nombreUsuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [JsonProperty("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;

    [JsonProperty("idEmpresa")]
    public int IdEmpresa { get; set; }

    [JsonProperty("tipoUsuario")]
    public int TipoUsuario { get; set; }

    [JsonProperty("idInspector")]
    public int IdInspector { get; set; }

    [JsonProperty("permisos")]
    public List<Permiso> Permisos { get; set; } = new();

    /// <summary>
    /// Convierte este DTO al modelo de Session que usa la app internamente
    /// </summary>
    public Session ToSession(string token, DateTime expiration)
    {
        return new Session
        {
            UserId = this.Id,
            Username = this.NombreUsuario,
            NombreUsuario = this.NombreUsuario,
            NombreCompleto = this.NombreCompleto,
            IdEmpresa = this.IdEmpresa,
            TipoUsuario = this.TipoUsuario,
            IdInspector = this.IdInspector,
            Token = token,
            ExpirationDate = expiration,
            CreatedAt = DateTime.Now,
            ExpiresAt = expiration,
            IsActive = true,
            PermisosJson = JsonConvert.SerializeObject(this.Permisos ?? new List<Permiso>())
        };
    }
    public class Permiso
    {
        [JsonProperty("idApp")]
        public int IdApp { get; set; }

        [JsonProperty("nombreApp")]
        public string NombreApp { get; set; }

        // --- AÑADIDO ---
        [JsonProperty("tipoUsuario")]
        public int TipoUsuario { get; set; }

        // --- AÑADIDO ---
        [JsonProperty("idInspector")]
        public int IdInspector { get; set; }

        [JsonProperty("permiso")]
        public bool TienePermiso { get; set; }
    }
}