# BitCore - Sistema de Información de Laboratorio (LIS)

BitCore es un backend modular y escalable desarrollado en **.NET 8** y **PostgreSQL** diseñado para la gestión integral de laboratorios clínicos y centros de salud. Implementa una arquitectura multi-tenant y un modelo de datos robusto enfocado en la trazabilidad clínica, financiera y epidemiológica, preparado para entornos de alta resiliencia operativa y automatización externa.

---

## 🗺️ Bitácora de Ruta y Progreso (Roadmap)

### Fase 1: Infraestructura y Base de Datos (Completada)
- [x] Configuración inicial del proyecto .NET 8 (LTS) y limpieza de la estructura de archivos.
- [x] Instalación y fijación de versiones de Entity Framework Core (8.0.11) y Npgsql.
- [x] Diseño e implementación de modelos de dominio (`Tenant`, `Paciente`, `ExamenCatalogo`, `OrdenLaboratorio`, `ResultadoDetalle`, `Usuario`).
- [x] Configuración del `AppDbContext` con Fluent API y habilitación de la extensión `uuid-ossp` en PostgreSQL.
- [x] Despliegue y ejecución exitosa de migraciones iniciales en Docker (`BitCoreLab_DB`).

### Fase 2: Seguridad, Autenticación y Control de Accesos (Completada)
- [x] Implementación de autenticación basada en JSON Web Tokens (JWT) mediante `TokenService`.
- [x] Configuración de roles y políticas de autorización por políticas (`Administrator`, `Cajero`).
- [x] Endpoints protegidos y pruebas de rechazo por roles (`403 Forbidden`).

### Fase 3: Módulos Operativos y Máquina de Estados (Completada)
- [x] **`PacientesController`**: CRUD y búsqueda de pacientes asociados al `Tenant`.
- [x] **`OrdenesController`**: Registro de órdenes de laboratorio mediante identificadores GUID y correlativos diarios.
- [x] **Máquina de Estados**: Transiciones controladas para las órdenes de laboratorio (`Registrada` $\rightarrow$ `Procesada` $\rightarrow$ `Validada`) con validación de bloqueos y duplicidades.

### Fase 4: Catálogo de Exámenes, Consultas y Sincronización Masiva (Completada / Base Consolidada)
- [x] **`ExamenesController`**: Consolidación y unificación del manejo de catálogos y tarifas.
- [x] **Procesamiento de Hojas de Cálculo**: Integración de **EPPlus 8** (configuración de licencia no comercial) para la lectura automatizada de archivos Excel.
- [x] Endpoints de previsualización (`POST /api/examenes/previsualizar-excel`) y sincronización masiva (`POST /api/examenes/sincronizar-precios`) para actualizar precios de exámenes de laboratorio, consultas y honorarios profesionales.

### Fase 5: Módulo Financiero, Caja Unificada, Control Fiscal y Morbilidad (Próximo Paso)
- [ ] **Facturación Genérica Multi-rubro y Normativa SENIAT**: Estructura unificada para clasificar ítems por rubro (`Laboratorio`, `Consulta`, `Ecografía`) con campos dinámicos, número de control fiscal independiente, configuración inicial/ajuste de correlativos y transacciones de pago mixtas/multidivisa.
- [ ] **Arqueos y Cierres de Caja por Categoría**: Control de ingresos diarios, semanales o por turno desglosados estrictamente por categoría (Laboratorio, Ecografía, Consulta) y método de pago.
- [ ] **Reportes Operativos y Financieros**: Métricas de rotación de exámenes y consolidados de ingresos segmentados por rubro en rangos de fechas personalizados.
- [ ] **Módulo de Morbilidad y Epidemiología**: Segmentación demográfica avanzada con cálculo automático de edad, distinción estricta de menores de edad vs. adultos, y cruce con patologías y resultados alterados.

### Fase 6: Expansión, Citas, Automatización WhatsApp e Inventario (Próximas Fases)
- [ ] **Arquitectura Offline-First**: Soporte de continuidad operativa para apagones y fallas de red mediante Progressive Web Apps (PWA) y sincronización local por colas (`Outbox Pattern`).
- [ ] **Módulo de Citas y Automatización con n8n / WhatsApp**: Agendamiento electrónico y manual con confirmación y ajuste de horarios por especialistas, complementado con flujos automatizados en n8n para autogestión de pacientes por WhatsApp sin duplicidad de datos en la API.
- [ ] **Control de Inventario**: Trazabilidad de stock y consumo de reactivos e insumos clínicos.
- [ ] **Módulo de Soporte (Ticketing)**: Canal centralizado para la atención de incidencias técnicas y administrativas.

---

## 🛠️ Stack Tecnológico
* **Backend:** .NET 8, C#, Web API, Entity Framework Core 8.0.11, JWT Bearer Authentication, EPPlus 8.
* **Orquestación y Automatización:** n8n + WhatsApp Business API (Cloud API).
* **Base de Datos:** PostgreSQL 15+ ejecutándose en contenedores Docker (`BitCoreLab_DB`).
* **Documentación:** Swagger / OpenAPI.
* **Herramientas de Apoyo:** Adminer para administración visual de base de datos en desarrollo.