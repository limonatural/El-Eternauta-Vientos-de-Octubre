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
