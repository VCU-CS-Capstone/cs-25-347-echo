using UnityEngine;
using System.Collections;

public class Squaress : MonoBehaviour
{
    public float squareSize = 0.5f; // Adjust the size of the square as needed
    public float moveSpeed = 15f; // Adjust the speed of the robotic arm movement

    void Start()
    {
        // Set the starting position to the center of the square
        StartCoroutine(MoveRoboticArm());
    }

    IEnumerator MoveRoboticArm()
    {
        while (true)
        {
            // Define the four corner positions of the square
            Vector3[] cornerPositions = new Vector3[]
            {
                new Vector3(0, 0, squareSize / 5),
                new Vector3(0, -squareSize*2, -squareSize* 3),
            };

            // Move to each corner position in sequence
            for (int i = 0; i < cornerPositions.Length; i++)
            {
                yield return MoveToPosition(cornerPositions[i]);
            }
        }
    }

    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}