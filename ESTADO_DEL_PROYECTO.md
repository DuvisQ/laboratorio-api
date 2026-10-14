# 📋 BitCore LIS - Bitácora de Progreso y Estado del Proyecto
**Organización:** Sociedad Anticancerosa de La Victoria  
**Última actualización:** 17 de Septiembre de 2026

---

## 🎯 Objetivo del Sistema
Modernizar y digitalizar la gestión integral del laboratorio clínico y citas médicas de la Sociedad Anticancerosa de La Victoria, sustituyendo procesos manuales por un sistema multi-tenant, robusto, seguro, trazable y con visión de Historia Clínica Digital (HCD).

---

## ✅ Lo que se ha Completado Hasta Hoy

### 1. Desbloqueo y Estabilidad del Frontend (Vue 3 + Vite)
* Se resolvió el error bloqueante en rojo de Vite en `PacientesView.vue`.
* Se creó el componente `PacienteModal.vue` completamente funcional para registro y edición.
* Sincronización del contrato de autenticación: la barra superior muestra el nombre real del usuario logueado (`👤 Hola, Administrador`).
* Normalización de clases CSS para compatibilidad completa con el compilador de Vite (construcción en 723ms con 0 errores).

### 2. Blindaje Arquitectónico del Backend (.NET 8 + EF Core)
* **Aislamiento Multi-Tenant Automático:** Implementación de `ITenantService`, `TenantService` e inyección de `HasQueryFilter` en `AppDbContext.cs`. Ninguna clínica o especialista privado puede filtrar ni consultar datos de otro por diseño.
* **Auto-asignación de Tenant:** En `SaveChangesAsync`, si una entidad nueva no trae `TenantId`, se le asigna automáticamente el del token.
* **Limpieza de DbSet Duplicado:** Eliminado el duplicado de catálogos en el `DbContext`.
* **Centralización de Roles:** Clase estática `Security/Roles.cs`.

### 3. Normalización y Reglas Clínicas de Pacientes
* **Norma de Mayúsculas:** Todo el formulario del paciente se visualiza y almacena en mayúsculas sostenidas (`.ToUpper().Trim()`), garantizando uniformidad en la base de datos.
* **Cédula con Prefijo:** Selector de tipo de documento `[ V- | E- | P- ]`.
* **Lógica de Menores (LOPNNA y SENIAT):**
  * Cálculo dinámico de edad a partir de la fecha de nacimiento.
  * **Menor sin cédula (< 9 años):** Generación automática de identificación clínica con cédula del representante + correlativo de hijo (ej: `V12345678-2`).
  * **Menor cedulado (9 a 17 años):** Permite cédula propia, pero exige datos del representante legal para emisión de facturas fiscales.
  * **Mayor de edad (18+):** Representante opcional.
* **Edición de Pacientes (`PUT`):** Endpoint `PUT /api/pacientes/{id}` y botón *"📄 Ver Ficha"* activo para actualizar datos o migrar cédulas provisionales.

### 4. Limpieza y Depuración de la Base de Datos (PostgreSQL en Docker)
* Depurados 919 pacientes reales importados del archivo histórico.
* Corregidos 155 registros con columnas invertidas y eliminadas 6 filas basura de pestañas de prueba.
* **Estado actual:** 919 pacientes limpios, de los cuales **761 tienen su número de historia médica intacto**.

### 5. Buscador Clínico Inteligente y Protección de Privacidad (Search-First)
* **Privacidad Médica Garantizada:** La pantalla de Pacientes ya no expone la lista de 919 personas al abrir. Inicia en un estado limpio que protege la confidencialidad visual de mostrador.
* **Buscador Inteligente Omnibox en Tiempo Real:** 
  * Busca automáticamente a partir de la **3ra letra o número** con **Debounce de 300ms**.
  * Coincidencia multivariable simultánea:
    * Cédula con o sin formato (`23526933`, `V-23.526.933`).
    * N° de Historia Médica (`0125-25`, `1412-16` o sin guiones `012525`).
    * Nombre o apellido (`Delgado`, `Omaira`).
    * Ubicación o especialidad física (`Ginecología`, `Urología`, `Archivo en Tránsito`).
* **Acceso Rápido Opcional:** Botón *"⏱️ Ver registros recientes del día"* para consultar a las últimas 15 personas atendidas.
* **Sincronización en Git:** Código de Backend y Frontend subido a las ramas `main` en GitHub.

### 6. Auditoría, Blindaje y Ecosistema QR del Módulo de Inventario (`api-inventario` y `frontend-inventario`)
* **Blindaje y Seguridad del Backend (Node/Express + PostgreSQL):**
  * **Escudo Térmico y Cabeceras HTTP:** Reordenados `helmet()` y `rateLimit()` antes de montar las rutas para proteger la API contra ataques de fuerza bruta y asegurar cabeceras estrictas.
  * **Cierre de Brecha IDOR:** Eliminado el endpoint desprotegido `GET /api/activos/:tenant_id` que exponía inventarios sin autenticación.
  * **Aislamiento Multi-Tenant:** `POST /api/movimientos` ahora extrae obligatoriamente el `tenant_id` del token JWT verificado (`req.usuario.tenant_id`).
  * **Limpieza de Esquema Roto:** Eliminadas rutas en `index.js` que intentaban insertar en columnas inexistentes (`codigo_void`, `metadata_perifericos`).
  * **Normalización SQL:** Consultas unificadas con `UPPER(estado)` para resolver discrepancias entre `'Activo'` y `'ACTIVO'`.
  * **Nuevos Endpoints QR:** Incorporados `GET /api/activos/qr/:qr_code` (consulta y búsqueda instantánea por código VOID) y `GET /api/activos/:id`.
* **Restauración y Limpieza del Frontend (Vue 3 + Pinia + Vite):**
  * **Archivo Histórico de Bajas (`BajasView.vue`):** Reconstruida la vista sustituyendo los gráficos duplicados por la tabla real de activos retirados con buscador dedicado, badge `RETIRADO / BAJA`, motivo de desincorporación y exportación a PDF y Excel.
  * **Variables de Entorno (`.env`):** Desacopladas las URLs fijas mediante `VITE_API_URL=http://localhost:3000`.
  * **Identidad de Usuario en Navbar (`MainLayout.vue`):** Insignia de usuario con nombre y rol activo (`👤 Nombre (ROL)`).
  * **Inmutabilidad de Precintos y Modal de Baja (`InventarioView.vue`):** Bloqueo visual y lógico del código VOID al reubicar (`🔒 Código Inmutable`) y modal interactivo para ingresar el motivo de baja.
* **Ecosistema Dual de Trazabilidad Total (QR 2D + Código de Barras 1D Code 128):**
  * **Plantilla e Impresión de Stickers Físicos (`QrEtiquetaModal.vue`):** Generador dual de QR gráfico (`qrcode`) y Código de Barras 1D (`jsbarcode`) con alternancia en tiempo real. Visualizador de tarjeta adhesiva térmica (50x30mm o 60x40mm) con membrete institucional dinámico del tenant y botón `window.print()` optimizado con `@media print`.
  * **Agnóstico al Formato Físico:** Soporta tanto etiquetas personalizadas impresas como precintos comerciales genéricos prenumerados (rollos adquiridos en Mercado Libre) y pistolas lectoras USB estándar.
  * **Escáner Óptico por Cámara Web / Smartphone (`QrScannerModal.vue`):** Integrado con `html5-qrcode` para lectura híbrida (2D QR y 1D Barcodes), captura en formulario y modo "📷 Auditar" con retroalimentación acústica (*beep* sintético).
  * **Miniaturas QR en Reportes PDF:** Al exportar a PDF, se incrustan códigos QR escaneables en la primera columna junto a cada activo físico.
  * **Pliego de Precintos Ciegos Imprimibles en Hoja Carta / A4 (`PliegoEtiquetasModal.vue`):** Diseño de **Precinto Ciego Minimalista** (sin datos de equipo expuestos en pasillos; solo membrete, código óptico centrado, identificador visible y leyenda de seguridad). Cuadrícula de 3 columnas compactas con guías de corte (`✂️`) que permite imprimir de 18 a 24 precintos por hoja blanca para pruebas de campo inmediatas.
  * **Autocorrelatividad Inteligente y Normalización Preventiva:** Endpoint `GET /api/activos/correlativo/siguiente` que sugiere el siguiente folio disponible (`VD-006` o `BAR-MED-1004`) y botón interactivo `⚡` en el formulario para autocompletar con 1 clic. Si se introducen dígitos huérfanos (ej. `6`), el sistema antepone automáticamente el prefijo institucional.
* **Gestión y Recuperación de Contraseñas:**
  * **Auto-servicio en Login:** Enlace *"¿Olvidaste tu clave?"* y endpoint `POST /api/usuarios/recuperar-password` para restablecer contraseña validando correo y encriptando con `bcrypt`.
  * **Cambio en Sesión:** Botón *"🔑 Clave"* en cabecera y endpoint `POST /api/usuarios/cambiar-password` con verificación de contraseña actual.
* **Demostración Multi-Tenant Dual (MVP / Producción Listo):**
  * **Cliente A (`BitCore Principal`):** Usuario `duvistest2024@gmail.com` con 5 activos informáticos normalizados (`VD-001` a `VD-005`, `VD-999`) y stickers predeterminados en QR.
  * **Cliente B (`Clínica Principal`):** Usuario `clinica_admin@bitcore.com` con 3 equipos médicos (`BAR-MED-1001`, `BAR-MED-1002`, `BAR-MED-1003`) y stickers predeterminados en Código de Barras 1D.
  * **Aislamiento Criptográfico:** Cada JWT garantiza estricta partición en consultas de base de datos (`404` si un cliente intenta consultar activos del otro) y unicidad relacional `UNIQUE(tenant_id, qr_code)`.
  * **Estrategia de Ramas Git:** Creada y publicada la rama `mvp` en ambos repositorios (`api-inventario-bitcore` y `frontend-inventario-bitcore`) reservada para el despliegue en la nube (Vercel + Render + Neon) sin alterar la rama `main`.

### 7. Recuperación y Cambio de Contraseña en BitCore LIS
* **Autoservicio 100% Autónomo (LAN / Offline-First):**
  * **Backend (.NET 8):** Creado el DTO `RecuperarPasswordDto` y el endpoint público `POST /api/Auth/recuperar-password`. Valida la identidad del usuario por correo o nombre de usuario, verifica su `PinAutorizacion` hasheado mediante `BCrypt` y actualiza la contraseña de forma segura.
  * **Acceso Dual en Login:** Mejorado el endpoint `POST /api/Auth/login` para permitir iniciar sesión indistintamente con el correo electrónico o con el nombre de usuario.
  * **Frontend (Vue 3 + Pinia):** Enlace interactivo *"¿Olvidaste tu contraseña?"* en `LoginView.vue`, tarjeta de restablecimiento integrada con validación de PIN, confirmación de clave y retroalimentación visual inmediata. Acción `recuperarPassword` conectada en `authStore.js` con soporte HMR y fallback de llamada directa.
  * **Estado y Sincronización:** Probado y validado en vivo con éxito. Código respaldado y subido a GitHub en las ramas `main` de ambos repositorios (`laboratorio-api` y `laboratorio-frontend`).

---

## 🧭 Alineación de Visión y Arquitectura Futura

1. **Estrategia de Códigos QR (Puente Físico-Digital):**
   * **QR Paciente:** Identificación inmediata en recepción para cargar el expediente sin teclear.
   * **QR Tubos / Muestras:** Trazabilidad de laboratorio. Agrupación por tipo de muestra (`Suero`, `Sangre Total`, `Orina`) para no imprimir etiquetas repetidas para pruebas que van en el mismo tubo.
   * **QR Validación de Resultados:** Código en el informe PDF que redirige a portal web seguro para validar autenticidad médica (protegiendo datos sensibles con PIN de consulta).
   * **QR Activos / Equipos:** Control de inventario con etiquetas VOID de alta seguridad.
2. **Arquitectura Multi-Tenant:**
   * Soporta tanto clínicas grandes (Sociedad Anticancerosa con múltiples áreas) como consultorios privados de especialistas independientes en una sola plataforma SaaS.
3. **Resiliencia Operativa (Offline-First para Venezuela):**
   * Base de datos ya preparada con **UUIDs** para evitar colisiones de IDs durante sincronización.
   * Estrategia híbrida: Servidor local en LAN para operar sin internet durante apagones, complementado con PWA / IndexedDB (Outbox Pattern) en el frontend.

---

## 📌 Dónde Quedamos y Agenda para la Próxima Sesión

### 🏆 Lo cerrado y blindado hoy:
1. **🎨 Selección e Integración Definitiva del Branding Institucional:**
   * **Opción 2 Aprobada e Implementada:** Isotipo oficial (cangrejo rojo con espada azul) + tipografía institucional nítida en el navbar superior (*Sociedad Anticancerosa de La Victoria / Clínica Dr. Rubén Merenfeld • RIF J-30426645-6*).
   * **Login Optimizado:** Emblema circular oficial sin márgenes sobrantes, ampliado a 90px y con datos oficiales de la clínica y RIF.
   * **Activos Médicos Digitalizados Guardados en `src/assets/`:**
     * `logo-isotipo.png` (en uso en navbar y listo para etiquetas térmicas de tubos).
     * `logo-horizontal.png` (para membretes anchos).
     * `logo-circular.png` (en uso en login).
     * `sello-bioanalista.png` (firma y sello húmedo del Lcdo. Ismael Peroza con fondo transparente listo para informes PDF).
     * `sello-institucional.png` (sello redondo de la Sociedad Anticancerosa listo con fondo transparente).

2. **👥 Módulo de Gestión de Personal y Roles Clínicos (Completado y Desplegado):**
   * **Backend (.NET 8):**
     * Modelo `Usuario.cs` enriquecido con `Cedula`, `Mpps`, `ColegioBioanalistas`, `CodigoRecuperacion`, `FechaExpiracionCodigo`.
     * Migración EF Core `20261011170019_AgregarCamposProfesionalesYRecuperacionUsuario` aplicada a PostgreSQL en Docker.
     * Controlador `UsuariosController.cs` protegido con `[Authorize(Roles = Roles.Administrador)]` con endpoints CRUD, activación/desactivación y reseteo maestro de claves.
   * **Frontend (Vue 3 + Vite):**
     * `usuarioStore.js` con Pinia para operaciones reactivas.
     * `UsuariosView.vue` con diseño institucional, métricas de personal, buscador reactivo, filtros por rol y estado.
     * `UsuarioModal.vue` para alta/edición de colaboradores con sección dinámica sanitaria para Bioanalistas (MPPS y Colegio).
     * `ResetCredencialesModal.vue` para restablecimiento seguro de contraseñas y PIN por parte del Administrador.
     * Enlace en `MainLayout.vue` exclusivo para Administradores (`⚙️ Personal y Roles`) y píldora de rol en cabecera.
     * Guardia de navegación global en `router/index.js` restringiendo `/usuarios` a usuarios con rol `Administrador`.
   * **Personal Clínico Inicial Registrado:**
     * **Administrador:** `admin@bitcore.com` (Control Total).
     * **Lcdo. Ismael E. Peroza E. (Bioanalista Jefe):** `duvistest2024@gmail.com` (C.I. `V-20.327.384`, MPPS `17879`, CB `110-962-17879`, PIN `1234`).
     * **Ana Morales (Secretaria / Recepción):** `amorales@anticancerosalavictoria.org` (C.I. `V-24.891.203`, PIN `2345`).
     * **Carlos Gómez (Cajero / Cobranza):** `cgomez@anticancerosalavictoria.org` (C.I. `V-26.114.789`, PIN `3456`).
   * **Control de Acceso Basado en Roles (RBAC) Validado:** El acceso a la API y vista de gestión rechaza solicitudes de no administradores con `403 Forbidden`.

3. **🔐 Trilogía de Seguridad y Recuperación (Nivel 2 - OTP por Correo Electrónico Completo):**
   * **Backend (.NET 8 + MailKit):**
     * Servicio `IEmailService` y `EmailService` con soporte para envío SMTP de correos HTML con membrete institucional de la Sociedad Anticancerosa.
     * Endpoints públicos en `AuthController`:
       * `POST /api/Auth/solicitar-codigo-otp`: Genera código criptográfico de 6 dígitos con validez de 15 minutos, lo despacha vía email, oculta la dirección con máscara (`i*****a@...`) y emite log legible en terminal Kestrel.
       * `POST /api/Auth/recuperar-con-otp`: Valida el código, renueva la contraseña (BCrypt), permite redefinir el PIN opcionalmente y consume el token temporal.
   * **Frontend (Vue 3):**
     * Selector de método en pantalla de recuperación (`🔢 Con PIN` o `📧 Por Correo`).
     * Enlace directo en el campo PIN: *«¿No recuerdas tu PIN? Solicitar código al correo ➔»*.
     * Flujo de dos fases: Solicitud inicial y formulario de verificación con banner de modo desarrollo (`codigoDev`) para agilizar pruebas locales.
   * **Sincronización en Git:** Rama `main` actualizada en ambos repositorios (`7833ffc` en API, `8eee389` en Frontend).

---

4. **👤 Autogestión de Perfil y Credenciales («Mi Perfil» Modal Global):**
   * **Backend (.NET 8):**
     * DTOs `PerfilUsuarioDto`, `CambiarPasswordDto`, `CambiarPinDto`, `ActualizarPerfilDto`.
     * Endpoints bajo `[Authorize]` en `AuthController.cs`:
       * `GET /api/Auth/perfil`: Ficha institucional completa del usuario actual.
       * `PUT /api/Auth/cambiar-password`: Valida contraseña actual con BCrypt y actualiza la clave.
       * `PUT /api/Auth/cambiar-pin`: Valida contraseña actual y actualiza PIN de 4-6 dígitos.
       * `PUT /api/Auth/actualizar-datos`: Actualiza Cédula y credenciales sanitarias (MPPS / CB).
   * **Frontend (Vue 3 + Vite):**
     * Botón interactivo de perfil en `MainLayout.vue` con avatar, nombre, rol y engranaje animado.
     * Modal `PerfilModal.vue` con ficha de identidad, vista de sello digital y 3 pestañas dinámicas (Cambiar Contraseña, PIN de Autorización, Datos Sanitarios).
     * Integración reactiva en `authStore.js`.
   * **Sincronización en Git:** Commits subidos a ramas `main` (`fe71e05` en API, `ccfef3b` en Frontend).

---

5. **🧪 Catálogo de Exámenes, Perfiles y Tarifas Clínicas (Completado y Blindado - Paso 4):**
   * **Decisión Clínica y Administrativa Respetada:**
     * **Exámenes de Rutina Internos (SALV):** Procesados localmente por el Lcdo. Ismael Peroza bajo nomenclatura institucional propia (`SALV-00001` a `SALV-00012`), con precios locales protegidos contra sobreescritura externa.
     * **Pruebas Especiales / Tercerizadas:** Cargadas y sincronizadas desde listados de referencia externos (`precios_lab.xlsx`), marcadas con laboratorio de destino y código de proveedor.
   * **Base Clínica Fidedigna Extraída de los Formatos Word Reales (`Formatos_Laboratorio`):**
     * `SALV-00001`: Perfil 20 Completo (20 analitos con rangos de referencia desglosados para H y M).
     * `SALV-00002`: Hematología Completa (Fórmula roja, blanca, plaquetas y examen diferencial).
     * `SALV-00003`: Química Sanguínea Básica (Glicemia, Urea, Creatinina, Ácido Úrico).
     * `SALV-00004`: Examen General de Orina y Heces (Físico, químico y microscópico completo).
     * `SALV-00005`: Tiempos de Coagulación y Serología Básica (PT, PTT, HIV, VDRL).
     * `SALV-00006`: Sangre Oculta en Heces.
     * `SALV-00007`: Grupo Sanguíneo y Factor Rh (Tipiaje).
     * `SALV-00008`: Serología Infecciosa (Proteína C Reactiva, VDRL, Dengue, Monotest).
     * `SALV-00009`: Descarte de Dengue (NS1, IgM, IgG).
     * `SALV-00010`: Reacciones Febriles (Widal / Aglutinaciones Febriles).
     * `SALV-00011`: Depuración de Creatinina en Orina de 24 Horas.
     * `SALV-00012`: Prueba de Embarazo en Sangre (HCG Cualitativa).
   * **Backend (.NET 8 + EF Core):**
     * DTOs completos en `Dtos/Examenes/` (`ExamenDto`, `CrearExamenDto`, `ActualizarExamenDto`, `ExamenParametroDto`).
     * `ExamenesController.cs`:
       * `GET /api/Examenes`: Filtros en tiempo real por búsqueda omnibox, categoría, tipo (`rutina`/`especial`) y estado.
       * `GET /api/Examenes/siguiente-codigo`: Generador correlativo inteligente (`SALV-00013`, etc.).
       * `POST /api/Examenes` y `PUT /api/Examenes/{id}`: Gestión completa de pruebas y parámetros clínicos/analitos del Bioanalista.
       * `POST /api/Examenes/seed-rutina-salv`: Sembrado institucional con 1 clic de los 12 exámenes base con todos sus parámetros y unidades de referencia.
       * `POST /api/Examenes/previsualizar-excel` y `POST /api/Examenes/sincronizar-precios`: Motor de importación masiva con EPPlus blindado contra sobreescritura de pruebas `SALV-`.
   * **Frontend (Vue 3 + Vite + Pinia):**
     * `catalogoStore.js`: Store reactivo para gestión de catálogo, filtrado, carga masiva y parámetros.
     * `CatalogosView.vue`: Vista integral con diseño institucional azul/rojo, KPIs de catálogo (Total, Rutina SALV, Especiales, Activos), pestañas de filtro rápido, selector de categorías, buscador omnibox y tabla con badges y acciones.
     * `ExamenModal.vue`: Modal con pestañas duales ("Datos Generales y Tarifa" y "Parámetros y Valores de Referencia") con botón generador de correlativo `SALV-`.
     * `ParametrosModal.vue`: Visor rápido de analitos y rangos de referencia para el Bioanalista.
     * `ImportarExcelModal.vue`: Zona Drag & Drop para actualizar tarifas desde Excel con alertas de seguridad preventiva.
    * **Sincronización y Validación Masiva en Vivo (Pruebas Especiales):**
      * **Corrección de Estructura Excel:** Identificado y resuelto el desfase de columnas del archivo `precios_lab.xlsx` (Columna A en blanco, Columna B para Nombre y Columna C para Precio). El parser ahora detecta automáticamente cualquier cabecera desplazada y asigna categorías especializadas por palabra clave.
      * **Resultado de Carga Real:** **747 Exámenes Activos en Total**:
        * **12 Exámenes de Rutina Interna SALV:** Extraídos de los formatos Word, con analitos y rangos del Lcdo. Peroza, entrega el mismo día.
        * **735 Pruebas Especiales de Referencia:** Sincronizadas desde Excel, entrega en 48 a 72 horas.
      * **Caso de Prueba Validado en Pantalla:**
        * `ESP-0030`: `(HCG) HEMOGLOBINA GLICOSILADA` ($14.00, Especial / Tercerizado, 48 a 72 Horas).
        * `SALV-00012`: `PRUEBA DE EMBARAZO (HCG)` ($6.00, Rutina Interna SALV, Mismo Día).
    * **Sincronización en Git:** Código respaldado y subido a las ramas `main` en GitHub (`afa889e` en API, `d8e1c79` en Frontend).

---

### 📋 Siguiente Fase en el Orden Establecido:

* **🧾 Paso 5 del Roadmap Oficial:**
  * **Módulo de Facturación y Emisión de Órdenes Clínicas (`OrdenesView.vue` y Facturación Unificada):**
    * Modelo de Facturación Unificada Multi-rubro (Laboratorio + Ecosonograma + Consulta Médica en una sola factura física o electrónica).
    * Despacho automático de órdenes clínicas derivadas para cada departamento.
    * Correlativos fiscales Serie "B" (Clínica "Dr. Rubén Merenfeld" RIF J-30426645-6) y gestión de cobranza/caja.

---

## 🛠️ Comandos Clave para Reanudar el Entorno

```bash
# 1. Verificar base de datos Docker
docker ps  # bitcore-db (5433) y bitcore-laboratorio-db (5434)

# 2. Correr el Backend Clínico (.NET 8)
cd /home/duvis/Proyectos/BitCore/backend/Laboratorio.Api
dotnet watch run

# 3. Correr el Frontend Clínico (Vite)
cd /home/duvis/Proyectos/BitCore/frontend/frontend-laboratorio
npm run dev
# URL: http://localhost:5173

# 4. Correr la API de Inventario (Node / Express)
cd /home/duvis/Proyectos/BitCore/api-inventario
node index.js
# URL API: http://localhost:3000 | Docs: http://localhost:3000/docs

# 5. Correr el Frontend de Inventario (Vite)
cd /home/duvis/Proyectos/BitCore/frontend-inventario
npm run dev
# URL Inventario: http://localhost:5173 (o puerto alterno asignado por Vite)
```