using System.Text;
using Newtonsoft.Json;
using sys21_campos_zukarmex.Models.DTOs.Authentication;

namespace sys21_campos_zukarmex.Services.Api;

/// <summary>
/// Servicio especializado para operaciones de autenticacion
/// </summary>
public class AuthenticationApiService : BaseApiService
{
    public AuthenticationApiService(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <summary>
    /// Realizar login con credenciales de usuario
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // Asegurar que tenemos la URL mas reciente
            UpdateBaseAddress();
            
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(AppConfigService.LoginEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<LoginResponse>(responseContent) ?? new LoginResponse();
            }
            
            return new LoginResponse { Success = false, Message = "Error de autenticacion" };
        }
        catch (Exception ex)
        {
            return new LoginResponse { Success = false, Message = ex.Message };
        }
    }
}