// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/Logic/Progresion.cs.
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using UnityEngine;

// TT11 - Implementar sistema de objetivos (TF05)
// GameObject: objeto del mapa que, al interactuar con E, completa el objetivo actual
// (por ejemplo la puerta del refugio). Mismos requisitos que Interactable.
public class ObjectiveInteractable : Interactable
{
    [SerializeField] private string completeMessage = "Lo lograste.";

    protected override void Interact()
    {
        MessageUI.Instance.Show(completeMessage);
        ObjectiveManager.Instance.CompleteCurrentObjective();
    }
}
