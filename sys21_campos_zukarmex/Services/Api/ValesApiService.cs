using System.Text;
using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Api;

namespace sys21_campos_zukarmex.Services.Api;

/// <summary>
/// Servicio especializado para operaciones de vales
/// </summary>
public class ValesApiService : BaseApiService
{
    public ValesApiService(HttpClient httpClient) : base(httpClient)
    {
    }

    #region Vale Operations

    public async Task<ApiResponse<Salida>> SaveValeAsync(Salida salida)
    {
        try
        {
            UpdateBaseAddress();
            var json = JsonConvert.SerializeObject(salida);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(AppConfigService.ValesSalidaEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<Salida>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<Salida>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.FirstData
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserialización legacy
                return JsonConvert.DeserializeObject<ApiResponse<Salida>>(responseContent) ?? 
                       new ApiResponse<Salida> { Success = false, Message = "Error al guardar vale" };
            }
            
            return new ApiResponse<Salida> { Success = false, Message = "Error al guardar vale" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<Salida> { Success = false, Message = ex.Message };
        }
    }

    public async Task<List<Salida>> GetValesStatusAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.ApiStatusEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<Salida>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        return standardResponse.Datos ?? new List<Salida>();
                    }
                }
                catch (JsonException)
                {
                    // Si falla, intentar con ApiResponse legacy
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<Salida>>(content);
                    return legacyResponse?.DataList ?? new List<Salida>();
                }
            }
            return new List<Salida>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GetValesStatusAsync: {ex.Message}");
            return new List<Salida>();
        }
    }

    public async Task<List<Salida>> GetValesAutorizacionAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.ApiAutorizacionEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<Salida>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        return standardResponse.Datos ?? new List<Salida>();
                    }
                }
                catch (JsonException)
                {
                    // Si falla, intentar con ApiResponse legacy
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<Salida>>(content);
                    return legacyResponse?.DataList ?? new List<Salida>();
                }
            }
            return new List<Salida>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GetValesAutorizacionAsync: {ex.Message}");
            return new List<Salida>();
        }
    }

    public async Task<ApiResponse<bool>> AuthorizeValeAsync(int valeId, bool authorize)
    {
        try
        {
            UpdateBaseAddress();
            var request = new { ValeId = valeId, Authorize = authorize };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(AppConfigService.ApiAutorizacionEndpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<bool>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.Success
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserialización legacy
                return JsonConvert.DeserializeObject<ApiResponse<bool>>(responseContent) ?? 
                       new ApiResponse<bool> { Success = false, Message = "Error al autorizar vale" };
            }
            
            return new ApiResponse<bool> { Success = false, Message = "Error al autorizar vale" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    #endregion
}