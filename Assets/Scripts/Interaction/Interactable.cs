// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/Core/EternautaGame.cs (BuscarInteraccion / Interactuar).
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using UnityEngine;

// TT09 - Implementar sistema de interacción con objetos (TF03)
// GameObject: cualquier objeto interactuable (puerta, radio, cartel, etc.)
// Requiere: Collider2D con "Is Trigger" activado (más grande que el sprite = rango de interacción).
// El Player debe tener el Tag "Player".
public class Interactable : MonoBehaviour
{
    [SerializeField] private string message = "Es un objeto viejo y cubierto de polvo.";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject hint; // opcional: cartel "E" que aparece al acercarse

    protected bool playerInRange;

    void Start()
    {
        if (hint != null) hint.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    // Las clases hijas (NPC, objetivos) cambian este método
    protected virtual void Interact()
    {
        MessageUI.Instance.Show(message);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (hint != null) hint.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (hint != null) hint.SetActive(false);
        }
    }
}
