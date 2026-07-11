# Level Area Spawner

Herramienta de editor para Unity enfocada en disenar niveles y colocar prefabs de forma rapida en el Scene View.

## Estado actual

La herramienta ya permite:

- dibujar areas con `Rectangle`, `Circle`, `Freehand`, `Line` y `Brush`
- colocar prefabs dentro del area o sobre el perimetro
- usar modo consecutivo para cercas, bardas, paredes y objetos modulares
- generar `Ground` como un solo mesh con material asignable
- usar `Tiled` para layouts tipo piso o mosaico
- visualizar previews antes de soltar el click
- usar `Eraser` para borrar objetos colocados por la herramienta
- hacer snap entre piezas existentes y nuevos trazos
- usar `Auto Seed` o un seed manual

## Tools

### Rectangle

Dibuja un rectangulo arrastrando el mouse en la Scene View.

Opciones principales:

- `Perimeter Only`: instancia solo en el borde
- `Consecutive Perimeter`: coloca piezas una tras otra para cerrar bordes sin huecos
- `Tiled`: llena el area con tiles unidos por sus lados
- `Ground`: genera un solo mesh rectangular con material

### Circle

Dibuja una circunferencia arrastrando desde un extremo del diametro hasta el otro.

Opciones principales:

- `Perimeter Only`: instancia solo en el borde circular
- `Consecutive Perimeter`: usa piezas consecutivas para completar el contorno
- `Tiled`: rellena el area con tiles unidos por sus lados
- `Ground`: genera un solo mesh circular con material

### Freehand

Permite trazar una linea libre y colocar prefabs siguiendo ese recorrido.

Opciones principales:

- `Consecutive Instancing`: coloca piezas conectadas entre si a lo largo del trazo
- `Ground`: genera un mesh unico que sigue la silueta del trazo

### Line

Similar a `Freehand`, pero pensado para trazos rectos.

Opciones principales:

- `Consecutive Instancing`: coloca piezas conectadas sobre la linea
- hover preview del prefab antes de comenzar el trazo

### Brush

Herramienta pensada para pintar una franja con grosor mientras se traza.

Opciones principales:

- `Tiled`: usa una logica de celdas para colocar tiles unidos entre si
- `Tolerance`: controla que tan estricto es el relleno de cada celda
- `Brush Radius`: define el area de pintado

En `Brush + Tiled`:

- el radio efectivo se deriva del `Tile Scale`
- el radio interno queda ligado al doble del `Tile Scale`
- la ocupacion evita duplicados sobre trazos repetidos
- la seleccion de celdas se hace segun la silueta final del trazo

## Ground

`Ground` cambia el flujo de instanciacion por un solo mesh por trazado.

- en `Rectangle` genera un plano rectangular
- en `Circle` genera un mesh circular
- en `Freehand` genera una banda abierta o cerrada segun el trazo
- puede usar material continuo o `Tile Material`
- `Tile Size` ajusta la escala del patron del material
- meshes del mismo material se pueden unir en una sola pieza

## Prefabs

La lista de prefabs define los objetos disponibles para instanciar.

- si hay un solo prefab, siempre se repite ese mismo
- si hay varios, la herramienta elige uno al azar
- si no hay prefabs, se crean `Level Object` basicos

## Escala

La herramienta permite:

- rango de escala para modos normales
- escala unica para `Tiled`
- ajuste automatico de tamano en piezas consecutivas segun la longitud util del prefab

## Random Rotation

Permite rotacion aleatoria de los objetos instanciados.

- en modos normales se pueden elegir ejes
- en `Tiled` las rotaciones se limitan a pasos de 90 grados
- en `Ground` no aplica

## Snap y continuidad

Los modos de linea, consecutivos y trazos libres pueden hacer snap con objetos ya existentes para continuar trazos o cerrarlos sin dejar separaciones visibles.

## Preview

La herramienta muestra previews en tiempo real mientras se dibuja:

- preview del area
- preview de prefabs
- preview de trazos consecutivos
- preview del brush
- preview del mesh de `Ground`

## Eraser

El modo `Eraser` desactiva el resto de opciones y permite borrar objetos instanciados por la herramienta.

## Notas

El script principal vive en:

- `Assets/Editor/LevelAreaSpawnerWindow.cs`

Esta version de la herramienta cubre el flujo principal de dibujo, preview, snap, relleno, borrado y generacion de pisos.
