// CÓDIGO ANTERIOR (prototipo 2D de las tareas TT07 a TT12). No participa en la beta 1.1:
// ninguna escena de la beta lo usa. Se conserva como antecedente del desarrollo.
// En la beta 1.1 esta función está en Assets/EternautaBeta/Scripts/World/Mundo.cs y Scripts/Data/ContenidoJuego.cs (mapa).
// Ver Assets/Scripts/LEEME_CODIGO_ANTERIOR.md

using UnityEngine;

// TT08 - Implementar sistema de exploración (TF02)
// GameObject: Player (junto a PlayerController)
// Limita la zona por la que el jugador puede moverse.
public class MapLimits : MonoBehaviour
{
    [SerializeField] private Vector2 minLimit = new Vector2(-10f, -6f); // esquina inferior izquierda
    [SerializeField] private Vector2 maxLimit = new Vector2(10f, 6f);   // esquina superior derecha

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minLimit.x, maxLimit.x);
        pos.y = Mathf.Clamp(pos.y, minLimit.y, maxLimit.y);
        transform.position = pos;
    }

    // Dibuja el rectángulo del mapa en la ventana Scene (solo para ayudar a configurarlo)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = (minLimit + maxLimit) / 2f;
        Vector3 size = maxLimit - minLimit;
        Gizmos.DrawWireCube(center, size);
    }
}
