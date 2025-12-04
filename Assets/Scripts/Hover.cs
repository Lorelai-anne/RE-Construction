using UnityEngine;

public class Hover : MonoBehaviour
{
    public float floatStrength = 0.5f; // Adjust this to control how high the object floats
    public float floatSpeed = 1f;    // Adjust this to control the speed of the floating motion

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position; // Store the initial position of the object
    }

    void Update()
    {
        // Calculate the new Y position using a sine wave
        // Time.time provides a continuously increasing value
        // floatSpeed controls the frequency of the wave
        // floatStrength controls the amplitude (how far up/down it moves)
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatStrength;

        // Update the object's position with the new Y value
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}
