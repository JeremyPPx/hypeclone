using UnityEngine;

// Basis-Movement: WASD + Sprung + Doppelsprung.
// Braucht eine CharacterController-Komponente auf demselben GameObject.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Bewegung")]
    public float moveSpeed = 6f;
    public float gravity = -20f;
    public float jumpHeight = 1.8f;
    public int maxJumps = 2; // 1 = normal, 2 = Doppelsprung

    [Header("Referenzen")]
    public Transform playerCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private int jumpsUsed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        bool grounded = controller.isGrounded;
        if (grounded && velocity.y < 0)
        {
            velocity.y = -2f; // leicht negativ hält den Controller "am Boden"
            jumpsUsed = 0;
        }

        // Bewegung relativ zur Blickrichtung (nur horizontal)
        Vector3 forward = playerCamera != null ? playerCamera.forward : transform.forward;
        Vector3 right = playerCamera != null ? playerCamera.right : transform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (forward * v + right * h).normalized * moveSpeed;

        if (Input.GetButtonDown("Jump") && jumpsUsed < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpsUsed++;
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    // Wird von HackSystem (Dash) benutzt, um kurzzeitig zusätzliche Bewegung reinzugeben.
    public void ApplyExternalMove(Vector3 worldMove)
    {
        controller.Move(worldMove);
    }

    public bool IsGrounded()
    {
        return controller.isGrounded;
    }
}
