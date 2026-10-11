// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/Logic/Inventario.cs y Scripts/Data/ContenidoJuego.cs (recursos).
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

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
