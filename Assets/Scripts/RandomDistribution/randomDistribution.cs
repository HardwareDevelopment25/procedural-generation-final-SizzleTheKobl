using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

public class randomDistribution : MonoBehaviour
{

    System.Random m_random = new System.Random();
    [SerializeField] GameObject m_sphere;
    [SerializeField] int amountOfCandidates = 4;
    [SerializeField] int totalSpheres;

    public List<Vector3> points = new List<Vector3>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        Vector3 SpherePos = new Vector3((m_random.Next(-2500, 2500) / 100), (m_random.Next(-2500, 2500) / 100), 0);
        Instantiate(m_sphere, SpherePos, Quaternion.identity);
        points.Add(SpherePos);

        StartCoroutine(SpawnSphere());
    }

    IEnumerator SpawnSphere() 
    {
        float currDis = 0;
        float minDis = float.MaxValue;
        float[] candDist = new float[amountOfCandidates];
        while (totalSpheres > 0)
        {
            Vector3[] candidates = new Vector3[amountOfCandidates];
            for (int i = 0; i < amountOfCandidates; i++)
            {
                candidates[i] = new Vector3(((float)(m_random.Next(-2500, 2500)) / 100), ((float)(m_random.Next(-2500, 2500)) / 100), 0);
            }
            for (int candidate = 0; candidate < candidates.Length; candidate++) {
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
            Instantiate(m_sphere, candidates[chosenCandidate], Quaternion.identity, this.transform);
            totalSpheres--;
            yield return new WaitForSeconds(0.01f);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
