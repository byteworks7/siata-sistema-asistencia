# SIATA — Sistema Integral de Asistencia Talma

> Sistema de control de asistencia desarrollado para Talma Servicios Aeroportuarios, con reconocimiento facial, panel de administración web y kiosco táctil de marcado.

---

## Stack Tecnológico

| Capa | Tecnología |
|------|------------|
| Backend | ASP.NET Core 8, C#, JWT, BCrypt, MailKit |
| Frontend | Angular 19 standalone, TailwindCSS |
| Kiosco | WinForms .NET 8, OpenCvSharp4 |
| Base de datos | SQL Server |

---

## Arquitectura
siata-sistema-asistencia/
├── SistemaAsistencia/          # API REST — ASP.NET Core 8
│   ├── Controllers/            # 7 controladores REST
│   ├── Models/                 # Entidades de BD
│   ├── Services/               # EmailService, CierreDiarioJob
│   └── Data/                   # AppDbContext (EF Core)
├── SistemaAsistencia.Desktop/  # Kiosco táctil — WinForms
│   ├── FaceService.cs          # Reconocimiento facial OpenCV
│   ├── FormSeleccion.cs        # Pantalla inicial ENTRADA/SALIDA
│   ├── FormMarcado2.cs         # Teclado táctil + verificación facial
│   └── FormOverlay.cs          # Tarjeta de confirmación animada
└── talma-asistencia/           # SPA — Angular 19
└── src/app/
├── admin/              # Dashboard, horarios, trabajadores, asistencias, amonestaciones
├── trabajador/         # Calendario personal, resumen del día
└── core/               # Guards JWT, interceptor, auth service

---

## Módulos Principales

### Panel de Administración (Angular)
- Dashboard con estadísticas del día en tiempo real
- Gestión de trabajadores con captura de foto desde cámara
- Configuración de horarios con ciclo rotativo (5x1, 2x2)
- Registro y corrección de asistencias por mes
- Módulo de amonestaciones con envío automático de correo HTML
- Calendario mensual con estados: Puntual, Tardanza, Falta, Vacaciones, Suspensión

### Kiosco de Marcado (WinForms)
- Teclado táctil para ingreso de DNI
- Reconocimiento facial con OpenCV (histograma normalizado)
- Detección automática de tipo de marcado (ENTRADA/SALIDA)
- Tarjeta de confirmación con barra de progreso animada
- Fallback a botones manuales si el facial no coincide

### Portal del Trabajador (Angular)
- Calendario mensual con colores por estado de asistencia
- Resumen del día actual
- Sesión por tab (sessionStorage) con verificación de token JWT

### API REST (ASP.NET Core 8)
- Autenticación JWT con roles (ADMIN / TRABAJADOR)
- 7 controladores: Auth, Trabajadores, Horarios, Calendario, Asistencias, Amonestaciones, Catálogos
- Job nocturno automático a las 23:59 (cierre de salidas y registro de faltas)
- Envío de correos vía Gmail SMTP con MailKit

---

## Instalación

### Requisitos
- .NET 8 SDK
- Node.js 18+
- SQL Server
- Visual Studio 2022

### Base de datos
```sql
-- Ejecutar en SQL Server Management Studio
database/schema.sql
```

### Backend
```bash
cd SistemaAsistencia
cp appsettings.example.json appsettings.json
# Completar cadena de conexión, JWT secret y credenciales de correo
dotnet run
# API disponible en http://localhost:5071
```

### Frontend
```bash
cd talma-asistencia
npm install
ng serve
# App disponible en http://localhost:4200
```

### Kiosco
Abrir SistemaAsistencia.sln en Visual Studio
Establecer SistemaAsistencia.Desktop como proyecto de inicio
Ejecutar con F5

---

## Flujos Principales
Trabajador llega → ingresa DNI en kiosco → reconocimiento facial
→ coincide ✅  → registra automáticamente
→ no coincide ❌ → botones manuales de confirmación
Admin → Amonestaciones → Nueva suspensión → backend marca días laborales
→ trabajador recibe correo HTML automático
→ calendario muestra días en rojo

---

## Credenciales de prueba

| Rol | Usuario | Contraseña |
|-----|---------|------------|
| Admin | admin | admin123 |
| Trabajador | DNI: 45678901 | — |

---

## Autor

**Bryan Soto** — Desarrollador Full Stack  
[GitHub](https://github.com/byteworks7)
