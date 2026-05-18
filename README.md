# Lauter Fichaje

Aplicación móvil de gestión de fichajes para empresas de actividades extraescolares. Desarrollada con .NET MAUI 10 para proporcionar una solución multiplataforma (iOS, Android, Windows, macOS).

## 📋 Descripción

Lauter Fichaje es una solución integral para el registro y seguimiento de fichajes de empleados en centros educativos. Permite:

- ✅ Registro de entrada y salida
- 📊 Visualización de jornada laboral
- 👥 Gestión de empleados
- 📈 Reportes y estadísticas
- 🔐 Autenticación segura
- 📱 Acceso multiplataforma

## 🚀 Requisitos Previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download) o superior
- [Visual Studio 2022](https://visualstudio.microsoft.com/es/) con carga de trabajo MAUI
  - O [Visual Studio Code](https://code.visualstudio.com/) con C# DevKit
- [Git](https://git-scm.com/)

## 📦 Instalación

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/tu-usuario/lauter-fichaje.git
   cd "Lauter Fichaje"
   ```

2. **Restaurar dependencias**
   ```bash
   dotnet restore
   ```

3. **Compilar el proyecto**
   ```bash
   dotnet build
   ```

## 🏃 Ejecución

### En Visual Studio
- Abre el archivo `.sln`
- Selecciona el destino (Android, iOS, Windows o macOS)
- Presiona `F5` o click en "Ejecutar"

### Desde línea de comandos

**Android:**
```bash
dotnet build -f net9.0-android
dotnet run -f net9.0-android
```

**iOS:**
```bash
dotnet build -f net9.0-ios
dotnet run -f net9.0-ios
```

**Windows:**
```bash
dotnet run -f net9.0-windows
```

**macOS:**
```bash
dotnet build -f net9.0-macos
dotnet run -f net9.0-macos
```

## 🧪 Pruebas

```bash
dotnet test
```

## 📁 Estructura del Proyecto

```
Lauter Fichaje/
├── App.xaml.cs              # Punto de entrada de la aplicación
├── MauiProgram.cs           # Configuración de servicios
├── Resources/               # Recursos (imágenes, fuentes, estilos)
├── Views/                   # Páginas XAML
├── ViewModels/              # Lógica de presentación
├── Models/                  # Modelos de datos
├── Services/                # Servicios (API, base de datos, etc.)
├── Converters/              # Convertidores XAML
└── Platforms/               # Código específico por plataforma
    ├── Android/
    ├── iOS/
    ├── Windows/
    └── MacCatalyst/
```

## 🔧 Configuración

### Variables de entorno
Crea un archivo `appsettings.json` en la raíz del proyecto:

```json
{
  "API": {
    "BaseUrl": "https://api.lauter.com",
    "Timeout": 30
  },
  "Database": {
    "ConnectionString": "Data Source=lauter.db"
  }
}
```

## 📚 Documentación

- [Documentación oficial de MAUI](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Comunidad de MAUI](https://github.com/dotnet/maui)

## 🤝 Contribución

Las contribuciones son bienvenidas. Para cambios significativos:

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Licencia

Este proyecto está bajo licencia propietaria de Lauter.

## 📧 Contacto

Para preguntas o soporte, contacta con el equipo de desarrollo de Lauter.

## ✨ Changelog

### [Versión 1.0.0] - 2026-05-18
- Lanzamiento inicial
- Funcionalidad básica de fichajes
- Autenticación de usuarios
- Reportes simples

---

**Desarrollado con ❤️ para Lauter**
