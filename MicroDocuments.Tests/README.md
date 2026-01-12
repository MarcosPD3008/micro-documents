# MicroDocuments Tests - Documentación de Tests

Este documento describe en detalle todos los tests del proyecto MicroDocuments, organizados por módulo y clase de test.

## Estructura de Tests

Los tests están organizados siguiendo la misma estructura del proyecto principal:

- **Domain/** - Tests para entidades, enums y objetos de valor del dominio
- **Application/** - Tests para casos de uso, DTOs, mapeos y servicios de aplicación
- **Infrastructure/** - Tests para implementaciones de infraestructura (repositorios, servicios externos, etc.)
- **Api/** - Tests para controladores y endpoints de la API

## Frameworks y Herramientas

- **xUnit**: Framework de testing para .NET
- **Moq**: Framework para crear mocks y stubs
- **FluentAssertions**: Biblioteca que proporciona aserciones más legibles
- **EntityFrameworkCore.InMemory**: Base de datos en memoria para tests de integración

## Ejecutar Tests

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con salida detallada
dotnet test --logger "console;verbosity=detailed"

# Ejecutar tests de un proyecto específico
dotnet test MicroDocuments.Tests/MicroDocuments.Tests.csproj
```

---

## Tests del Dominio

### DocumentTests

**Ubicación**: `Domain/Entities/DocumentTests.cs`

Tests para la entidad `Document` del dominio.

#### `Document_Should_Create_WithAllProperties`
- **Propósito**: Verifica que un documento se puede crear con todas sus propiedades
- **Escenario**: Se crea un documento usando el `DocumentBuilder` con todas las propiedades configuradas
- **Validaciones**: 
  - Todas las propiedades tienen los valores esperados
  - El ID no está vacío

#### `Document_Should_AllowNull_OptionalProperties`
- **Propósito**: Verifica que las propiedades opcionales pueden ser nulas
- **Escenario**: Se crea un documento con propiedades opcionales (CustomerId, CorrelationId, Url) en null
- **Validaciones**: Las propiedades opcionales son correctamente null

#### `Document_Should_HaveDefaultValues`
- **Propósito**: Verifica que un documento tiene valores por defecto cuando se crea sin parámetros
- **Escenario**: Se crea un documento usando el constructor por defecto
- **Validaciones**: 
  - Filename y ContentType son strings vacíos
  - El ID no está vacío
  - Size es 0

### EnumTests

**Ubicación**: `Domain/Enums/EnumTests.cs`

Tests para los enums del dominio (DocumentType, Channel, DocumentStatus).

#### `DocumentType_Should_HaveValidValues`
- **Propósito**: Verifica que todos los valores del enum DocumentType son válidos
- **Escenario**: Se prueba cada valor del enum (KYC, CONTRACT, FORM, SUPPORTING_DOCUMENT, OTHER)
- **Validaciones**: Cada valor es un entero >= 0 y está definido en el enum

#### `Channel_Should_HaveValidValues`
- **Propósito**: Verifica que todos los valores del enum Channel son válidos
- **Escenario**: Se prueba cada valor del enum (BRANCH, DIGITAL, BACKOFFICE, OTHER)
- **Validaciones**: Cada valor es un entero >= 0 y está definido en el enum

#### `DocumentStatus_Should_HaveValidValues`
- **Propósito**: Verifica que todos los valores del enum DocumentStatus son válidos
- **Escenario**: Se prueba cada valor del enum (RECEIVED, SENT, FAILED)
- **Validaciones**: Cada valor es un entero >= 0 y está definido en el enum

#### `DocumentType_Should_SerializeAndDeserialize`
- **Propósito**: Verifica que el enum DocumentType se serializa y deserializa correctamente en JSON
- **Escenario**: Se serializa un valor del enum a JSON y luego se deserializa
- **Validaciones**: El valor deserializado coincide con el original

#### `Channel_Should_SerializeAndDeserialize`
- **Propósito**: Verifica que el enum Channel se serializa y deserializa correctamente en JSON
- **Escenario**: Se serializa un valor del enum a JSON y luego se deserializa
- **Validaciones**: El valor deserializado coincide con el original

---

## Tests de Aplicación

### UploadDocumentUseCaseTests

**Ubicación**: `Application/UseCases/UploadDocumentUseCaseTests.cs`

Tests para el caso de uso de carga de documentos.

#### `ExecuteAsync_Should_SaveDocument_When_ValidRequest`
- **Propósito**: Verifica que un documento se guarda correctamente cuando la solicitud es válida
- **Escenario**: Se envía una solicitud válida con un archivo codificado en base64
- **Validaciones**: 
  - El documento se guarda en el repositorio
  - El archivo se guarda en el almacenamiento
  - Se retorna un ID de documento válido

#### `ExecuteAsync_Should_ThrowException_When_EncodedFileIsInvalid`
- **Propósito**: Verifica que se lanza una excepción cuando el archivo codificado es inválido
- **Escenario**: Se envía una solicitud con un string base64 inválido
- **Validaciones**: Se lanza una `FormatException`

#### `ExecuteAsync_Should_CleanupFile_When_SaveFails`
- **Propósito**: Verifica que se limpia el archivo temporal cuando falla el guardado en la base de datos
- **Escenario**: El archivo se guarda correctamente pero falla el guardado en el repositorio
- **Validaciones**: 
  - Se llama al método de eliminación del archivo
  - Se propaga la excepción

#### `ExecuteStreamAsync_Should_SaveDocument_When_ValidStream`
- **Propósito**: Verifica que un documento se guarda correctamente cuando se usa un stream
- **Escenario**: Se envía una solicitud válida con un stream de archivo
- **Validaciones**: 
  - El documento se guarda en el repositorio
  - El stream se guarda en el almacenamiento
  - Se retorna un ID de documento válido

#### `ExecuteStreamAsync_Should_ThrowException_When_FileStreamIsNull`
- **Propósito**: Verifica que se lanza una excepción cuando el stream es null
- **Escenario**: Se envía una solicitud con FileStream en null
- **Validaciones**: Se lanza una `ArgumentException`

#### `ExecuteStreamAsync_Should_CleanupFile_When_SaveFails`
- **Propósito**: Verifica que se limpia el archivo cuando falla el guardado desde stream
- **Escenario**: El guardado del stream falla
- **Validaciones**: 
  - Se llama al método de eliminación del archivo
  - Se propaga la excepción

### SearchDocumentsUseCaseTests

**Ubicación**: `Application/UseCases/SearchDocumentsUseCaseTests.cs`

Tests para el caso de uso de búsqueda de documentos.

#### `ExecuteAsync_Should_ReturnDocuments_When_NoFilters`
- **Propósito**: Verifica que se retornan todos los documentos cuando no hay filtros
- **Escenario**: Se realiza una búsqueda sin filtros
- **Validaciones**: Se retornan todos los documentos en la base de datos

#### `ExecuteAsync_Should_ApplyFilters_When_FilterStringProvided`
- **Propósito**: Verifica que se aplican los filtros correctamente cuando se proporciona un string de filtro
- **Escenario**: Se realiza una búsqueda con filtros (ej: filename contiene "test")
- **Validaciones**: Solo se retornan los documentos que cumplen los criterios de filtro

### SearchDocumentsPagedUseCaseTests

**Ubicación**: `Application/UseCases/SearchDocumentsPagedUseCaseTests.cs`

Tests para el caso de uso de búsqueda paginada de documentos.

#### `ExecuteAsync_Should_ReturnPagedResults_When_ValidRequest`
- **Propósito**: Verifica que se retornan resultados paginados correctamente
- **Escenario**: Se realiza una búsqueda paginada con 25 documentos, página 1, tamaño 10
- **Validaciones**: 
  - Se retornan 10 documentos
  - El total es 25
  - La paginación funciona correctamente

#### `ExecuteAsync_Should_ApplyFilters_When_FilterProvided`
- **Propósito**: Verifica que se aplican filtros en búsquedas paginadas
- **Escenario**: Se realiza una búsqueda paginada con filtros
- **Validaciones**: Los filtros se aplican correctamente a los resultados paginados

### FilterParserTests

**Ubicación**: `Application/Filtering/FilterParserTests.cs`

Tests para el parser de filtros.

#### Tests de Parsing
- Verifica que el parser puede parsear diferentes formatos de filtros
- Valida el manejo de operadores (equals, contains, greater than, etc.)
- Verifica el manejo de valores múltiples

### FilterExpressionBuilderTests

**Ubicación**: `Application/Filtering/FilterExpressionBuilderTests.cs`

Tests para el constructor de expresiones de filtro.

#### Tests de Construcción de Expresiones
- Verifica que se construyen expresiones LINQ correctas a partir de filtros parseados
- Valida el manejo de diferentes tipos de propiedades
- Verifica la combinación de múltiples filtros

### QueryableExtensionsTests

**Ubicación**: `Application/Extensions/QueryableExtensionsTests.cs`

Tests para las extensiones de IQueryable.

#### Tests de Paginación
- Verifica que `ToPagedAsync` retorna resultados paginados correctamente
- Valida el cálculo de totales y páginas
- Verifica el indicador de página siguiente

#### Tests de Filtrado
- Verifica que `ApplyFilters` aplica filtros correctamente
- Valida el manejo de strings de filtro vacíos o nulos

#### Tests de Ordenamiento
- Verifica que `ApplySorting` ordena correctamente
- Valida ordenamiento ascendente y descendente

### DocumentMappingsTests

**Ubicación**: `Application/Mappings/DocumentMappingsTests.cs`

Tests para los mapeos entre entidades y DTOs.

#### Tests de Mapeo
- Verifica que las entidades se mapean correctamente a DTOs
- Valida que todas las propiedades se mapean correctamente
- Verifica el manejo de valores nulos

---

## Tests de Infraestructura

### DocumentRepositoryTests

**Ubicación**: `Infrastructure/Persistence/DocumentRepositoryTests.cs`

Tests para el repositorio de documentos.

#### `SaveAsync_Should_AddNewDocument_When_IdIsEmpty`
- **Propósito**: Verifica que se crea un nuevo documento cuando el ID está vacío
- **Escenario**: Se guarda un documento con `Guid.Empty`
- **Validaciones**: 
  - Se genera un nuevo ID
  - El documento se guarda en la base de datos
  - El conteo de documentos es 1

#### `SaveAsync_Should_UpdateExistingDocument_When_IdExists`
- **Propósito**: Verifica que se actualiza un documento existente cuando el ID ya existe
- **Escenario**: Se guarda un documento con un ID que ya existe en la base de datos
- **Validaciones**: 
  - El documento se actualiza (no se crea uno nuevo)
  - Los valores actualizados son correctos
  - El conteo de documentos sigue siendo 1

#### `SaveAsync_Should_SetCreated_When_NewDocument`
- **Propósito**: Verifica que se establece la fecha de creación para documentos nuevos
- **Escenario**: Se guarda un documento nuevo
- **Validaciones**: 
  - La fecha `Created` no es el valor por defecto
  - La fecha está cerca de la hora actual (dentro de 5 segundos)

#### `SaveAsync_Should_SetUpdated_When_ExistingDocument`
- **Propósito**: Verifica que se establece la fecha de actualización para documentos existentes
- **Escenario**: Se actualiza un documento existente
- **Validaciones**: 
  - La fecha `Updated` no es null
  - La fecha está cerca de la hora actual (dentro de 5 segundos)

#### `GetByIdAsync_Should_ReturnDocument_When_Exists`
- **Propósito**: Verifica que se retorna un documento cuando existe
- **Escenario**: Se busca un documento por ID que existe en la base de datos
- **Validaciones**: 
  - El resultado no es null
  - El ID coincide
  - Las propiedades son correctas

#### `GetByIdAsync_Should_ReturnNull_When_NotExists`
- **Propósito**: Verifica que se retorna null cuando el documento no existe
- **Escenario**: Se busca un documento por ID que no existe
- **Validaciones**: El resultado es null

#### `GetAll_Should_ReturnQueryable`
- **Propósito**: Verifica que `GetAll` retorna un IQueryable funcional
- **Escenario**: Se obtiene el queryable y se cuenta el número de documentos
- **Validaciones**: 
  - El resultado no es null
  - El conteo es correcto

### RateLimitingMiddlewareTests

**Ubicación**: `Infrastructure/Middleware/RateLimitingMiddlewareTests.cs`

Tests para el middleware de rate limiting.

#### `InvokeAsync_Should_PassThrough_When_RateLimitingDisabled`
- **Propósito**: Verifica que las solicitudes pasan cuando el rate limiting está deshabilitado
- **Escenario**: El rate limiting está deshabilitado en la configuración
- **Validaciones**: 
  - La solicitud pasa al siguiente middleware
  - El código de estado es 200

#### `InvokeAsync_Should_AllowRequest_When_UnderLimit`
- **Propósito**: Verifica que las solicitudes pasan cuando están bajo el límite
- **Escenario**: Se realiza una solicitud cuando el contador está bajo el límite
- **Validaciones**: 
  - La solicitud pasa al siguiente middleware
  - El código de estado es 200

#### `InvokeAsync_Should_BlockRequest_When_OverLimit`
- **Propósito**: Verifica que las solicitudes se bloquean cuando exceden el límite
- **Escenario**: Se realizan más solicitudes que el límite permitido (ej: 3 solicitudes con límite de 2)
- **Validaciones**: 
  - Las solicitudes excedentes retornan código 429 (Too Many Requests)
  - Solo las solicitudes dentro del límite pasan al siguiente middleware

#### `InvokeAsync_Should_ResetWindow_AfterOneMinute`
- **Propósito**: Verifica que la ventana de tiempo se resetea después de un minuto
- **Escenario**: Se realizan solicitudes que exceden el límite en la misma ventana de tiempo
- **Validaciones**: Las solicitudes excedentes son bloqueadas

#### `InvokeAsync_Should_HandleConcurrentRequests`
- **Propósito**: Verifica que el middleware maneja correctamente solicitudes concurrentes
- **Escenario**: Se realizan múltiples solicitudes concurrentes (ej: 10 solicitudes con límite de 5)
- **Validaciones**: 
  - El middleware es thread-safe
  - Solo se permiten las solicitudes dentro del límite
  - No hay condiciones de carrera

### LocalFileStorageTests

**Ubicación**: `Infrastructure/ExternalServices/LocalFileStorageTests.cs`

Tests para el servicio de almacenamiento de archivos local.

#### Tests de Guardado
- Verifica que los archivos se guardan correctamente
- Valida el guardado desde bytes y desde stream
- Verifica que se crean los directorios necesarios

#### Tests de Lectura
- Verifica que los archivos se pueden leer correctamente
- Valida el retorno de streams

#### Tests de Eliminación
- Verifica que los archivos se eliminan correctamente
- Valida el manejo de archivos inexistentes

### DocumentPublisherTests

**Ubicación**: `Infrastructure/ExternalServices/DocumentPublisherTests.cs`

Tests para el servicio de publicación de documentos.

#### Tests de Publicación
- Verifica que los documentos se publican correctamente al servicio externo
- Valida el manejo de errores de red
- Verifica los reintentos en caso de fallo

### DocumentPublisherMockTests

**Ubicación**: `Infrastructure/ExternalServices/DocumentPublisherMockTests.cs`

Tests para el mock del servicio de publicación.

#### Tests del Mock
- Verifica que el mock simula correctamente el comportamiento del servicio real
- Valida que se pueden configurar diferentes escenarios (éxito, fallo, etc.)

### DocumentUploadBackgroundServiceTests

**Ubicación**: `Infrastructure/BackgroundJobs/DocumentUploadBackgroundServiceTests.cs`

Tests para el servicio en segundo plano de carga de documentos.

#### Tests de Procesamiento
- Verifica que los documentos pendientes se procesan correctamente
- Valida el cambio de estado de los documentos
- Verifica el manejo de errores y reintentos

---

## Tests de API

### DocumentsControllerTests

**Ubicación**: `Api/Controllers/DocumentsControllerTests.cs`

Tests para el controlador de documentos.

#### `UploadDocument_Should_ReturnAccepted_When_ValidRequest`
- **Propósito**: Verifica que se retorna 202 Accepted cuando la solicitud es válida
- **Escenario**: Se envía una solicitud de carga válida
- **Validaciones**: 
  - El código de estado es 202
  - Se llama al caso de uso correctamente

#### `UploadDocument_Should_ReturnBadRequest_When_FileIsNull`
- **Propósito**: Verifica que se retorna 400 Bad Request cuando el archivo es null
- **Escenario**: Se envía una solicitud sin archivo
- **Validaciones**: 
  - El código de estado es 400
  - El mensaje indica que el archivo es requerido

#### `UploadDocument_Should_ReturnBadRequest_When_ModelStateInvalid`
- **Propósito**: Verifica que se retorna 400 Bad Request cuando el ModelState es inválido
- **Escenario**: Se envía una solicitud con datos inválidos (ej: DocumentType faltante)
- **Validaciones**: El código de estado es 400

#### `UploadDocument_Should_UseFilenameFromFile_When_FilenameNotProvided`
- **Propósito**: Verifica que se usa el nombre del archivo cuando no se proporciona uno
- **Escenario**: Se envía una solicitud sin especificar el nombre del archivo
- **Validaciones**: Se usa el nombre del archivo del IFormFile

#### `SearchDocuments_Should_ReturnOk_When_ValidRequest`
- **Propósito**: Verifica que se retorna 200 OK cuando la búsqueda es válida
- **Escenario**: Se realiza una búsqueda válida
- **Validaciones**: 
  - El código de estado es 200
  - Se retornan los documentos correctos

#### `SearchDocumentsPaged_Should_ReturnOk_When_ValidRequest`
- **Propósito**: Verifica que se retorna 200 OK cuando la búsqueda paginada es válida
- **Escenario**: Se realiza una búsqueda paginada válida
- **Validaciones**: 
  - El código de estado es 200
  - Se retornan los resultados paginados correctos

#### `SearchDocumentsPaged_Should_ReturnBadRequest_When_SortByIsMissing`
- **Propósito**: Verifica que se retorna 400 Bad Request cuando falta el campo de ordenamiento
- **Escenario**: Se realiza una búsqueda paginada sin especificar SortBy
- **Validaciones**: 
  - El código de estado es 400
  - El mensaje indica que SortBy es requerido

---

## Helpers de Test

### DocumentBuilder

**Ubicación**: `TestHelpers/DocumentBuilder.cs`

Patrón Builder para crear instancias de `Document` en los tests. Proporciona métodos fluidos para configurar las propiedades del documento.

### MockDataFactory

**Ubicación**: `TestHelpers/MockDataFactory.cs`

Factory para crear datos de prueba (documentos, DTOs, etc.) con valores por defecto razonables.

### InMemoryDbContextFactory

**Ubicación**: `TestHelpers/InMemoryDbContextFactory.cs`

Factory para crear instancias de `AppDbContext` usando una base de datos en memoria para tests de integración.

---

## Mejores Prácticas

1. **Arrange-Act-Assert**: Todos los tests siguen el patrón AAA
2. **Nombres Descriptivos**: Los nombres de los tests describen claramente qué se está probando
3. **Aislamiento**: Cada test es independiente y no depende de otros tests
4. **Mocks**: Se usan mocks para aislar las dependencias externas
5. **Datos de Prueba**: Se usan factories y builders para crear datos de prueba consistentes

---

## Cobertura de Tests

Para ver la cobertura de código de los tests:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Esto generará un archivo de cobertura que puede ser visualizado con herramientas como ReportGenerator.


