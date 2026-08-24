using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDir;
    public float moveSpeed;
    public float notGroundedPenalty;
    public Vector3 rotate;
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
        moveDir.z = Input.GetAxis("Vertical");
        rb.AddRelativeForce(moveSpeed * Time.deltaTime * moveDir, ForceMode.Impulse);

        // rotate body (yaw)
        rotate.y = Input.GetAxis("Horizontal");
        if (Mathf.Abs(rotate.y) > 0f)
        {
            // use rotateSpeed as sensitivity; multiply by Time.deltaTime for frame-rate independence
            transform.Rotate(Vector3.up * (rotate.y * rotateSpeed * Time.deltaTime));
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
