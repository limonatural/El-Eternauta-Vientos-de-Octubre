# Respuesta a la revisión técnica: beta 1.1

**El Eternauta: Vientos de Octubre** · Unity 6000.6.0f1

Respondemos las tres comprobaciones pendientes. No se rehicieron etapas ni se agregaron funcionalidades al juego:
solo se identificó el código anterior y se agregó una herramienta de prueba con su informe.

## 1. Arquitectura

Los scripts de `Assets/Scripts/` quedaron identificados como **código anterior**: son el prototipo 2D de las
tareas TT07 a TT12 y **no participan en la beta 1.1**. Cada uno tiene un comentario al principio que lo indica, y
`Assets/Scripts/LEEME_CODIGO_ANTERIOR.md` muestra dónde está cada función en la beta. Se verificó que la escena de
la beta (`Assets/EternautaBeta/Scenes/Eternauta_Beta.unity`) solo usa la cámara y el componente `EternautaGame`, y
que el código de `Assets/EternautaBeta/` no llama a ningún script de `Assets/Scripts/`.

La implementación actual está en `Assets/EternautaBeta/` (estructura en la sección 7 de `README_BETA.md`).

## 2. ERR-01: fuentes y textos

**Estado: RESUELTO** (prueba del 10/10/2026, 1920x1080, Editor de Unity 6000.6.0f1, Windows 10).

| Criterio | Resultado |
|---|---|
| Avisos "Unable to load font face" en la consola | 0 |
| Textos que no entran en su caja | 0 |
| Textos que se cruzan con otro texto | 0 |
| Textos de menos de 14 px reales | 0 |
| Textos distintos revisados | 468, en 1561 cuadros dibujados |
| Tamaño de letra más chico usado | 28 px |
| Fuentes usadas | Roboto Condensed y Liberation Sans, incluidas en el proyecto |

La prueba revisa automáticamente todos los textos de cada cuadro dibujado mientras recorre todas las pantallas
(menú, opciones, créditos, intro, HUD, diálogo, inventario, pausa, confirmaciones, final). Detalle de los criterios
en `docs/beta/PRUEBAS_BETA_1.1.md`, sección 2.

## 3. Testing de la versión entregada

**Resultado: 39 de 39 comprobaciones OK**, sin errores en la consola (duración 77 s).

| Área | Resultado |
|---|---|
| Apertura (menú principal, mundo 3D, fuentes) | OK (3 de 3) |
| Recorrido: traje, puertas, comida, radio, Informante | OK (22 de 22) |
| Inventario (abrir, categorías, UTILIZAR, cerrar) | OK (3 de 3) |
| Pausa (abrir, inventario desde la pausa, CONTINUAR) | OK (3 de 3) |
| Guardado en JSON | OK (2 de 2) |
| VOLVER AL MENÚ y CONTINUAR recuperando la partida | OK (3 de 3) |
| Finalización del prólogo (final, créditos, progreso 100 %) | OK (3 de 3) |

El informe completo, con cada comprobación, está en `docs/beta/pruebas/INFORME_PRUEBAS_1920x1080.md`, y se
adjuntan las 31 capturas que sacó la prueba.

**Cómo repetir la prueba:** abrir el proyecto en Unity y elegir el menú **Eternauta → Ejecutar prueba automática**.
El resultado queda en `Capturas/Pruebas/`. La prueba usa las mismas funciones del juego que usa el jugador, pero
ubica al protagonista al lado de cada objeto en lugar de caminar hasta él. El recorrido caminando con teclado y mouse
tiene su planilla de prueba manual en `docs/beta/PRUEBAS_BETA_1.1.md`, sección 3.

## Dónde está todo

- Código: https://github.com/limonatural/El-Eternauta-Vientos-de-Octubre, rama `beta-estilo-doom`
  (pull request #6).
- `README_BETA.md`: cómo abrir y jugar la beta.
- `docs/beta/PRUEBAS_BETA_1.1.md`: procedimiento de pruebas y criterios de ERR-01.
- `docs/beta/pruebas/INFORME_PRUEBAS_1920x1080.md`: informe de la prueba.
- `Assets/Scripts/LEEME_CODIGO_ANTERIOR.md`: código anterior.
