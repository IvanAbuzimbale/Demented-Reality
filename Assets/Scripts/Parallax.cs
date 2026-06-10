using UnityEngine;

public class Parallax : MonoBehaviour
{

    private float startPos;
    private GameObject cam;

    // Parallax effect multiplier to control how much the background layer 
    // moves relative to the camera movement. A value of 0 means the layer 
    // will not move, while a value of 1 means it will move at the same 
    // speed as the camera.
    [SerializeField] private float parallaxEffect;

    // Optional vertical offset to allow for parallax layers that are 
    // not perfectly aligned with the camera's Y position
    [SerializeField] private float verticalOffset;

    void Start()
    {
        // Store the initial X position of the parallax layer and find the main camera
        startPos = transform.position.x;
        cam = GameObject.FindGameObjectWithTag("MainCamera");
    }

    void FixedUpdate()
    {
        // Calculate the distance the camera has moved and apply the parallax effect
        float dist = cam.transform.position.x * parallaxEffect;
        transform.position = new Vector3(startPos + dist, cam.transform.position.y + verticalOffset, transform.position.z);
    }
}
