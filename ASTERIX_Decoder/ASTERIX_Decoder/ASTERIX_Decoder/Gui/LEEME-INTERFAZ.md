# Interfaz gráfica ASTERIX Decoder (PGTA · P2)

Interfaz de escritorio en **Windows Forms (.NET 6)** construida *sobre* el
decodificador ya existente, sin reescribirlo. Todo el código nuevo vive en esta
carpeta `Gui/`.

---

## 1. Qué se ha respetado

- **No se ha modificado ninguna de las 11 clases del núcleo**: `DecoderCAT021`,
  `DecoderCAT048`, `FiltroCAT021`, `FiltroCAT048`, `MensajeCAT021`,
  `MensajeCAT048`, `GestorTrayectorias`, `PuntoTrayectoria`, `Trayectoria` **ni
  `Program.cs`**. Están exactamente igual que en el repositorio original (se
  puede comprobar con `git diff`).
- Se respetan los namespaces: decodificadores/mensajes/filtros en
  `ASTERIX_Decoder_p1` y trayectorias en `ASTERIX_Decoder`.
- El orden de procesado es idéntico al de `Program.cs`: escanear → decodificar
  CAT048 (paralelo) → QNH → posición geodésica → decodificar CAT021 → QNH →
  filtrar → generar trayectorias. Se usan **tus** métodos (`DecodificarMensaje`,
  `CalcularAltitudesQNH`, `CalcularPosicionGeodesica`, `Aplicar`,
  `CrearDesdeCAT048/021`, `ExportarCSV`).

## 2. Único archivo original modificado: `ASTERIX_Decoder.csproj`

Imprescindible para convertir la app de consola en app de ventanas:

| Cambio | Motivo |
|---|---|
| `net6.0` → `net6.0-windows` | Windows Forms solo funciona en ese *target*. |
| `OutputType` `Exe` → `WinExe` | Evita que se abra la ventana negra de consola. |
| `<UseWindowsForms>true` | Activa WinForms. |
| `<StartupObject>…Gui.AppLauncher` | La app arranca por la interfaz nueva; así **`Program.cs` no se toca** y su `Main` simplemente no se usa. |
| `PackageReference` GMap.NET.Core / .WinForms 2.1.7 | Mapa interactivo con OpenStreetMap. |

## 3. Cómo compilar y ejecutar (Visual Studio 2022, en Windows)

1. Abrir `ASTERIX_Decoder\ASTERIX_Decoder\ASTERIX_Decoder.sln`.
2. Visual Studio restaura solo los paquetes NuGet (GMap.NET) al abrir o al
   compilar. Si no, botón derecho en la solución → **Restaurar paquetes NuGet**.
3. Compilar (**Ctrl+Mayús+B**) y ejecutar (**F5**).
4. En la app: **Abrir archivo** → elegir `asterix_combinado.ast` (o los ficheros
   por separado) → pulsar **Play** en la barra inferior.

> El mapa descarga las teselas de OpenStreetMap: hace falta **conexión a
> internet** la primera vez (luego quedan en caché). Sin internet, el mapa sale
> en negro pero los aviones y trayectorias se siguen dibujando.

## 4. Archivos nuevos (todos en `Gui/`)

| Archivo | Función |
|---|---|
| `AppLauncher.cs` | Punto de entrada que arranca WinForms. |
| `MainForm.cs` | Ventana principal: barra superior, menú lateral, integración. |
| `Tema.cs` | Paleta de colores oscura y estilos. |
| `AsterixProcessingService.cs` | Carga/escaneo/decodificación/filtrado (usa el núcleo). |
| `SimuladorTrafico.cs` | Reloj virtual de la simulación (velocidades 0.5x–20x). |
| `AeronaveSimulada.cs` | Interpolación de la posición de cada avión en el tiempo. |
| `MapaControl.cs` | Mapa GMap.NET + marcadores + trayectorias. |
| `MarcadorAvion.cs` | Icono de avión orientado por rumbo, con etiqueta. |
| `VistaMapa.cs` | Pantalla de mapa: herramientas + mapa + panel de detalles. |
| `PanelDetalles.cs` | Panel lateral con los datos del avión seleccionado. |
| `BarraReproduccion.cs` | Controles Play/Pausa/Stop/±1s, línea de tiempo, reloj. |
| `VistaTrayectorias.cs` | Lista de trayectorias; al elegir una se centra en el mapa. |
| `VistaFiltros.cs` | Filtros CAT048/CAT021 (config inicial = la de `Program.cs`). |
| `VistaTabla.cs` | Tablas CAT048/CAT021 (DataGridView virtual, búsqueda, orden, CSV). |
| `VistaEstadisticas.cs` | Dashboard con totales, trayectorias y aeronaves únicas. |
| `ExportacionCsv.cs` | Exportación CSV usando tus `ExportarCSV` (formato del proyecto). |
| `Formato.cs` | Utilidades de formato (hora HH:mm:ss, tamaños). |

## 5. Pruebas NO realizadas (importante)

Este código se ha escrito en **macOS**, donde **no es posible compilar ni
ejecutar** Windows Forms ni GMap.NET. Por tanto:

- **No se ha compilado** el proyecto. Al abrirlo en Visual Studio podría hacer
  falta algún ajuste menor (p. ej. si tu VS resuelve otra versión de GMap.NET).
- **No se ha probado con datos reales**: los `.ast` del repositorio son
  punteros de **Git LFS** (no se descargaron en el Mac). En tu equipo, asegúrate
  de tener los binarios reales (`git lfs pull`) antes de probar la carga.
- El resto de la lógica (decodificación, filtros, trayectorias) es la tuya, ya
  validada en la práctica de consola.

### Qué conviene comprobar al ejecutar por primera vez
1. Que compila (si algún tipo de GMap.NET cambia de nombre entre versiones, VS lo
   señalará en `MapaControl.cs`).
2. Abrir un `.ast` y ver mensajes CAT048/CAT021 en la barra superior.
3. Play: los aviones se mueven; Radar en azul, ADS-B en verde.
4. Seleccionar un avión → panel derecho con sus datos.
5. Filtros → Aplicar → cambian los contadores y la simulación.
6. Tablas → buscar/ordenar/Exportar CSV.

## 6. Checklist del encargo

- [x] No se modifican las clases protegidas ni `Program.cs`.
- [x] Namespaces correctos, sin duplicar tipos.
- [x] Mapa real de Barcelona (OpenStreetMap) con arrastre, zoom y centrado.
- [x] Simulación temporal con interpolación entre puntos reales.
- [x] Radar y ADS-B diferenciados y simultáneos (sin fusionar).
- [x] Filtros conectados a `FiltroCAT048/021` (reaplicables sin redecodificar).
- [x] Tablas CAT048/CAT021 con búsqueda, orden y exportación.
- [x] Exportación CSV mediante los `ExportarCSV` existentes.
- [x] Estadísticas.
- [ ] Compilación y prueba con datos reales → **pendiente en Windows** (ver §5).
