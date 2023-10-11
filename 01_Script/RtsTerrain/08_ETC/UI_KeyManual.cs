using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class UI_KeyManual : MonoBehaviour
{
    public GameObject panel;
    Toggle tg_help;
        
    void Awake()
    {
        tg_help = GetComponent<Toggle>();

        tg_help.onValueChanged.AddListener(
            (value) =>
            {
                panel.SetActive(value);
            });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
