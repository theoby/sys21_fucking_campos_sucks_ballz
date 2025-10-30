# sys21_campos_zukarmex - Sistema de Gestion de Vales

## Caracteristicas Principales

### Sistema de Autenticacion
- Diferentes niveles de usuario (Admin, Supervisor, Usuario)

### Interfaz Material Design
- Optimizado para dispositivos Android

### Base de Datos Local

### Sincronizacion
- Sincronizacion automatica de catalogos al login
- Trabajo offline con sincronizacion posterior
- Indicadores de progreso en tiempo real
### Gestion de Vales
- Crear y editar vales de salida
- Autorizacion de vales (segun permisos)
- Consulta de estado de vales
### Patron MVVM
- ViewModels con CommunityToolkit.Mvvm
- Binding bidireccional
- Commands para acciones
- **DatabaseService**: Gestion de SQLite
- **ApiService**: Comunicacion con APIs
- **SyncService**: Sincronizacion de catalogos
- **SessionService**: Manejo de sesiones de usuario

2. **Articulo** - Articulos del inventario
3. **Campo** - Campos de cultivo
4. **Empresa** - Empresas del sistema
5. **Familia** - Familias de articulos
6. **Inspector** - Inspectores de campos
7. **Lote** - Lotes de cultivo por campo
8. **Maquinaria** - Equipos y maquinaria
9. **Receta** - Recetas de art�culos (con lista de RecetaArticulos)
10. **Salida** - Vales de salida (principal)
11. **SalidaDetalle** - Detalles de vales
12. **SubFamilia** - Subfamilias de articulos
13. **User** - Usuarios del sistema
14. **Session** - Sesiones activas

## Paginas de la Aplicacion

- Informacion del usuario y empresa
- Lista de vales creados
- Formulario para crear/editar vales
- Busqueda y filtrado
- Operaciones CRUD completas

### Autorizacion (AuthorizationPage)
- Lista de vales pendientes de autorizacion
- Botones para aprobar/rechazar
- Solo visible para usuarios con permisos
### Endpoints de Autenticacion
- `POST /inicio_sesion` - Login con JWT

### Endpoints de Vales
- `POST /vales_salida` - Crear/actualizar vale
- `GET /api_status` - Obtener estado de vales
- `POST /api_autorizacion` - Autorizar/rechazar vale

### Endpoints de Catalogos
- `GET /almacenes` - Sincronizar almacenes
- `GET /campos` - Sincronizar campos
- `GET /empresas` - Sincronizar empresas
- `GET /familias` - Sincronizar familias
- `GET /inspectores` - Sincronizar inspectores
- `GET /lotes` - Sincronizar lotes
- `GET /maquinarias` - Sincronizar maquinarias
- `GET /recetas` - Sincronizar recetas (incluye art�culos de recetas)
- `GET /subfamilias` - Sincronizar subfamilias

## Configuracion

### Configurar URL de la API
Edita el archivo `Services/AppConfigService.cs` y actualiza la URL base:

```csharp
public const string ApiBaseUrl = "https://tu-api-url.com/api/";
```

### Niveles de Usuario
- **Tipo 1**: Administrador (todos los permisos)
- **Tipo 2**: Supervisor (puede autorizar vales)
- **Tipo 3**: Usuario regular (crear vales)

## Requisitos del Sistema

### Desarrollo
- Visual Studio 2022 (17.8 o superior)
- .NET 9 SDK
- Android SDK (API 21 o superior)

### Dispositivo
- Android 5.0 (API 21) o superior
- Espacio libre: 50 MB minimo
- Conexion a internet (para sincronizacion)

## Instalacion y Compilacion

1. Clonar el repositorio
2. Abrir `sys21_campos_zukarmex.sln` en Visual Studio
3. Restaurar paquetes NuGet
4. Configurar la URL de la API en `AppConfigService.cs`
5. Compilar y ejecutar en emulador o dispositivo

## Funcionalidades Futuras

- [ ] Notificaciones push
- [ ] Reportes en PDF
- [ ] Sincronizacion en segundo plano
- [ ] Modo completamente offline
- [ ] Soporte para iOS
- [ ] Dashboard analitico avanzado

## Tecnologias Utilizadas

- **.NET MAUI 9.0** - Framework multiplataforma
- **SQLite** - Base de datos local
- **CommunityToolkit.Mvvm** - MVVM helpers
- **Newtonsoft.Json** - Serializacion JSON
- **System.IdentityModel.Tokens.Jwt** - Manejo de JWT
- **CommunityToolkit.Maui** - Controles adicionales

## Contacto

Para soporte tecnico o consultas sobre la aplicacion, contactar al equipo de desarrollo.

---

**Version**: 1.0  
**Ultima actualizacion**: 2024  
**Plataforma objetivo**: Android (.NET 9)
### Dispositivo
- Android 5.0 (API 21) o superior
- Espacio libre: 50 MB m�nimo
- Conexi�n a internet (para sincronizaci�n)

## Instalaci�n y Compilaci�n

1. Clonar el repositorio
2. Abrir `sys21_campos_zukarmex.sln` en Visual Studio
3. Restaurar paquetes NuGet
4. Configurar la URL de la API en `AppConfigService.cs`
5. Compilar y ejecutar en emulador o dispositivo

## Funcionalidades Futuras

- [ ] Notificaciones push
- [ ] Reportes en PDF
- [ ] Sincronizaci�n en segundo plano
- [ ] Modo completamente offline
- [ ] Soporte para iOS
- [ ] Dashboard anal�tico avanzado

## Tecnolog�as Utilizadas

- **.NET MAUI 9.0** - Framework multiplataforma
- **SQLite** - Base de datos local
- **CommunityToolkit.Mvvm** - MVVM helpers
- **Newtonsoft.Json** - Serializaci�n JSON
- **System.IdentityModel.Tokens.Jwt** - Manejo de JWT
- **CommunityToolkit.Maui** - Controles adicionales

## Contacto

Para soporte t�cnico o consultas sobre la aplicaci�n, contactar al equipo de desarrollo.

---

**Versi�n**: 1.0  
**�ltima actualizaci�n**: 2024  
**Plataforma objetivo**: Android (.NET 9)**Plataforma objetivo**: Android (.NET 9)**Plataforma objetivo**: Android (.NET 9)**Plataforma objetivo**: Android (.NET 9)