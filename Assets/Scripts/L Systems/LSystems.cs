using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class LSystems : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] string m_axiom = "F";
    [SerializeField] float m_angle = 60.0f;
    [SerializeField] int m_iterations = 5;
    [SerializeField] public string[] m_laws;

    [SerializeField] LineRenderer m_lineRenderer;
    [SerializeField] GameObject m_turtle;

    float m_speed = 0.5f;

    private string currentString;

    [SerializeReference]
    private Dictionary<char, string> rules = new Dictionary<char, string>();

    public Stack<Transform> m_transforms = new Stack<Transform>();
    public List<Vector3> m_positions = new List<Vector3>();

    private void Awake()
    {
        currentString = m_axiom;
        foreach (string law in m_laws)
        {
            string[] l = law.Split("->");
            rules.Add(l[0][0], l[1]);
        }
        GenerateLSystemString();
        GenerateFractal(currentString);

    }

    void GenerateLSystemString()
    {
        for (int i = 0; i < m_iterations; i++)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in currentString)
            {
                stringBuilder.Append(rules.ContainsKey(c) ? rules[c] : c.ToString());
            }
            currentString = stringBuilder.ToString();
        }

        Debug.Log(currentString);
    }

    void GenerateFractal(string currentString)
    {
        Transform currPos = m_turtle.transform;
        m_positions.Add(m_turtle.transform.position);

        foreach (char c in currentString)
        {
            switch (c)
            {
                case 'F':
                    m_turtle.transform.Translate(Vector3.up * m_speed);
                    currPos = m_turtle.transform;
                    m_positions.Add(m_turtle.transform.position);
                    
                    break;
                case '[':
                    m_transforms.Push(currPos);
                    break;
                case ']':
                    currPos = m_transforms.Pop();
                    m_turtle.transform.position = currPos.position;
                    m_turtle.transform.rotation = currPos.rotation;
                    break;
                case '+':
                    m_turtle.transform.Rotate(Vector3.forward * m_angle);
                    currPos = m_turtle.transform;
                    break;
                case '-':
                    m_turtle.transform.Rotate(Vector3.forward * -m_angle);
                    currPos = m_turtle.transform;

                    break;
            }
        }
        m_lineRenderer.positionCount = m_positions.Count;
        for (int i = 0; i < m_lineRenderer.positionCount; i++) 
        {
            m_lineRenderer.SetPosition(i, m_positions[i]);
        }
    }
}
