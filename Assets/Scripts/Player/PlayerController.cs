using UnityEngine;

// TT07 - Implementar controlador de movimiento (TF01)
// GameObject: Player
// Requiere: Rigidbody2D (Gravity Scale = 0) y un Collider2D. Tag del objeto: "Player"
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 4f; // Se cambia desde el Inspector

    private Rigidbody2D rb;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // WASD o flechas
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized; // evita ir más rápido en diagonal
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }
}
