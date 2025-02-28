using System.Collections;
using UnityEngine;

public class Collisions : MonoBehaviour
{

    public UpdatedMove manager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        manager.YourBooleanProperty = true;
        Debug.Log(other.name + "Entered");
    }

    void OnTriggerExit(Collider other)
    {
        manager.YourBooleanProperty = false;
        Debug.Log(other.name + "Exit");
    }
}
