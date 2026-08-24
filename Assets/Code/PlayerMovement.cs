using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDir;
    public float moveSpeed;
    public float notGroundedPenalty;
    public Rigidbody rb;
    public float jumpStrength;
    public float gravity;
    public float maxDampening;
    public float minDampening;

    [Header("Cam movement")]
    public Vector3 bodyRotate;
    public Vector3 camRotate;
    public float rotateSpeed;
    public Transform cam;
    public float minClamp;
    public float maxClamp;

    [Header("Ground checks")]
    public float groundDistance;
    public RaycastHit groundedHit;
    public bool grounded;

    // internal camera pitch tracked in degrees (-180..180)
    private float cameraPitch;

    void Start()
    {
        // Initialize cameraPitch from current local rotation and normalize to -180..180 range
        if (cam != null)
        {
            cameraPitch = cam.localEulerAngles.x;
            if (cameraPitch > 180f)
            {
                cameraPitch -= 360f;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        BodyMovement();
        Jump();
    }

    private void BodyMovement()
    {
        // body movement
        moveDir.x = Input.GetAxis("Horizontal");
        moveDir.z = Input.GetAxis("Vertical");

        if (grounded == true)
        {
            rb.AddRelativeForce(moveSpeed * Time.deltaTime * moveDir, ForceMode.Impulse);
        }
        else
        {
            rb.AddRelativeForce(moveSpeed * notGroundedPenalty * Time.deltaTime * moveDir, ForceMode.Impulse);
        }

        // mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // rotate body (yaw)
        if (Mathf.Abs(mouseX) > 0f)
        {
            // use rotateSpeed as sensitivity; multiply by Time.deltaTime for frame-rate independence
            transform.Rotate(Vector3.up * (mouseX * rotateSpeed * Time.deltaTime));
        }

        // rotate camera (pitch) with clamping
        if (cam != null)
        {
            // invert mouseY to match original intent (moving mouse up looks up)
            cameraPitch -= mouseY * rotateSpeed * Time.deltaTime;
            cameraPitch = Mathf.Clamp(cameraPitch, minClamp, maxClamp);

            // apply only pitch locally to avoid messing with player's yaw
            cam.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
        }
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (grounded == true)
            {
                rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);
            }
        }

        // checks for sky vs ground linear dampening
        if (grounded == true)
        {
            rb.linearDamping = maxDampening;
        }
        else
        {
            rb.linearDamping = minDampening;
        }

        // checks if player is allowed to jump
        if (Physics.Raycast(transform.position, -transform.up, out groundedHit, groundDistance))
        {
            grounded = true;
        }
        else
        {
            grounded = false;
        }
    }
}
