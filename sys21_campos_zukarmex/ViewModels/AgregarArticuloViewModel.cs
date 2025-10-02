using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using sys21_campos_zukarmex.Models;
using sys21_campos_zukarmex.Services;

namespace sys21_campos_zukarmex.ViewModels;

[QueryProperty(nameof(RequiereAutorizacionStr), "RequiereAutorizacion")]
[QueryProperty(nameof(IdAlmacenStr), "IdAlmacen")]

public partial class AgregarArticuloViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly ValeNavigationService _navigationService;
    private readonly SessionService _sessionService;
    private readonly ApiService _apiService;
    private Dictionary<int, decimal> _cantidadesEnValeActual = new();
    private readonly ConnectivityService _connectivityService;
    public ConnectivityService ConnectivitySvc => _connectivityService;


    public AgregarArticuloViewModel(DatabaseService databaseService, SessionService sessionService, ValeNavigationService navigationService, ApiService apiService, ConnectivityService connectivityService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
        _sessionService = sessionService;
        _apiService = apiService;
        Title = "Agregar Articulo";
        _connectivityService = connectivityService;

        // Initialize collections
        Familias = new ObservableCollection<Familia>();
        SubFamilias = new ObservableCollection<SubFamilia>();
        Articulos = new ObservableCollection<Articulo>();
        Maquinarias = new ObservableCollection<Maquinaria>();
        Lotes = new ObservableCollection<Lote>();
    }

    #region Properties

    [ObservableProperty]
    private ObservableCollection<Familia> familias;

    [ObservableProperty]
    private ObservableCollection<SubFamilia> subFamilias;

    [ObservableProperty]
    private ObservableCollection<Articulo> articulos;

    [ObservableProperty]
    private ObservableCollection<Maquinaria> maquinarias;

    [ObservableProperty]
    private ObservableCollection<Lote> lotes;

    [ObservableProperty]
    private Familia? selectedFamilia;

    [ObservableProperty]
    private SubFamilia? selectedSubFamilia;

    [ObservableProperty]
    private Articulo? selectedArticulo;

    [ObservableProperty]
    private Maquinaria? selectedMaquinaria;

    [ObservableProperty]
    private Lote? selectedLote;

    [ObservableProperty]
    private decimal cantidad = 0;

    [ObservableProperty]
    private string concepto = string.Empty;

    [ObservableProperty]
    private string unidad = string.Empty;

    [ObservableProperty]
    private bool isPromotora;

    [ObservableProperty]
    private bool showMaquinariaSection;

    [ObservableProperty]
    private bool showLotesSection;

    [ObservableProperty]
    private string debugInfo = "Inicializando...";

    [ObservableProperty]
    private string idAlmacenStr;

    [ObservableProperty]
    private bool? requiereAutorizacion;
    [ObservableProperty]
    private string requiereAutorizacionStr;

    private int idAlmacen;
    #endregion

    #region Commands

    partial void OnIdAlmacenStrChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            idAlmacen = result;
            Debug.WriteLine($"Almacen ID recibido: {idAlmacen}");
        }
    }

    [RelayCommand]
    private async Task PageAppearingAsync()
    {
        // 1. Recoge la lista de articulos que ya estan en el vale desde el "buzon"
        var detallesActuales = _navigationService.RecogerDetallesActuales();

        // 2. Convierte esa lista en nuestro diccionario de cantidades
        _cantidadesEnValeActual = detallesActuales
            .GroupBy(d => d.IdArticulo)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

        DebugInfo = $"Recibidos {detallesActuales.Count} articulos del vale actual.";

        // 3. Carga los catalogos del formulario
        await LoadCatalogosAsync();
    }


    [RelayCommand]
    private async Task LoadCatalogosAsync()
    {
        SetBusy(true);
        DebugInfo = "Cargando catalogos...";
        
        try
        {
            // Load all catalogs
            var familias = await _databaseService.GetAllAsync<Familia>();

            //ejecuta la modificacion de la coleccion en el hilo UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Familias.Clear();

                // Si RequiereAutorizacion es null -> mostrar todas, si no -> filtrar
                var listaParaAgregar = (RequiereAutorizacion == null)
                    ? familias.OrderBy(f => f.Nombre)
                    : familias.Where(f => f.RequiereAutorizacion == RequiereAutorizacion)
                              .OrderBy(f => f.Nombre);

                foreach (var familia in listaParaAgregar)
                    Familias.Add(familia);
            });

            var maquinarias = await _databaseService.GetAllAsync<Maquinaria>();
            var lotes = await _databaseService.GetAllAsync<Lote>();

            Familias.Clear();
            Maquinarias.Clear();
            Lotes.Clear();

            foreach (var familia in familias.OrderBy(f => f.Nombre))
                Familias.Add(familia);

            foreach (var maquinaria in maquinarias.OrderBy(m => m.Nombre))
                Maquinarias.Add(maquinaria);

            foreach (var lote in lotes.OrderBy(l => l.Nombre))
                Lotes.Add(lote);

            // Check if user is promotora
            await CheckUserPromotoraAsync();

            SetMaquinariaVisibility();
            SetLotesVisibility();

            DebugInfo = $"Catalogos cargados - Usuario Es Promotora: {IsPromotora}";
            System.Diagnostics.Debug.WriteLine($"LoadCatalogosAsync: ShowMaquinariaSection={ShowMaquinariaSection}, ShowLotesSection={ShowLotesSection}");
            if (RequiereAutorizacion != null)
                await FiltradoDeFamiliasPorAutorizacion(RequiereAutorizacion);
        }
        catch (Exception ex)
        {
            DebugInfo = $"Error: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al cargar catalogos: {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task LoadSubFamiliasAsync()
    {
        if (SelectedFamilia == null) return;

        try
        {
            var subFamilias = await _databaseService.GetSubFamiliasByFamiliaAsync(SelectedFamilia.Id);
            
            SubFamilias.Clear();
            foreach (var subFamilia in subFamilias.OrderBy(sf => sf.Nombre))
                SubFamilias.Add(subFamilia);

            // Clear dependent selections
            SelectedSubFamilia = null;
            SelectedArticulo = null;
            Articulos.Clear();

            DebugInfo = $"SubFamilias cargadas: {SubFamilias.Count} | Familia RequiereAutorizacion: {SelectedFamilia.RequiereAutorizacion} | UsaMaquinaria: {SelectedFamilia.UsaMaquinaria}";
        }
        catch (Exception ex)
        {
            DebugInfo = $"Error SubFamilias: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al cargar subfamilias: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LoadArticulosAsync()
    {
        if (SelectedSubFamilia == null) return;

        try
        {
            var articulos = await _databaseService.GetArticulosBySubFamiliaAsync(SelectedSubFamilia.Id);
            
            Articulos.Clear();
            foreach (var articulo in articulos.OrderBy(a => a.Nombre))
                Articulos.Add(articulo);

            // Clear dependent selection
            SelectedArticulo = null;

            DebugInfo = $"Articulos cargados: {Articulos.Count}";
        }
        catch (Exception ex)
        {
            DebugInfo = $"Error Articulos: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", $"Error al cargar articulos: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!ValidateForm())
        { 
            return; 
        }
            try
            {
                if (ConnectivitySvc.IsConnected)
                {
                    decimal cantidadDisponibleApi = 0;
                var saldoResponse = await _apiService.GetSaldoArticuloAsync(idAlmacen, SelectedFamilia!.Id, SelectedSubFamilia!.Id, SelectedArticulo!.Id);


                if (saldoResponse?.Estado == 200 && saldoResponse.Datos != null)
                    {
                        cantidadDisponibleApi = saldoResponse.Datos.Cantidad;
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error de Conexion", "No se pudo verificar el saldo del articulo.", "OK");
                        return;
                    }

                    _cantidadesEnValeActual.TryGetValue(SelectedArticulo.Id, out decimal cantidadYaEnVale);
                    decimal saldoRealDisponible = cantidadDisponibleApi - cantidadYaEnVale;

                    if (Cantidad > saldoRealDisponible)
                    {
                        await Shell.Current.DisplayAlert("Saldo Insuficiente",
                            $"Quieres agregar {Cantidad} {SelectedArticulo.Unidad}, pero el saldo disponible real es de solo {saldoRealDisponible}.\n\n" +
                            $"(Inventario Total: {cantidadDisponibleApi} | Ya agregados a este vale: {cantidadYaEnVale})", "OK");
                        return;
                    }
                }

                    var salidaDetalle = new SalidaDetalle
                    {
                        // Campos principales con mapeo correcto
                        IdFamilia = SelectedFamilia!.Id,
                        FamiliaNombre = SelectedFamilia.Nombre ?? string.Empty,
                        IdSubFamilia = SelectedSubFamilia!.Id,
                        SubFamiliaNombre = SelectedSubFamilia.Nombre ?? string.Empty,
                        IdArticulo = SelectedArticulo!.Id,
                        ArticuloNombre = SelectedArticulo.Nombre ?? string.Empty,
                        Cantidad = Cantidad,
                        Concepto = Concepto ?? string.Empty,
                        Unidad = Unidad ?? string.Empty,

                        // Inicializar todos los campos de maquinaria y lotes con valores por defecto seguros
                        IdMaquinaria = 0,
                        MaquinariaNombre = string.Empty,
                        IdGrupoMaquinaria = 0,
                        MaquinariaNombreGrupo = string.Empty,
                        IdLote = 0,
                        LoteNombre = string.Empty,
                        LoteHectarea = 0,
                        FolioSalida = string.Empty,
                        OrdenSalida = 0
                    };

                    // Solo agregar info de maquinaria si la familia UsaMaquinaria = true Y hay maquinaria seleccionada
                    if (ShowMaquinariaSection && SelectedMaquinaria != null)
                    {
                        salidaDetalle.IdMaquinaria = SelectedMaquinaria.IdPk;
                        salidaDetalle.MaquinariaNombre = SelectedMaquinaria.Nombre ?? string.Empty;
                        salidaDetalle.IdGrupoMaquinaria = SelectedMaquinaria.IdGrupo;
                        salidaDetalle.MaquinariaNombreGrupo = SelectedMaquinaria.NombreGrupo ?? string.Empty;
                    }

                    // Solo agregar info de lotes si la familia UsaMaquinaria = false Y hay lote seleccionado
                    if (ShowLotesSection && SelectedLote != null)
                    {
                        salidaDetalle.IdLote = SelectedLote.Id;
                        salidaDetalle.LoteNombre = SelectedLote.Nombre ?? string.Empty;
                        salidaDetalle.LoteHectarea = SelectedLote.Hectareas;
                    }

                    DebugInfo = $"Guardando: {salidaDetalle.FamiliaNombre} > {salidaDetalle.ArticuloNombre} | Cantidad: {salidaDetalle.Cantidad}";

                    _navigationService.ColocarNuevoDetalle(salidaDetalle);
                        await Shell.Current.GoToAsync("..", new Dictionary<string, object>
                        {
                                ["Result"] = "Saved"
                        });
            }
            catch (Exception ex)
            {
                DebugInfo = $"Error al guardar: {ex.Message}";
                await Shell.Current.DisplayAlert("Error", $"Error al guardar: {ex.Message}", "OK");
            }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    #endregion

    #region Private Methods

    private async Task FiltradoDeFamiliasPorAutorizacion(bool? requiereAutorizacion)
    {
        if (requiereAutorizacion == null)
        {
            DebugInfo = "No se filtro: valor de autorizacion es null.";
            return;
        }

        var todasFamilias = await _databaseService.GetAllAsync<Familia>();
        var familiasFiltradas = todasFamilias
            .Where(f => f.RequiereAutorizacion == requiereAutorizacion)
            .OrderBy(f => f.Nombre)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Familias.Clear();
            foreach (var familia in familiasFiltradas)
                Familias.Add(familia);
        });

        DebugInfo = $"Filtradas {familiasFiltradas.Count} familias con RequiereAutorizacion = {requiereAutorizacion}";
    }


    private async Task CheckUserPromotoraAsync()
    {
        try
        {
            var session = await _sessionService.GetCurrentSessionAsync();
            IsPromotora = session?.IsPromotora == true;
            DebugInfo += $" | Usuario IsPromotora: {IsPromotora}";
        }
        catch (Exception ex)
        {
            IsPromotora = false;
            DebugInfo += $" | Error session: {ex.Message}";
        }
    }

    /// <summary>
    /// Verifica si debe mostrar la seccion de maquinaria basado en la familia seleccionada
    /// Solo se muestra cuando la familia UsaMaquinaria = true
    /// </summary>
    private void SetMaquinariaVisibility()
    {
        // Mostrar maquinaria solo cuando la familia usa maquinaria (UsaMaquinaria = true)
        ShowMaquinariaSection = SelectedFamilia?.UsaMaquinaria == true;
        
        System.Diagnostics.Debug.WriteLine($"SetMaquinariaVisibility: UsaMaquinaria={SelectedFamilia?.UsaMaquinaria}, ShowMaquinariaSection={ShowMaquinariaSection}");
    }

    /// <summary>
    /// Verifica si debe mostrar la seccion de lotes basado en la familia seleccionada
    /// Solo se muestra cuando la familia UsaMaquinaria = false
    /// </summary>
    private void SetLotesVisibility()
    {
        // Mostrar lotes solo cuando la familia NO usa maquinaria (UsaMaquinaria = false)
        ShowLotesSection = SelectedFamilia?.UsaMaquinaria == false;
        
        System.Diagnostics.Debug.WriteLine($"SetLotesVisibility: UsaMaquinaria={SelectedFamilia?.UsaMaquinaria}, ShowLotesSection={ShowLotesSection}");
    }

    /// <summary>
    /// Obtiene la cantidad predeterminada basada en el historial del articulo
    /// </summary>
    private async Task<decimal> GetCantidadPorArticuloAsync(int articuloId)
    {
        try
        {
            // Buscar en registros historicos de SalidaDetalle la cantidad mas comun para este articulo
            var detallesHistoricos = await _databaseService.GetWhereAsync<SalidaDetalle>(sd => sd.IdArticulo == articuloId);
            
            if (detallesHistoricos.Any())
            {
                // Obtener la cantidad mas frecuente
                var cantidadMasFrecuente = detallesHistoricos
                    .GroupBy(sd => sd.Cantidad)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? 1m;
                
                DebugInfo += $" | Cantidad historica encontrada: {cantidadMasFrecuente}";
                return cantidadMasFrecuente;
            }

            // Si no hay historial, usar cantidad por defecto
            return 1m;
        }
        catch (Exception ex)
        {
            DebugInfo += $" | Error obteniendo cantidad: {ex.Message}";
            return 1m; // Valor por defecto seguro
        }
    }

    private bool ValidateForm()
    {
        // Basic validation logic, can be expanded
        if (SelectedFamilia == null)
        {
            Shell.Current.DisplayAlert("Validacion", "Seleccione una familia.", "OK");
            return false;
        }

        if (SelectedSubFamilia == null)
        {
            Shell.Current.DisplayAlert("Validacion", "Seleccione una subfamilia.", "OK");
            return false;
        }

        if (SelectedArticulo == null)
        {
            Shell.Current.DisplayAlert("Validacion", "Seleccione un articulo.", "OK");
            return false;
        }

        if (Cantidad <= 0)
        {
            Shell.Current.DisplayAlert("Validacion", "La cantidad debe ser mayor a 0.", "OK");
            return false;
        }

        if (ShowMaquinariaSection && SelectedMaquinaria == null)
        {
            if (SelectedMaquinaria == null)
            {
                Shell.Current.DisplayAlert("Validacion", "Seleccione una maquinaria.", "OK");
                return false;
            }
        }

        // Solo agregar info de lotes si la familia UsaMaquinaria = false Y hay lote seleccionado
        if (ShowLotesSection && SelectedLote == null)
        {
            if (SelectedLote == null)
            {
                Shell.Current.DisplayAlert("Validacion", "Seleccione un Lote.", "OK");
                return false;
            }
            
        }


        return true;
    }

    #endregion

    #region Property Changed Handlers

    partial void OnSelectedFamiliaChanged(Familia? value)
    {
        if (value != null)
        {
            _ = LoadSubFamiliasAsync();
            
            // Recalcular visibilidad de maquinaria basado en la familia seleccionada
            SetMaquinariaVisibility();
            
            // Recalcular visibilidad de lotes basado en la familia seleccionada
            SetLotesVisibility();
            
            // Limpiar selecci�n de maquinaria si la nueva familia no la usa
            if (!value.UsaMaquinaria)
            {
                SelectedMaquinaria = null;
            }
            
            // Limpiar selecci�n de lotes si la nueva familia usa maquinaria (UsaMaquinaria = true)
            if (value.UsaMaquinaria)
            {
                SelectedLote = null;
            }
        }
        else
        {
            // Si no hay familia seleccionada, ocultar ambas secciones
            ShowMaquinariaSection = false;
            ShowLotesSection = false;
            SelectedMaquinaria = null;
            SelectedLote = null;
        }
    }

    partial void OnSelectedSubFamiliaChanged(SubFamilia? value)
    {
        if (value != null)
        {
            _ = LoadArticulosAsync();
        }
    }

    partial void OnSelectedArticuloChanged(Articulo? value)
    {
        if (value != null)
        {
            // Auto-llenar unidad desde el articulo seleccionado
            Unidad = value.Unidad ?? string.Empty;
            
            // Auto-llenar cantidad desde la logica de negocio de forma asincrona
            _ = Task.Run(async () =>
            {
                var cantidadCalculada = await GetCantidadPorArticuloAsync(value.Id);
                
                // Actualizar en el hilo principal
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Cantidad = cantidadCalculada;
                    DebugInfo = $"Articulo: {value.Nombre}, Unidad: {Unidad}, Cantidad: {Cantidad}";
                });
            });
        }
        else
        {
            Cantidad = 0;
            Unidad = string.Empty;
        }
    }

    partial void OnRequiereAutorizacionStrChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            RequiereAutorizacion = null;
        }
        else if (bool.TryParse(value, out bool result))
        {
            RequiereAutorizacion = result;
        }
        else
        {
            RequiereAutorizacion = null;
        }

        System.Diagnostics.Debug.WriteLine($"Valor recibido: RequiereAutorizacion = {RequiereAutorizacion}");
        _ = FiltradoDeFamiliasPorAutorizacion(RequiereAutorizacion);
    }

    #endregion

    public override async Task InitializeAsync()
    {
        DebugInfo = "InitializeAsync iniciado...";
        await LoadCatalogosAsync();
    }
}