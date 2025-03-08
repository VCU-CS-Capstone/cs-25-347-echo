using nuitrack;
using nuitrack.device;
using NuitrackSDK.Loader;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LicenseActivator : MonoBehaviour
{
    [SerializeField] InputField textField;
    [SerializeField] Text messageText;

    public void TryActivate()
    {
        ActivateLicenseKey(textField.text);
    }

    public void SkipActivate()
    {
        GoToScene(Application.loadedLevel + 1);
    }

    void ActivateLicenseKey(string key, int id = 0)
    {
        string result;

        NuitrackLoader.InitNuitrackLibraries();

        Nuitrack.Init();

        List<NuitrackDevice> devices = Nuitrack.GetDeviceList();

        if (id >= devices.Count)
        {
            messageText.text = "Connect the sensor!";
            return;
        }

        if (NuitrackManager.TryActivateLicense(devices[id], key, out result))
        {
            messageText.text = "Success!\n" + result;
            GoToScene(Application.loadedLevel + 1);
        }
        else
            messageText.text = "Fail!\n" + result;

        Debug.Log(result);

        Nuitrack.Release();
    }

    void GoToScene(int id)
    {
        Application.LoadLevel(id);
    }
}
