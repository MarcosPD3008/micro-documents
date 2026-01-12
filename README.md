# MicroDocuments - Sistema de Gestión de Documentos

## Descripción del Proyecto

MicroDocuments es una API REST desarrollada en **.NET 8** siguiendo **Arquitectura Hexagonal (Clean Architecture)** que proporciona una solución completa para la gestión y almacenamiento de documentos internos. El sistema actúa como un proxy que gestiona el proceso de carga de documentos, almacena metadatos y orquesta la publicación de documentos a servicios internos de almacenamiento mediante procesamiento asíncrono.

### Características Principales

- **Autenticación con API Keys**: Sistema de autenticación basado en API keys con hashing HMAC-SHA256
- **Caché en Memoria**: Carga de API keys en memoria al iniciar para validaciones rápidas
- **Carga Asíncrona de Documentos**: Procesamiento asíncrono con `BackgroundService` para publicación en segundo plano
- **Sistema de Filtros Genérico**: Filtrado dinámico mediante construcción de expresiones LINQ en tiempo de ejecución
- **Paginación Genérica**: Extensión `ToPagedAsync<T>` reutilizable para cualquier entidad
- **Ordenamiento Dinámico**: Ordenamiento genérico mediante reflexión y expresiones LINQ
- **Rate Limiting**: Middleware thread-safe con `SemaphoreSlim` para control de límites de solicitudes
- **Streaming de Archivos**: Procesamiento eficiente de archivos mediante streams sin cargar todo en memoria
- **Arquitectura Hexagonal**: Separación clara de capas (Domain, Application, Infrastructure, API)
- **Persistencia**: Almacenamiento de metadatos en SQLite con Entity Framework Core
- **Auditoría**: Registro de quién creó, actualizó o eliminó cada registro mediante API key ID
- **Health Checks**: Endpoints para monitoreo del estado de la aplicación
- **Dockerizado**: Contenedorización completa para despliegue simplificado

---

## Índice

- [Descripción del Proyecto](#descripción-del-proyecto)
  - [Características Principales](#características-principales)
- [Pre-requisitos](#pre-requisitos)
  - [Requisitos Obligatorios](#requisitos-obligatorios)
  - [Requisitos Opcionales](#requisitos-opcionales)
- [Cómo Ejecutarlo](#cómo-ejecutarlo)
  - [Ejecución Local](#ejecución-local)
  - [Ejecución con Docker](#ejecución-con-docker)
- [Configuración](#configuración)
  - [Archivos de Configuración](#archivos-de-configuración)
  - [Variables de Configuración](#variables-de-configuración)
  - [Variables de Entorno](#variables-de-entorno)
  - [Configuración de Desarrollo](#configuración-de-desarrollo)
- [Tests](#tests)
  - [Ejecutar los Tests](#ejecutar-los-tests)
  - [Documentación Detallada de Tests](#documentación-detallada-de-tests)
- [Estructura del Proyecto](#estructura-del-proyecto)
  - [Principios de Diseño](#principios-de-diseño)
- [Características Técnicas Implementadas](#características-técnicas-implementadas)
  - [Rate Limiting Middleware](#rate-limiting-middleware)
  - [Paginación Genérica](#paginación-genérica)
  - [Sistema de Filtros Genérico](#sistema-de-filtros-genérico)
  - [Ordenamiento Dinámico](#ordenamiento-dinámico)
  - [Procesamiento Asíncrono con Background Service](#procesamiento-asíncrono-con-background-service)
  - [Streaming de Archivos](#streaming-de-archivos)
  - [Arquitectura Hexagonal](#arquitectura-hexagonal)
- [Endpoints Principales](#endpoints-principales)
- [Autenticación con API Keys](#autenticación-con-api-keys)
- [Gestión de API Keys](#gestión-de-api-keys)
- [Configuración de API Keys](#configuración-de-api-keys)
- [Características de Seguridad](#características-de-seguridad)
- [Tecnologías Utilizadas](#tecnologías-utilizadas)
- [Licencia](#licencia)
- [Soporte](#soporte)

---

## Pre-requisitos

Para ejecutar este proyecto, necesitas tener instalado:

### Requisitos Obligatorios

- **.NET 8 SDK** o superior
  - Descarga desde: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verifica la instalación ejecutando: `dotnet --version`

- **Docker** (opcional, solo si deseas ejecutar con Docker)
  - Docker Desktop: https://www.docker.com/products/docker-desktop
  - Verifica la instalación ejecutando: `docker --version`

- **Git** (para clonar el repositorio)
  - Descarga desde: https://git-scm.com/downloads

---

### Requisitos Opcionales

- **Visual Studio 2022** o **Visual Studio Code** con extensión C#

---

## Cómo Ejecutarlo

### Ejecución Local

#### 1. Clonar el Repositorio

```bash
git clone <url-del-repositorio>
cd micro-documents
```

#### 2. Restaurar Dependencias

```bash
dotnet restore
```

#### 3. Compilar el Proyecto

```bash
dotnet build
```

#### 4. Ejecutar la Aplicación

```bash
cd MicroDocuments.Api
dotnet run
```

O desde la raíz del proyecto:

```bash
dotnet run --project MicroDocuments.Api/MicroDocuments.Api.csproj
```

La aplicación estará disponible en:
- **HTTP**: `http://localhost:5000` o `http://localhost:8080`
- **HTTPS**: `https://localhost:5001` o `https://localhost:8081`
- **Swagger UI**: `https://localhost:5001/swagger` (en modo desarrollo)

#### 5. Verificar que la Aplicación Está Funcionando

```bash
curl http://localhost:8080/health
```

Deberías recibir una respuesta `200 OK`.

**📝 Nota:** Para probar los endpoints de la API, necesitarás una API key válida. Consulta la sección [Autenticación con API Keys](#autenticación-con-api-keys) y [Gestión de API Keys](#gestión-de-api-keys) para obtener más información sobre cómo crear y usar API keys.

---

### Ejecución con Docker

#### 1. Construir la Imagen Docker

```bash
docker-compose build
```

O construir manualmente:

```bash
docker build -t microdocuments-api .
```

#### 2. Ejecutar con Docker Compose

```bash
docker-compose up
```

Para ejecutar en segundo plano:

```bash
docker-compose up -d
```

#### 3. Verificar el Estado del Contenedor

```bash
docker-compose ps
```

#### 4. Ver los Logs

```bash
docker-compose logs -f microdocuments-api
```

#### 5. Detener los Contenedores

```bash
docker-compose down
```

Para detener y eliminar volúmenes:

```bash
docker-compose down -v
```

#### 6. Acceder a la Aplicación

Una vez ejecutándose, la aplicación estará disponible en:
- **HTTP**: `http://localhost:8080`
- **Health Check**: `http://localhost:8080/health`

**📝 Nota:** Para probar los endpoints de la API, necesitarás una API key válida. Consulta la sección [Autenticación con API Keys](#autenticación-con-api-keys) y [Gestión de API Keys](#gestión-de-api-keys) para obtener más información sobre cómo crear y usar API keys.

---

## Configuración

### Archivos de Configuración

La configuración principal se encuentra en `MicroDocuments.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=db/documents.db"
  },
  "FileUpload": {
    "MaxFileSizeMB": 100
  },
  "DocumentPublisher": {
    "Url": "https://internal-document-storage.bhd.com.do/api/documents",
    "UseMock": true
  },
  "ApiKey": {
    "SecretKey": "SSbhaHFpR6ojH7WFLniC81AYrfh7yJJlsvKQkoTsly7DwQeJvikliAL37R0l/usC2Wu8h3YtPW01bb/awZyusQ==",
    "MasterKey": "bhd-1234567890-1234567890",
    "GlobalFilter": false
  },
  "Resilience": {
    "RateLimiter": {
      "Enabled": true,
      "RequestsPerMinute": 100
    },
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetryAttempts": 3
    }
  }
}
```

### Variables de Configuración

#### ConnectionStrings

- **DefaultConnection**: Cadena de conexión a la base de datos SQLite
  - Formato: `Data Source=<ruta>/documents.db`
  - En Docker: `Data Source=/app/db/documents.db`

---

#### FileUpload

- **MaxFileSizeMB**: Tamaño máximo permitido para archivos en megabytes (por defecto: 100 MB)
  - La validación se realiza en el controlador antes de procesar el archivo
  - Si el archivo excede el límite, se retorna `400 Bad Request` con un mensaje descriptivo
  - Este valor también se usa para configurar los límites de `FormOptions` y `Kestrel` para permitir archivos grandes

---

#### DocumentPublisher

- **Url**: URL del servicio interno de publicación de documentos
- **UseMock**: Si es `true`, utiliza un mock del servicio de publicación (útil para desarrollo)

---

#### ApiKey

- **SecretKey**: Clave secreta utilizada para hashear las API keys (requerido)
  - Debe ser una cadena segura y aleatoria
  - **Nunca compartas este valor** y cámbialo en producción
- **MasterKey**: API key maestra que se crea automáticamente al inicializar la base de datos
  - Se usa para crear las primeras API keys del sistema
  - Debe tener el formato: `bhd-{guid}-{random}`
- **GlobalFilter**: Habilita/deshabilita el filtro global por API key (Row Level Security - RLS)
  - Si es `true`: Los documentos solo son visibles para la API key que los creó
  - Si es `false`: Todos los documentos son visibles para todas las API keys (comportamiento por defecto)
  - Actúa como un interruptor para habilitar/deshabilitar el aislamiento de datos por API key

---

#### Resilience

- **RateLimiter.Enabled**: Habilita o deshabilita el rate limiting
- **RateLimiter.RequestsPerMinute**: Número máximo de solicitudes por minuto
- **RetryPolicy.Enabled**: Habilita o deshabilita la política de reintentos
- **RetryPolicy.MaxRetryAttempts**: Número máximo de intentos de reintento

---

### Variables de Entorno

Puedes sobrescribir la configuración usando variables de entorno:

```bash
export ConnectionStrings__DefaultConnection="Data Source=/custom/path/documents.db"
export DocumentPublisher__UseMock="false"
export Resilience__RateLimiter__RequestsPerMinute="200"
```

En Docker Compose, las variables de entorno se configuran en el archivo `docker-compose.yml`.

---

### Configuración de Desarrollo

Para desarrollo local, existe `appsettings.Development.json` que puede contener configuraciones específicas para el entorno de desarrollo.

---

## Tests

### Ejecutar los Tests

Para ejecutar todos los tests del proyecto:

```bash
dotnet test
```

Para ejecutar tests de un proyecto específico:

```bash
dotnet test MicroDocuments.Tests/MicroDocuments.Tests.csproj
```

Para ejecutar con cobertura de código:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Para ejecutar tests con salida detallada:

```bash
dotnet test --logger "console;verbosity=detailed"
```

---

### Documentación Detallada de Tests

Para una descripción detallada de todos los tests, incluyendo qué prueba cada uno y cómo están organizados, consulta el archivo:

**[MicroDocuments.Tests/README.md](MicroDocuments.Tests/README.md)**

Este archivo contiene:
- Descripción de cada clase de test
- Explicación de los casos de prueba individuales
- Estructura y organización de los tests
- Frameworks y herramientas utilizadas

---

## Estructura del Proyecto

El proyecto sigue una arquitectura hexagonal (Clean Architecture) con las siguientes capas:

```
micro-documents/
├── MicroDocuments.Domain/          # Capa de Dominio
│   ├── Entities/                   # Entidades del dominio
│   ├── Enums/                      # Enumeraciones
│   └── Ports/                      # Interfaces (puertos)
│
├── MicroDocuments.Application/     # Capa de Aplicación
│   ├── DTOs/                       # Objetos de transferencia de datos
│   ├── UseCases/                   # Casos de uso
│   ├── Mappings/                   # Mapeos entre entidades y DTOs
│   ├── Filtering/                  # Lógica de filtrado
│   ├── Pagination/                 # Lógica de paginación
│   └── Sorting/                    # Lógica de ordenamiento
│
├── MicroDocuments.Infrastructure/  # Capa de Infraestructura
│   ├── Persistence/                # Repositorios y DbContext
│   ├── ExternalServices/           # Servicios externos
│   ├── BackgroundJobs/             # Servicios en segundo plano
│   ├── Middleware/                 # Middlewares personalizados
│   └── Configuration/              # Configuraciones
│
├── MicroDocuments.Api/             # Capa de Presentación
│   ├── Controllers/                # Controladores REST
│   └── DTOs/                       # DTOs específicos de la API
│
└── MicroDocuments.Tests/            # Proyecto de Tests
    ├── Domain/                      # Tests del dominio
    ├── Application/                 # Tests de aplicación
    ├── Infrastructure/              # Tests de infraestructura
    ├── Api/                         # Tests de API
    └── TestHelpers/                 # Utilidades para tests
```

### Principios de Diseño

- **Separación de Responsabilidades**: Cada capa tiene una responsabilidad específica
- **Inversión de Dependencias**: Las capas superiores dependen de abstracciones (interfaces)
- **Testabilidad**: Diseño que facilita las pruebas unitarias e integración
- **Escalabilidad**: Estructura que permite agregar nuevas funcionalidades fácilmente

---

## Características Técnicas Implementadas

Esta sección detalla las características técnicas avanzadas implementadas en el proyecto.

### Rate Limiting Middleware

Implementación de rate limiting thread-safe utilizando `SemaphoreSlim` para garantizar concurrencia segura.

**Características:**
- **Ventana deslizante de 1 minuto**: El contador se resetea automáticamente cada minuto
- **Thread-safe**: Utiliza `SemaphoreSlim` para sincronización en entornos concurrentes
- **Configurable**: Habilitado/deshabilitado mediante configuración
- **Respuesta HTTP 429**: Retorna `TooManyRequests` cuando se excede el límite

**Implementación:**
```csharp
public class RateLimitingMiddleware
{
    private readonly SemaphoreSlim _semaphore;
    private DateTime _windowStart;
    private int _requestCount;
    
    // Controla solicitudes por minuto con ventana deslizante
}
```

**Configuración:**
```json
{
  "Resilience": {
    "RateLimiter": {
      "Enabled": true,
      "RequestsPerMinute": 100
    }
  }
}
```

---

### Paginación Genérica

Sistema de paginación genérico implementado como extensión de `IQueryable<T>` que funciona con cualquier entidad.

**Características:**
- **Genérico**: `ToPagedAsync<T>` funciona con cualquier tipo de entidad
- **Eficiente**: Utiliza `CountAsync` y `Skip/Take` de Entity Framework para optimizar consultas
- **Metadata completa**: Retorna total de registros, indicador de página siguiente, etc.

**Implementación:**
```csharp
public static async Task<PaginationResponse<T>> ToPagedAsync<T>(
    this IQueryable<T> queryable,
    PaginationRequest pagination,
    CancellationToken cancellationToken = default)
{
    var total = await queryable.CountAsync(cancellationToken);
    var skip = (pagination.Page - 1) * pagination.PageSize;
    var items = await queryable.Skip(skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
    
    return new PaginationResponse<T>
    {
        Content = items,
        Total = total,
        NextPage = pagination.Page < Math.Ceiling(total / (double)pagination.PageSize)
    };
}
```

**Uso:**
```csharp
var result = await queryable
    .ApplyFilters(filterString)
    .ApplySorting(sortRequest)
    .ToPagedAsync(paginationRequest);
```

---

### Sistema de Filtros Genérico

Sistema completo de filtrado dinámico que construye expresiones LINQ en tiempo de ejecución mediante `Expression Trees`.

**Características:**
- **Parser de filtros**: Parsea strings de filtro en objetos `FilterCriteria`
- **Construcción dinámica de expresiones**: Utiliza `Expression Trees` para construir predicados LINQ
- **Múltiples operadores**: Soporta 11 operadores diferentes
- **Operadores lógicos**: Soporta `AND` y `OR` entre filtros
- **Conversión automática de tipos**: Convierte strings a tipos apropiados (enums, DateTime, Guid, etc.)

**Operadores Soportados:**
- `eq` / `equals` - Igualdad
- `ne` / `neq` / `notEquals` - Desigualdad
- `gt` / `greaterThan` - Mayor que
- `ge` / `gte` / `greaterThanOrEqual` - Mayor o igual que
- `lt` / `lessThan` - Menor que
- `le` / `lte` / `lessThanOrEqual` - Menor o igual que
- `contains` - Contiene (strings)
- `startswith` - Comienza con
- `endswith` - Termina con
- `in` - En lista de valores
- `isnull` / `isnotnull` - Verificación de null

**Ejemplo de uso:**
```
filename contains 'test' AND status eq 'RECEIVED' OR uploadDate gt '2024-01-01'
```

**Implementación:**
```csharp
public static Expression<Func<T, bool>> BuildFilterExpression<T>(List<FilterCriteria> filters)
{
    // Construye expresiones LINQ dinámicamente usando Expression Trees
    // Combina múltiples filtros con operadores lógicos AND/OR
}
```

**Flujo:**
1. `FilterParser.Parse()` - Parsea el string de filtro
2. `FilterExpressionBuilder.BuildFilterExpression<T>()` - Construye la expresión LINQ
3. `queryable.Where(expression)` - Aplica el filtro al queryable

---

### Ordenamiento Dinámico

Sistema de ordenamiento genérico que utiliza reflexión y expresiones LINQ para ordenar por cualquier propiedad.

**Características:**
- **Genérico**: Funciona con cualquier entidad y propiedad
- **Reflexión**: Utiliza `PropertyInfo` para acceder a propiedades dinámicamente
- **Expresiones LINQ**: Construye expresiones `OrderBy`/`OrderByDescending` dinámicamente
- **Case-insensitive**: Búsqueda de propiedades sin distinguir mayúsculas/minúsculas

**Implementación:**
```csharp
public static IQueryable<T> ApplySorting<T>(
    this IQueryable<T> queryable,
    SortRequest sortRequest)
{
    var propertyInfo = typeof(T).GetProperty(
        sortRequest.SortBy,
        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    
    // Construye expresión OrderBy/OrderByDescending dinámicamente
    var methodName = sortRequest.SortDirection.ToUpper() == "DESC" 
        ? "OrderByDescending" 
        : "OrderBy";
    
    // Utiliza Expression.Call para construir la llamada al método dinámicamente
}
```

**Uso:**
```csharp
var sortedQuery = queryable.ApplySorting(new SortRequest 
{ 
    SortBy = "UploadDate", 
    SortDirection = "DESC" 
});
```

---

### Procesamiento Asíncrono con Background Service

Servicio en segundo plano (`BackgroundService`) que procesa documentos pendientes de forma asíncrona.

**Características:**
- **Procesamiento en lotes**: Procesa hasta 10 documentos por ciclo
- **Intervalo configurable**: Ejecuta cada 5 segundos
- **Limpieza automática**: Limpia archivos huérfanos periódicamente (cada 5 minutos)
- **Manejo de errores robusto**: Marca documentos como `FAILED` solo si falla la publicación, no si falla la limpieza posterior
- **Scope management**: Utiliza `IServiceScope` para acceso a servicios con scoped lifetime
- **Gestión de streams**: Asegura que los streams se cierren correctamente antes de intentar eliminar archivos

**Flujo de procesamiento:**
1. Busca documentos con estado `RECEIVED`
2. Lee el archivo desde almacenamiento temporal mediante stream
3. Publica el documento al servicio externo mediante stream
4. Cierra el stream correctamente antes de continuar
5. Actualiza el estado a `SENT` y guarda la URL
6. Intenta eliminar el archivo temporal (con reintentos automáticos si está bloqueado)
7. Si la eliminación falla, solo se registra un warning; el documento ya está marcado como `SENT`

**Manejo de errores:**
- Si la publicación falla: El documento se marca como `FAILED` y se intenta limpiar el archivo temporal
- Si la eliminación falla después de publicación exitosa: Solo se registra un warning; el documento permanece como `SENT` (el archivo se limpiará en el siguiente ciclo de limpieza)

**Implementación:**
```csharp
public class DocumentUploadBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingUploads(stoppingToken);
            await CleanupOrphanedFilesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

---

### Streaming de Archivos

Procesamiento eficiente de archivos mediante streams para evitar cargar archivos completos en memoria.

**Características:**
- **Streaming de entrada**: `IFormFile.OpenReadStream()` para leer archivos
- **Streaming de almacenamiento**: `SaveFromStreamAsync()` para guardar sin cargar en memoria
- **Streaming de publicación**: `PublishStreamAsync()` para enviar al servicio externo
- **Eficiencia de memoria**: Procesa archivos grandes sin consumir memoria excesiva
- **Gestión de streams**: Los streams se cierran correctamente usando `await using` para evitar bloqueos de archivos
- **Retry logic en eliminación**: `DeleteAsync()` implementa reintentos automáticos (hasta 5 intentos) si el archivo está bloqueado por otro proceso

**Interfaces:**
```csharp
public interface IFileStorage
{
    Task SaveFromStreamAsync(Guid documentId, Stream stream, CancellationToken cancellationToken);
    Task<Stream> GetStreamAsync(Guid documentId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken);
}
```

**Flujo:**
1. Cliente envía archivo → `IFormFile` stream
2. Validación de tamaño en el controlador (antes de procesar)
3. Guardado temporal → `SaveFromStreamAsync()` (stream directo)
4. Background service lee → `GetStreamAsync()` (stream directo)
5. Publicación → `PublishStreamAsync()` (stream directo)
6. Cierre de stream → El stream se cierra automáticamente con `await using`
7. Limpieza → `DeleteAsync()` con reintentos si es necesario

**Validación de tamaño de archivo:**
- La validación se realiza en el controlador antes de procesar el archivo
- El tamaño máximo se configura mediante `FileUploadSettings.MaxFileSizeMB`
- Si el archivo excede el límite, se retorna `400 Bad Request` inmediatamente

---

### Arquitectura Hexagonal

Implementación completa de Arquitectura Hexagonal (Clean Architecture) con separación clara de responsabilidades.

**Capas:**

1. **Domain Layer** (`MicroDocuments.Domain`)
   - Entidades del dominio (`Document`, `BaseEntity`)
   - Enumeraciones (`DocumentType`, `Channel`, `DocumentStatus`)
   - Puertos/Interfaces (`IDocumentRepository`, `IFileStorage`, `IDocumentPublisher`)
   - Sin dependencias externas

2. **Application Layer** (`MicroDocuments.Application`)
   - Casos de uso (`UploadDocumentUseCase`, `SearchDocumentsUseCase`)
   - DTOs y mapeos
   - Lógica de negocio y reglas de aplicación
   - Extensiones genéricas (paginación, filtros, ordenamiento)
   - Depende solo de Domain

3. **Infrastructure Layer** (`MicroDocuments.Infrastructure`)
   - Implementaciones de repositorios (`DocumentRepository`)
   - Servicios externos (`DocumentPublisher`, `LocalFileStorage`)
   - Middleware (`RateLimitingMiddleware`)
   - Background services (`DocumentUploadBackgroundService`)
   - Configuraciones de Entity Framework
   - Depende de Domain y Application

4. **API Layer** (`MicroDocuments.Api`)
   - Controladores REST
   - Configuración de la aplicación
   - Depende de Application e Infrastructure

**Principios aplicados:**
- **Inversión de Dependencias**: Las capas superiores dependen de abstracciones (interfaces)
- **Separación de Responsabilidades**: Cada capa tiene una responsabilidad específica
- **Testabilidad**: Fácil de testear mediante mocks de interfaces
- **Independencia de frameworks**: El dominio no depende de frameworks externos

---

## Endpoints Principales

### Documentos

- **POST** `/api/bhd/mgmt/1/documents/actions/upload` - Cargar un documento
- **GET** `/api/bhd/mgmt/1/documents` - Buscar documentos (sin paginación)
- **GET** `/api/bhd/mgmt/1/documents/search` - Buscar documentos (con paginación)

---

### API Keys

- **POST** `/api/bhd/mgmt/1/apikeys` - Crear una nueva API key
- **GET** `/api/bhd/mgmt/1/apikeys` - Listar todas las API keys activas
- **GET** `/api/bhd/mgmt/1/apikeys/{id}` - Obtener una API key por ID
- **DELETE** `/api/bhd/mgmt/1/apikeys/{id}` - Eliminar (soft delete) una API key

---

### Health Check

- **GET** `/health` - Estado de salud de la aplicación

---

### Documentación

- **GET** `/swagger` - Interfaz Swagger UI (solo en desarrollo)

---

## Autenticación con API Keys

El sistema utiliza autenticación basada en API Keys para proteger todos los endpoints (excepto `/health` y `/swagger`).

### Cómo Funciona

1. **Autenticación Requerida**: Todos los endpoints requieren el header `X-API-Key` con una API key válida
2. **Middleware de Autenticación**: El `ApiKeyAuthenticationMiddleware` valida cada solicitud
3. **Caché en Memoria**: Las API keys se cargan en memoria al iniciar la aplicación para optimizar el rendimiento
4. **Write-Through Cache**: Las modificaciones se escriben tanto en la base de datos como en la caché

### Usar una API Key

Incluye el header `X-API-Key` en todas tus solicitudes:

```bash
curl -H "X-API-Key: bhd-1234567890-1234567890" \
     https://localhost:5001/api/bhd/mgmt/1/documents
```

O en Postman:
- Agrega el header `X-API-Key` con el valor de tu API key
- O usa la variable `{{api_key}}` si está configurada en el entorno

### Respuestas de Error

Si no proporcionas una API key o es inválida, recibirás una respuesta `401 Unauthorized` con el siguiente formato:

```json
{
  "error": "Unauthorized",
  "message": "API Key is required. Please provide X-API-Key header.",
  "statusCode": 401
}
```

---

## Gestión de API Keys

### Crear una API Key

**Endpoint:** `POST /api/bhd/mgmt/1/apikeys`

**Request Body:**
```json
{
  "name": "Mi API Key de Producción",
  "rateLimitPerMinute": 1000,
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

**Campos:**
- `name` (requerido): Nombre descriptivo para la API key
- `rateLimitPerMinute` (opcional): Límite de solicitudes por minuto (default: 100)
- `expiresAt` (opcional): Fecha de expiración en formato ISO 8601

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Mi API Key de Producción",
  "apiKey": "bhd-1234567890abcdef1234567890-ABCDEF123456",
  "isActive": true,
  "expiresAt": "2025-12-31T23:59:59Z",
  "rateLimitPerMinute": 1000,
  "created": "2024-01-15T10:30:00Z"
}
```

**⚠️ Importante:** El campo `apiKey` en la respuesta contiene el valor de la API key que **solo se muestra una vez** al crear la key. Guárdalo de forma segura, ya que no podrás recuperarlo después.

### Listar API Keys

**Endpoint:** `GET /api/bhd/mgmt/1/apikeys`

**Response (200 OK):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Mi API Key de Producción",
    "isActive": true,
    "expiresAt": "2025-12-31T23:59:59Z",
    "lastUsedAt": "2024-01-20T15:45:00Z",
    "rateLimitPerMinute": 1000,
    "created": "2024-01-15T10:30:00Z",
    "updated": null
  }
]
```

**Nota:** Este endpoint solo retorna API keys activas y no expiradas. El campo `apiKey` (valor real) nunca se incluye por seguridad.

### Obtener una API Key por ID

**Endpoint:** `GET /api/bhd/mgmt/1/apikeys/{id}`

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Mi API Key de Producción",
  "isActive": true,
  "expiresAt": "2025-12-31T23:59:59Z",
  "lastUsedAt": "2024-01-20T15:45:00Z",
  "rateLimitPerMinute": 1000,
  "created": "2024-01-15T10:30:00Z",
  "updated": null
}
```

### Eliminar una API Key

**Endpoint:** `DELETE /api/bhd/mgmt/1/apikeys/{id}`

**Response (204 No Content):** Sin cuerpo de respuesta

**Nota:** La eliminación es un "soft delete" - la API key se marca como eliminada pero no se borra físicamente de la base de datos. Esto permite mantener un historial de auditoría.

---

## Configuración de API Keys

### Master Key

Al crear la base de datos por primera vez, el sistema crea automáticamente una "Master Key" usando el valor configurado en `appsettings.json`:

```json
{
  "ApiKey": {
    "SecretKey": "tu-secret-key-para-hashing",
    "MasterKey": "bhd-1234567890-1234567890"
  }
}
```

**Características:**
- Se crea automáticamente si no existen API keys en la base de datos
- Si existen API keys antiguas que no coinciden con el `MasterKey`, estas se marcan como eliminadas y se crea una nueva Master Key
- La Master Key tiene un límite de 10,000 solicitudes por minuto por defecto
- Usa esta Master Key para crear las primeras API keys del sistema

### Secret Key

El `SecretKey` se utiliza para hashear las API keys antes de almacenarlas en la base de datos. **Nunca compartas este valor** y cámbialo en producción.

---

## Características de Seguridad

### Hashing de API Keys

- Las API keys se hashean usando HMAC-SHA256 antes de almacenarse
- Solo se almacena el hash en la base de datos, nunca el valor plano
- El valor plano de la API key solo se muestra una vez al crearla

### Caché en Memoria

- Todas las API keys activas se cargan en memoria al iniciar la aplicación
- Esto permite validaciones rápidas sin consultar la base de datos en cada solicitud
- La caché se actualiza automáticamente cuando se crean, modifican o eliminan API keys (write-through strategy)

### Auditoría

Todas las operaciones de creación, actualización y eliminación de documentos y API keys registran:
- `CreatedBy`: ID de la API key que creó el registro
- `UpdatedBy`: ID de la API key que actualizó el registro
- `DeletedBy`: ID de la API key que eliminó el registro

Esto permite rastrear quién realizó cada acción en el sistema.

### Row Level Security (RLS) - GlobalFilter

El sistema implementa un mecanismo de Row Level Security (RLS) mediante el campo `GlobalFilter` en la configuración:

- **Cuando `GlobalFilter` es `true`**:
  - Los documentos solo son visibles para la API key que los creó
  - Las búsquedas y consultas automáticamente filtran los resultados por `CreatedBy`
  - Esto proporciona aislamiento de datos entre diferentes API keys
  - Útil para escenarios multi-tenant o cuando se necesita separar datos por cliente/sistema

- **Cuando `GlobalFilter` es `false`** (por defecto):
  - Todos los documentos son visibles para todas las API keys
  - No se aplica ningún filtro adicional
  - Comportamiento estándar de acceso compartido

**Ejemplo de uso:**
```json
{
  "ApiKey": {
    "GlobalFilter": true  // Habilita RLS - cada API key solo ve sus propios documentos
  }
}
```

Este filtro se aplica automáticamente en:
- `GetAll()` - Filtra todos los documentos por API key
- `GetByIdAsync()` - Solo retorna el documento si fue creado por la API key actual

### Endpoints Excluidos

Los siguientes endpoints **no requieren** autenticación:
- `/health` - Health check
- `/swagger` - Documentación Swagger (solo en desarrollo)

Todos los demás endpoints requieren el header `X-API-Key`.

---

## Colección de Postman

El proyecto incluye una colección completa de Postman para facilitar las pruebas de la API.

### Archivos Incluidos

Los archivos de Postman se encuentran en la carpeta `postman/`:

- **`postman/MicroDocuments.Api.postman_collection.json`** - Colección principal con todos los endpoints
- **`postman/MicroDocuments.Api.postman_environment.json`** - Variables de entorno para ejecución local
- **`postman/MicroDocuments.Api.postman_environment_docker.json`** - Variables de entorno para ejecución con Docker

### Cómo Importar

1. Abre Postman
2. Haz clic en **Import** (botón superior izquierdo)
3. Selecciona los archivos desde la carpeta `postman/`:
   - `postman/MicroDocuments.Api.postman_collection.json`
   - `postman/MicroDocuments.Api.postman_environment.json` (o el de Docker)
4. Selecciona el entorno correspondiente en el menú desplegable de entornos

### Endpoints Incluidos

La colección incluye:

- **Health Check**: Verificación del estado de la aplicación
- **API Keys**:
  - **Create API Key**: Crear una nueva API key
  - **List API Keys**: Listar todas las API keys activas
  - **Get API Key**: Obtener una API key por ID
  - **Delete API Key**: Eliminar una API key
- **Upload Document**: Carga de documentos con todos los parámetros
- **Search Documents**: Búsqueda sin paginación con múltiples filtros
- **Search Documents (Minimal)**: Búsqueda mínima con solo parámetros obligatorios
- **Search Documents Paged**: Búsqueda paginada con filtros genéricos
- **Search Documents Paged (Page 2)**: Ejemplo de segunda página
- **Search Documents Paged (Advanced Filters)**: Ejemplo con filtros complejos

### Variables de Entorno

- **`base_url`**: URL base de la API (por defecto: `http://localhost:8080`)
- **`api_key`**: API key para autenticación (por defecto: valor del `MasterKey` de `appsettings.json`)

Puedes modificar estas variables según tu configuración local o de Docker.

### Tests Automatizados

La colección incluye tests automatizados que verifican:
- Códigos de estado HTTP correctos
- Estructura de respuestas
- Propiedades requeridas en las respuestas

---

## Tecnologías Utilizadas

### Backend

- **.NET 8**: Framework de desarrollo multiplataforma
- **ASP.NET Core Web API**: Framework para construcción de APIs REST
- **Entity Framework Core 8.0**: ORM para acceso a datos con soporte para SQLite
- **SQLite**: Base de datos embebida, sin necesidad de servidor separado

### Testing

- **xUnit 2.9.3**: Framework de testing unitario
- **Moq 4.20.72**: Framework para creación de mocks y stubs
- **FluentAssertions 7.0.0**: Biblioteca de aserciones con sintaxis fluida
- **EntityFrameworkCore.InMemory 8.0.0**: Base de datos en memoria para tests de integración

### Infraestructura

- **Docker**: Contenedorización de la aplicación
- **Docker Compose**: Orquestación de contenedores

### Características Técnicas Clave

- **Expression Trees**: Para construcción dinámica de consultas LINQ (filtros genéricos)
- **Reflection**: Para acceso dinámico a propiedades y tipos (ordenamiento genérico)
- **SemaphoreSlim**: Para sincronización thread-safe en rate limiting
- **BackgroundService**: Para procesamiento asíncrono en segundo plano
- **Streaming I/O**: Para procesamiento eficiente de archivos grandes
- **Dependency Injection**: Patrón IoC nativo de ASP.NET Core
- **Extension Methods**: Para implementar paginación, filtros y ordenamiento genéricos

---
