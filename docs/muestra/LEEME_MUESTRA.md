# El Eternauta: Vientos de Octubre — versión para la muestra anual

Misma beta (mismo inicio, intro, HUD y controles), con estas novedades agregadas:

## Controles
| Tecla | Acción |
|---|---|
| W A S D / flechas | moverse (Shift corre) |
| Mouse | mirar (RePág / AvPág mira arriba y abajo) |
| E / Espacio / Enter | interactuar (puertas, cajones, heladeras, ascensor, personajes, radio) |
| Clic izquierdo | disparar / golpear / usar el objeto equipado |
| R | recargar la Glock 19 |
| 1 a 4, rueda del mouse, Q | elegir ranura de la hotbar (Q vuelve a la anterior) |
| Tab / I | inventario (en el inventario, 1 a 4 pone el objeto en esa ranura) |
| M | mapa (se va descubriendo al explorar) |
| Esc | pausa |

## Qué hay de nuevo
- **Mapa grande de Avellaneda** (unas 3 cuadras) hecho sobre la foto satelital: Plaza Alsina, Av. Mitre, Av. Belgrano, Ing. Marconi, la Técnica 7. Los límites son muros de nieve (si insistís contra uno, Pablo avisa que está tapado de nieve).
- **Lugares donde entrar** (con cajones y heladeras): McDonald's, Burger King, Instituto CIEC, Cerrajería Ferretería 25 de Mayo, Escuela Técnica 7, Dental Total, Sede Racing Club, Universidad Siglo 21, Kiosco, Supermercado, Farmacia, una casa de familia y la casa abandonada de la radio.
- **Departamento** (antes "refugio"): cuadro de la familia (Pablo, Celeste, Mateo y Patricio), cajones, mesa de trabajo y ascensor. Se sale con el ascensor (corte a negro) una vez armado el traje.
- **Cajones y heladeras** reemplazan los objetos del suelo (1 a 3 objetos, siempre los mismos en una misma partida).
- **Glock 19**: está en el cajón de la mesa de luz (pedido opcional en segundo plano). Empieza con 15 en el cargador + 5 de repuesto. Hay munición en cajones.
- **Armas cuerpo a cuerpo**: cuchillo, palo de escoba, bate. Balance: pistola 3 disparos, cuchillo/bate 4 golpes, palo 8 golpes.
- **Cascarudos gigantes** (único bicho): entre 4 y 10 en Avellaneda, afuera y en algunos interiores. Cada mordida quita el 20 % de la salud. El cadáver desaparece a los 15 s.
- **Hotbar de 4 ranuras** (armas, comida, medicinas y el mapa).
- **Pantalla de muerte** con la causa; se puede despertar en la cama ("fue un mal sueño") o volver al menú.
- **Capítulo 2** (se desbloquea al terminar el prólogo, en lugar de los créditos): encontrar la radio (se puede saltear con E), cruzar el Puente Pueyrredón, ayudar a los soldados en la Av. 9 de Julio y hablar con el sargento. Termina en "CONTINUARÁ". Los créditos del menú dicen "FIN DE LA BETA".
- Pablo habla solo de vez en cuando y extraña a su familia.

## Probar
1. Abrir el proyecto con Unity 6 (6000.6), escena `Assets/Scenes/Eternauta_Beta.unity` (o menú **Eternauta > Crear escena Beta**), y dar Play.
2. Prueba automática: menú **Eternauta > Ejecutar prueba automática** (escribe un informe en `Capturas/Pruebas`).
3. Para compilar: File > Build Settings > Windows > Build. La versión para la muestra está pensada para una PC de 4 GB de RAM (resolución interna baja, mallas por bloques y luces horneadas).

## Herramientas
`tools/generar_mapas.py` regenera `Data/MapasGenerados.cs` (los dos mapas) si se quiere cambiar el trazado.

## Atajos de desarrollador (F9 los activa)
F2 invulnerable · F3 completar objetivo · F4 postales · F5 un ítem de cada recurso · F6 cambiar salud · F7 ir al acceso del puente · F8 efecto VHS · F11 modo foto · F12 captura.
