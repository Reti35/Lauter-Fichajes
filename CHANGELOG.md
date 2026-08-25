# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [2026-05-18]

### Añadido
- Primer commit del proyecto.

## [2026-05-26]

### Añadido
- Login con autenticación segura, gestión de fichajes y exportación a PDF.

## [2026-05-27]

### Añadido
- Captura de geolocalización en los fichajes (opcional, no bloquea el guardado).
- Tema claro/oscuro.

## [2026-06-03]

### Añadido
- Filtros y agrupación de fichajes.

## [2026-07-02]

### Añadido
- Estilo visual renovado en la app.

### Corregido
- Errores en la exportación (Excel/PDF).

## [2026-07-27]

### Añadido
- Creación de usuarios desde la app.

## [Unreleased]

### Añadido
- Selector de empleado en la pantalla de exportación: permite restringir el Excel exportado a los fichajes de un usuario concreto (por defecto, "Todos los empleados").

### Cambiado

### Corregido
- Crash intermitente al arrancar la app (`SQLite.SQLiteException: no such table: local_session`) causado por una condición de carrera en la inicialización de la base de datos local (`LocalStorageService`/`FichajeService`).
- En la vista de admin/gestor no aparecían las cabeceras (nombre + mes/año) de los fichajes de otros empleados, solo las del propio usuario: el caché local nunca descargaba fichajes ajenos. Añadida `SyncAllUsersFromSupabaseAsync()` para rellenar el caché con todos los empleados.
