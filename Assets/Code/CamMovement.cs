using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CamMovement : MonoBehaviour
{
    public Transform player;
    public Transform locationTarget;
    public Transform cam;
    public float camSpeed;
    public float maxDistance;

    private void Start()
    {
        cam.position = new Vector3(0,0,0);   
    }
    // Update is called once per frame
    void Update()
    {
        Vector3 c = cam.position - locationTarget.position;
        c.Normalize();
        c *= maxDistance;
        cam.position = locationTarget.position + c;
        cam.LookAt(player);
        cam.position = Vector3.MoveTowards(cam.position, c, camSpeed * Time.deltaTime);        
    }
}
