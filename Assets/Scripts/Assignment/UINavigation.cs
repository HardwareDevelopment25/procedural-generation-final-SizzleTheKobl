using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Windows;

public class UINavigation : MonoBehaviour
{
    //UI COMPONENTS
    //Serializable
    [SerializeField] GameObject m_seedText;
    [SerializeField] GameObject m_XText;
    [SerializeField] GameObject m_YText;
    //EVENTS
    public UnityEvent OpenOptions;
    public UnityEvent CloseOptions;

    public UnityEvent OpenBSPOptions; //These will play based on whether or not the checkbox is selected for a BSP cave.
    public UnityEvent CloseBSPOptions;

    public UnityEvent OpenCaveOptions;
    public UnityEvent CloseCaveOptions;

    //VARIABLES
    bool m_optionsOpen = false;

    private void Awake()
    {
        m_optionsOpen = false;
        CloseOptions.Invoke();
        CloseBSPOptions.Invoke();
        CloseCaveOptions.Invoke();
    }
    //SETTERS
    public void SetSeed(string seedInput)
    {
        int seed = 0;
        seed = GameManager.SetSeed(seedInput);
        m_seedText.GetComponent<TextMeshProUGUI>().text = $"Current Seed: {seed.ToString()}";
    }
    
    public void SetXSize(Slider xSlider) 
    {
        int xInput = (int)xSlider.value;
        GameManager.m_mapX = xInput;
        m_XText.GetComponent<TextMeshProUGUI>().text = $"X: {xInput.ToString()}";
    }

    public void SetYSize(Slider ySlider)
    {
        int yInput = (int)ySlider.value;
        GameManager.m_mapY = yInput;
        m_YText.GetComponent<TextMeshProUGUI>().text = $"Y: {yInput.ToString()}";
    }


    //EVENTS
    public void OptionsButton() 
    { 
        if (m_optionsOpen) 
        { 
            CloseOptions.Invoke();
            m_optionsOpen = false;
        }
        else 
        {
            OpenOptions.Invoke();
            m_optionsOpen = true;
        }
    }
}
