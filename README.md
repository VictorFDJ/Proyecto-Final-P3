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
- SQL Server LocalDB para desarrollo sin contenedores.
- Node.js 22 o compatible y npm.

Para ejecutar la aplicación completa con contenedores solamente se necesita Docker Desktop con contenedores Linux y, como mínimo, 4 GB de memoria disponible.

## Ejecutar todo con Docker

Desde la raíz del repositorio, crea el archivo local de variables de entorno:

```powershell
Copy-Item .\docker.env.example .\.env
```

Abre `.env` y reemplaza los dos valores de ejemplo por contraseñas propias. La clave `JWT_KEY` debe tener al menos 32 caracteres. Después inicia SQL Server, la API y React con un solo comando:

```powershell
docker compose up --build
```

Cuando los servicios terminen de iniciar estarán disponibles en:

- Aplicación web: `http://localhost:8081`
- Swagger: `http://localhost:8080/swagger`
- Estado de la API: `http://localhost:8080/health`
- SQL Server desde SSMS: servidor `localhost,14330`, autenticación SQL Server, usuario `sa` y la contraseña `DB_PASSWORD` del archivo `.env`.

La API espera a que SQL Server esté disponible y aplica automáticamente las migraciones. Los datos quedan guardados en un volumen de Docker, por lo que no se pierden al detener los contenedores.

Esta configuración está preparada para la demostración y entrega local. Por eso muestra en pantalla el código temporal de recuperación. Antes de exponerla públicamente se debe conectar un servicio de correo, establecer `PasswordReset__ExposeToken=false`, usar secretos nuevos y habilitar HTTPS.

Para detenerlos:

```powershell
docker compose down
```

Para ver los mensajes de la API:

```powershell
docker compose logs -f api
```

Solo si deseas borrar por completo la base de datos creada por Docker, ejecuta `docker compose down --volumes`. Ese comando elimina permanentemente los datos del volumen.

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
- Recuperación de contraseña con código aleatorio de un solo uso y expiración de 15 minutos.
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

Las contraseñas se almacenan usando PBKDF2-SHA256 con salt aleatorio y comparación en tiempo constante. Los códigos de recuperación se guardan únicamente como hash, expiran después de 15 minutos y se invalidan al utilizarlos. En desarrollo y en la configuración Docker de demostración, la pantalla muestra el código para facilitar las pruebas locales; al publicar deberá conectarse un proveedor de correo y el código nunca se incluirá en la respuesta.

La clave JWT incluida en `appsettings.json` es exclusivamente para desarrollo y deberá reemplazarse mediante configuración de entorno al publicar.
