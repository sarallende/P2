# ASTERIX Decoder (PGTA - P2)

Decodificador de mensajes de vigilancia de tráfico aéreo en formato **Eurocontrol ASTERIX**:
- **CAT 021**: Mensajes ADS-B (Automatic Dependent Surveillance - Broadcast)
- **CAT 048**: Reportes de blancos de Radar Monopulso (Monopulse SSR Target Reports)

El sistema procesa y decodifica las tramas binarias ASTERIX, aplica filtros y reconstruye las trayectorias de vuelo de las aeronaves.

---

## 📋 Requisitos

1. **.NET SDK 6.0** o superior: [Descargar .NET](https://dotnet.microsoft.com/download)
2. **Visual Studio 2022** (con la carga de trabajo de *.NET Desktop Development* o consola) o **Visual Studio Code** con la extensión de C#.
3. **Git LFS** (necesario para descargar los ficheros de datos `.ast` de gran tamaño):
   ```bash
   git lfs install
   git lfs pull
   ```

---

## 🚀 Estructura del Proyecto

```text
P2/
├── .gitignore
├── .gitattributes
├── README.md
└── ASTERIX_Decoder/
    └── ASTERIX_Decoder/
        ├── ASTERIX_Decoder.sln          # Solución de Visual Studio
        ├── asterix_adsb.ast             # Fichero de datos ADS-B
        ├── asterix_radar.ast            # Fichero de datos Radar
        ├── asterix_combinado.ast        # Fichero combinado radar + ADS-B
        └── ASTERIX_Decoder/
            ├── ASTERIX_Decoder.csproj   # Proyecto C# (.NET 6)
            ├── Program.cs               # Punto de entrada principal
            ├── DecoderCAT021.cs         # Decodificador CAT 021
            ├── DecoderCAT048.cs         # Decodificador CAT 048
            ├── MensajeCAT021.cs         # Modelo de datos CAT 021
            ├── MensajeCAT048.cs         # Modelo de datos CAT 048
            ├── FiltroCAT021.cs          # Filtros para CAT 021
            ├── FiltroCAT048.cs          # Filtros para CAT 048
            ├── GestorTrayectorias.cs    # Reconstrucción de trayectorias
            ├── Trayectoria.cs           # Modelo de trayectoria
            └── PuntoTrayectoria.cs      # Puntos de trayectoria
```

---

## 🛠️ Cómo compilar y ejecutar

### Opción 1: Con Visual Studio 2022
1. Abrir la solución `ASTERIX_Decoder\ASTERIX_Decoder\ASTERIX_Decoder.sln`.
2. Compilar la solución (`Ctrl + Mayús + B`).
3. Ejecutar el proyecto con `F5` o `Ctrl + F5`.

### Opción 2: Desde la consola con .NET CLI
```bash
# Situarse en la carpeta del proyecto
cd ASTERIX_Decoder/ASTERIX_Decoder/ASTERIX_Decoder

# Compilar
dotnet build

# Ejecutar
dotnet run
```

---

## 📦 Ficheros de Datos Grandes (Git LFS)
Los ficheros `.ast` superan los límites habituales de tamaño de Git (más de 100 MB), por lo que están gestionados mediante **Git LFS** (*Large File Storage*).
Al clonar por primera vez, ejecuta:
```bash
git lfs pull
```
para asegurarte de que se han descargado los binarios completos de los ficheros `.ast`.
