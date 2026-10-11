# Informe de pruebas: El Eternauta, Vientos de Octubre (beta 1.1.0, ejecutable)

- Fecha: 11/10/2026 01:16
- Versión del juego: 1.1.0
- Dónde se ejecutó: ejecutable de Windows compilado (build f32bd0223430466caf7e3fe64dc3b838)
- Unity: 6000.6.0f1
- Sistema: Windows 10  (10.0.19045) 64bit · GPU: AMD Radeon HD 7480D
- Resolución probada: 1366×768
- Duración: 62 s
- Resultado: **39 de 39 comprobaciones OK**

## 1. Testing de la versión entregada

| # | Comprobación | Resultado | Detalle |
|---|---|---|---|
| 1 | La beta abre en el menú principal | OK | Estado: MenuPrincipal |
| 2 | El mundo 3D se dibuja | OK | Imagen interna 356×200 |
| 3 | Se cargan las fuentes incluidas en el proyecto | OK | RobotoCondensed-Regular / LiberationSans-Regular |
| 4 | NUEVA PARTIDA empieza con la introducción | OK | Estado: Intro |
| 5 | Primer objetivo: armar el traje | OK |  |
| 6 | Se detecta la puerta del refugio | OK | Puerta detectada: 15,31 |
| 7 | Sin traje, la puerta del refugio no se abre | OK |  |
| 8 | Recoger lona | OK |  |
| 9 | Recoger alambre | OK |  |
| 10 | Recoger botiquín | OK |  |
| 11 | Objetivo 1 completado: traje armado | OK |  |
| 12 | Con traje, la puerta se abre | OK |  |
| 13 | La puerta abierta se puede cerrar (no desaparece) | OK |  |
| 14 | La puerta se vuelve a abrir | OK |  |
| 15 | Segundo objetivo: buscar comida | OK |  |
| 16 | Objetivo 2 completado: comida | OK |  |
| 17 | La radio se enciende | OK |  |
| 18 | La radio pasa todas sus líneas | OK | 4 de 4 |
| 19 | La radio avisa del ataque a los militares del Obelisco | OK |  |
| 20 | La señal se corta en medio del aviso | OK |  |
| 21 | Tercer objetivo: hablar con el sobreviviente | OK |  |
| 22 | Se abre el diálogo | OK | Estado: Dialogo |
| 23 | Aparece la decisión de darle un medicamento | OK |  |
| 24 | Objetivo 3 completado y se recibe el mapa | OK |  |
| 25 | Cuarto objetivo: llegar al Puente Pueyrredón | OK |  |
| 26 | Se abre el inventario | OK |  |
| 27 | UTILIZAR cura y descuenta el recurso | OK | Salud 40 → 55 |
| 28 | Se cierra el inventario | OK |  |
| 29 | Esc abre la pausa | OK |  |
| 30 | El inventario desde la pausa vuelve a la pausa | OK |  |
| 31 | CONTINUAR de la pausa vuelve al juego | OK |  |
| 32 | Se escribe el archivo de guardado (JSON) | OK | C:/Users/leand/AppData/LocalLow/DefaultCompany/El Eternauta Vientos de Octubre\eternauta_partida.json |
| 33 | El JSON guarda posición, traje, inventario y objetivos | OK |  |
| 34 | VOLVER AL MENÚ lleva al menú principal | OK |  |
| 35 | CONTINUAR queda habilitado | OK |  |
| 36 | CONTINUAR recupera la partida guardada | OK | Estado: Jugando |
| 37 | Al cruzar la línea del puente empieza el final | OK | Estado: Final |
| 38 | Termina en los créditos | OK | Estado: Creditos |
| 39 | Objetivo 4 completado y progreso 100 % | OK | Progreso 100 % |

## 2. ERR-01: fuentes y textos

Estado: **RESUELTO** en 1366×768.

| Criterio | Resultado |
|---|---|
| Avisos "Unable to load font face" en la consola | 0 |
| Textos distintos revisados (en 2901 cuadros) | 469 |
| Textos que no entran en su caja | 0 |
| Textos que se cruzan con otro texto | 0 |
| Textos de menos de 14 px reales | 0 |
| Tamaño de letra más chico usado | 20 px |
| Fuentes usadas | RobotoCondensed-Regular, LiberationSans-Regular |

## 3. Errores en la consola durante la prueba

Ninguno.

## 4. Capturas

- `01_menu_principal.png`
- `02_opciones.png`
- `03_creditos.png`
- `04_confirmar_nueva_partida.png`
- `05_intro.png`
- `06_hud_objetivo_actual.png`
- `07_puerta_sin_traje.png`
- `08_recoger_objeto.png`
- `09_traje_armado.png`
- `10_puerta_abierta.png`
- `11_puerta_cerrada.png`
- `12_casa_abandonada.png`
- `13_radio_bichos.png`
- `14_radio_corte.png`
- `15_dialogo.png`
- `16_dialogo_decision.png`
- `17_inventario_medicamentos.png`
- `18_inventario_comida.png`
- `19_inventario_materiales.png`
- `20_inventario_objetos_encontrados.png`
- `21_inventario_utilizar.png`
- `22_hud_salud_baja.png`
- `23_pausa.png`
- `24_pausa_opciones.png`
- `25_pausa_confirmar_salir.png`
- `26_menu_continuar_habilitado.png`
- `27_partida_continuada.png`
- `28_puente_pueyrredon.png`
- `29_final_obelisco.png`
- `30_fin_del_prologo.png`
- `31_creditos_final.png`

La prueba usa las mismas funciones del juego (recoger, mesa de trabajo, puertas, radio, diálogo, inventario,
pausa, guardado, CONTINUAR y final) y ubica al protagonista al lado de cada objeto en lugar de caminar hasta él.
El recorrido caminando con teclado y mouse se prueba a mano (ver docs/beta/PRUEBAS_BETA_1.1.md).
