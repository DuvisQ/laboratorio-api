# 📋 Credenciales y Fichas de Usuarios para Pruebas (BitCore LIS)
### Sociedad Anticancerosa del Estado Aragua — Clínica de Prevención del Cáncer "Dr. Rubén Merenfeld"
> **Documento Oficial de Referencia Rápida para Pruebas y Auditoría**  
> *Última actualización y verificación directa en base de datos: Octubre 2026*

---

## ⚡ Tabla Resumen de Acceso Rápido

| Rol en el Sistema | Nombre del Colaborador | Correo / Usuario | Contraseña | PIN (4 dígitos) | Cédula | Avatar |
| :--- | :--- | :--- | :--- | :---: | :---: | :---: |
| 🛡️ **SuperAdmin** | Administrador del Sistema | `admin@bitcore.com` | `12369*` | `1234` | `V-12.482.761` | ⚡ Logo BitCore |
| 👑 **Administrador** | lpariata (Admin Clínica) | `luzmpariata@gmail.com` | *(Definida al crear)* | *(Definido)* | `V-3.376.050` | 🏥 Logo Clínica SALV |
| 🔬 **Bioanalista** | Lcdo. Ismael Peraza | `duvistest2024@gmail.com` | `Bioanalista2026*` | `1234` | `V-20.327.384` | 🔬 Bioanalista Azul |
| 📋 **Secretaria** | Ana Morales (Recepción) | `amorales@anticancerosalavictoria.org` | `Secretaria2026*` | `2345` | `V-24.891.203` | 📋 Secretaria Morado |
| 💵 **Cajero** | Carlos Gómez (Caja) | `cgomez@anticancerosalavictoria.org` | `Cajero2026*` | `3456` | `V-26.114.789` | 💵 Cajero Verde |

---

## 👤 Detalle de Cada Usuario y Matriz de Permisos

### 1. 🛡️ Super Administrador General
* **Nombre de Usuario:** Administrador
* **Email de Acceso:** `admin@bitcore.com`
* **Contraseña:** `12369*`
* **PIN de Autorización:** `1234`
* **Rol:** `SuperAdmin`
* **Avatar Asignado:** Insignia dorada institucional (`avatar:admin`)
* **Alcance y Permisos:**
  * Control total e irrestricto de toda la plataforma BitCore.
  * Acceso exclusivo al módulo **⚙️ Personal y Roles** (`/usuarios`).
  * Capacidad de dar de alta nuevos usuarios, alternar su estado activo/inactivo y aplicar **Restablecimiento Maestro de Contraseña y PIN**.
  * Edición de datos sanitarios y configuración global del tenant.

---

### 2. 🔬 Bioanalista Jefe de Laboratorio
* **Nombre Completo:** Lcdo. Ismael Peraza
* **Email de Acceso:** `duvistest2024@gmail.com` *(Configurado con correo real para pruebas activas de OTP por Email)*
* **Contraseña:** `Bioanalista2026*`
* **PIN de Autorización:** `1234`
* **Cédula de Identidad:** `V-20.327.384`
* **Registro Sanitario MPPS:** `17879`
* **Colegio de Bioanalistas:** `110-962-17879`
* **Rol:** `Bioanalista`
* **Avatar Asignado:** Microscopio clínico azul cyan (`avatar:bioanalista`)
* **Alcance y Permisos:**
  * **Responsabilidad técnica total del Laboratorio:** Ingreso de valores observados, parámetros, rangos de referencia (hombres/mujeres) y validación clínica con firma digital de órdenes procesadas.
  * Configuración técnica y edición de analitos en el Catálogo de Exámenes (`/catalogos`).
  * **Precios de exámenes:** Modo **Solo Lectura** (puede consultar las tarifas institucionales pero no modificarlas arbitrariamente).

---

### 3. 📋 Secretaria / Recepción y Admisión
* **Nombre Completo:** Ana Morales (Recepción)
* **Email de Acceso:** `amorales@anticancerosalavictoria.org`
* **Contraseña:** `Secretaria2026*`
* **PIN de Autorización:** `2345`
* **Cédula de Identidad:** `V-24.891.203`
* **Rol:** `Secretaria`
* **Avatar Asignado:** Portapapeles y admisión morado institucional (`avatar:secretaria`)
* **Alcance y Permisos:**
  * Admisión, búsqueda y registro de pacientes (`/pacientes`).
  * Emisión de órdenes iniciales de laboratorio.
  * Creación y consulta de exámenes generales en el catálogo.
  * **Restricción Clínica:** **No tiene acceso** a la edición ni incorporación de valores de referencia, componentes analíticos o resultados médicos (exclusivos del Bioanalista).
  * **Precios de exámenes:** Modo **Solo Lectura**.

---

### 4. 💵 Cajero / Cobranza y Facturación
* **Nombre Completo:** Carlos Gómez (Caja)
* **Email de Acceso:** `cgomez@anticancerosalavictoria.org`
* **Contraseña:** `Cajero2026*`
* **PIN de Autorización:** `3456`
* **Cédula de Identidad:** `V-26.114.789`
* **Rol:** `Cajero`
* **Avatar Asignado:** Caja registradora y finanzas verde esmeralda (`avatar:cajero`)
* **Alcance y Permisos:**
  * Cobranza, registro de pagos en bolívares/divisas y emisión de recibos y facturas fiscales Serie "B".
  * **Creación de Exámenes:** Puede registrar un examen nuevo en el catálogo para poder cobrarlo de inmediato al paciente en taquilla si no existía previamente.
  * **Restricción Clínica Estricta:** La pestaña de *«Parámetros y Valores de Referencia»* permanece bloqueada/oculta; no puede manipular analitos ni rangos médicos.
  * **Precios de exámenes:** Modo **Solo Lectura** en exámenes ya existentes (precios fijados institucionalmente).

---

## 🔐 Métodos de Recuperación y Autogestión

1. **Recuperación con PIN de Seguridad:**
   * En la pantalla de Login, presionar *«¿Olvidaste tu contraseña?»*.
   * Seleccionar la pestaña **«🔢 Con PIN de Seguridad»**, ingresar el correo y el PIN de 4 dígitos indicado en este archivo para definir una nueva contraseña de inmediato.
2. **Recuperación con Código OTP por Correo:**
   * En la misma pantalla, seleccionar **«📧 Por Correo»** para solicitar un código OTP de 6 dígitos (válido por 15 minutos).
   * Para pruebas locales sin servidor de correo activo, el backend imprime el código en la terminal Kestrel y el modal en modo desarrollo muestra el banner `[DEV: Código XXXXXX]`.
3. **Modal «Mi Perfil»:**
   * Cualquier usuario autenticado puede pulsar en su avatar en la barra superior para abrir el modal institucional, cambiar su clave, actualizar su PIN o consultar sus datos colegiados.

---

## 🌐 URLs y Puertos del Entorno de Desarrollo

* **Frontend Web:** `http://localhost:5173`
* **Backend API REST:** `http://localhost:5034`
* **Documentación Swagger:** `http://localhost:5034/swagger`
* **Base de Datos PostgreSQL (Docker):** Contenedor `bitcore-laboratorio-db` en puerto `5434` (Base de datos: `BitCoreLab_DB`).
