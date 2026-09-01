using UnityEngine;

public class Player2 : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float rotateSpeed = 180f;
    public float lateralDamping = 5f;

    public Rigidbody rb;

    // input cached between Update and FixedUpdate
    private float inputForward;
    private float inputTurn;

    // Update is called once per frame: capture input here
    void Update()
    {
        inputForward = Input.GetAxis("Vertical");
        inputTurn = Input.GetAxis("Horizontal");
    }

    // FixedUpdate is used for physics
    void FixedUpdate()
    {
        HandleRotation();
        HandleMovement();
        DampLateralVelocity();
    }

    private void HandleRotation()
    {
        if (Mathf.Abs(inputTurn) > 0f)
        {
            // rotate the Rigidbody (keeps physics engine in control)
            Quaternion delta = Quaternion.Euler(Vector3.up * (inputTurn * rotateSpeed * Time.fixedDeltaTime));
            rb.MoveRotation(rb.rotation * delta);
        }
    }

    private void HandleMovement()
    {
        // apply acceleration in the current forward direction (after rotation)
        Vector3 force = transform.forward * (inputForward * moveSpeed);
        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void DampLateralVelocity()
    {
        // remove velocity component that is sideways relative to the player's forward
        Vector3 lateralVel = Vector3.Project(rb.linearVelocity, transform.right);
        if (lateralVel.sqrMagnitude > 0f)
        {
            // tune lateralDamping to taste
            rb.AddForce(-lateralVel * lateralDamping, ForceMode.Acceleration);
        }
    }
}
