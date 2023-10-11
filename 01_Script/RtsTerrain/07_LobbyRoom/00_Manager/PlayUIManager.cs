using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class PlayUIManager : MonoBehaviour
{
    [Header("GameTime_Info")]
    public Text gameTimeInfo;

    [Header("Kill_Info")]
    public RectTransform blueBar;
    public RectTransform redBar;
    public Text blueCount;
    public Text redCount;

    [Header("Panel")]
    public GameObject panel_Setting;
    public GameObject panel_End;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
