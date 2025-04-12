# VitalCare (CitasEPS)

Una aplicación web para gestionar citas médicas, construida con ASP.NET Core 8 y PostgreSQL.

## Tecnologías Utilizadas

*   **Framework:** ASP.NET Core 8 (Razor Pages)
*   **Base de Datos:** PostgreSQL
*   **ORM:** Entity Framework Core 8
*   **Autenticación:** ASP.NET Core Identity

## Prerrequisitos

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [PostgreSQL](https://www.postgresql.org/download/) (asegúrate de que el servidor esté en ejecución)

## Cómo Empezar

1.  **Clonar el repositorio:**
    ```bash
    git clone https://github.com/itemt/vitalcare.git
    cd vitalcare
    ```

2.  **Configurar la Conexión a la Base de Datos:**
    *   Abre el archivo `appsettings.json`.
    *   Localiza la sección `ConnectionStrings`.
    *   Actualiza la cadena `DefaultConnection` con los detalles de tu servidor PostgreSQL (Host, Puerto, Nombre de la Base de Datos, Usuario, Contraseña).
        ```json
        {
          // ... otras configuraciones
          "ConnectionStrings": {
            "DefaultConnection": "Host=localhost;Port=5432;Database=CitasEPSDb;Username=tu_usuario;Password=tu_contraseña"
          }
        }
        ```
    *   *Nota:* El nombre de base de datos predeterminado usado durante la configuración inicial fue `CitasEPSDb`.

3.  **Crear la Base de Datos (si no existe):**
    *   Conéctate a tu instancia de PostgreSQL (p.ej., usando `psql` o una herramienta gráfica).
    *   Ejecuta el comando: `CREATE DATABASE "CitasEPSDb";` (Reemplaza `CitasEPSDb` si usaste un nombre diferente en la cadena de conexión).

4.  **Aplicar las Migraciones de Base de Datos:**
    *   Abre una terminal en el directorio raíz del proyecto.
    *   Ejecuta el comando:
        ```bash
        dotnet ef database update
        ```
    *   Esto creará las tablas necesarias basadas en los modelos de Entity Framework Core.

5.  **Ejecutar la aplicación:**
    *   En la terminal, ejecuta:
        ```bash
        dotnet run
        ```
    *   La aplicación debería estar ahora en ejecución, típicamente en `https://localhost:xxxx` o `http://localhost:yyyy`.

## Características (Ejemplo)

*   Registro e Inicio de Sesión de Usuarios (Pacientes, Administradores)
*   Programación y Gestión de Citas
*   Gestión de Médicos y Especialidades
*   Panel de Administración

