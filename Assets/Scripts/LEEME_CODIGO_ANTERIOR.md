# Assets/Scripts: código anterior

Estos scripts son el **prototipo 2D** del primer desarrollo (tareas TT07 a TT12 del plan de trabajo).
**No participan en la beta 1.1.** La escena de la beta (`Assets/EternautaBeta/Scenes/Eternauta_Beta.unity`) no
usa ninguno de ellos, y el código de la beta tampoco los llama. Se conservan como antecedente del desarrollo.

La implementación actual está en `Assets/EternautaBeta/`:

| Script anterior | Tarea | Dónde está esa función en la beta 1.1 |
|---|---|---|
| `Player/PlayerController.cs` | TT07 (TF01) movimiento | `Scripts/Logic/Jugador.cs`, `Scripts/Core/Entrada.cs` |
| `Exploration/MapLimits.cs` | TT08 (TF02) exploración | `Scripts/World/Mundo.cs`, mapa en `Scripts/Data/ContenidoJuego.cs` |
| `Interaction/Interactable.cs` | TT09 (TF03) interacción con objetos | `Scripts/Core/EternautaGame.cs` (`BuscarInteraccion`, `Interactuar`) |
| `Resources/ResourceItem.cs`, `Resources/ResourceInventory.cs` | TT10 (TF04) recolección de recursos | `Scripts/Logic/Inventario.cs` |
| `Objectives/ObjectiveManager.cs`, `Objectives/ObjectiveInteractable.cs` | TT11 (TF05) objetivos | `Scripts/Logic/Progresion.cs` |
| `Dialogue/NPCDialogue.cs` | TT12 (TF06) interacción con personajes | `Scripts/Core/EternautaGame.cs` (diálogos) |
| `UI/MessageUI.cs` | mensajes en pantalla | `Scripts/UI/EternautaGameUI.cs` |
