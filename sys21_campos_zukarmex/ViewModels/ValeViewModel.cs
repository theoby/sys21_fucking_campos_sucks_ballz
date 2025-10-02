using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;
using sys21_campos_zukarmex.Views; 

namespace sys21_campos_zukarmex.ViewModels;

[QueryProperty(nameof(Result), "Result")]
[QueryProperty(nameof(SalidaDetalleResult), "SalidaDetalle")]

public partial class ValeViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly ApiService _apiService;
    private readonly SessionService _sessionService;
    private readonly ValeNavigationService _navigationService;
    private bool isInitialized = false;
    //campo para filtrar si el vale requiere autorizacion
    [ObservableProperty]
    private bool? requiereAutorizacion = null;
    private readonly ConnectivityService _connectivityService;
    public ConnectivityService ConnectivitySvc => _connectivityService;
    //Declaracion de TipoEntrada Global
    public ObservableCollection<string> TipoEntrada { get; set; }

    public ValeViewModel(DatabaseService databaseService, ApiService apiService, SessionService sessionService, ValeNavigationService navigationService, ConnectivityService connectivityService)
    {
        _databaseService = databaseService;
        _apiService = apiService;
        _sessionService = sessionService;
        _navigationService = navigationService;
        Title = "Vales de Salida";
        Vales = new ObservableCollection<Salida>();
        Campos = new ObservableCollection<Campo>();
        Almacenes = new ObservableCollection<Almacen>();
        Lotes = new ObservableCollection<Lote>();
        Recetas = new ObservableCollection<Receta>();
        ArticulosDetalle = new ObservableCollection<SalidaDetalle>();
        DebugInfo = string.Empty; // Inicializar para evitar CS8618
        _connectivityService = connectivityService;

        // Llenado de Picker de Tipo de Recetas
        TipoEntrada = new ObservableCollection<string>
        {
            "Receta",
            "Articulo"
        };
    }

    #region Properties

    [ObservableProperty]
    private ObservableCollection<Salida> vales;

    [ObservableProperty]
    private ObservableCollection<Campo> campos;

    [ObservableProperty]
    private ObservableCollection<Almacen> almacenes;

    [ObservableProperty]
    private ObservableCollection<Lote> lotes;

    [ObservableProperty]
    private ObservableCollection<Receta> recetas;

    [ObservableProperty]
    private ObservableCollection<SalidaDetalle> articulosDetalle;

    [ObservableProperty]
    private Salida? selectedVale;

    [ObservableProperty]
    private Lote? selectedLote;

    [ObservableProperty]
    private Receta? selectedReceta;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string searchText = string.Empty;

    // Form properties
    [ObservableProperty]
    private Campo? selectedCampo;

    [ObservableProperty]
    private Almacen? selectedAlmacen;

    [ObservableProperty]
    private DateTime fecha = DateTime.Now;

    [ObservableProperty]
    private string concepto = string.Empty;

    [ObservableProperty]
    private string usuario = string.Empty;

    [ObservableProperty]
    private bool hasArticulos;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string articulosCountMessage = "No hay articulos agregados";
    
    [ObservableProperty]
    private string? selectedTipoEntrada;
    
    [ObservableProperty]
    private bool showLotesAndRecetas = false;
    
    [ObservableProperty]
    private bool areFieldsLocked = false;
    
    // Campo para recordar el tipo original cuando se agregan artículos  
    private string? _originalTipoEntrada = null;
    
    // Navigation properties
    public string Result { get; set; } = string.Empty;
    public object SalidaDetalleResult { get; set; } = new();
    public string DebugInfo { get; private set; } = string.Empty; // Inicializar propiedad

    #endregion

    #region Existing Commands

    public override async Task InitializeAsync()//verificar entrada de ruta para que no borre los selects
    {
        if (isInitialized)
            return;

        await LoadCatalogsAsync();
        await LoadValesAsync();
        await LoadCurrentUserAsync();
        await VerifyLotesDataAsync(); // Verificar datos de lotes

        // Verificar y crear recetas de prueba
        await VerifyRecetasDataAsync();

        isInitialized = true;
    }

    /// <summary>
    /// Verifica si existen lotes en la base de datos, si no existen, crea algunos de prueba
    /// </summary>
    private async Task VerifyLotesDataAsync()
    {
        try
        {
            var lotesCount = await _databaseService.CountAsync<Lote>();
            var camposCount = await _databaseService.CountAsync<Campo>();
            
            System.Diagnostics.Debug.WriteLine($"Verificación de datos: {lotesCount} lotes, {camposCount} campos");
            
            // Si no hay lotes pero hay campos, crear algunos lotes de prueba
            if (lotesCount == 0 && camposCount > 0)
            {
                var campos = await _databaseService.GetAllAsync<Campo>();
                var lotesPrueba = new List<Lote>();
                
                foreach (var campo in campos.Take(3)) // Solo los primeros 3 campos
                {
                    for (int i = 1; i <= 2; i++) // 2 lotes por campo
                    {
                        var lote = new Lote
                        {
                            Nombre = $"Lote {i} - {campo.Nombre}",
                            IdCampo = campo.Id,
                            Hectareas = 10.5m + i
                        };
                        lotesPrueba.Add(lote);
                    }
                }
                
                // Guardar los lotes de prueba
                foreach (var lote in lotesPrueba)
                {
                    await _databaseService.SaveAsync(lote);
                }
                
                System.Diagnostics.Debug.WriteLine($"Creados {lotesPrueba.Count} lotes de prueba");
                
                // Recargar los catálogos para mostrar los nuevos lotes
                await LoadCatalogsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en VerifyLotesDataAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si existen recetas en la base de datos, si no existen, crea algunas de prueba
    /// </summary>
    private async Task VerifyRecetasDataAsync()
    {
        try
        {
            var recetasCount = await _databaseService.CountAsync<Receta>();
            var almacenesCount = await _databaseService.CountAsync<Almacen>();
            
            System.Diagnostics.Debug.WriteLine($"Verificación de recetas: {recetasCount} recetas, {almacenesCount} almacenes");
            
            // Si no hay recetas pero hay almacenes, crear algunas recetas de prueba
            if (recetasCount == 0 && almacenesCount > 0)
            {
                var almacenes = await _databaseService.GetAllAsync<Almacen>();
                var recetasPrueba = new List<Receta>();
                
                var tiposReceta = new[] { 
                    "Fertilización NPK", 
                    "Fumigación General", 
                    "Herbicida Selectivo", 
                    "Insecticida Sistémico",
                    "Control de Plagas",
                    "Nutrición Foliar"
                };
                
                foreach (var almacen in almacenes.Take(3)) // Primeros 3 almacenes
                {
                    for (int i = 0; i < tiposReceta.Length; i++)
                    {
                        var receta = new Receta
                        {
                            NombreReceta = $"{tiposReceta[i]} - {almacen.Nombre}",
                            IdAlmacen = almacen.Id,
                            IdCampo = 1, // Asignar a campo 1 por defecto para pruebas
                            TipoReceta = i + 1,
                            IdReceta = (almacen.Id * 100) + i + 1 // Generar ID único
                        };
                        recetasPrueba.Add(receta);
                    }
                }
                
                // Guardar las recetas de prueba
                foreach (var receta in recetasPrueba)
                {
                    await _databaseService.SaveAsync(receta);
                }
                
                System.Diagnostics.Debug.WriteLine($"Creadas {recetasPrueba.Count} recetas de prueba");
                
                // Crear artículos de prueba para algunas recetas
                await CreateRecetaArticulosPruebaAsync(recetasPrueba);
                
                // Recargar los catálogos para mostrar las nuevas recetas
                await LoadCatalogsAsync();
            }
            else if (recetasCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Ya existen {recetasCount} recetas en la base de datos");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en VerifyRecetasDataAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Crea artículos de prueba para las recetas generadas
    /// </summary>
    private async Task CreateRecetaArticulosPruebaAsync(List<Receta> recetas)
    {
        try
        {
            var articulos = await _databaseService.GetAllAsync<Articulo>();
            var familias = await _databaseService.GetAllAsync<Familia>();
            var subfamilias = await _databaseService.GetAllAsync<SubFamilia>();
            
            if (!articulos.Any() || !familias.Any())
            {
                System.Diagnostics.Debug.WriteLine("No hay artículos o familias disponibles para crear RecetaArticulos de prueba");
                return;
            }
            
            var articulosPrueba = new List<RecetaArticulo>();
            var random = new Random();
            
            // Para cada receta, agregar 2-4 artículos aleatorios
            foreach (var receta in recetas.Take(3)) // Solo para las primeras 3 recetas
            {
                var cantidadArticulos = random.Next(2, 5); // 2-4 artículos por receta
                var articulosUsados = new HashSet<int>();
                
                for (int i = 0; i < cantidadArticulos; i++)
                {
                    var articulo = articulos[random.Next(articulos.Count)];
                    
                    // Evitar duplicados en la misma receta
                    if (articulosUsados.Contains(articulo.Id))
                        continue;
                        
                    articulosUsados.Add(articulo.Id);
                    
                    var familia = familias.FirstOrDefault(f => f.Id == articulo.IdFamilia);
                    var subfamilia = subfamilias.FirstOrDefault(sf => sf.Id == articulo.IdSubFamilia);
                    
                    var recetaArticulo = new RecetaArticulo
                    {
                        IdReceta = receta.IdReceta, // Usar IdReceta de la API
                        IdArticulo = articulo.Id,
                        IdFamilia = articulo.IdFamilia,
                        IdSubFamilia = articulo.IdSubFamilia,
                        Dosis = Math.Round((decimal)(random.NextDouble() * 5 + 0.5), 2), // 0.5 - 5.5
                        Total = Math.Round((decimal)(random.NextDouble() * 100 + 10), 2) // 10 - 110
                    };
                    
                    articulosPrueba.Add(recetaArticulo);
                }
            }
            
            // Guardar todos los artículos de receta
            foreach (var articuloPrueba in articulosPrueba)
            {
                await _databaseService.SaveAsync(articuloPrueba);
            }
            
            System.Diagnostics.Debug.WriteLine($"Creados {articulosPrueba.Count} RecetaArticulos de prueba para {articulosPrueba.Select(ra => ra.IdReceta).Distinct().Count()} recetas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en CreateRecetaArticulosPruebaAsync: {ex.Message}");
        }
    }

    private async Task RefreshPageAsync()
    {
        await LoadCatalogsAsync();
        await LoadValesAsync();
        Concepto = string.Empty;
        ArticulosDetalle.Clear();
    }

    [RelayCommand]
    private async Task LoadCatalogsAsync()
    {
        try
        {
            var session = await _sessionService.GetCurrentSessionAsync();
            var campos = await _databaseService.GetAllAsync<Campo>();
            var almacenes = await _databaseService.GetAllAsync<Almacen>();
            var lotes = await _databaseService.GetAllAsync<Lote>();
            var recetas = await _databaseService.GetAllAsync<Receta>();
            
            // DEBUG: Verificar cuántos lotes hay en la base de datos
            System.Diagnostics.Debug.WriteLine($"=== CARGA DE CATALOGOS ===");
            System.Diagnostics.Debug.WriteLine($"Campos en BD: {campos.Count}");
            System.Diagnostics.Debug.WriteLine($"Almacenes en BD: {almacenes.Count}");
            System.Diagnostics.Debug.WriteLine($"Lotes en BD: {lotes.Count}");
            System.Diagnostics.Debug.WriteLine($"Recetas en BD: {recetas.Count}");
            
            // Lista por si se necesitan filtrar los campos
            var camposFiltrados = new List<Campo>();

            Campos.Clear();
            Almacenes.Clear();
            Lotes.Clear();
            Recetas.Clear();

            // Agregar almacenes a la colección
            foreach (var almacen in almacenes) 
                Almacenes.Add(almacen);

            // Agregar TODOS los lotes a la colección inicialmente
            foreach (var lote in lotes.OrderBy(l => l.Nombre))
            {
                Lotes.Add(lote);
                System.Diagnostics.Debug.WriteLine($"Lote agregado: ID={lote.Id}, Nombre={lote.Nombre}, IdCampo={lote.IdCampo}");
            }

            // Agregar TODAS las recetas a la colección
            foreach (var receta in recetas.OrderBy(r => r.NombreReceta))
            {
                Recetas.Add(receta);
                System.Diagnostics.Debug.WriteLine($"Receta agregada: IdReceta={receta.IdReceta}, Nombre={receta.NombreReceta}, Almacen={receta.IdAlmacen}");
            }
           
            if (session.TipoUsuario == 1) //Admin =1
            {
                // TIPO 1 (Admin): Puede ver todos los campos.
                camposFiltrados = campos;
            }
            else// Supervisor/Auxiliar = 2,3
            {
                // TIPO 2 y 3 (Supervisor/Usuario): Solo ven campos asignados a su IdInspector.
                camposFiltrados = campos.Where(c => c.IdInspector == session.IdInspector).ToList();//Modificar cuando se tengan los IdInspector asignados en SESION
                // Si no tienen un IdInspector assigned, la lista 'camposFiltrados' quedara vacia.
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var campo in camposFiltrados.OrderBy(c => c.Nombre))
                {
                    Campos.Add(campo);
                }
                
                System.Diagnostics.Debug.WriteLine($"=== RESULTADO FINAL ===");
                System.Diagnostics.Debug.WriteLine($"Campos agregados a UI: {Campos.Count}");
                System.Diagnostics.Debug.WriteLine($"Almacenes agregados a UI: {Almacenes.Count}");
                System.Diagnostics.Debug.WriteLine($"Lotes agregados a UI: {Lotes.Count}");
                System.Diagnostics.Debug.WriteLine($"Recetas agregadas a UI: {Recetas.Count}");
            });

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en LoadCatalogsAsync: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", $"Error al cargar catalogos: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task PageAppearingAsync()
    {
        System.Diagnostics.Debug.WriteLine("PageAppearingAsync started");
        
        if (!isInitialized)
        {
            await LoadCatalogsAsync();
            await LoadCurrentUserAsync();
            isInitialized = true;
        }

        await LoadValesAsync();

        // Handle navigation result from AgregarArticuloPage
        // Check ValeNavigationService first (this is where the SalidaDetail object is)
        RecogerArticuloAgregado();
        
        // Then handle any simple Shell navigation result
        HandleNavigationResult();
        
        System.Diagnostics.Debug.WriteLine("PageAppearingAsync completed");
    }

    [RelayCommand]//Edicion para filtrar campos por tipo de usuario - 1,(2,3)
    private async Task LoadValesAsync()
    {
        SetBusy(true);
        IsRefreshing = true;
        try
        {
            
            var campos = await _databaseService.GetAllAsync<Campo>();
            var almacenes = await _databaseService.GetAllAsync<Almacen>();
            var lotes = await _databaseService.GetAllAsync<Lote>();

            //Visualizacion No se usa actualmente (se paso para Status)
            var valesFromDb = await _databaseService.GetAllAsync<Salida>();
           
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                valesFromDb = valesFromDb.Where(v =>
                    (v.Concepto != null && v.Concepto.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (v.Usuario != null && v.Usuario.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (v.Folio != null && v.Folio.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var valesEnriquecidos = new List<Salida>();

            foreach (var vale in valesFromDb)
            {
                vale.CampoNombre = campos.FirstOrDefault(c => c.Id == vale.IdCampo)?.Nombre ?? "Campo N/A";
                vale.AlmacenNombre = almacenes.FirstOrDefault(a => a.Id == vale.IdAlmacen)?.Nombre ?? "Almacen N/A";
                valesEnriquecidos.Add(vale);
            }

            Vales.Clear();

            foreach (var vale in valesEnriquecidos.OrderByDescending(v => v.Fecha))
            {
                Vales.Add(vale);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar vales: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LoadCurrentUserAsync()
    {
        try
        {
            Usuario = await _sessionService.GetCurrentUserNameAsync();
        }
        catch
        {
            Usuario = "Usuario";
        }
    }

    [RelayCommand]
    private void NewVale()
    {
        ClearForm();
        IsEditing = true;
    }

    [RelayCommand]
    private async Task EditValeAsync(Salida vale)
    {
        if (vale == null) return;

        SetBusy(true);

        try
        {
            ClearForm();
            SelectedVale = vale;

            SelectedCampo = Campos.FirstOrDefault(c => c.Id == vale.IdCampo);
            await Task.Delay(100);
            SelectedAlmacen = Almacenes.FirstOrDefault(a => a.Id == vale.IdAlmacen);
            SelectedLote = Lotes.FirstOrDefault(l => l.Id == vale.IdLote);
            
            // Cargar receta seleccionada si existe
            if (vale.IdReceta > 0)
            {
                var receta = Recetas.FirstOrDefault(r => r.IdReceta == vale.IdReceta);
                if (receta != null)
                {
                    SelectedReceta = receta;
                    SelectedTipoEntrada = "Receta"; // Si hay receta seleccionada, el tipo es Receta
                }
            }
            else
            {
                SelectedTipoEntrada = "Articulo"; // Si no hay receta, el tipo es Articulo
            }

            Fecha = vale.Fecha;
            Concepto = vale.Concepto;
            Usuario = vale.Usuario;
            IsEditing = true;

            await LoadArticulosDetalleAsync();

            IsEditing = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"No se pudieron cargar los datos del vale para editar: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task SaveValeAsync()
    {
        if (!ValidateForm()) return;

        SetBusy(true);
        bool valeGuardadoLocalmente = false;
        int valeIdLocal = 0;
        
        try
        {
            var vale = SelectedVale ?? new Salida();

            vale.IdCampo = SelectedCampo!.Id;
            vale.IdAlmacen = SelectedAlmacen!.Id;
            vale.IdLote = SelectedLote?.Id ?? 0;
            vale.IdReceta = SelectedReceta?.IdReceta ?? 0;
            vale.Fecha = Fecha;
            vale.Concepto = Concepto;
            vale.Usuario = Usuario;
            //vale.EsReceta = Selected
            vale.Status = false; // Pending by default

            // Set the detail list
            vale.SalidaDetalle = ArticulosDetalle.ToList();

            // =========================================================================
            // LOGICA DE GUARDADO MODIFICADA SEGUN REQUERIMIENTOS
            // =========================================================================
            
            // Verificar conectividad antes de decidir la estrategia de guardado
            bool isConnected = _connectivityService?.IsConnected ?? false;
            
            if (isConnected)
            {
                System.Diagnostics.Debug.WriteLine("=== USUARIO ONLINE - INTENTANDO GUARDAR EN API PRIMERO ===");
                
                try
                {
                    // Si hay conexión, intentar guardar en API PRIMERO
                    var apiResponse = await _apiService.SaveValeAsync(vale);
                    
                    if (apiResponse.Success)
                    {
                        // ✅ API guardó exitosamente - NO guardar en BD local
                        System.Diagnostics.Debug.WriteLine("Vale guardado exitosamente en API - NO se guarda localmente");
                        
                        await LoadValesAsync();
                        string mensaje = (RequiereAutorizacion ?? false)
                         ? "Vale enviado correctamente, Requiere Autorización."
                         : "Vale enviado correctamente, No requiere autorización.";

                        await Shell.Current.DisplayAlert("Éxito", mensaje, "OK");
                        return; // Salir inmediatamente, no guardar localmente
                    }
                    else
                    {
                        // ❌ API falló - verificar si es por sesión caducada (401)
                        if (apiResponse.Message != null && apiResponse.Message.Contains("Sesion caducada", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine("🚨 SESION CADUCADA DETECTADA - NO SE GUARDA EL VALE LOCALMENTE");
                            // En este caso, ValidateHttpResponseAsync ya mostró el mensaje y redirigió al login
                            // No guardamos el vale localmente y salimos
                            return;
                        }
                        else
                        {
                            // ❌ API falló por otro motivo - Guardar localmente para sync manual
                            System.Diagnostics.Debug.WriteLine($"API falló con mensaje: {apiResponse.Message} - Guardando localmente");
                            
                            // Guardar localmente como fallback
                            await _databaseService.SaveAsync(vale);
                            valeGuardadoLocalmente = true;
                            valeIdLocal = vale.Id;
                            
                            // Si es nuevo vale, actualizar los detalles con el ID generado
                            if (SelectedVale == null && vale.Id > 0)
                            {
                                foreach (var detalle in ArticulosDetalle)
                                {
                                    detalle.IdSalida = vale.Id;
                                    detalle.SalidaId = vale.Id;
                                    await _databaseService.SaveAsync(detalle);
                                }
                            }

                            await LoadValesAsync();
                            await Shell.Current.DisplayAlert("Aviso", "Vale guardado en base de datos para sincronizacion manual", "OK");
                        }
                    }
                }
                catch (Exception apiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en API: {apiEx.Message} - Guardando localmente");
                    
                    // Si hay error en la comunicación con API (no 401), guardar localmente
                    await _databaseService.SaveAsync(vale);
                    valeGuardadoLocalmente = true;
                    valeIdLocal = vale.Id;
                    
                    // Si es nuevo vale, actualizar los detalles con el ID generado
                    if (SelectedVale == null && vale.Id > 0)
                    {
                        foreach (var detalle in ArticulosDetalle)
                        {
                            detalle.IdSalida = vale.Id;
                            detalle.SalidaId = vale.Id;
                            await _databaseService.SaveAsync(detalle);
                        }
                    }

                    await LoadValesAsync();
                    await Shell.Current.DisplayAlert("Aviso", "Vale guardado en base de datos para sincronizacion manual", "OK");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("=== USUARIO OFFLINE - GUARDANDO SOLO LOCALMENTE ===");
                
                // Si no hay conexión, guardar directamente en BD local
                await _databaseService.SaveAsync(vale);
                valeGuardadoLocalmente = true;
                valeIdLocal = vale.Id;

                // Si es nuevo vale, actualizar los detalles con el ID generado
                if (SelectedVale == null && vale.Id > 0)
                {
                    foreach (var detalle in ArticulosDetalle)
                    {
                        detalle.IdSalida = vale.Id;
                        detalle.SalidaId = vale.Id;
                        await _databaseService.SaveAsync(detalle);
                    }
                }

                await LoadValesAsync();
                await Shell.Current.DisplayAlert("Exito", "Vale guardado en base de datos para sincronizacion manual", "OK");
            }

            // =========================================================================
            // BLOQUE DE VERIFICACION (solo si se guardó localmente)
            // =========================================================================
            if (valeGuardadoLocalmente && valeIdLocal > 0)
            {
                System.Diagnostics.Debug.WriteLine("\n--- VERIFICANDO DATOS GUARDADOS EN BD ---");

                var valeGuardado = await _databaseService.GetByIdAsync<Salida>(valeIdLocal);
                if (valeGuardado != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Vale Guardado ID: {valeGuardado.Id}");
                    System.Diagnostics.Debug.WriteLine($"    Concepto: {valeGuardado.Concepto}");
                    System.Diagnostics.Debug.WriteLine($"    Campo ID: {valeGuardado.IdCampo}, Almacen ID: {valeGuardado.IdAlmacen}");

                    var detallesGuardados = await _databaseService.GetDetallesBySalidaAsync(valeGuardado.Id);
                    System.Diagnostics.Debug.WriteLine($"    Total de Detalles Encontrados en BD: {detallesGuardados.Count}");
                    if (detallesGuardados.Any())
                    {
                        int i = 1;
                        foreach (var detalle in detallesGuardados)
                        {
                            System.Diagnostics.Debug.WriteLine($"    - Detalle #{i}: ArticuloID={detalle.IdArticulo}, Cantidad={detalle.Cantidad}, FK IdSalida={detalle.IdSalida}, Maquinaria={detalle.IdMaquinaria}, Grupo= {detalle.IdGrupoMaquinaria}");
                            i++;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("xxx ERROR: No se pudo encontrar el vale en la BD despues de guardarlo. xxx");
                }
                System.Diagnostics.Debug.WriteLine("--- FIN DE LA VERIFICACION ---\n");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al guardar vale: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
            ClearForm();
        }
    }

    [RelayCommand]
    private async Task DeleteValeAsync(Salida vale)
    {
        if (vale == null) return;

        var confirm = await Shell.Current.DisplayAlert("Confirmar",
            "Esta seguro de eliminar este vale?", "Si", "No");

        if (!confirm) return;

        SetBusy(true);
        try
        {
            // Delete details first
            await _databaseService.DeleteDetallesBySalidaAsync(vale.Id);

            // Delete the vale
            await _databaseService.DeleteAsync(vale);
            await LoadValesAsync();

            await Shell.Current.DisplayAlert("Exito", "Vale eliminado correctamente", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al eliminar vale: {ex.Message}", "OK");
        }
        finally
        {
            CancelEdit();
            SetBusy(false);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearForm();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadValesAsync();
    }

    #endregion

    #region New Article Commands

    [RelayCommand]
    private async Task AgregarArticuloAsync()
    {
        // Validar campos obligatorios antes de permitir agregar artículos
        if (!await ValidateBeforeAddingArticleAsync())
        {
            return; // Si la validación falla, no continuar
        }

        // Verificar incompatibilidad de tipos
        if (!ValidateArticleTypeCompatibility())
        {
            return; // Si hay incompatibilidad, no continuar
        }

        // Verificar si el TipoEntrada es "Receta"
        if (!string.IsNullOrWhiteSpace(SelectedTipoEntrada) && SelectedTipoEntrada.Equals("Receta", StringComparison.OrdinalIgnoreCase))
        {
            // Flujo para TipoEntrada = "Receta"
            await ProcessRecetaArticlesAsync();
        }
        else
        {
            // Flujo original para TipoEntrada = "Articulo"
            await ProcessRegularArticleAsync();
        }
    }

    /// <summary>
    /// Procesa los artículos de una receta y los agrega a ArticulosDetalle
    /// </summary>
    private async Task ProcessRecetaArticlesAsync()
    {
        try
        {
            if (SelectedReceta == null || SelectedLote == null)
            {
                await Shell.Current.DisplayAlert("Error", "Debe seleccionar una receta y un lote válidos", "OK");
                return;
            }

            SetBusy(true);

            // Obtener los artículos de la receta
            var recetaArticulos = await _databaseService.GetRecetaArticulosByRecetaAsync(SelectedReceta.IdReceta);

            if (!recetaArticulos.Any())
            {
                await Shell.Current.DisplayAlert("Aviso", "La receta seleccionada no tiene artículos configurados", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"=== PROCESANDO RECETA ===");
            System.Diagnostics.Debug.WriteLine($"Receta: {SelectedReceta.NombreReceta}");
            System.Diagnostics.Debug.WriteLine($"Lote: {SelectedLote.Nombre} (Hectáreas: {SelectedLote.Hectareas})");
            System.Diagnostics.Debug.WriteLine($"Artículos en receta: {recetaArticulos.Count}");

            // Separacion por articulos 
            var articulosConSaldoInsuficiente = new List<string>();
            var cantidadesEnValeActual = ArticulosDetalle
            .GroupBy(d => d.IdArticulo)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

            // Modalidad ONLINE / OFFLINE
            if (ConnectivitySvc.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("Modo ONLINE: Verificando saldos contra la API...");
                foreach (var recetaArticulo in recetaArticulos)
                {
                    var articulo = await _databaseService.GetByIdAsync<Articulo>(recetaArticulo.IdArticulo);
                    if (articulo == null) continue; // Omitir si el artículo no existe localmente

                    // Calcular la cantidad requerida para este artículo según la receta y el lote
                    var cantidadRequerida = recetaArticulo.Dosis * SelectedLote.Hectareas;

                    // Consultar saldo en la API
                    decimal cantidadDisponibleApi = 0;
                    var saldoResponse = await _apiService.GetSaldoArticuloAsync(SelectedAlmacen!.Id, recetaArticulo.IdFamilia, recetaArticulo.IdSubFamilia, recetaArticulo.IdArticulo);


                    if (saldoResponse?.Estado == 200 && saldoResponse.Datos != null)
                    {
                        cantidadDisponibleApi = saldoResponse.Datos.Cantidad;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error de Conexión", $"No se pudo verificar el saldo del artículo: {articulo.Nombre}.", "OK");
                        return; // Detener todo si una llamada a la API falla
                    }

                    // Calcular el saldo real disponible (API - lo que ya está en el vale)
                    cantidadesEnValeActual.TryGetValue(recetaArticulo.IdArticulo, out decimal cantidadYaEnVale);
                    decimal saldoRealDisponible = cantidadDisponibleApi - cantidadYaEnVale;

                    // Si la cantidad requerida es mayor que el saldo disponible, se registra el problema
                    if (cantidadRequerida > saldoRealDisponible)
                    {
                        articulosConSaldoInsuficiente.Add(
                            $"- {articulo.Nombre}: \n  Necesitas: {cantidadRequerida:F2}, Disponible: {saldoRealDisponible:F2}"
                        );
                    }
                }

                // Si la lista de problemas no está vacía, mostrar error y detener
                if (articulosConSaldoInsuficiente.Any())
                {
                    var mensajeError = "Saldo insuficiente para los siguientes artículos:\n\n" +
                                       string.Join("\n", articulosConSaldoInsuficiente) +
                                       "\n\nPor favor, seleccione otra receta.";

                    await Shell.Current.DisplayAlert("Saldo Insuficiente", mensajeError, "OK");
                    return; 
                }
            }
            else // MODO OFFLINE: No hay conexión
            {
                System.Diagnostics.Debug.WriteLine("Modo OFFLINE: Pidiendo confirmación al usuario.");
                var confirmacion = await Shell.Current.DisplayAlert(
                    "Sin Conexión",
                    "No hay conexión a internet. ¿Está seguro de añadir la receta sin verificación de saldos?",
                    "Sí, añadir", "No");

                if (!confirmacion)
                {
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("Validación exitosa. Agregando artículos de la receta al vale...");
            // Creacion de la lista de Articulos
            var nuevosArticulosParaAgregar = new List<SalidaDetalle>();



            foreach (var recetaArticulo in recetaArticulos)
            {
                // Obtener información de Familia, SubFamilia y Artículo
                var familia = await _databaseService.GetByIdAsync<Familia>(recetaArticulo.IdFamilia);
                var subFamilia = await _databaseService.GetByIdAsync<SubFamilia>(recetaArticulo.IdSubFamilia);
                var articulo = await _databaseService.GetByIdAsync<Articulo>(recetaArticulo.IdArticulo);

                if (familia == null || subFamilia == null || articulo == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Datos faltantes para artículo {recetaArticulo.IdArticulo}");
                    continue;
                }

                // Calcular cantidad: Dosis * Hectáreas del lote
                var cantidadCalculada = recetaArticulo.Dosis * SelectedLote.Hectareas;

                var salidaDetalle = new SalidaDetalle
                {
                    IdFamilia = recetaArticulo.IdFamilia,
                    FamiliaNombre = familia.Nombre ?? string.Empty,
                    IdSubFamilia = recetaArticulo.IdSubFamilia,
                    SubFamiliaNombre = subFamilia.Nombre ?? string.Empty,
                    IdArticulo = recetaArticulo.IdArticulo,
                    ArticuloNombre = articulo.Nombre ?? string.Empty,
                    Cantidad = cantidadCalculada,
                    Concepto = Concepto ?? string.Empty,
                    Unidad = articulo.Unidad ?? string.Empty,
                    
                    // Campos de maquinaria (inicializados en 0)
                    IdMaquinaria = 0,
                    MaquinariaNombre = string.Empty,
                    IdGrupoMaquinaria = 0,
                    MaquinariaNombreGrupo = string.Empty,
                    
                    // Información del lote
                    IdLote = SelectedLote.Id,
                    LoteNombre = SelectedLote.Nombre ?? string.Empty,
                    LoteHectarea = SelectedLote.Hectareas,
                    
                    FolioSalida = string.Empty,
                    OrdenSalida = 0
                };

                nuevosArticulosParaAgregar.Add(salidaDetalle);

                System.Diagnostics.Debug.WriteLine($"Artículo procesado: {articulo.Nombre} - Cantidad: {cantidadCalculada} {articulo.Unidad}");
            }

            // Agregar todos los artículos a la lista
            foreach (var nuevoArticulo in nuevosArticulosParaAgregar)
            {
                ArticulosDetalle.Add(nuevoArticulo);
            }

            // Bloquear campos después del primer artículo
            LockFieldsAfterFirstArticle();
            UpdateArticulosStatus();

            await Shell.Current.DisplayAlert("Éxito", 
                $"Se agregaron {nuevosArticulosParaAgregar.Count} artículos de la receta '{SelectedReceta.NombreReceta}'", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en ProcessRecetaArticlesAsync: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", $"Error al procesar la receta: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Procesa un artículo regular (TipoEntrada = "Articulo")
    /// </summary>
    private async Task ProcessRegularArticleAsync()
    {
        _navigationService.ColocarDetallesActuales(ArticulosDetalle);

        var parameters = new Dictionary<string, object>();

        if (SelectedAlmacen != null)
        {
            parameters.Add("IdAlmacen", SelectedAlmacen.Id.ToString());
        }

        if (RequiereAutorizacion.HasValue)
        {
            parameters.Add("RequiereAutorizacion", RequiereAutorizacion.Value.ToString());
        }

        await Shell.Current.GoToAsync(nameof(AgregarArticuloPage), parameters);
    }

    /// <summary>
    /// Valida que no se mezclen tipos de artículos incompatibles basándose en el picker TipoEntrada
    /// </summary>
    private bool ValidateArticleTypeCompatibility()
    {
        if (ArticulosDetalle.Count == 0)
        {
            // Si no hay artículos, recordar el tipo actual para futuras validaciones
            _originalTipoEntrada = SelectedTipoEntrada;
            return true; // Si no hay artículos, cualquier tipo es válido
        }

        // Si ya hay artículos, usar el tipo original recordado para la validación
        var currentType = _originalTipoEntrada ?? "Articulo"; // Por defecto es Articulo si no se estableció
        var newType = SelectedTipoEntrada ?? string.Empty;

        System.Diagnostics.Debug.WriteLine($"Validando compatibilidad: TipoOriginal='{currentType}', TipoNuevo='{newType}'");

        if (!currentType.Equals(newType, StringComparison.OrdinalIgnoreCase))
        {
            var message = currentType == "Receta" 
                ? "Ya tiene artículos de receta agregados. No puede agregar artículos individuales."
                : "Ya tiene artículos individuales agregados. No puede agregar artículos de receta.";
                
            Shell.Current.DisplayAlert("Tipo Incompatible", message, "OK");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Bloquea los campos después de agregar el primer artículo
    /// </summary>
    private void LockFieldsAfterFirstArticle()
    {
        if (ArticulosDetalle.Count > 0)
        {
            AreFieldsLocked = true;
            System.Diagnostics.Debug.WriteLine("Campos bloqueados después de agregar artículos");
        }
    }

    /// <summary>
    /// Desbloquea los campos cuando se limpian los artículos
    /// </summary>
    private void UnlockFields()
    {
        AreFieldsLocked = false;
        System.Diagnostics.Debug.WriteLine("Campos desbloqueados");
    }
    #endregion

    #region Navigation Handling

    public async Task RecogerArticuloAgregado()
    {
        var nuevoDetalle = _navigationService.RecogerNuevoDetalle();
        if (nuevoDetalle != null)
        {
            System.Diagnostics.Debug.WriteLine("\n--- Articulo Recibido desde AgregarArticuloPage ---");
            System.Diagnostics.Debug.WriteLine($"  ID Articulo: {nuevoDetalle.IdArticulo}");
            System.Diagnostics.Debug.WriteLine($"  Nombre: {nuevoDetalle.ArticuloNombre}");
            System.Diagnostics.Debug.WriteLine($"  Cantidad: {nuevoDetalle.Cantidad}");
            System.Diagnostics.Debug.WriteLine($"  Unidad: {nuevoDetalle.Unidad}");
            System.Diagnostics.Debug.WriteLine($"  Familia: {nuevoDetalle.FamiliaNombre}");
            System.Diagnostics.Debug.WriteLine("--------------------------------------------------\n");

            if (ArticulosDetalle.Count() == 0) { 
                var NuevoDetalleFamilia = await _databaseService.GetByIdAsync<Familia>(nuevoDetalle.IdFamilia);
                Debug.WriteLine($"Familia encontrada: {NuevoDetalleFamilia?.Nombre ?? "Familia no encontrada"}");
                Debug.WriteLine($"RequiereAutorizacion: {NuevoDetalleFamilia?.RequiereAutorizacion}");
                Debug.WriteLine($"UsaMaquinaria: {NuevoDetalleFamilia?.UsaMaquinaria}");
                RequiereAutorizacion = NuevoDetalleFamilia?.RequiereAutorizacion;
            }

            ArticulosDetalle.Add(nuevoDetalle);
            UpdateArticulosStatus(); // Esto manejará el bloqueo automáticamente
        }
    }

    public void HandleNavigationResult()
    {
        System.Diagnostics.Debug.WriteLine($"HandleNavigationResult called - Result: '{Result}'");
        
        if (!string.IsNullOrEmpty(Result))
        {
            if (Result == "Saved")
            {
                System.Diagnostics.Debug.WriteLine("Navigation result indicates successful save, checking ValeNavigationService...");
            }

            Result = string.Empty;
            SalidaDetalleResult = new();
        }
    }

    #endregion

    #region Private Methods

    private bool ValidateForm()
    {
        if (SelectedCampo == null)
        {
            Shell.Current.DisplayAlert("Error", "Seleccione un campo", "OK");
            return false;
        }

        if (SelectedAlmacen == null)
        {
            Shell.Current.DisplayAlert("Error", "Seleccione un almacen", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Concepto))
        {
            Shell.Current.DisplayAlert("Error", "Ingrese un concepto", "OK");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(SelectedTipoEntrada))
        {
            Shell.Current.DisplayAlert("Error", "Seleccione un tipo (Receta o Articulo)", "OK");
            return false;
        }
        
        if (!ArticulosDetalle.Any())
        {
            Shell.Current.DisplayAlert("Validacion", "Hace falta anadir Articulos", "OK");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valida que los campos obligatorios estén llenos antes de permitir agregar artículos
    /// </summary>
    private async Task<bool> ValidateBeforeAddingArticleAsync()
    {
        var missingFields = new List<string>();

        // Validar campos obligatorios básicos
        if (SelectedCampo == null)
            missingFields.Add("Campo");

        if (SelectedAlmacen == null)
            missingFields.Add("Almacén");

        if (string.IsNullOrWhiteSpace(Concepto))
            missingFields.Add("Concepto");

        if (string.IsNullOrWhiteSpace(SelectedTipoEntrada))
            missingFields.Add("Tipo (Receta o Artículo)");

        // Validaciones específicas para cuando el tipo es "Receta"
        if (!string.IsNullOrWhiteSpace(SelectedTipoEntrada) && SelectedTipoEntrada.Equals("Receta", StringComparison.OrdinalIgnoreCase))
        {
            if (SelectedLote == null)
                missingFields.Add("Lote");

            if (SelectedReceta == null)
                missingFields.Add("Receta");
        }

        // Si hay campos faltantes, mostrar mensaje de error
        if (missingFields.Any())
        {
            var missingFieldsText = string.Join(", ", missingFields);
            var message = missingFields.Count == 1 
                ? $"Falta llenar el campo: {missingFieldsText}" 
                : $"Faltan llenar los siguientes campos: {missingFieldsText}";

            await Shell.Current.DisplayAlert("Campos Obligatorios", message, "OK");
            return false;
        }

        return true;
    }

    private void ClearForm()
    {
        SelectedVale = null;
        SelectedCampo = null;
        SelectedAlmacen = null;
        SelectedLote = null;
        SelectedReceta = null;
        SelectedTipoEntrada = null;
        _originalTipoEntrada = null; // Resetear el tipo original
        Fecha = DateTime.Now;
        Concepto = string.Empty;
        ArticulosDetalle.Clear();
        UnlockFields(); // Desbloquear campos al limpiar el formulario
        UpdateArticulosStatus();
        UpdateLotesAndRecetasVisibility();
    }

    private void UpdateArticulosStatus()
    {
        var count = ArticulosDetalle.Count;
        HasArticulos = count > 0;

        if (count == 0)
        {
            ArticulosCountMessage = "No hay articulos agregados";
            RequiereAutorizacion = null;
            UnlockFields(); // Desbloquear campos cuando no hay artículos
        }
        else if (count == 1)
        {
            ArticulosCountMessage = "Este Vale tiene 1 Articulo";
            LockFieldsAfterFirstArticle(); // Bloquear campos después del primer artículo
        }
        else
        {
            ArticulosCountMessage = $"Este Vale tiene {count} Articulos";
            LockFieldsAfterFirstArticle(); // Mantener campos bloqueados
        }

        // Force UI update
        OnPropertyChanged(nameof(ArticulosDetalle));
        OnPropertyChanged(nameof(HasArticulos));
        OnPropertyChanged(nameof(ArticulosCountMessage));
    }

    /// <summary>
    /// Actualiza la visibilidad de las secciones de Lotes y Recetas basado en el TipoEntrada seleccionado
    /// </summary>
    private void UpdateLotesAndRecetasVisibility()
    {
        // Mostrar Lotes y Recetas solo cuando se selecciona "Receta"
        ShowLotesAndRecetas = !string.IsNullOrEmpty(SelectedTipoEntrada) && 
                             SelectedTipoEntrada.Equals("Receta", StringComparison.OrdinalIgnoreCase);
        
        // Si se cambia a "Articulo", limpiar las selecciones de Lote y Receta
        if (!ShowLotesAndRecetas)
        {
            SelectedLote = null;
            SelectedReceta = null;
        }
        
        System.Diagnostics.Debug.WriteLine($"TipoEntrada cambiado a: '{SelectedTipoEntrada}' - ShowLotesAndRecetas: {ShowLotesAndRecetas}");
    }

    [RelayCommand]
    private async Task EliminarArticuloAsync(SalidaDetalle detalle)
    {
        if (detalle == null) return;

        var confirm = await Shell.Current.DisplayAlert("Confirmar",
            "Esta seguro de eliminar este articulo?", "Si", "No");

        if (!confirm) return;

        ArticulosDetalle.Remove(detalle);
        UpdateArticulosStatus();
    }

    private async Task LoadArticulosDetalleAsync()
    {
        if (SelectedVale == null) return;

        try
        {
            var detalles = await _databaseService.GetDetallesBySalidaAsync(SelectedVale.Id);

            ArticulosDetalle.Clear();
            foreach (var detalle in detalles)
            {
                ArticulosDetalle.Add(detalle);
            }

            // Actualizar el mensaje de count
            ArticulosCountMessage = detalles.Count > 0 ? $"{detalles.Count} articulos" : "No hay articulos agregados";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar articulos detalle: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadAlmacenasByCampoAsync()
    {
        if (SelectedCampo == null) return;
        try
        {
            var almacenes = await _databaseService.GetAlmacenesByCampoAsync(SelectedCampo.Id);
            Almacenes.Clear();
            foreach (var almacen in almacenes.OrderBy(a => a.Nombre))
                Almacenes.Add(almacen);
            DebugInfo = $"Almacenes cargados: {almacenes.Count}";
        }
        catch (Exception ex)
        {
            DebugInfo = $"Error Almacenes: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al cargar almacenes: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LoadLotesByCampoAsync()
    {
        if (SelectedCampo == null) return;
        try
        {
            var lotes = await _databaseService.GetLotesByCampoAsync(SelectedCampo.Id);
            Lotes.Clear();
            foreach (var lote in lotes.OrderBy(l => l.Nombre))
                Lotes.Add(lote);
            DebugInfo += $" | Lotes cargados: {lotes.Count}";
        }
        catch (Exception ex)
        {
            DebugInfo = $"Error Lotes: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al cargar lotes: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Property Changed Handlers

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(Result) || e.PropertyName == nameof(SalidaDetalleResult))
        {
            HandleNavigationResult();
        }
    }

    partial void OnSelectedCampoChanged(Campo? value)
    {
        if (value != null)
        {
            _ = LoadAlmacenasByCampoAsync();
            _ = LoadLotesByCampoAsync();
        }
    }

    partial void OnSelectedTipoEntradaChanged(string? value)
    {
        UpdateLotesAndRecetasVisibility();
    }

    /// <summary>
    /// Comando de prueba para verificar recetas con artículos
    /// </summary>
    [RelayCommand]
    private async Task DebugRecetasConArticulosAsync()
    {
        try
        {
            var todasLasRecetas = await _databaseService.GetAllAsync<Receta>();
            System.Diagnostics.Debug.WriteLine($"\n🔍 === DEBUG RECETAS CON ARTICULOS ===");
            System.Diagnostics.Debug.WriteLine($"📊 Total de recetas en BD: {todasLasRecetas.Count}");
            
            foreach (var receta in todasLasRecetas.Take(5))
            {
                System.Diagnostics.Debug.WriteLine($"\n📋 Receta: {receta.NombreReceta}");
                System.Diagnostics.Debug.WriteLine($"   - IdReceta: {receta.IdReceta}");
                System.Diagnostics.Debug.WriteLine($"   - IdCampo: {receta.IdCampo}");
                System.Diagnostics.Debug.WriteLine($"   - IdAlmacen: {receta.IdAlmacen}");
                System.Diagnostics.Debug.WriteLine($"   - TipoReceta: {receta.TipoReceta}");
                
                // Obtener artículos de esta receta
                var articulos = await _databaseService.GetRecetaArticulosByRecetaAsync(receta.IdReceta);
                System.Diagnostics.Debug.WriteLine($"   - Artículos: {articulos.Count}");
                
                foreach (var articulo in articulos)
                {
                    System.Diagnostics.Debug.WriteLine($"     • IdArticulo: {articulo.IdArticulo}, Dosis: {articulo.Dosis}, Total: {articulo.Total}");
                }
            }
            
            // Probar método GetRecetaWithArticulosAsync
            if (todasLasRecetas.Any())
            {
                var primeraReceta = todasLasRecetas.First();
                var recetaCompleta = await _databaseService.GetRecetaWithArticulosAsync(primeraReceta.IdReceta);
                
                if (recetaCompleta != null)
                {
                    System.Diagnostics.Debug.WriteLine($"\n🎯 Receta completa: {recetaCompleta.NombreReceta}");
                    System.Diagnostics.Debug.WriteLine($"   - Campo: {recetaCompleta.CampoNombre}");
                    System.Diagnostics.Debug.WriteLine($"   - Almacén: {recetaCompleta.AlmacenNombre}");
                    System.Diagnostics.Debug.WriteLine($"   - Artículos enriquecidos: {recetaCompleta.Articulos.Count}");
                    
                    foreach (var articulo in recetaCompleta.Articulos)
                    {
                        System.Diagnostics.Debug.WriteLine($"     • {articulo.ArticuloNombre} - {articulo.FamiliaNombre} - Dosis: {articulo.Dosis}");
                    }
                }
            }
            
            await Shell.Current.DisplayAlert("Debug Recetas", 
                $"✅ Encontradas {todasLasRecetas.Count} recetas.\n\n📝 Ver Output Debug para detalles de artículos.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Error en DebugRecetasConArticulosAsync: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", $"❌ Error: {ex.Message}", "OK");
        }
    }

    #endregion
}