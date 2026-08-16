# Despliegue público — Mi Presupuesto

Esta guía publica React, ASP.NET Core y SQL Server bajo un mismo sitio HTTPS en MonsterASP.NET. Está orientada a la demostración académica y uso personal con el plan gratuito.

## 1. Crear los recursos

1. Crear una cuenta en `https://www.monsterasp.net/`.
2. En el panel, crear un sitio web gratuito y seleccionar .NET 10.
3. Elegir el centro de datos de Estados Unidos si está disponible.
4. Crear una base de datos MSSQL desde el panel.
5. Guardar la cadena de conexión mostrada por el panel. No publicarla en GitHub ni compartirla.

## 2. Configurar secretos

En el panel: **Websites → Manage website → Scripting → Environment Variables**.

Agregar las siguientes variables:

| Variable | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión MSSQL entregada por el panel |
| `Jwt__Key` | Clave aleatoria de al menos 32 caracteres |
| `Jwt__Issuer` | `MiPresupuesto.Api` |
| `Jwt__Audience` | `MiPresupuesto.Client` |
| `Jwt__ExpirationMinutes` | `120` |
| `Database__MigrateOnStartup` | `true` |
| `Swagger__Enabled` | `false` |
| `HttpsRedirection__Enabled` | `true` |
| `PasswordReset__ExposeToken` | `false` |

Se puede generar una clave JWT segura en PowerShell con:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

Copiar el resultado directamente al panel y no guardarlo en archivos del repositorio.

## 3. Descargar el perfil Web Deploy

1. Abrir el sitio creado en el panel.
2. Activar la cuenta de **Web Deploy**.
3. Descargar el archivo de perfil `.publishSettings`.
4. Tratar ese archivo como una contraseña: no subirlo a GitHub ni incluirlo en la entrega.

## 4. Publicar desde Visual Studio

1. Abrir la solución `API-MiPresupuesto/API-MiPresupuesto.slnx`.
2. En el explorador de soluciones, hacer clic derecho sobre el proyecto `API-MiPresupuesto`.
3. Seleccionar **Publish / Publicar**.
4. Elegir **Import Profile / Importar perfil**.
5. Seleccionar el `.publishSettings` descargado.
6. Usar configuración `Release` y destino `net10.0`.
7. Presionar **Publish / Publicar**.

Durante la publicación, el proyecto ejecuta `npm ci`, compila React con `/api` como dirección del backend y lo copia dentro del paquete ASP.NET Core. La API aplica las migraciones pendientes al iniciar.

## 5. Verificar

Sustituir `https://tu-sitio.runasp.net` por el dominio entregado:

- Aplicación: `https://tu-sitio.runasp.net/`
- Estado de la API: `https://tu-sitio.runasp.net/health`

Comprobar este flujo:

1. Registrar una cuenta nueva.
2. Iniciar sesión.
3. Crear categoría y método de pago.
4. Crear presupuesto y gasto.
5. Abrir dashboard y descargar un reporte.
6. Cerrar sesión y volver a entrar.

## 6. Seguridad

- El sitio público no muestra códigos de recuperación porque todavía no existe un proveedor de correo configurado.
- Los secretos se almacenan únicamente como variables del hosting.
- HTTPS debe permanecer habilitado.
- Para una aplicación comercial se debe utilizar un plan con garantías de disponibilidad, dominio propio, monitoreo y correo transaccional.
