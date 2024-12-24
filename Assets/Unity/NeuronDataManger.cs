using System.IO;
using UnityEngine;

public class NeuronDataManager
{
    private string basePath;

    public NeuronDataManager(string basePath)
    {
        this.basePath = basePath;
    }

    public void SaveConfiguration(string filePath, NeuronConfiguration config)
    {
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(filePath, json);
    }

    public float[][] LoadNeuronData(string neuronType)
    {
        string filePath = Path.Combine(basePath, $"{neuronType}.csv");
        if (!File.Exists(filePath))
            return null;

        string[] lines = File.ReadAllLines(filePath);
        float[][] data = new float[lines.Length][];

        for (int i = 0; i < lines.Length; i++)
        {
            string[] tokens = lines[i].Split(',');
            data[i] = System.Array.ConvertAll(tokens, float.Parse);
        }

        return data;
    }
}

[System.Serializable]
public class NeuronConfiguration
{
    public bool[] MyelinStates;
    public float[] Speeds;
}
