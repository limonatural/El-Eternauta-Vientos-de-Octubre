// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/Core/EternautaGame.cs (diálogos).
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using UnityEngine;

// TT12 - Implementar interacción con personajes (TF06)
// GameObject: cada NPC / sobreviviente.
// Requiere: SpriteRenderer y Collider2D con "Is Trigger" activado (el rango donde detecta al jugador).
// Para agregar más diálogos: sumar líneas a la lista "lines" en el Inspector.
public class NPCDialogue : Interactable
{
    [SerializeField] private string npcName = "Sobreviviente";
    [SerializeField] private string[] lines =
    {
        "¡Cuidado! La nieve mata si te toca.",
        "Necesitamos comida y medicinas para seguir.",
        "Buscá el refugio, yo te cubro."
    };
    [SerializeField] private bool completesObjective = false; // si es true, al terminar el diálogo avanza el objetivo

    private int lineIndex = 0;

    protected override void Interact()
    {
        MessageUI.Instance.Show(npcName + ": " + lines[lineIndex], 4f);
        lineIndex++;

        if (lineIndex >= lines.Length)
        {
            lineIndex = 0; // vuelve a empezar la próxima vez
            if (completesObjective)
            {
                ObjectiveManager.Instance.CompleteCurrentObjective();
            }
        }
    }
}
