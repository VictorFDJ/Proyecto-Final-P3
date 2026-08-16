# Mi Presupuesto

Aplicación web completa para registrar y analizar gastos personales. El backend usa ASP.NET Core Web API, Entity Framework Core, SQL Server, JWT y una arquitectura por capas. El frontend usa React, TypeScript, Vite y Recharts, con un diseño adaptable a computadoras y celulares.

## Arquitectura

- `MiPresupuesto.Domain`: entidades y reglas centrales, sin dependencias de infraestructura.
- `MiPresupuesto.Application`: DTOs, interfaces, validaciones y servicios de aplicación.
- `MiPresupuesto.Infrastructure`: EF Core, repositorios, SQL Server, hash PBKDF2 y creación de JWT.
- `API-MiPresupuesto`: controladores, autenticación y manejo HTTP de errores.
- `MiPresupuesto.Tests`: pruebas unitarias con xUnit.
- `Frontend-MiPresupuesto`: interfaz React, rutas protegidas, formularios, gráficas y cliente HTTP de la API.

## Requisitos locales

- .NET SDK 10.0.301 o compatible.
- SQL Server LocalDB para desarrollo. La configuración con contenedores se añadirá antes de la entrega.
- Node.js 22 o compatible y npm.

## Ejecutar el backend

Desde la raíz del repositorio:

```powershell
dotnet restore .\API-MiPresupuesto\API-MiPresupuesto.slnx
dotnet ef database update --project .\API-MiPresupuesto\MiPresupuesto.Infrastructure\MiPresupuesto.Infrastructure.csproj --startup-project .\API-MiPresupuesto\API-MiPresupuesto\API-MiPresupuesto.csproj
dotnet run --project .\API-MiPresupuesto\API-MiPresupuesto\API-MiPresupuesto.csproj
```

Al iniciarse en ambiente Development, la API también aplica automáticamente las migraciones pendientes.

La API queda disponible por defecto en `http://localhost:5044` y su documentación Swagger en `http://localhost:5044/swagger`.

## Ejecutar el frontend

En otra terminal, desde la raíz del repositorio:

```powershell
cd .\Frontend-MiPresupuesto
npm install
npm run dev
```

Abre `http://localhost:5173`. La dirección de la API se puede cambiar copiando `.env.example` a `.env` y modificando `VITE_API_URL`.

Para comprobar la compilación de producción:

```powershell
npm run build
```

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
- CRUD de presupuestos mensuales por categoría.
- Cálculo de gasto acumulado, porcentaje y restante por presupuesto.
- Alertas automáticas al 50 %, 80 %, 100 % y al exceder el límite.
- Reporte mensual de categorías con presupuesto excedido.
- Reporte mensual con totales, promedio, comparación con el mes anterior y top de categorías.
- Datos diarios y por categoría preparados para gráficas del dashboard.
- Exportación de reportes a JSON, TXT y Excel.
- Importación masiva de gastos desde Excel con validación y reporte por fila.
- Plantilla Excel descargable con instrucciones y formato de ejemplo.
- Interfaz React con registro, login, cierre de sesión y edición segura del perfil.
- Dashboard con tarjetas de resumen, gráfica diaria, distribución por categorías y estado de presupuestos.
- Pantallas completas para gastos, categorías, métodos de pago y presupuestos.
- Formularios con validaciones de la API, mensajes claros, filtros, paginación e importación de Excel.
- Descarga de reportes JSON, TXT y Excel desde el dashboard.
- Navegación adaptable a celular y carga diferida de cada pantalla.
- Aislamiento de todos los recursos por usuario autenticado.
- Eliminación protegida cuando una categoría o método de pago tiene registros asociados.
- Respuestas consistentes para errores de validación, conflictos y recursos inexistentes.

Los endpoints principales están disponibles en `/api/categories`, `/api/payment-methods`, `/api/expenses` y `/api/budgets`. Todos requieren el encabezado `Authorization: Bearer {token}`.

## Seguridad

Las contraseñas se almacenan usando PBKDF2-SHA256 con salt aleatorio y comparación en tiempo constante. La clave JWT incluida en `appsettings.json` es exclusivamente para desarrollo y deberá reemplazarse mediante configuración de entorno al publicar.
