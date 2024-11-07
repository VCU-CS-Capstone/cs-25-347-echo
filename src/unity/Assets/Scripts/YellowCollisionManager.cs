using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Will be on sphere
public class YellowCollisionManager : MonoBehaviour
{
    public Manager manager;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnter(Collider other)
    {
        string tag = other.gameObject.tag;

        if (tag == "Capsule")
        {
            manager.RedBoolean();
            manager.Send();
        }
    }

}

