# 🗺️ Guía del Ecosistema BitCore: Carpetas, Logos e Imágenes
### BitCore LIS & Sociedad Anticancerosa del Estado Aragua — Clínica "Dr. Rubén Merenfeld"
> **Documento de Referencia Práctica para Administración y Operaciones**  
> *Redactado de forma clara y directa para consulta rápida.*

---

## 1. 📂 Estructura General de Carpetas (¿Dónde está cada cosa?)

Cuando abres la carpeta principal del proyecto (`BitCore`), verás varias subcarpetas. Las dos más importantes para el Laboratorio Clínico son:

```text
BitCore/                              <-- Carpeta Raíz de Todo el Proyecto
│
├── frontend/
│   └── frontend-laboratorio/         <-- 💻 SISTEMA VISUAL (PANTALLAS / VUE 3)
│       ├── public/                   <-- Iconos estáticos del navegador (favicon)
│       └── src/
│           ├── assets/               <-- 🖼️ AQUÍ VIVEN TODOS LOS LOGOS E IMÁGENES
│           │   ├── avatars/          <-- Avatares vectoriales (SVG)
│           │   ├── logo-bitcore.png  <-- Logo oficial de BitCore
│           │   ├── logo-circular.png <-- Sello circular oficial de la Clínica
│           │   ├── logo-horizontal.png <-- Membrete oficial de la Clínica
│           │   └── sello-bioanalista.png <-- Sello y firma del Bioanalista
│           └── modules/              <-- Código de pantallas (auth, usuarios, catalogos, etc.)
│
├── backend/
│   └── Laboratorio.Api/              <-- ⚙️ CEREBRO Y REGLAS DE NEGOCIO (.NET 8)
│       ├── Controllers/              <-- Rutas de la API (Auth, Usuarios, Exámenes, etc.)
│       ├── Models/                   <-- Estructura de tablas en la base de datos
│       └── Properties/
│           └── launchSettings.json   <-- Configuración de arranque (puerto 5034)
│
├── CREDENCIALES_USUARIOS_PRUEBAS.md  <-- 📋 Tus credenciales, contraseñas y PINs
└── GUIA_ESTRUCTURA_Y_RECURSOS.md     <-- 🗺️ Este documento de referencia
```

---

## 2. 🖼️ Formatos, Extensiones y Carpetas de Imágenes

### 2.1. ¿Qué extensión o formato se usa para los perfiles y logos?
* **Avatares y Logos Institucionales (los que vienen con el sistema):**
  * Formato `.svg` (gráficos vectoriales nítidos que no se pixelan nunca).
  * Formato `.png` con fondo transparente (para el logo circular de la clínica y el logo de BitCore).
* **Fotografías o Logos que subas desde tu computadora:**
  * Puedes subir imágenes con extensión **`.png`**, **`.jpg`**, **`.jpeg`** o **`.webp`**.
  * **Optimización inteligente:** El sistema acepta imágenes de hasta 5 MB y las recorta/redimensiona automáticamente a **256x256 píxeles** en un formato ultra-ligero para que la aplicación cargue al instante y no consuma espacio innecesario en la base de datos.

### 2.2. ¿Dónde están ubicados los logos en el proyecto?

| Recurso | Archivo Físico | Ubicación Exacta | Uso Principal |
| :--- | :--- | :--- | :--- |
| ⚡ **Logo BitCore** | `logo-bitcore.png` | `frontend/frontend-laboratorio/src/assets/logo-bitcore.png` | Avatar del SuperAdmin y marca tecnológica |
| 🏥 **Sello Circular Clínica** | `logo-circular.png` | `frontend/frontend-laboratorio/src/assets/logo-circular.png` | Avatar del Administrador local y membretes |
| 📋 **Membrete Horizontal Clínica** | `logo-horizontal.png` | `frontend/frontend-laboratorio/src/assets/logo-horizontal.png` | Cabeceras de reportes y pantallas |
| 🔬 **Sello Digital Bioanalista** | `sello-bioanalista.png` | `frontend/frontend-laboratorio/src/assets/sello-bioanalista.png` | Validación y firma electrónica de exámenes |

### 2.3. ¿Dónde están los avatares predeterminados?
Están en la carpeta:
📁 `frontend/frontend-laboratorio/src/assets/avatars/`
* `admin.svg` (Administrador / IT)
* `bioanalista.svg` (Bioanalista Clínico)
* `secretaria.svg` (Recepción / Admisión)
* `cajero.svg` (Caja y Cobranza)
* `medico.svg` (Médico Especialista)
* `medica.svg` (Médica Especialista)

---

## 3. 👑 ¿Cómo Asignar el Logo de BitCore o de la Clínica?

Ahora tienes dos formas sencillas e inmediatas:

### Método A: A un solo clic (Avatares Oficiales)
Tanto en **«Gestión de Personal y Roles»** (`/usuarios` -> Editar o Nuevo) como en **«Mi Perfil»** (pulsando tu nombre arriba a la derecha):
1. Verás los nuevos botones redondeados oficiales:
   * **`⚡ Logo BitCore (Master IT)`**
   * **`🏥 Logo Clínica SALV`**
2. Haz clic en cualquiera de ellos y el sistema asignará el logo institucional de inmediato.

### Método B: Cargar cualquier imagen propia desde tu equipo
1. En la misma sección verás el recuadro punteado:
   * **`📷 Haga clic aquí para seleccionar imagen (PNG, JPG o WebP)`**
2. Seleccionas el archivo desde tu explorador y el sistema lo optimiza y asigna al instante.

---

## 4. 🪪 Estandarización de Cédulas Venezolanas (V- y E-)

### ¿Por qué solo usamos V y E para el personal?
* **`V-` (Venezolano/a):** Aplica para la inmensa mayoría de colaboradores nacionales.
* **`E-` (Extranjero/a):** Aplica para profesionales y personal de nacionalidad extranjera residentes en el país.
* **¿Por qué no usamos J-, G-, etc.?:**
  * Las letras `J-` (Jurídico), `G-` (Gubernamental) y `C-` (Comunal) pertenecen al RIF de **empresas o instituciones** (por ejemplo, el RIF de la clínica es `J-30426645-6`).
  * En cambio, los colaboradores del sistema siempre son **personas naturales**, por lo que legalmente solo les corresponde `V-` o `E-`.

### ¿Cómo funciona el nuevo campo estandarizado?
1. **Selector automático:** Tienes una casilla pequeña fija donde seleccionas si es `V-` o `E-` (viene en `V-` por defecto).
2. **Número sin complicaciones:** En la casilla de al lado solo escribes los números (ej: `3376050`).
3. **Formateo automático:** El sistema automáticamente le coloca los separadores de miles para que se lea perfecto: **`V-3.376.050`**.
4. **En las tablas:** La cédula queda protegida en **una sola línea limpia** (`white-space: nowrap`), sin partirse nunca feo entre líneas.

---

## 5. 👥 Estado Actual de los Usuarios y Logos Asignados

| Usuario / Alias | Correo Electrónico | Rol | Cédula Normalizada | Avatar / Logo Configurado |
| :--- | :--- | :--- | :---: | :--- |
| **Administrador** | `admin@bitcore.com` | `SuperAdmin` | `V-12.482.761` | ⚡ **Logo BitCore** |
| **lpariata** | `luzmpariata@gmail.com` | `Administrador` | `V-3.376.050` | 🏥 **Logo Clínica SALV** |
| **Lcdo. Ismael Peraza** | `duvistest2024@gmail.com` | `Bioanalista` | `V-20.327.384` | 🔬 **Bioanalista Clínico** |
| **Ana Morales** | `amorales@anticancerosalavictoria.org` | `Secretaria` | `V-24.891.203` | 📋 **Recepción / Secretaria** |
| **Carlos Gómez** | `cgomez@anticancerosalavictoria.org` | `Cajero` | `V-26.114.789` | 💵 **Caja y Cobranza** |
