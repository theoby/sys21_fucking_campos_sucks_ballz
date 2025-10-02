using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Models.DTOs.Api;
using sys21_campos_zukarmex.Models.DTOs.Catalog;

namespace sys21_campos_zukarmex.Services.Api;

/// <summary>
/// Servicio especializado para operaciones de catalogos
/// </summary>
public class CatalogApiService : BaseApiService
{
    public CatalogApiService(HttpClient httpClient) : base(httpClient)
    {
    }

    #region Generic Catalog Operations

    protected async Task<List<T>> GetCatalogAsync<T>(string endpoint)
    {
        try
        {
            var fullUrl = GetFullUrl(endpoint);
            var response = await _httpClient.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"API Response - Estado: {standardResponse.Estado}, TotalDatos: {standardResponse.TotalDatos}, Mensaje: {standardResponse.Mensaje}");
                        return standardResponse.Datos ?? new List<T>();
                    }
                    else if (standardResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"API Error - Estado: {standardResponse.Estado}, Mensaje: {standardResponse.Mensaje}");
                        return new List<T>();
                    }
                }
                catch (JsonException)
                {
                    // Si falla la deserializacion de StandardApiResponse, intentar con ApiResponse legacy
                    System.Diagnostics.Debug.WriteLine("Intentando deserializar con estructura legacy...");
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                    return legacyResponse?.DataList ?? new List<T>();
                }
            }
            return new List<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GetCatalogAsync: {ex.Message}");
            return new List<T>();
        }
    }

    public async Task<T?> GetByIdAsync<T>(string endpoint, int id) where T : class
    {
        try
        {
            var fullUrl = GetFullUrl($"{endpoint}/{id}");
            var response = await _httpClient.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar con la nueva estructura StandardApiResponse
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<T>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        return standardResponse.FirstData;
                    }
                }
                catch (JsonException)
                {
                    // Si falla, intentar con ApiResponse legacy
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                    return legacyResponse?.Data;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GetByIdAsync: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Empresa Operations

    /// <summary>
    /// Obtiene las empresas desde la API usando el mapeo especifico para idEmpresa, nombre y esPromotora
    /// </summary>
    public async Task<List<Empresa>> GetEmpresasAsync()
    {
        try
        {
            var fullUrl = GetFullUrl(AppConfigService.EmpresasEndpoint);
            var response = await _httpClient.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Empresas: {content}");
                
                // Intentar deserializar con la nueva estructura StandardApiResponse<EmpresaApiDto>
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<EmpresaApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas API Response - Estado: {standardResponse.Estado}, TotalDatos: {standardResponse.TotalDatos}, Mensaje: {standardResponse.Mensaje}");
                        
                        // Convertir DTOs a modelos de dominio
                        var empresas = standardResponse.Datos.Select(dto => dto.ToEmpresa()).ToList();
                        
                        System.Diagnostics.Debug.WriteLine($"Empresas convertidas: {empresas.Count}");
                        foreach (var empresa in empresas)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - ID: {empresa.Id}, Nombre: {empresa.Nombre}, IsPromotora: {empresa.IsPromotora}");
                        }
                        
                        return empresas;
                    }
                    else if (standardResponse != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas API Error - Estado: {standardResponse.Estado}, Mensaje: {standardResponse.Mensaje}");
                        return new List<Empresa>();
                    }
                }
                catch (JsonException ex)
                {
                    // Si falla la deserializacion de StandardApiResponse, intentar con ApiResponse legacy
                    System.Diagnostics.Debug.WriteLine($"Error deserializando StandardApiResponse, intentando legacy: {ex.Message}");
                    
                    var legacyResponse = JsonConvert.DeserializeObject<ApiResponse<Empresa>>(content);
                    if (legacyResponse?.DataList != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas desde estructura legacy: {legacyResponse.DataList.Count}");
                        return legacyResponse.DataList;
                    }
                    
                    // Intentar deserializacion directa como lista de empresas
                    var directList = JsonConvert.DeserializeObject<List<Empresa>>(content);
                    if (directList != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Empresas desde lista directa: {directList.Count}");
                        return directList;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"HTTP Error - StatusCode: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase}");
            }
            
            return new List<Empresa>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetEmpresasAsync: {ex}");
            return new List<Empresa>();
        }
    }

    #endregion

    #region Almacen Operations

    /// <summary>
    /// Obtiene almacenes desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<Almacen>> GetAlmacenesAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.AlmacenesEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Almacenes: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<AlmacenApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var almacenes = standardResponse.Datos.Select(dto => dto.ToAlmacen()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Almacenes convertidos: {almacenes.Count}");
                        return almacenes;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Almacen>(AppConfigService.AlmacenesEndpoint);
                }
            }
            return new List<Almacen>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetAlmacenesAsync: {ex}");
            return new List<Almacen>();
        }
    }

    public async Task<Almacen?> GetAlmacenByIdAsync(int id)
    {
        return await GetByIdAsync<Almacen>(AppConfigService.AlmacenesEndpoint, id);
    }

    #endregion

    #region Articulo Operations

    /// <summary>
    /// Obtiene articulos desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<Articulo>> GetArticulosAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.ArticulosEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Articulos: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<ArticuloApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var articulos = standardResponse.Datos.Select(dto => dto.ToArticulo()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Articulos convertidos: {articulos.Count}");
                        return articulos;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Articulo>(AppConfigService.ArticulosEndpoint);
                }
            }
            return new List<Articulo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetArticulosAsync: {ex}");
            return new List<Articulo>();
        }
    }

    public async Task<Articulo?> GetArticuloByIdAsync(int id)
    {
        return await GetByIdAsync<Articulo>(AppConfigService.ArticulosEndpoint, id);
    }

    #endregion

    #region Campo Operations

    /// <summary>
    /// Obtiene campos desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<Campo>> GetCamposAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.CamposEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Campos: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<CampoApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var campos = standardResponse.Datos.Select(dto => dto.ToCampo()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Campos convertidos: {campos.Count}");
                        return campos;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Campo>(AppConfigService.CamposEndpoint);
                }
            }
            return new List<Campo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetCamposAsync: {ex}");
            return new List<Campo>();
        }
    }

    public async Task<Campo?> GetCampoByIdAsync(int id)
    {
        return await GetByIdAsync<Campo>(AppConfigService.CamposEndpoint, id);
    }

    #endregion

    #region Familia Operations

    /// <summary>
    /// Obtiene familias desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<Familia>> GetFamiliasAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.FamiliasEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Familias: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<FamiliaApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var familias = standardResponse.Datos.Select(dto => dto.ToFamilia()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Familias convertidas: {familias.Count}");
                        return familias;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Familia>(AppConfigService.FamiliasEndpoint);
                }
            }
            return new List<Familia>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetFamiliasAsync: {ex}");
            return new List<Familia>();
        }
    }

    public async Task<Familia?> GetFamiliaByIdAsync(int id)
    {
        return await GetByIdAsync<Familia>(AppConfigService.FamiliasEndpoint, id);
    }

    #endregion

    #region Inspector Operations

    /// <summary>
    /// Obtiene inspectores desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<Inspector>> GetInspectoresAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.InspectoresEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Inspectores: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<InspectorApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var inspectores = standardResponse.Datos.Select(dto => dto.ToInspector()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Inspectores convertidos: {inspectores.Count}");
                        return inspectores;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Inspector>(AppConfigService.InspectoresEndpoint);
                }
            }
            return new List<Inspector>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetInspectoresAsync: {ex}");
            return new List<Inspector>();
        }
    }

    public async Task<Inspector?> GetInspectorByIdAsync(int id)
    {
        return await GetByIdAsync<Inspector>(AppConfigService.InspectoresEndpoint, id);
    }

    #endregion

    #region Maquinaria Operations

    /// <summary>
    /// Obtiene maquinaria desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<Maquinaria>> GetMaquinariasAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.MaquinariasEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Maquinarias: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<MaquinariaApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var maquinarias = standardResponse.Datos.Select(dto => dto.ToMaquinaria()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Maquinarias convertidas: {maquinarias.Count}");
                        return maquinarias;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Maquinaria>(AppConfigService.MaquinariasEndpoint);
                }
            }
            return new List<Maquinaria>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetMaquinariasAsync: {ex}");
            return new List<Maquinaria>();
        }
    }

    public async Task<Maquinaria?> GetMaquinariaByIdAsync(int id)
    {
        return await GetByIdAsync<Maquinaria>(AppConfigService.MaquinariasEndpoint, id);
    }

    #endregion

    #region SubFamilia Operations

    /// <summary>
    /// Obtiene subfamilias desde la API con mapeo especifico de DTOs
    /// </summary>
    public async Task<List<SubFamilia>> GetSubFamiliasAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.SubFamiliasEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - SubFamilias: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<SubFamiliaApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var subfamilias = standardResponse.Datos.Select(dto => dto.ToSubFamilia()).ToList();
                        System.Diagnostics.Debug.WriteLine($"SubFamilias convertidas: {subfamilias.Count}");
                        return subfamilias;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<SubFamilia>(AppConfigService.SubFamiliasEndpoint);
                }
            }
            return new List<SubFamilia>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetSubFamiliasAsync: {ex}");
            return new List<SubFamilia>();
        }
    }

    public async Task<SubFamilia?> GetSubFamiliaByIdAsync(int id)
    {
        return await GetByIdAsync<SubFamilia>(AppConfigService.SubFamiliasEndpoint, id);
    }

    #endregion

    #region Lote Operations

    /// <summary>
    /// Obtiene lotes desde la API con mapeo específico de DTOs
    /// </summary>
    public async Task<List<Lote>> GetLotesAsync()
    {
        try
        {
            UpdateBaseAddress();
            var response = await _httpClient.GetAsync(AppConfigService.LotesEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Raw API Response - Lotes: {content}");
                
                try
                {
                    var standardResponse = JsonConvert.DeserializeObject<StandardApiResponse<LoteApiDto>>(content);
                    if (standardResponse != null && standardResponse.Success)
                    {
                        var lotes = standardResponse.Datos.Select(dto => dto.ToLote()).ToList();
                        System.Diagnostics.Debug.WriteLine($"Lotes convertidos: {lotes.Count}");
                        return lotes;
                    }
                }
                catch (JsonException)
                {
                    return await GetCatalogAsync<Lote>(AppConfigService.LotesEndpoint);
                }
            }
            return new List<Lote>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception en GetLotesAsync: {ex}");
            return new List<Lote>();
        }
    }

    public async Task<Lote?> GetLoteByIdAsync(int id)
    {
        return await GetByIdAsync<Lote>(AppConfigService.LotesEndpoint, id);
    }

    #endregion
}