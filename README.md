# BitCore - Sistema de Información de Laboratorio (LIS)

BitCore es un backend modular y escalable desarrollado en **.NET 8** y **PostgreSQL** diseñado para la gestión integral de laboratorios clínicos. Implementa una arquitectura multi-tenant y un modelo de datos robusto enfocado en la trazabilidad clínica y operativa.

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

### Fase 4: Gestión de Caja y Pagos (Próximo Paso / Integración Adelantada)
- [ ] Módulo de transacciones financieras y cobros asociados a las órdenes de laboratorio.
- [ ] Registro de métodos de pago y control de estado financiero (pendiente / pagado / abonado) para condicionar el flujo clínico.

### Fase 5: Exámenes, Resultados y Reportes
- [ ] Asociación y filtrado del catálogo de exámenes (`ExamenesCatalogo`).
- [ ] Endpoints para la carga y actualización de resultados detallados con validación de rangos de referencia.
- [ ] Servicio de generación de reportes clínicos en formato PDF.
- [ ] Cobertura de pruebas automatizadas con `xUnit`.

---

## 🛠️ Stack Tecnológico
* **Backend:** .NET 8, C#, Web API, Entity Framework Core 8.0.11, JWT Bearer Authentication.
* **Base de Datos:** PostgreSQL 15+ ejecutándose en contenedores Docker (`BitCoreLab_DB`).
* **Documentación:** Swagger / OpenAPI.
* **Herramientas de Apoyo:** Adminer para administración visual de base de datos en desarrollo.