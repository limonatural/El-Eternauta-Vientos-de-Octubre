# Pruebas de la beta 1.1

Este documento responde a las comprobaciones pendientes de la revisión técnica:
**ERR-01** (fuentes y textos) y el **testing de la versión entregada** (apertura, recorrido, inventario,
pausa, guardado, CONTINUAR y final del prólogo). La arquitectura (código anterior en `Assets/Scripts/`)
está explicada en `Assets/Scripts/LEEME_CODIGO_ANTERIOR.md` y en la sección 7 de `README_BETA.md`.

Las pruebas se hacen de dos formas: una **prueba automática** que recorre el juego sola y escribe un
informe con capturas, y una **prueba manual** jugando con teclado y mouse. Los resultados se toman
del juego funcionando: el informe y las capturas salen de la propia ejecución.

---

## 1. Prueba automática

### Cómo se ejecuta

- **En Unity:** menú **Eternauta → Ejecutar prueba automática**. Entra en Play, recorre el prólogo
  (unos 90 segundos) y al terminar abre la carpeta con el informe. No hay que tocar nada mientras corre.
- **En el juego compilado (.exe):** abrirlo con el argumento `-prueba`. Por ejemplo, desde una
  consola en la carpeta del juego:

  ```
  "El Eternauta Vientos de Octubre.exe" -prueba
  "El Eternauta Vientos de Octubre.exe" -prueba -screen-fullscreen 0 -screen-width 1366 -screen-height 768
  "El Eternauta Vientos de Octubre.exe" -prueba -screen-fullscreen 0 -screen-width 1024 -screen-height 600
  ```

El resultado queda en `Capturas/Pruebas/<fecha>_<resolución>/`:

- `INFORME_PRUEBAS.md`: tabla con cada comprobación (OK / FALLA), el estado de ERR-01 y los errores de consola.
- Una captura PNG de cada pantalla recorrida (menú, opciones, créditos, intro, HUD, puertas, radio,
  diálogo y decisión, inventario por categoría, pausa, confirmación, CONTINUAR, puente, final y créditos).

La prueba respalda la partida guardada del jugador antes de empezar y la repone al terminar.

### Qué comprueba

| Área | Comprobaciones |
|---|---|
| Apertura | Abre en el menú principal, el mundo 3D se dibuja, se cargan las fuentes incluidas |
| Recorrido | Puerta del refugio bloqueada sin traje; recoger lona, alambre y botiquín; mesa de trabajo (objetivo 1); puerta que se abre, se cierra y se vuelve a abrir; comida en la casa abandonada (objetivo 2); radio completa con el aviso del Obelisco y el corte de señal; diálogo con el Informante y decisión (objetivo 3) |
| Inventario | Se abre, muestra las 4 categorías, UTILIZAR cura y descuenta el recurso, se cierra |
| Pausa | Esc abre la pausa, inventario y opciones desde la pausa, confirmación de salida, CONTINUAR vuelve al juego |
| Guardado | Se escribe el JSON con posición, traje, inventario y objetivos |
| CONTINUAR | VOLVER AL MENÚ, CONTINUAR habilitado, y la partida se recupera desde el archivo |
| Final | Al cruzar el puente empieza el final, Obelisco, "FIN DEL PRÓLOGO", créditos, objetivo 4 y progreso 100 % |
| ERR-01 | Ver la sección 2 |

La prueba usa las mismas funciones del juego que usa el jugador (las de la tecla E, el inventario, la
pausa y los menús), pero ubica al protagonista al lado de cada objeto en lugar de caminar hasta él.
Por eso el recorrido caminando se prueba también a mano (sección 3).

---

## 2. ERR-01: fuentes y textos

**Problema original:** la consola mostraba `Unable to load font face for [Arial Narrow]` y en varias
pantallas los textos se veían chicos, se salían de su caja o se cruzaban entre sí.

**Corrección (beta 1.1):** la interfaz ya no pide fuentes al sistema operativo. Usa Roboto Condensed y
Liberation Sans incluidas en `Assets/EternautaBeta/Resources/Fuentes` (con "Include Font Data"). Las letras
se dibujan a su tamaño real y cada texto se achica solo si no entra en su caja.

**Cómo se verifica:** durante la prueba automática se revisan **todos los textos de cada cuadro dibujado**:

| Criterio | Se considera resuelto si |
|---|---|
| Avisos "Unable to load font face" en la consola | 0 |
| Textos que no entran en su caja | 0 |
| Textos que se cruzan con otro texto | 0 |
| Textos de menos de 14 px reales | 0 |

El informe dice **RESUELTO** o **PENDIENTE** y, si algo falla, lista la pantalla y el texto.
Hay que correrla en las tres resoluciones de la Etapa 12 (lámina 9C): en Unity cambiando la resolución de la
pestaña Game (1920x1080, 1366x768 y 1024x600) antes de cada ejecución, o en el .exe con los argumentos de arriba.

### Registro de ERR-01 (completar con los informes)

| Resolución | Avisos de fuente | Desbordes | Cruces | Textos chicos | Estado | Informe |
|---|---|---|---|---|---|---|
| 1920x1080 | | | | | | |
| 1366x768 | | | | | | |
| 1024x600 | | | | | | |

---

## 3. Prueba manual (jugando)

Con el .exe o con Play en Unity, jugando normalmente con teclado y mouse. Sacar captura (F12) de cada paso.

| # | Caso | Pasos | Resultado esperado | Resultado | Captura |
|---|---|---|---|---|---|
| M01 | Apertura | Abrir el juego | Aparece el menú principal, sin errores | | |
| M02 | Nueva partida | NUEVA PARTIDA | Intro y luego el refugio con el objetivo "Armar el traje aislante" | | |
| M03 | Movimiento y cámara | W A S D, mouse, R / F | El protagonista camina, gira y mira arriba y abajo | | |
| M04 | Puerta sin traje | E en la puerta del refugio | "Afuera la nieve mata...", la puerta no se abre | | |
| M05 | Traje | Recoger 2 materiales y usar la mesa de trabajo | "Armaste el traje aislante", objetivo completado | | |
| M06 | Puertas | Abrir la puerta del refugio, salir del marco y apretar E otra vez | La puerta se abre y se cierra, no desaparece | | |
| M07 | Comida | Ir a la casa abandonada y recoger la lata | Objetivo 2 completado | | |
| M08 | Radio | Usar la radio y esperar | Avisa de los militares del Obelisco y se corta la señal | | |
| M09 | Informante | Hablar en el almacén y elegir con 1 o 2 | Diálogo, decisión y objetivo 3 completado | | |
| M10 | Inventario | Tab, recorrer categorías, UTILIZAR un medicamento | Se ve el objeto en 3D, sube la salud, baja la cantidad | | |
| M11 | Pausa | Esc, recorrer las opciones, CONTINUAR | El juego se detiene y vuelve | | |
| M12 | Guardado | Completar un objetivo, Esc → VOLVER AL MENÚ → SÍ | Vuelve al menú con CONTINUAR habilitado | | |
| M13 | CONTINUAR | CONTINUAR | Se retoma en el mismo lugar, con el mismo inventario y objetivo | | |
| M14 | Final | Ir por la Av. Mitre y cruzar el Puente Pueyrredón | Obelisco, "FIN DEL PRÓLOGO" y créditos | | |
| M15 | Textos (ERR-01) | Mirar todas las pantallas anteriores | Ningún texto chico, cortado ni encimado | | |
