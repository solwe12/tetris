using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Button : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject OptionPanel;

    public void InOption()
    {
        OptionPanel.SetActive(true);
    }
    public void OutOption()
    {
        OptionPanel.SetActive(false);
    }
}
