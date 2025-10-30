using Newtonsoft.Json;
using sys21_campos_zukarmex.Models;

namespace sys21_campos_zukarmex.Models.DTOs.Catalog;

/// <summary>
/// DTOs para catalogos de la API con metodos de mapeo
/// </summary>

#region Empresa

public class EmpresaApiDto
{
    public int idEmpresa { get; set; }
    public string nombre { get; set; } = string.Empty;
    public bool esPromotora { get; set; }

    public Empresa ToEmpresa()
    {
        return new Empresa
        {
            Id = idEmpresa,
            Nombre = nombre,
            IsPromotora = esPromotora
        };
    }
}

#endregion

#region Almacen

public class AlmacenApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public int idCampo { get; set; }

    public Campo Campo = new Campo();
    public Almacen ToAlmacen()
    {
        return new Almacen
        {
            Id = id,
            Nombre = nombre,
            IdCampo = Campo.Id
        };
    }
}

#endregion

#region Articulo

public class ArticuloApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string unidad { get; set; } = string.Empty;
    public int idSubFamilia { get; set; }
    public int idFamilia { get; set; }

    public SubFamilia SubFamilia = new SubFamilia();

    public Articulo ToArticulo()
    {
        return new Articulo
        {
            Id = id,
            Nombre = nombre,
            Unidad = unidad,
            IdSubFamilia = SubFamilia.Id,
            IdFamilia = SubFamilia.Familia.Id
        };
    }
}

#endregion

#region Campo

public class CampoApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public int idInspector { get; set; }
    public string nombreInspector { get; set; } = string.Empty;
    public int idEmpresa { get; set; }
    public int idPredio { get; set; }

    public Inspector Inspector { get; set;} = new Inspector();

    public Campo ToCampo()
    {
        return new Campo
        {
            Id = id,
            Nombre = nombre,
            IdInspector = Inspector.Id,
            NombreInspector = Inspector.Nombre,
            IdEmpresa = idEmpresa,
            IdPredio = idPredio
        };
    }
}

#endregion

#region Familia

public class FamiliaApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public bool requiereAutorizacion { get; set; }
    public bool usaMaquinaria { get; set; }

    public Familia ToFamilia()
    {
        return new Familia
        {
            Id = id,
            Nombre = nombre,
            RequiereAutorizacion = requiereAutorizacion,
            UsaMaquinaria = usaMaquinaria
        };
    }
}

#endregion

#region Inspector

public class InspectorApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;

    public Inspector ToInspector()
    {
        return new Inspector
        {
            Id = id,
            Nombre = nombre
        };
    }
}

#endregion

#region Maquinaria

public class MaquinariaApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;

    public Maquinaria ToMaquinaria()
    {
        return new Maquinaria
        {
            IdPk = id,
            Nombre = nombre
        };
    }
}

#endregion

#region SubFamilia

public class SubFamiliaApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public int idFamilia { get; set; }

    public Familia Familia = new Familia();

    public SubFamilia ToSubFamilia()
    {
        return new SubFamilia
        {
            Id = id,
            Nombre = nombre,
            IdFamilia = Familia.Id
        };
    }
}

#endregion

#region User

public class UserApiDto
{
    public int id { get; set; }
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public int tipo { get; set; }
    // Campos adicionales que vienen de la API pero no estan en el modelo
    public string nombre { get; set; } = string.Empty;
    public bool activo { get; set; }

    public User ToUser()
    {
        return new User
        {
            Id = id,
            Username = username,
            Password = password,
            Tipo = tipo,
            // Mapear algunos campos similares
            NombreCompleto = nombre,
            IsActive = activo
            // Nota: Algunas propiedades del DTO no se mapean porque no existen en el modelo actual
        };
    }
}

#endregion

#region Receta

public class RecetaApiDto
{
    public int idCampo { get; set; }
    public int idAlmacen { get; set; }
    public int idReceta { get; set; }
    public int tipoReceta { get; set; }
    public string nombreReceta { get; set; } = string.Empty;
    public List<RecetaArticuloApiDto> articulos { get; set; } = new();

    public Receta ToReceta()
    {
        return new Receta
        {
            IdCampo = idCampo,
            IdAlmacen = idAlmacen,
            IdReceta = idReceta,
            TipoReceta = tipoReceta,
            NombreReceta = nombreReceta
        };
    }
}

public class RecetaArticuloApiDto
{
    public int idFamilia { get; set; }
    public int idSubFamilia { get; set; }
    public int idArticulo { get; set; }
    public decimal dosis { get; set; }
    public decimal total { get; set; }

    public RecetaArticulo ToRecetaArticulo(int idReceta)
    {
        return new RecetaArticulo
        {
            IdReceta = idReceta,
            IdFamilia = idFamilia,
            IdSubFamilia = idSubFamilia,
            IdArticulo = idArticulo,
            Dosis = dosis,
            Total = total
        };
    }
}

#endregion

#region SalidaDetalle
 
    public class SalidaDetalleApiDto
    {
        public int id { get; set; }
        public int idSalida { get; set; }
        public int salidaId { get; set; }
        public int idFamilia { get; set; }
        public string familiaNombre { get; set; } = string.Empty;
        public int idSubFamilia { get; set; }
        public string subFamiliaNombre { get; set; } = string.Empty;
        public int idArticulo { get; set; }
        public string articuloNombre { get; set; } = string.Empty;
        public decimal cantidad { get; set; }
        public string conceptoDeterminado { get; set; } = string.Empty;
        public string unidad { get; set; } = string.Empty;
        public int idMaquinaria { get; set; }
        public string maquinariaNombre { get; set; } = string.Empty;
        public int idGrupoMaquinaria { get; set; }
        public string maquinariaNombreGrupo { get; set; } = string.Empty;
        public int ordenSalida { get; set; }
        public string folioSalida { get; set; } = string.Empty;

    public SalidaDetalle ToSalidaDetalle()
        {
            return new SalidaDetalle
            {
                Id = this.id,
                IdSalida = this.idSalida,
                SalidaId = this.salidaId,
                IdFamilia = this.idFamilia,
                FamiliaNombre = this.familiaNombre,
                IdSubFamilia = this.idSubFamilia,
                SubFamiliaNombre = this.subFamiliaNombre,
                IdArticulo = this.idArticulo,
                ArticuloNombre = this.articuloNombre,
                Cantidad = this.cantidad,
                Concepto = this.conceptoDeterminado,
                Unidad = this.unidad,
                IdMaquinaria = this.idMaquinaria,
                MaquinariaNombre = this.maquinariaNombre,
                IdGrupoMaquinaria = this.idGrupoMaquinaria,
                MaquinariaNombreGrupo = this.maquinariaNombreGrupo,
                FolioSalida = this.folioSalida,
                OrdenSalida = this.ordenSalida
            };
        }
    }
    #endregion

#region Salida

    public class SalidaApiDto
{
    public int id { get; set; }
    public string folio { get; set; } = string.Empty;
    public int idCampo { get; set; }
    public int idAlmacen { get; set; }
    public DateTime fecha { get; set; }
    public string concepto { get; set; } = string.Empty;
    public string usuario { get; set; } = string.Empty;
    public bool status { get; set; }
    public string? statusText { get; set; }
    public DateTime? fechaCreacion { get; set; }
    public DateTime? fechaModificacion { get; set; }
    public bool? autorizado { get; set; }
    public string? autorizadoPor { get; set; }
    public DateTime? fechaAutorizacion { get; set; }
    public List<SalidaDetalleApiDto> salidaDetalle { get; set; } = new();

    public Salida ToSalida()
    {
        return new Salida
        {
            Id = this.id,
            Folio = this.folio,
            IdCampo = this.idCampo,
            IdAlmacen = this.idAlmacen,
            Fecha = this.fecha,
            Concepto = this.concepto,
            Usuario = this.usuario,
            Status = this.status,
            StatusText = this.statusText,
            FechaCreacion = this.fechaCreacion,
            FechaModificacion = this.fechaModificacion,
            Autorizado = this.autorizado,
            AutorizadoPor = this.autorizadoPor,
            FechaAutorizacion = this.fechaAutorizacion,
            SalidaDetalle = this.salidaDetalle.Select(detalleDto => detalleDto.ToSalidaDetalle()).ToList()
        };
    }
}

#endregion

#region Lote

public class LoteApiDto
{
    public int id { get; set; }
    public string nombre { get; set; } = string.Empty;
    public decimal hectarea { get; set; }
    public int idCampo { get; set; }

    public Lote ToLote()
    {
        return new Lote
        {
            Id = id,
            Nombre = nombre,
            Hectareas = hectarea,
            IdCampo = idCampo
        };
    }
}

#endregion

#region LineaDeRiego

public class LineaDeRiegoApiDto
{
    [JsonProperty("id")] 
    public int Id { get; set; }

    [JsonProperty("nombre")] 
    public string Nombre { get; set; } = string.Empty;

    [JsonProperty("cantidadEquiposBombeo")]
    public int CantidadEquiposBombeo { get; set; } = 0;

    [JsonProperty("cantidadLaminaRiego")]
    public decimal CantidadLaminaRiego { get; set; } = 0; // Cambiado a decimal

    [JsonProperty("campo")]
    public object Campo { get; set; }

    public LineaDeRiego ToLineaDeRiego()
    {
        return new LineaDeRiego
        {
            Id = Id,
            Nombre = Nombre,
            CantidadEquiposBombeo = CantidadEquiposBombeo,
            CantidadLaminaRiego = CantidadLaminaRiego
        };
    }
}

#endregion

