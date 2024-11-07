using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public bool red;
    public bool yellow;
    public PacketSender packetSender;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Send()
    {
        if(red)
        {
            packetSender.SendR();
        }
        if(yellow)
        {
            packetSender.SendY();
        }
        if(!red && !yellow)
        {
            packetSender.SendG();
        }
    }

    public void RedBoolean()
    {
        red = !red;
    }

    public void YellowBoolean()
    {
        yellow = !yellow;
    }
}
