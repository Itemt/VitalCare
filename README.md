# VitalCare

**VitalCare** es una aplicación diseñada para facilitar la gestión de citas médicas en el sistema de salud. Proporciona una plataforma intuitiva para que los usuarios programen, consulten y administren sus citas con facilidad.

## Características

- **Programación de citas**: Permite a los usuarios agendar nuevas citas con profesionales de la salud.
- **Gestión de citas**: Consulta y modificación de citas existentes.
- **Notificaciones**: Recordatorios automáticos para próximas citas.
- **Interfaz amigable**: Diseño intuitivo para una experiencia de usuario óptima.

## Tecnologías utilizadas

- **Backend**: ASP.NET Core
- **Frontend**: HTML, CSS, JavaScript
- **Base de datos**: SQL Server
- **Contenedores**: Docker

## Requisitos previos

Antes de instalar y ejecutar VitalCare, asegúrate de tener instalados:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

## Instalación

1. **Clonar el repositorio**:

   ```bash
   git clone https://github.com/Itemt/VitalCare.git
   ```

2. **Navegar al directorio del proyecto**:

   ```bash
   cd VitalCare
   ```

3. **Configurar la base de datos**:

   - Asegúrate de que SQL Server esté en funcionamiento.
   - Ejecuta el script `init.sql` para crear la base de datos necesaria:

     ```bash
     sqlcmd -S <tu_servidor> -U <tu_usuario> -P <tu_contraseña> -i init.sql
     ```

4. **Configurar las variables de entorno**:

   Crea un archivo `.env` en la raíz del proyecto con las siguientes variables:

   ```env
   DB_SERVER=<tu_servidor>
   DB_USER=<tu_usuario>
   DB_PASSWORD=<tu_contraseña>
   DB_NAME=VitalCareDB
   ```

5. **Construir y ejecutar la aplicación con Docker**:

   - **Construir la imagen**:

     ```bash
     docker-compose build
     ```

   - **Ejecutar los contenedores**:

     ```bash
     docker-compose up
     ```

   La aplicación estará disponible en `http://localhost:5000`.

## Uso

1. **Registro e inicio de sesión**: Crea una cuenta o inicia sesión con tus credenciales.
2. **Programar una cita**: Navega a la sección de citas y selecciona "Nueva cita". Completa los detalles requeridos y confirma.
3. **Ver y gestionar citas**: En la sección "Mis citas", puedes ver, modificar o cancelar tus citas programadas.

## Contribuciones

¡Las contribuciones son bienvenidas! Para contribuir:

1. Haz un fork del repositorio.
2. Crea una nueva rama (`git checkout -b feature/nueva-funcionalidad`).
3. Realiza tus cambios y haz commit (`git commit -m 'Añadir nueva funcionalidad'`).
4. Haz push a la rama (`git push origin feature/nueva-funcionalidad`).
5. Abre un Pull Request.

## Licencia

Este proyecto está bajo la licencia MIT.

---
