# Taskbar Tactics

Vertical slice offline de un RPG táctico idle para la barra de tareas de Windows.
El jugador elige tres de seis héroes, configura formación, habilidades, equipo y
prioridad de ruta, y deja que la expedición avance mediante combates automáticos
deterministas.

## Ejecutar la build

La build local está en:

`TaskbarTactics/Builds/Windows/TaskbarTactics.exe`

La aplicación comienza en modo gestión. Desde allí se puede elegir el escuadrón,
equipar objetos, cambiar habilidades y formación, seleccionar una prioridad de
ruta e iniciar la expedición. El botón **Volver a la barra** activa la franja
compacta; **Gestionar** vuelve a abrir el panel.

## Abrir el proyecto

- Unity: 6.3 LTS (`6000.3.8f1`)
- Plataforma: Windows x64
- Escena principal: `Assets/Scenes/Main.unity`
- Menú de generación: `Taskbar Tactics > Build Editable Vertical Slice`
- Menú de build: `Taskbar Tactics > Build Windows x64`

Los datos de héroes, habilidades, objetos, afijos, enemigos, encuentros y mapa
se generan como ScriptableObjects editables dentro de `Assets/Generated/Content`.
La simulación no depende de referencias directas a esos assets para persistir:
el guardado JSON utiliza IDs estables y semillas.

Si se abre una copia nueva del repositorio y faltan recursos de TextMeshPro,
ejecutar primero `Taskbar Tactics > Import TextMeshPro Essentials`.

## Pruebas

Las suites están en:

- `Assets/Tests/EditMode`: simulación, targeting, botín, rutas, sinergias,
  guardado, respaldo, progreso offline, localización, contenido, escena y prefab.
- `Assets/Tests/PlayMode`: arranque de la escena, cambio de modo e inicio de una
  expedición desde la interfaz.

Pueden ejecutarse desde `Window > General > Test Runner`.

## Alcance del slice

Incluye seis héroes, dos habilidades activas y dos pasivas seleccionables por héroe,
equipo con cuatro ranuras, rarezas y afijos, sinergias por etiquetas, ocho
enemigos normales, un jefe y un mapa fijo de 18 nodos. El progreso se guarda de
forma local y la ausencia se resuelve por eventos hasta un máximo de ocho horas.

El arte, audio y balance son provisionales. No hay tienda, backend, cuentas,
multijugador, crafting, prestigio ni integración con Steam.
