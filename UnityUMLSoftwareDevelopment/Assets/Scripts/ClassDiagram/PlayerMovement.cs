using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;  // Z·kladn· r˝chlosù
    public float boostMultiplier = 2f; // N·sobok r˝chlosti pri podrûanÌ Shiftu

    private Vector3 moveDirection;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>(); // Ak Rigidbody neexistuje, prid· ho
            rb.useGravity = false;  // Nepad· dole
            rb.isKinematic = true;  // Ovl·dame ho ruËne
        }
    }

    void Update()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Keyboard.current.upArrowKey.isPressed) moveDirection += transform.forward;
        if (Keyboard.current.downArrowKey.isPressed) moveDirection -= transform.forward;
        if (Keyboard.current.rightArrowKey.isPressed) moveDirection += transform.right;
        if (Keyboard.current.leftArrowKey.isPressed) moveDirection -= transform.right;
        if (Keyboard.current.pageUpKey.isPressed) moveDirection += transform.up;
        if (Keyboard.current.pageDownKey.isPressed) moveDirection -= transform.up;

        transform.position += moveDirection.normalized * speed * Time.deltaTime;
    }

}
