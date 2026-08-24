using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDir;
    public float moveSpeed;
    public Vector3 rotate;
    public Rigidbody rb;
    public float rotateSpeed;


    // internal camera pitch tracked in degrees (-180..180)
    private float cameraPitch;

    // Update is called once per frame
    void Update()
    {
        BodyMovement();
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
}
