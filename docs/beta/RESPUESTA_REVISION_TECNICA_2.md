# Respuesta a la segunda revisión técnica: beta 1.1.0

**El Eternauta: Vientos de Octubre** · Unity 6000.6.0f1 · versión del juego **1.1.0**

Respondemos las tres comprobaciones pedidas. No se rehicieron etapas ni se agregaron funcionalidades al juego.
El detalle completo, con los criterios y las tablas, está en `docs/beta/PRUEBAS_BETA_1.1.md`.

Todas las pruebas se hicieron en la misma PC: Windows 10 (10.0.19045) 64 bit, AMD Radeon HD 7480D.
Los resultados del **Editor de Unity** y del **ejecutable compilado** se presentan por separado. Cada informe
dice en su encabezado dónde se ejecutó, y el pie del menú principal muestra "BETA 1.1.0 (Editor de Unity)" o
"BETA 1.1.0 (ejecutable)".

## 1. ERR-01 en 1366×768 y 1024×600 (Editor de Unity)

| Resolución | Avisos de fuente | Desbordes | Cruces | Textos chicos | Letra mínima | Comprobaciones | Estado |
|---|---|---|---|---|---|---|---|
| 1920×1080 | 0 | 0 | 0 | 0 | 28 px | 39 de 39 OK | **RESUELTO** |
| 1366×768 | 0 | 0 | 0 | 0 | 20 px | 39 de 39 OK | **RESUELTO** |
| 1024×600 | 0 | 0 | 0 | 0 | 15 px | 39 de 39 OK | **RESUELTO** |

Informes: `docs/beta/pruebas/INFORME_PRUEBAS_editor_1920x1080.md`, `..._editor_1366x768.md` y `..._editor_1024x600.md`.

## 2. Testing manual M01 a M15 (ejecutable, teclado y mouse)

Se jugó el prólogo completo con teclado y mouse sobre el ejecutable de Windows, en una ventana de 1024×600, el
11/10/2026 de 01:21 a 01:30. **Resultado: 15 de 15 casos OK.** Cada caso tiene su captura F12 en
`docs/beta/pruebas/manual_exe/`.

| # | Caso | Resultado | Captura |
|---|---|---|---|
| M01 | Apertura (menú con "BETA 1.1.0 (ejecutable)") | OK | `012127_menu.png` |
| M02 | Nueva partida, objetivo "Armar el traje aislante" | OK | `012150_refugio.png` |
| M03 | Movimiento y cámara | OK | `012218_refugio.png`, `012251_refugio.png` |
| M04 | Puerta sin traje ("Afuera la nieve mata...") | OK | `012315_refugio.png` |
| M05 | Traje armado en la mesa de trabajo | OK | `012349_refugio.png`, `012354_refugio.png` |
| M06 | Puerta que se abre y se cierra | OK | `012354_refugio.png`, `012406_refugio.png` |
| M07 | Comida en la casa abandonada | OK | `012505_casa_abandonada.png` |
| M08 | Radio con el aviso del Obelisco y corte de señal | OK | `012516_casa_abandonada.png` |
| M09 | Diálogo con el Informante y mapa recibido | OK | `012713_almacen.png` |
| M10 | Inventario por categorías y UTILIZAR medicamento | OK | `012750_plaza_alsina.png`, `012835_plaza_alsina.png` |
| M11 | Pausa | OK | `012844_plaza_alsina.png` |
| M12 | Guardado: VOLVER AL MENÚ → ¿Estás seguro? → SÍ | OK | `012906_plaza_alsina.png` |
| M13 | CONTINUAR ("Partida cargada" en el mismo lugar) | OK | `012910_almacen.png` |
| M14 | Puente Pueyrredón, final y créditos | OK | `012926_puente_pueyrredon.png`, `013007_puente_pueyrredon.png` |
| M15 | Textos legibles, sin cortes ni cruces en todas las pantallas | OK | todas las anteriores |

## 3. Pruebas sobre el ejecutable compilado

Versión **1.1.0**, build `f32bd0223430466caf7e3fe64dc3b838`, compilado con Unity 6000.6.0f1. La prueba automática
se corrió sobre el `.exe` con los accesos `PRUEBA_AUTOMATICA_*.bat` el 11/10/2026 entre las 01:16 y las 01:19.

| Resolución | Avisos de fuente | Desbordes | Cruces | Textos chicos | Letra mínima | Comprobaciones | Estado |
|---|---|---|---|---|---|---|---|
| 1366×768 (corrida 1) | 0 | 0 | 0 | 0 | 20 px | 39 de 39 OK | **RESUELTO** |
| 1366×768 (corrida 2) | 0 | 0 | 0 | 0 | 20 px | 39 de 39 OK | **RESUELTO** |
| 1024×600 | 0 | 0 | 0 | 0 | 15 px | 39 de 39 OK | **RESUELTO** |

Ninguna corrida tuvo errores en la consola. Las 39 comprobaciones cubren apertura, recorrido, inventario, pausa,
guardado en JSON, CONTINUAR y final del prólogo, igual que en el Editor.

**1920×1080 en el ejecutable:** el acceso de 1920×1080 abrió la ventana en 1366×768 (corrida 1). Creemos que la
pantalla de la PC de prueba es de 1366×768, y Windows no permite una ventana más grande que la pantalla. Esa
resolución está verificada en el Editor; falta correrla en el ejecutable sobre un monitor Full HD.

Informes: `docs/beta/pruebas/INFORME_PRUEBAS_exe_1366x768_corrida1.md`, `..._exe_1366x768_corrida2.md` y
`..._exe_1024x600.md`.
