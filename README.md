# BitCore - Sistema de Información de Laboratorio (LIS)

BitCore es un backend modular y escalable desarrollado en **.NET 8** y **PostgreSQL** diseñado para la gestión integral de laboratorios clínicos. Implementa una arquitectura multi-tenant y un modelo de datos robusto enfocado en la trazabilidad clínica y operativa.

---

## 🗺️ Bitácora de Ruta y Progreso (Roadmap)

Usa esta lista de verificación para seguir el desarrollo paso a paso. Puedes marcar los elementos con una `[x]` a medida que los vayamos completando.

### Fase 1: Infraestructura y Base de Datos (Completada)
- [x] Configuración inicial del proyecto .NET 8 (LTS) y limpieza de la estructura de archivos.
- [x] Instalación y fijación de versiones estáneas de Entity Framework Core (8.0.11) y Npgsql.
- [x] Diseño e implementación de modelos de dominio:
  - [x] `Tenant` (Gestión multi-clínica / empresas)
  - [x] `Paciente` (Ficha clínica y datos demográficos)
  - [x] `ExamenCatalogo` (Catálogo de parámetros y pruebas)
  - [x] `OrdenLaboratorio` (Órdenes de trabajo y correlativos diarios)
  - [x] `ResultadoDetalle` (Resultados por parámetro, rangos y trazabilidad)
- [x] Configuración del `AppDbContext` con Fluent API y habilitación de la extensión `uuid-ossp` en PostgreSQL.
- [x] Despliegue y ejecución exitosa de migraciones iniciales en Docker (`BitCoreLab_DB`).
- [x] Verificación de la base de datos mediante Adminer.

### Fase 2: Capa de Controladores y Endpoints Base (Próximo paso)
- [x] Creación de la estructura de carpetas para Controladores (`Controllers`).
- [ ] Desarrollo del CRUD para la gestión de laboratorios/tenants (`TenantsController`).
- [ ] Desarrollo del módulo de gestión y búsqueda de pacientes (`PacientesController`).
- [ ] Configuración y pruebas iniciales de los endpoints mediante Swagger.

### Fase 3: Lógica Operativa del Laboratorio
- [ ] Implementación de la lógica para el registro de órdenes de laboratorio (`OrdenesController`).
- [ ] Generación automática de correlativos diarios por tenant.
- [ ] Asociación y filtrado del catálogo de exámenes (`ExamenesCatalogo`).

### Fase 4: Gestión de Resultados y Trazabilidad
- [ ] Desarrollo de endpoints para la carga y actualización de resultados detallados (`ResultadosController`).
- [ ] Validación de rangos de referencia y alertas de valores fuera de rango.
- [ ] Control de estados tercerizados y trazabilidad por UUID de bioanalista.

---

## 🛠️ Stack Tecnológico
* **Backend:** .NET 8, C#, Web API, Entity Framework Core 8.0.11.
* **Base de Datos:** PostgreSQL 15+ ejecutándose en contenedores Docker.
* **Documentación:** Swagger / OpenAPI.
* **Herramientas de Apoyo:** Adminer para administración visual de base de datos en desarrollo.

## Bitácora de Ruta - LIS BitCore

### Fase 1: Arquitectura Base e Infraestructura (Completada)
- Configuración de entorno con .NET 8 y PostgreSQL sobre Docker.
- Estructuración del modelo Multi-tenant (`Tenant` como entidad raíz).
- Configuración de Entity Framework Core y migraciones iniciales.
- Documentación e interfaz de pruebas activas mediante Swagger.

### Fase 2: Módulos y Controladores Principales (En Progreso / Avanzado)
- **`TenantsController`**: CRUD funcional para la gestión y registro de laboratorios/clínicas (con validación de RIF y estado activo).
- **`PacientesController`**: Endpoint de registro de pacientes asociado a su respectivo `Tenant`, con soporte para cédula y datos demográficos base (campo de historia física flexible temporalmente).
- **`ExamenesController`**: Catálogo de exámenes médicos configurables por cada laboratorio (categorías, parámetros, unidades y referencias).

### Próximos Pasos
- Definición de la lógica de negocio final para el número de historia física del paciente tras confirmación con el personal del laboratorio.
- Desarrollo de transacciones para Órdenes de Exámenes y asociación de resultados.