# Mi Presupuesto

Aplicación para registrar y analizar gastos personales. El backend usa ASP.NET Core Web API, Entity Framework Core, SQL Server, JWT y una arquitectura por capas. El frontend en React se incorporará en el siguiente bloque.

## Arquitectura

- `MiPresupuesto.Domain`: entidades y reglas centrales, sin dependencias de infraestructura.
- `MiPresupuesto.Application`: DTOs, interfaces, validaciones y servicios de aplicación.
- `MiPresupuesto.Infrastructure`: EF Core, repositorios, SQL Server, hash PBKDF2 y creación de JWT.
- `API-MiPresupuesto`: controladores, autenticación y manejo HTTP de errores.
- `MiPresupuesto.Tests`: pruebas unitarias con xUnit.

## Requisitos locales

- .NET SDK 10.0.301 o compatible.
- SQL Server LocalDB para desarrollo. La configuración con contenedores se añadirá antes de la entrega.

## Ejecutar el backend

Desde la raíz del repositorio:

```powershell
dotnet restore .\API-MiPresupuesto\API-MiPresupuesto.slnx
dotnet ef database update --project .\API-MiPresupuesto\MiPresupuesto.Infrastructure\MiPresupuesto.Infrastructure.csproj --startup-project .\API-MiPresupuesto\API-MiPresupuesto\API-MiPresupuesto.csproj
dotnet run --project .\API-MiPresupuesto\API-MiPresupuesto\API-MiPresupuesto.csproj
```

Al iniciarse en ambiente Development, la API también aplica automáticamente las migraciones pendientes.

## Pruebas

```powershell
dotnet test .\API-MiPresupuesto\MiPresupuesto.Tests\MiPresupuesto.Tests.csproj
```

El archivo `API-MiPresupuesto/API-MiPresupuesto/API-MiPresupuesto.http` contiene ejemplos de registro, login y edición de perfil.

## Funcionalidades implementadas

- Registro, inicio de sesión con JWT y edición de perfil.
- CRUD de categorías con color, estado activo/inactivo y validación de duplicados.
- CRUD de métodos de pago con icono opcional, estado activo/inactivo y validación de duplicados.
- CRUD de gastos con monto, fecha, descripción, categoría y método de pago.
- Filtros de gastos por rango de fechas, categoría, método de pago y descripción.
- Listados paginados con total de registros y páginas.
- Aislamiento de todos los recursos por usuario autenticado.
- Eliminación protegida cuando una categoría o método de pago tiene registros asociados.
- Respuestas consistentes para errores de validación, conflictos y recursos inexistentes.

Los endpoints de categorías están disponibles en `/api/categories`, los métodos de pago en `/api/payment-methods` y los gastos en `/api/expenses`. Todos requieren el encabezado `Authorization: Bearer {token}`.

## Seguridad

Las contraseñas se almacenan usando PBKDF2-SHA256 con salt aleatorio y comparación en tiempo constante. La clave JWT incluida en `appsettings.json` es exclusivamente para desarrollo y deberá reemplazarse mediante configuración de entorno al publicar.
