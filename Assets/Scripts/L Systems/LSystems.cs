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
    [SerializeField] string m_name;
    [SerializeField] string m_axiom = "F"; //Starting Word
    [SerializeField] float m_angle = 60.0f; //Angle used in + and -
    [SerializeField] int m_iterations = 5;
    [SerializeField] string[] m_laws;
    [SerializeField] int m_seed;
    [SerializeField] int m_candidates = 6;
    [SerializeField] int m_totalSpawns = 1;
    [SerializeField] int m_gridSize = 64;

    [SerializeField] LineRenderer m_lineRenderer;
    [SerializeField] GameObject m_turtle; //Used to draw the L-System
    [SerializeField] GameObject m_cylinder;
    [SerializeField] GameObject m_leaf;
    [SerializeField] float m_cylScale;

    [SerializeField] float m_speed = 1f;


    private string currentString;
    System.Random m_random;

    [SerializeReference]
    private Dictionary<char, string> rules = new Dictionary<char, string>();

    public Stack<TransformData> m_transforms = new Stack<TransformData>();
    public List<Vector3> m_positions = new List<Vector3>();
    List<Vector3> m_treePos;

    private void Awake()
    {
        m_random = new System.Random(m_seed);
        currentString = m_axiom;
        foreach (string law in m_laws)
        {
            string[] l = law.Split("->");
            rules.Add(l[0][0], l[1]);
        }
        GenerateLSystemString();
        m_cylinder.transform.localScale = new Vector3(m_cylScale, (m_speed / 2), m_cylScale);
        //Adding Best Candidate to make a forest
        m_treePos = BestCandidate(m_totalSpawns, m_candidates, m_gridSize);
        //
        foreach (Vector3 pos in m_treePos)
        {
            GenerateFractal(currentString, pos);
        }
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

    List<Vector3> BestCandidate(int totalSpawns, int amountOfCandidates, int gridSize) 
    {
        List<Vector3> points = new List<Vector3>();
        points.Add (Vector3.zero);
        float currDis = 0;
        float minDis = float.MaxValue;
        float[] candDist = new float[amountOfCandidates];
        while (totalSpawns > 0)
        {
            Vector3[] candidates = new Vector3[amountOfCandidates];
            for (int i = 0; i < amountOfCandidates; i++)
            {
                candidates[i] = new Vector3(RandomFloat(gridSize/2), RandomFloat(1)+ this.transform.position.y, RandomFloat(gridSize/2)); //To allow proper float positioning.
            }
            for (int candidate = 0; candidate < candidates.Length; candidate++)
            {
                minDis = float.MaxValue;
                foreach (Vector3 point in points)
                {
                    currDis = Mathf.Abs(Vector3.Distance(candidates[candidate], point));
                    if (currDis < minDis) { minDis = currDis; }
                }
                candDist[candidate] = minDis;
            }
            int chosenCandidate = candDist.ToList().IndexOf(candDist.Max());
            points.Add(candidates[chosenCandidate]);
            totalSpawns--;
        }
        points.Remove(Vector3.zero);
        return points;
    }


    float RandomFloat(int number) //Just goes from -number to number.
    {
        return (float)(m_random.Next(-(number*100), (number * 100)) / 100);
    }

    void GenerateFractal(string currentString, Vector3 position)
    {

        TransformData currPos;
        currPos.pos = position;
        currPos.rot = Quaternion.identity;
        m_turtle.transform.position = currPos.pos;
        m_turtle.transform.rotation = currPos.rot;
        if (m_name == "Barnsley Fern") { m_turtle.transform.Rotate(new Vector3(RandomFloat(25), RandomFloat(180), 0)); }
        else if (m_name == "Dragon Curve")
        {
            m_turtle.transform.Rotate(new Vector3(90, 0, 0));
        }
            m_positions.Add(m_turtle.transform.position);

        foreach (char c in currentString)
        {
            switch (c)
            {
                case 'X':
                    //No movement
                    Instantiate(m_leaf, m_turtle.transform.position, m_turtle.transform.rotation, this.transform);
                    break;
                case 'F':
                case 'G':
                    if (m_name == "Barnsley Fern") { m_turtle.transform.Rotate(new Vector3(RandomFloat(10), RandomFloat(10), RandomFloat(10))); }
                    Vector3 beginPos = m_turtle.transform.position;
                    m_turtle.transform.Translate(Vector3.up * m_speed);
                    Vector3 endPos = m_turtle.transform.position;
                    Vector3 cylPos = (beginPos + endPos) / 2;
                    Instantiate(m_cylinder, cylPos, m_turtle.transform.rotation, this.transform); //Highly inefficent, but, it is a good visualiser.
                    //m_positions.Add(m_turtle.transform.position); outdated Line Renderer code.
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

            //m_lineRenderer.positionCount = m_positions.Count;
            //m_lineRenderer.SetPositions(m_positions.ToArray()); Outdated line renderer code.
        }
    }

    public struct TransformData 
    {
        public Vector3 pos;
        public Quaternion rot;
    }
}
