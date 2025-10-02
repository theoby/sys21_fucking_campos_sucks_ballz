using System.Text;
using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Api;
using sys21_campos_zukarmex.Models.DTOs.Bulk;

namespace sys21_campos_zukarmex.Services.Api;

/// <summary>
/// Servicio especializado para operaciones CRUD de API
/// </summary>
public class CrudApiService : BaseApiService
{
    public CrudApiService(HttpClient httpClient) : base(httpClient)
    {
    }

    #region Generic CRUD Operations

    public async Task<ApiResponse<T>> CreateAsync<T>(string endpoint, T item) where T : class
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(fullUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<T>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.FirstData
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent) ?? 
                       new ApiResponse<T> { Success = false, Message = "Error al crear registro" };
            }
            
            return new ApiResponse<T> { Success = false, Message = "Error al crear registro" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<T>> UpdateAsync<T>(string endpoint, int id, T item) where T : class
    {
        try
        {
            var fullUrl = GetFullUrl($"{endpoint}/{id}");
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(fullUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Intentar deserializar con StandardApiResponse primero
            try
            {
                var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(responseContent);
                if (standardResponse != null)
                {
                    return new ApiResponse<T>
                    {
                        Success = standardResponse.Success,
                        Message = standardResponse.Mensaje,
                        Data = standardResponse.FirstData
                    };
                }
            }
            catch (JsonException)
            {
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent) ?? 
                       new ApiResponse<T> { Success = false, Message = "Error al actualizar registro" };
            }
            
            return new ApiResponse<T> { Success = false, Message = "Error al actualizar registro" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string endpoint, int id)
    {
        try
        {
            var fullUrl = GetFullUrl($"{endpoint}/{id}");
            var response = await _httpClient.DeleteAsync(fullUrl);
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
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<bool>>(responseContent) ?? 
                       new ApiResponse<bool> { Success = false, Message = "Error al eliminar registro" };
            }
            
            return new ApiResponse<bool> { Success = false, Message = "Error al eliminar registro" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    #endregion

    #region Bulk Operations

    public async Task<ApiResponse<bool>> BulkCreateAsync<T>(string endpoint, List<T> items) where T : class
    {
        try
        {
            var fullUrl = GetFullUrl($"{endpoint}/bulk");
            var json = JsonConvert.SerializeObject(items);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(fullUrl, content);
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
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<bool>>(responseContent) ?? 
                       new ApiResponse<bool> { Success = false, Message = "Error en creacion masiva" };
            }
            
            return new ApiResponse<bool> { Success = false, Message = "Error en creacion masiva" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<bool>> BulkUpdateAsync<T>(string endpoint, List<T> items) where T : class
    {
        try
        {
            var fullUrl = GetFullUrl($"{endpoint}/bulk");
            var json = JsonConvert.SerializeObject(items);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(fullUrl, content);
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
                // Si falla, usar deserializacion legacy
                return JsonConvert.DeserializeObject<ApiResponse<bool>>(responseContent) ?? 
                       new ApiResponse<bool> { Success = false, Message = "Error en actualizacion masiva" };
            }
            
            return new ApiResponse<bool> { Success = false, Message = "Error en actualizacion masiva" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    #endregion

    #region Empresa CRUD Operations

    public async Task<ApiResponse<Empresa>> CreateEmpresaAsync(Empresa empresa)
    {
        return await CreateAsync(AppConfigService.EmpresasEndpoint, empresa);
    }

    public async Task<ApiResponse<Empresa>> UpdateEmpresaAsync(int id, Empresa empresa)
    {
        return await UpdateAsync(AppConfigService.EmpresasEndpoint, id, empresa);
    }

    public async Task<ApiResponse<bool>> DeleteEmpresaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.EmpresasEndpoint, id);
    }

    #endregion

    #region Almacen CRUD Operations

    public async Task<ApiResponse<Almacen>> CreateAlmacenAsync(Almacen almacen)
    {
        return await CreateAsync(AppConfigService.AlmacenesEndpoint, almacen);
    }

    public async Task<ApiResponse<Almacen>> UpdateAlmacenAsync(int id, Almacen almacen)
    {
        return await UpdateAsync(AppConfigService.AlmacenesEndpoint, id, almacen);
    }

    public async Task<ApiResponse<bool>> DeleteAlmacenAsync(int id)
    {
        return await DeleteAsync(AppConfigService.AlmacenesEndpoint, id);
    }

    #endregion

    #region Articulo CRUD Operations

    public async Task<ApiResponse<Articulo>> CreateArticuloAsync(Articulo articulo)
    {
        return await CreateAsync(AppConfigService.ArticulosEndpoint, articulo);
    }

    public async Task<ApiResponse<Articulo>> UpdateArticuloAsync(int id, Articulo articulo)
    {
        return await UpdateAsync(AppConfigService.ArticulosEndpoint, id, articulo);
    }

    public async Task<ApiResponse<bool>> DeleteArticuloAsync(int id)
    {
        return await DeleteAsync(AppConfigService.ArticulosEndpoint, id);
    }

    #endregion

    #region Campo CRUD Operations

    public async Task<ApiResponse<Campo>> CreateCampoAsync(Campo campo)
    {
        return await CreateAsync(AppConfigService.CamposEndpoint, campo);
    }

    public async Task<ApiResponse<Campo>> UpdateCampoAsync(int id, Campo campo)
    {
        return await UpdateAsync(AppConfigService.CamposEndpoint, id, campo);
    }

    public async Task<ApiResponse<bool>> DeleteCampoAsync(int id)
    {
        return await DeleteAsync(AppConfigService.CamposEndpoint, id);
    }

    #endregion

    #region Familia CRUD Operations

    public async Task<ApiResponse<Familia>> CreateFamiliaAsync(Familia familia)
    {
        return await CreateAsync(AppConfigService.FamiliasEndpoint, familia);
    }

    public async Task<ApiResponse<Familia>> UpdateFamiliaAsync(int id, Familia familia)
    {
        return await UpdateAsync(AppConfigService.FamiliasEndpoint, id, familia);
    }

    public async Task<ApiResponse<bool>> DeleteFamiliaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.FamiliasEndpoint, id);
    }

    #endregion

    #region Inspector CRUD Operations

    public async Task<ApiResponse<Inspector>> CreateInspectorAsync(Inspector inspector)
    {
        return await CreateAsync(AppConfigService.InspectoresEndpoint, inspector);
    }

    public async Task<ApiResponse<Inspector>> UpdateInspectorAsync(int id, Inspector inspector)
    {
        return await UpdateAsync(AppConfigService.InspectoresEndpoint, id, inspector);
    }

    public async Task<ApiResponse<bool>> DeleteInspectorAsync(int id)
    {
        return await DeleteAsync(AppConfigService.InspectoresEndpoint, id);
    }

    #endregion

    #region Maquinaria CRUD Operations

    public async Task<ApiResponse<Maquinaria>> CreateMaquinariaAsync(Maquinaria maquinaria)
    {
        return await CreateAsync(AppConfigService.MaquinariasEndpoint, maquinaria);
    }

    public async Task<ApiResponse<Maquinaria>> UpdateMaquinariaAsync(int id, Maquinaria maquinaria)
    {
        return await UpdateAsync(AppConfigService.MaquinariasEndpoint, id, maquinaria);
    }

    public async Task<ApiResponse<bool>> DeleteMaquinariaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.MaquinariasEndpoint, id);
    }

    #endregion

    #region SubFamilia CRUD Operations

    public async Task<ApiResponse<SubFamilia>> CreateSubFamiliaAsync(SubFamilia subFamilia)
    {
        return await CreateAsync(AppConfigService.SubFamiliasEndpoint, subFamilia);
    }

    public async Task<ApiResponse<SubFamilia>> UpdateSubFamiliaAsync(int id, SubFamilia subFamilia)
    {
        return await UpdateAsync(AppConfigService.SubFamiliasEndpoint, id, subFamilia);
    }

    public async Task<ApiResponse<bool>> DeleteSubFamiliaAsync(int id)
    {
        return await DeleteAsync(AppConfigService.SubFamiliasEndpoint, id);
    }

    #endregion
    
    #region User CRUD Operations

    public async Task<ApiResponse<User>> CreateUserAsync(User user)
    {
        return await CreateAsync("users", user);
    }

    public async Task<ApiResponse<User>> UpdateUserAsync(int id, User user)
    {
        return await UpdateAsync("users", id, user);
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
    {
        return await DeleteAsync("users", id);
    }

    #endregion
}