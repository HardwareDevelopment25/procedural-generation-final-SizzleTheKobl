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
    [SerializeField] GameObject m_XBSPText;
    [SerializeField] GameObject m_YBSPText;
    [SerializeField] GameObject m_DepthText;
    [SerializeField] GameObject m_LeafText;
    [SerializeField] GameObject m_minRoomXText;
    [SerializeField] GameObject m_minRoomYText;
    [SerializeField] GameObject m_maxRoomXText;
    [SerializeField] GameObject m_maxRoomYText;
    [SerializeField] GameObject m_PercentageFillText;
    [SerializeField] GameObject m_IterationsText;

    //EVENTS
    public UnityEvent OpenOptions;
    public UnityEvent CloseOptions;

    public UnityEvent OpenBSPOptions; //These will play based on whether or not the checkbox is selected for a BSP cave.
    public UnityEvent CloseBSPOptions;

    public UnityEvent OpenCaveOptions;
    public UnityEvent CloseCaveOptions;

    public UnityEvent FixSliderX;
    public UnityEvent FixSliderY;

    //VARIABLES
    bool m_optionsOpen = false;

    private void Awake()
    {
        m_optionsOpen = false;
        CloseOptions.Invoke();
        CloseBSPOptions.Invoke();
        CloseCaveOptions.Invoke();
        SetSeed("");
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
        FixSliderX.Invoke();
    }

    public void SetYSize(Slider ySlider)
    {
        int yInput = (int)ySlider.value;
        GameManager.m_mapY = yInput;
        m_YText.GetComponent<TextMeshProUGUI>().text = $"Y: {yInput.ToString()}";
        FixSliderY.Invoke();
    }

    public void SetBSPXSize(Slider xSlider)
    {
        int xInput = (int)xSlider.value;
        m_XBSPText.GetComponent<TextMeshProUGUI>().text = $"X: {xInput.ToString()}";
    }

    public void SetBSPYSize(Slider ySlider)
    {
        int yInput = (int)ySlider.value;
        m_YBSPText.GetComponent<TextMeshProUGUI>().text = $"Y: {yInput.ToString()}";
    }

    public void SetDepth(Slider depthSlider)
    {
        int depthInput = (int)depthSlider.value;
        m_DepthText.GetComponent<TextMeshProUGUI>().text = $"Max Depth: {depthInput.ToString()}";
    }
    public void SetLeaf(Slider leafSlider)
    {
        int leafInput = (int)leafSlider.value;
        m_LeafText.GetComponent<TextMeshProUGUI>().text = $"Min Leaf Size: {leafInput.ToString()}";
    }
    public void SetMinRoomX(Slider xSlider)
    {
        int xInput = (int)xSlider.value;
        m_minRoomXText.GetComponent<TextMeshProUGUI>().text = $"X: {xInput.ToString()}";
    }
    public void SetMinRoomY(Slider ySlider)
    {
        int yInput = (int)ySlider.value;
        m_minRoomYText.GetComponent<TextMeshProUGUI>().text = $"Y: {yInput.ToString()}";
    }
    public void SetMaxRoomX(Slider xSlider)
    {
        int xInput = (int)xSlider.value;
        m_maxRoomXText.GetComponent<TextMeshProUGUI>().text = $"X: {xInput.ToString()}";
    }
    public void SetMaxRoomY(Slider ySlider)
    {
        int yInput = (int)ySlider.value;
        m_maxRoomYText.GetComponent<TextMeshProUGUI>().text = $"Y: {yInput.ToString()}";
    }

    public void SetPercFill(Slider slider) 
    {
        float percentage = slider.value * 100;
        m_PercentageFillText.GetComponent<TextMeshProUGUI>().text = $"Percentage Fill: {percentage}%";
    }
    public void SetIterations(Slider slider)
    {
        m_IterationsText.GetComponent<TextMeshProUGUI>().text = $"Iterations: {(int)slider.value}%";
    }
    
    public void LimitXSlider(Slider slider) 
    { 
        if (slider.value > GameManager.m_mapX) { slider.value = GameManager.m_mapX; }
    }

    public void LimitYSlider(Slider slider)
    {
        if (slider.value > GameManager.m_mapY) { slider.value = GameManager.m_mapY; }
    }
    public void BSPOptions(Toggle bspOpen) 
    {
        if (bspOpen.isOn)
        {
            OpenBSPOptions.Invoke();
        }
        else 
        {
            CloseBSPOptions.Invoke();
        }
    }

    public void CaveOptions(Toggle caveOpen)
    {
        if (caveOpen.isOn)
        {
            OpenCaveOptions.Invoke();
        }
        else
        {
            CloseCaveOptions.Invoke();
        }
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
