using TMPro;
using UnityEngine;

public static class GameManager
{
    //Singleton to allow for common variables to be reused across scripts.
    public static int m_seed;
    public static System.Random m_random; //Random being here ensures all generation will use the same seed, regardless of script.
    public static int m_mapX = 50;
    public static int m_mapY = 50;
    public static string m_currentSeed;
    public static int SetSeed(string seedInput) 
    {
        int seed;
        m_currentSeed = seedInput;
        if (!int.TryParse(seedInput, out seed)) { seed = 0; }
        
        if (seed != 0) { m_random = new System.Random(seed); } //Set the seed if it's not 0.
        else
        {
            m_random = new System.Random();
            seed = m_random.Next(1000000, 9999999);
            m_random = new System.Random(seed); //Allows for a seed value to recreate what is shown.
        } //It will be clearly stated that 0 will create a random seed.
        return seed;
    }
}
