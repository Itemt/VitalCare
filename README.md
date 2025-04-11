# VitalCare (CitasEPS)

A web application for managing medical appointments, built with ASP.NET Core 8 and PostgreSQL.

## Technology Stack

*   **Framework:** ASP.NET Core 8 (Razor Pages)
*   **Database:** PostgreSQL
*   **ORM:** Entity Framework Core 8
*   **Authentication:** ASP.NET Core Identity

## Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [PostgreSQL](https://www.postgresql.org/download/) (ensure the server is running)

## Getting Started

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/itemt/vitalcare.git
    cd vitalcare
    ```

2.  **Configure Database Connection:**
    *   Open the `appsettings.json` file.
    *   Locate the `ConnectionStrings` section.
    *   Update the `DefaultConnection` string with your PostgreSQL server details (Host, Port, Database Name, Username, Password).
        ```json
        {
          // ... other settings
          "ConnectionStrings": {
            "DefaultConnection": "Host=localhost;Port=5432;Database=CitasEPSDb;Username=your_username;Password=your_password"
          }
        }
        ```
    *   *Note:* The default database name used during setup was `CitasEPSDb`.

3.  **Create Database (if it doesn't exist):**
    *   Connect to your PostgreSQL instance (e.g., using `psql` or a GUI tool).
    *   Run the command: `CREATE DATABASE "CitasEPSDb";` (Replace `CitasEPSDb` if you used a different name in the connection string).

4.  **Apply Database Migrations:**
    *   Open a terminal in the project root directory.
    *   Run the command:
        ```bash
        dotnet ef database update
        ```
    *   This will create the necessary tables based on the Entity Framework Core models.

5.  **Run the application:**
    *   In the terminal, run:
        ```bash
        dotnet run
        ```
    *   The application should now be running, typically at `https://localhost:xxxx` or `http://localhost:yyyy`.

## Features (Example)

*   User Registration and Login (Patients, Admins)
*   Appointment Scheduling and Management
*   Doctor and Specialty Management
*   Admin Dashboard

