using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class ActionPotential : MonoBehaviour
{
    public Transform[] sensoryPoints;
    public Transform[] extensorPoints;
    public Transform[] inhibitoryPoints;
    public Transform[] flexorPoints;

    public GameObject[] actionPotentialObjects; // 0: Sensory, 1: Extensor, 2: Inhibitory, 3: Flexor
    public GameObject[] myelinSheaths;
    public GameObject sensoryNeuron;

    public Toggle[] Toggles;

    public GameObject legObject;
    public Toggle legToggle;

    public TextMeshProUGUI info;

    public Material flexorActiveMaterial;
    public Material flexorInactiveMaterial;

    public Volume somaVolume;
    Bloom somaBloom;

    public Volume actionPotentialVolume;
    Bloom actionPotentialBloom;

    public HingeJoint kneeJoint;
    JointSpring jointSpring;

    public Button overLappingGraphButton;
    public Button seperatedGraphsButton;

    public float reflexSpeed = 44f;
    float originalBloomIntensity = 50f;
    float baseSpeed = 4;

    float[][] neuronData;
    float[] peaks;

    string jsonFilePath;

    bool isProcessing = false;
    bool isSimulationStarted = false;
    bool reflexTriggered = false;

    void Start()
    {
        jsonFilePath = Path.Combine(Application.dataPath, "../ExternalData", "myelinToggles.json");

        for (int i = 0; i < Toggles.Length; i++)
        {
            int index = i;
            Toggles[i].onValueChanged.AddListener((isOn) => ToggleMyelinSheath(index, isOn));
        }

        if (somaVolume != null && somaVolume.profile != null)
        {
            if (somaVolume.profile.TryGet(out somaBloom))
                UnityEngine.Debug.Log("Bloom found");
            else
                UnityEngine.Debug.LogError("Null Bloom profile");
        }

        if (actionPotentialVolume != null && actionPotentialVolume.profile != null)
        {
            if (actionPotentialVolume.profile.TryGet(out actionPotentialBloom))
                UnityEngine.Debug.Log("Action potential bloom found");
            else
                UnityEngine.Debug.LogError("Null Bloom profile for action potential");
        }

        jointSpring = kneeJoint.spring;
        jointSpring.targetPosition = 0f;
        kneeJoint.spring = jointSpring;
    }

    public void ShowOverLappingGraph() => Process.Start(new ProcessStartInfo(Path.Combine(Application.dataPath, "../ExternalData", "overlapping_graph.png")) { UseShellExecute = true });

    public void ShowSepratedGraphs() => Process.Start(new ProcessStartInfo(Path.Combine(Application.dataPath, "../ExternalData", "separated_graphs.png")) { UseShellExecute = true });

    public void ShowLeg()
    {
        if (legToggle.isOn)
        {
            legObject.SetActive(true);
            sensoryNeuron.transform.localScale = new Vector3(-0.0006f, -0.0006f, 0.0006f);
        }
        else
        {
            legObject.SetActive(false);
            // sensoryNeuron.transform.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
        }
    }

    public void ToggleMyelinSheath(int index, bool isActive)
    {
        myelinSheaths[index].SetActive(isActive);
        SaveStates();
    }

    public void SaveStates()
    {
        ToggleStates toggleStates = new ToggleStates
        {
            SensoryMyelination = Toggles[0].isOn ? 1 : 0,
            ExtensorMyelination = Toggles[1].isOn ? 1 : 0,
            InhibitoryMyelination = Toggles[2].isOn ? 1 : 0,
            FlexorMyelination = Toggles[3].isOn ? 1 : 0
        };

        string json = JsonUtility.ToJson(toggleStates, true);
        File.WriteAllText(jsonFilePath, json);
    }

    public void RunPythonScript()
    {
        SaveStates();

        string pythonScriptPath = "ExternalData/HH_Unity.py";
        string arguments = $"\"{jsonFilePath}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = "python",
            Arguments = $"{pythonScriptPath} {arguments}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
    }

    public void StartSimulation()
    {
        RunPythonScript();

        isSimulationStarted = true;
        neuronData = LoadNeuronData(Path.Combine("ExternalData", "neurons_volages.csv"));
        peaks = LoadNeuronPeaks(Path.Combine("ExternalData", "peaks.csv"));

        if (neuronData == null)
        {
            info.text = "Error loading data!";
            info.color = Color.red;

            return;
        }

        LoadSprite(Path.Combine(Application.dataPath, "../ExternalData", "overlapping_graph.png"), overLappingGraphButton);
        LoadSprite(Path.Combine(Application.dataPath, "../ExternalData", "separated_graphs.png"), seperatedGraphsButton);
        info.text = "Simulation ready. Press the spacebar to simulate knee tap.";
        info.color = Color.green;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

        if (!isSimulationStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                info.text = "Please make sure you pressed the start button first.";
                info.color = Color.red;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !isProcessing)
        {
            info.enabled = false;
            isProcessing = true;
            StartCoroutine(ProcessAllTimeSteps());
        }
    }

    void TriggerKneeReflex(float targetPosition)
    {
        if (reflexTriggered) return;

        reflexTriggered = true;
        jointSpring.targetPosition = targetPosition;
        kneeJoint.spring = jointSpring;

        StartCoroutine(ResetKneeReflex());// we need to reset the leg after it being triggered
    }

    void AdjustBloomIntensity(int timeStep)
    {
        if (somaBloom == null || neuronData == null) return;

        float maxIntensityAtTimestep = 0f;
        for (int neuronIndex = 0; neuronIndex < neuronData.Length; neuronIndex++)
            maxIntensityAtTimestep = Mathf.Max(maxIntensityAtTimestep, neuronData[neuronIndex][timeStep]);

        float mappedIntensity = Mathf.Clamp(maxIntensityAtTimestep / 10f, 0f, 100f);

        somaBloom.intensity.value = mappedIntensity;
    }

    void AdjustBloomIntensity(float originalIntensity, float progress)
    {
        if (actionPotentialBloom != null)
        {
            float attenuatedIntensity = Mathf.Lerp(Mathf.Abs(originalIntensity), 0f, progress);
            actionPotentialBloom.intensity.value = attenuatedIntensity;
        }
    }

    void LoadSprite(string filePath, Button button)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        button.image.sprite = newSprite;
    }

    float[] LoadNeuronPeaks(string filePath)
    {
        try
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);

            float[] peaks = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                if (float.TryParse(lines[i], out float peak))
                    peaks[i] = peak;

                else
                    return null;
            }

            return peaks;
        }

        catch (System.Exception)
        {
            return null;
        }
    }

    float[][] LoadNeuronData(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            int rows = lines.Length;
            int columns = lines[0].Split(',').Length;

            float[][] data = new float[rows][];
            for (int i = 0; i < rows; i++)
            {
                string[] values = lines[i].Split(',');
                data[i] = System.Array.ConvertAll(values, float.Parse);
            }

            return data;
        }
        catch (System.Exception ex)
        {
            info.text = "Error reading CSV file: " + ex.Message;
            info.color = Color.red;
            return null;
        }
    }

    Transform[] GetNeuronPoints(int neuronIndex)
    {
        switch (neuronIndex)
        {
            case 0: return sensoryPoints;

            case 1: return extensorPoints;

            case 2: return inhibitoryPoints;

            case 3: return flexorPoints;

            default:
                return null;
        }
    }

    Transform[] GetPartialNeuronPoints(int neuronIndex)
    {
        switch (neuronIndex)
        {
            case 0:
                return sensoryPoints.Take(sensoryPoints.Length / 2).ToArray();

            case 1:
                return extensorPoints.Take(extensorPoints.Length / 2).ToArray();

            case 2:
                return inhibitoryPoints.Take(inhibitoryPoints.Length / 2).ToArray();

            case 3:
                return flexorPoints.Take(flexorPoints.Length / 2).ToArray();

            default:
                return new Transform[0];
        }
    }

    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Create a smoother curve by adjusting control points
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        // Apply a factor to the control points to increase the curvature
        Vector3 p = uuu * p0; // Start point influence
        p += 3f * uu * t * p1; // First control point influence
        p += 3f * u * tt * p2; // Second control point influence
        p += ttt * p3; // End point influence

        return p;
    }

    IEnumerator ProcessAllTimeSteps()
    {
        if (neuronData == null || peaks == null)
            yield break;

        int neuronCount = neuronData.Length;
        float[] maxIntensities = new float[neuronCount];
        bool[] hasActionPotentialTriggered = new bool[neuronCount];
        bool[] hasProcessed = new bool[neuronCount];

        for (int neuronIndex = 0; neuronIndex < neuronCount; neuronIndex++)
            maxIntensities[neuronIndex] = neuronData[neuronIndex].Max();

        int timeSteps = neuronData[0].Length;

        for (int timeStep = 0; timeStep < timeSteps; timeStep++)
        {
            AdjustBloomIntensity(timeStep);
            List<Coroutine> runningCoroutines = new List<Coroutine>();

            for (int neuronIndex = 0; neuronIndex < neuronCount; neuronIndex++)
            {
                if (hasProcessed[neuronIndex]) continue;

                float intensity = neuronData[neuronIndex][timeStep];
                float maxIntensity = maxIntensities[neuronIndex];

                if (Mathf.Abs(intensity - maxIntensity) < 0.01f)
                {
                    hasActionPotentialTriggered[neuronIndex] = true;
                    hasProcessed[neuronIndex] = true;

                    if (maxIntensity < -61)

                        UnityEngine.Debug.Log($"Neuron at {neuronIndex}: Below threshold, no action potential will be triggered.");

                    else if (maxIntensity >= -61 && maxIntensity < 47) //attenuated signal
                    {
                        Coroutine neuronCoroutine = StartCoroutine(TriggerNeuronAttenuated(neuronIndex, GetPartialNeuronPoints(neuronIndex), intensity, baseSpeed * 0.25f));
                        runningCoroutines.Add(neuronCoroutine);

                        // Trigger knee reflex based on neuron
                        if (neuronIndex == 1) TriggerKneeReflex(16f);
                        else if (neuronIndex == 3) TriggerKneeReflex(-16f);
                    }
                    else if (maxIntensity >= 47)// full signal
                    {
                        Coroutine neuronCoroutine = StartCoroutine(TriggerNeuron(neuronIndex, GetNeuronPoints(neuronIndex), intensity, baseSpeed));
                        runningCoroutines.Add(neuronCoroutine);

                        if (neuronIndex == 1) TriggerKneeReflex(120f);
                        else if (neuronIndex == 3) TriggerKneeReflex(-120f);
                    }
                }
            }

            foreach (var coroutine in runningCoroutines)
                yield return coroutine;

            if (hasActionPotentialTriggered.All(triggered => triggered))
                break;

            yield return new WaitForSeconds(0.01f);
        }

        isProcessing = false;
        info.enabled = true;
        info.text = "Simulation complete.";
        info.color = Color.green;
    }

    IEnumerator TriggerNeuron(int neuronIndex, Transform[] points, float intensity, float speed, bool useInactiveMaterial = false)
    {
        GameObject actionPotential = actionPotentialObjects[neuronIndex];
        Renderer renderer = actionPotential.GetComponent<Renderer>();

        if (renderer != null)
        {
            if (neuronIndex == 3)
            {
                Material materialToAssign = useInactiveMaterial
                    ? flexorInactiveMaterial
                    : (intensity > 0f ? flexorActiveMaterial : flexorInactiveMaterial);
                renderer.material = materialToAssign;
            }
        }

        if (intensity <= 0f && !useInactiveMaterial)
            yield break;

        float t = 0f;

        actionPotential.SetActive(true);
        actionPotential.transform.position = points[0].position;

        while (t <= 1f)
        {
            t += Time.deltaTime * speed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 newPos = CalculateCubicBezierPoint(smoothT, points[0].position, points[1].position, points[2].position, points[3].position);
            actionPotential.transform.position = newPos;

            AdjustBloomIntensity(originalBloomIntensity, smoothT);

            yield return null;
        }

        AdjustBloomIntensity(originalBloomIntensity, 1f);

        actionPotential.SetActive(false);
    }

    IEnumerator TriggerNeuronAttenuated(int neuronIndex, Transform[] points, float intensity, float speed)
    {
        GameObject actionPotential = actionPotentialObjects[neuronIndex];

        actionPotential.SetActive(true);
        actionPotential.transform.position = points[0].position;

        float t = 0f;

        Vector3 startPosition = points[0].position;
        Vector3 endPosition = points[1].position;

        while (t <= 1f)
        {
            t += Time.deltaTime * speed;
            float smoothT = SmoothEaseInOut(t);

            actionPotential.transform.position = Vector3.Lerp(startPosition, endPosition, smoothT);

            AdjustBloomIntensity(intensity, smoothT);

            yield return null;
        }
        actionPotential.SetActive(false);
    }

    float SmoothEaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    IEnumerator ResetKneeReflex()
    {
        while (Mathf.Abs(jointSpring.targetPosition) > 0.01f)
        {
            jointSpring.targetPosition = Mathf.Lerp(jointSpring.targetPosition, 0, reflexSpeed * Time.deltaTime);
            kneeJoint.spring = jointSpring;
            yield return null;
        }
        jointSpring.targetPosition = 0f;
        kneeJoint.spring = jointSpring;
        reflexTriggered = false;
    }
}

[System.Serializable]
public class ToggleStates
{
    public int SensoryMyelination;
    public int ExtensorMyelination;
    public int InhibitoryMyelination;
    public int FlexorMyelination;
}