using UnityEngine;

public enum ResourceType
{
    Food,
    Medicine,
    Materials
}

// TT10 - Implementar sistema de recolección de recursos (TF04)
// GameObject: cada recurso del mapa (lata de comida, botiquín, chatarra).
// Requiere: SpriteRenderer y Collider2D con "Is Trigger" activado.
public class ResourceItem : MonoBehaviour
{
    [SerializeField] private ResourceType type = ResourceType.Food;
    [SerializeField] private int amount = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ResourceInventory.Instance.Add(type, amount);
            MessageUI.Instance.Show("Recogiste: " + type + " x" + amount);
            Destroy(gameObject); // el objeto desaparece
        }
    }
}
