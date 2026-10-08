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
