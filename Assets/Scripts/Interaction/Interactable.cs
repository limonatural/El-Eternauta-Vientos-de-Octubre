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
