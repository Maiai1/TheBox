# TheBox

Gestión de asegurados de The Box Seguros: portal web para consultar pólizas, cuotas, pagos y siniestros, solicitar presupuestos y administrar la contraseña del asegurado.

## Stack

- **Backend:** ASP.NET Core 8 (minimal APIs), Entity Framework Core + SQL Server (LocalDB), autenticación JWT y BCrypt.
- **Frontend:** HTML, CSS y JavaScript sin framework, servido por la propia API.

## Funcionalidades

- Gestión de pólizas, cuotas, pagos (pasarela simulada), comprobantes y siniestros.
- Solicitud de cambio de fecha de vencimiento de cuotas impagas no vencidas: se permite hasta 15 días corridos después del vencimiento original, se actualiza el cronograma y se registra una notificación interna. Las pólizas con suspensión por impagos no son habilitadas.
- Descarga de documentación de pólizas en PDF (frente de póliza, tarjeta de circulación digital y comprobante de vigencia), individual o en un ZIP. Los PDF se guardan en el dispositivo y se pueden consultar sin conexión desde la PWA.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server Express con LocalDB (la cadena de conexión por defecto usa `(localdb)\MSSQLLocalDB`)

## Ejecución

Desde la raíz del repositorio:

```bash
dotnet run
```

La API y el frontend quedan disponibles en **http://localhost:5080**. La documentación Swagger está en **http://localhost:5080/swagger**.

> En entorno `Development` la base se elimina y se vuelve a crear en cada arranque (`DbSeeder`), con datos de prueba. No ejecutar así en producción.

## Usuarios de prueba

| DNI | Contraseña | Observaciones |
| --- | --- | --- |
| `38450123` | `12345678901` | Debe cambiar la contraseña al iniciar sesión |
| `27999888` | `Maria2026` | Acceso completo |
| `30555444` | `Carlos2026` | Usuario inactivo |

## Configuración

`appsettings.json` contiene la cadena de conexión y la configuración JWT. La clave `Jwt:Key` es un valor de ejemplo: reemplazala por una clave secreta de al menos 32 caracteres antes de publicar la aplicación. Para un despliegue real también conviene limitar CORS (actualmente permite cualquier origen para desarrollo) y usar un entorno que no sea `Development`.

## Estructura

- `Api/Domain`: entidades del modelo.
- `Api/Data`: contexto de EF Core y sembrado de datos de prueba.
- `Api/Dtos`: contratos de entrada/salida.
- `Api/Endpoints`: rutas de la API.
- `Api/Services`: lógica de negocio (autenticación, pagos, pólizas, siniestros, presupuestos).
- `TheBox Frontend`: páginas HTML del portal.
- `App_Data`: archivos subidos con las solicitudes de presupuesto.
