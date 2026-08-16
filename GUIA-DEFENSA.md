# Guía de defensa — Mi Presupuesto

## 1. Presentación breve

Mi Presupuesto es una aplicación web para registrar, clasificar y analizar gastos personales. Está formada por un frontend React, una API REST ASP.NET Core y una base de datos SQL Server. Cada usuario accede únicamente a sus propios datos mediante autenticación JWT.

## 2. Arquitectura

El backend está separado en cuatro proyectos:

- `MiPresupuesto.Domain`: entidades y reglas centrales. No depende de infraestructura.
- `MiPresupuesto.Application`: DTOs, interfaces, validaciones y servicios con los casos de uso.
- `MiPresupuesto.Infrastructure`: Entity Framework Core, repositorios, SQL Server, hash de contraseñas y JWT.
- `API-MiPresupuesto`: controladores y configuración HTTP. Los controladores delegan la lógica a servicios.

Además contiene:

- `MiPresupuesto.Tests`: pruebas unitarias con xUnit.
- `Frontend-MiPresupuesto`: interfaz React con TypeScript, rutas protegidas, formularios, filtros y gráficas.
- `compose.yaml`: integra SQL Server, API y frontend en contenedores.

Flujo de una solicitud:

1. React envía una petición HTTP con el JWT.
2. El controlador recibe y valida el DTO.
3. El servicio de aplicación ejecuta el caso de uso.
4. El repositorio utiliza EF Core para consultar o modificar SQL Server.
5. La API devuelve un DTO o una respuesta de error consistente.

## 3. Demostración recomendada

### Preparación

Desde la raíz del proyecto:

```powershell
Copy-Item .\docker.env.example .\.env
docker compose up -d
docker compose ps
```

Abrir:

- Aplicación: `http://localhost:8081`
- Swagger: `http://localhost:8080/swagger`

### Orden de la demostración

1. Registrar un usuario e iniciar sesión.
2. Mostrar que las rutas internas requieren autenticación.
3. Crear una categoría y comprobar la validación de nombres duplicados.
4. Crear un método de pago.
5. Crear un presupuesto mensual para la categoría.
6. Registrar varios gastos y mostrar filtros, edición y paginación.
7. Mostrar el porcentaje consumido y las alertas del presupuesto.
8. Abrir el dashboard y explicar totales, comparación mensual y gráficas.
9. Descargar el reporte en JSON, TXT y Excel.
10. Descargar la plantilla Excel, importar gastos y mostrar el resultado por fila.
11. Intentar una operación inválida para enseñar los mensajes de error.
12. Iniciar sesión con una segunda cuenta y demostrar que no ve los datos de la primera.

## 4. Decisiones técnicas que se deben explicar

### JWT

Al iniciar sesión, la API genera un token firmado que identifica al usuario. React lo envía en el encabezado `Authorization: Bearer`. Los endpoints protegidos obtienen el identificador del usuario desde los claims; nunca aceptan un `UserId` enviado por el navegador.

### Contraseñas

No se guardan contraseñas en texto plano. Se utiliza PBKDF2-SHA256 con salt aleatorio y comparación en tiempo constante.

### DTOs

Las entidades de EF Core no se exponen al frontend. Los DTOs limitan los campos de entrada y salida, facilitan las validaciones y evitan modificar propiedades internas accidentalmente.

### Repositorios y servicios

Los repositorios encapsulan el acceso a datos. Los servicios contienen los casos de uso y reglas. Esto mantiene los controladores pequeños y permite probar la lógica sin depender directamente del protocolo HTTP.

### Inyección de dependencias

Las interfaces se registran en el contenedor de ASP.NET Core. Los controladores y servicios reciben sus dependencias por constructor, reduciendo el acoplamiento y facilitando las pruebas.

### Manejo de errores

La API utiliza validaciones de modelos y un middleware global. Los errores tienen código, mensaje, detalles por campo y `traceId`. El frontend interpreta esa respuesta y muestra mensajes comprensibles.

### Aislamiento por usuario

Todas las consultas filtran por el identificador del usuario autenticado. Aunque una persona conozca el ID de un gasto ajeno, no puede consultarlo ni modificarlo.

### Contenedores

Docker Compose crea tres servicios: `database`, `api` y `frontend`. SQL Server tiene una prueba de salud y un volumen persistente; la API espera a que la base esté saludable y aplica migraciones; Nginx sirve React y redirige `/api` al backend.

## 5. Preguntas técnicas frecuentes

**¿Por qué separar Domain, Application e Infrastructure?**  
Para mantener las reglas del negocio independientes de SQL Server, EF Core y HTTP. Las dependencias apuntan hacia el núcleo de la aplicación.

**¿Por qué usar interfaces?**  
Para que la lógica dependa de contratos y no de implementaciones concretas. Esto permite reemplazar repositorios y crear dobles de prueba.

**¿Qué diferencia existe entre autenticación y autorización?**  
La autenticación comprueba quién es el usuario; la autorización comprueba si puede acceder a un recurso.

**¿Por qué no existe un administrador?**  
El enunciado establece que solo existen usuarios normales. Cada cuenta administra exclusivamente sus propios recursos.

**¿Cómo evita gastos con datos incorrectos?**  
El backend valida monto positivo, fecha, categoría, método de pago y propiedad de los recursos. El frontend también valida para ofrecer respuesta inmediata, pero la API conserva la validación definitiva.

**¿Qué ocurre al importar un Excel?**  
La API analiza cada fila, valida los campos y referencias, inserta masivamente las filas correctas y devuelve un resumen con éxitos y errores por fila.

**¿Cómo se calculan los reportes?**  
Las consultas agrupan los gastos del usuario por período y categoría. El reporte calcula total, promedio, top de categorías y diferencia con el mes anterior. Los mismos datos alimentan las gráficas y exportaciones.

**¿Qué prueban las pruebas unitarias?**  
Prueban servicios, reglas, validaciones, presupuestos, reportes, autenticación e importación. Actualmente pasan 50 pruebas.

## 6. Comandos útiles

```powershell
docker compose up -d
docker compose ps
docker compose logs -f api
docker compose down
dotnet test .\API-MiPresupuesto\MiPresupuesto.Tests\MiPresupuesto.Tests.csproj
```

`docker compose down` conserva la base de datos. No utilizar `docker compose down --volumes` antes de la defensa porque elimina el volumen y sus datos.

## 7. Cierre sugerido

El proyecto cumple autenticación JWT, separación por capas, repositorios, DTOs, inyección de dependencias, EF Core, CRUDs, presupuestos, reportes, importación y exportación. El frontend consume datos reales de la API y toda la solución puede ejecutarse de forma reproducible con Docker Compose.
