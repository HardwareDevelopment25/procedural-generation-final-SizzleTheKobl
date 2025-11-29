using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class LSystems : MonoBehaviour
{
    //Koch's Curve
    //Axiom: F
    //Law: F->F+F--F+F
    //Angle: 60

    //Koch's Snowflake
    //Axiom: F--F--F
    //Laws: Koch's Curve
    //Angle: 60

    //Dragon Curve
    //Axiom: F
    //Laws: F->F+G , G->F-G
    //Angle: 90

    //Barnsley Fern
    //Axiom: -X
    //Laws: X->F+[[X]-X]-F[-FX]+X , F->FF
    //Angle: 25

    [SerializeField] string m_axiom = "F"; //Starting Word
    [SerializeField] float m_angle = 60.0f; //Angle used in + and -
    [SerializeField] int m_iterations = 5;
    [SerializeField] string[] m_laws;

    [SerializeField] LineRenderer m_lineRenderer;
    [SerializeField] GameObject m_turtle; //Used to draw the L-System
    [SerializeField] GameObject m_cylinder;

    [SerializeField] float m_speed = 1f;

    private string currentString;

    [SerializeReference]
    private Dictionary<char, string> rules = new Dictionary<char, string>();

    public Stack<TransformData> m_transforms = new Stack<TransformData>();
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
        StartCoroutine(GenerateFractal(currentString));

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

    IEnumerator GenerateFractal(string currentString)
    {
        TransformData currPos;
        m_positions.Add(m_turtle.transform.position);

        foreach (char c in currentString)
        {
            switch (c)
            {
                case 'X':
                    //No movement
                    break;
                case 'F':
                case 'G':
                    Vector3 beginPos = m_turtle.transform.position;
                    m_turtle.transform.Translate(Vector3.up * m_speed);
                    Vector3 endPos = m_turtle.transform.position;
                    Vector3 cylPos = (beginPos + endPos) / 2;
                    Instantiate(m_cylinder, cylPos, m_turtle.transform.rotation, this.transform); //Highly inefficent, but, it is a good visualiser.
                    m_positions.Add(m_turtle.transform.position);
                    break;
                case '[':
                    currPos = new TransformData();
                    currPos.pos = m_turtle.transform.position;
                    currPos.rot = m_turtle.transform.rotation;
                    m_transforms.Push(currPos);
                    break;
                case ']':
                    currPos = new TransformData();
                    currPos = m_transforms.Pop();
                    m_turtle.transform.position = currPos.pos;
                    m_turtle.transform.rotation = currPos.rot;
                    break;
                case '+':
                    m_turtle.transform.Rotate(Vector3.forward * m_angle);
                    break;
                case '-':
                    m_turtle.transform.Rotate(Vector3.forward * -m_angle);
                    break;
            }

            m_lineRenderer.positionCount = m_positions.Count;
            m_lineRenderer.SetPositions(m_positions.ToArray());
            yield return new WaitForSeconds(0f);
        }
    }

    public struct TransformData 
    {
        public Vector3 pos;
        public Quaternion rot;
    }
}
