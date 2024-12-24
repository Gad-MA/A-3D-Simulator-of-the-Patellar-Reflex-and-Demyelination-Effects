import csv
from matplotlib import pyplot as plt
from HH_Computational_Model import I_stim, dt
import numpy as np
import json

def myelinToggles():
    with open('ExternalData/myelinToggles.json', 'r') as file:
        data = json.load(file)
        return {
            "isSensoryMyelinated": data['SensoryMyelination'],
            "isExtensorMyelinated": data['ExtensorMyelination'],
            "isInhibitoryMyelinated": data['InhibitoryMyelination'],
            "isFlexorMyelinated": data['FlexorMyelination'],
        }

def toCSV(output_name,l1, l2, l3, l4):
    with open("ExternalData/"+output_name, "w", newline="") as file:
        writer = csv.writer(file)
        writer.writerows([l1, l2, l3, l4])


def plot(stimulus_initial_time, stimulus_duration, simulation_duration, model, saveGraphs=False):
    """
    Plotting separated
    """
    t = np.arange(0, simulation_duration, dt)
    # Plot results
    plt.figure(figsize=(12, 10))

    # Stimulus
    plt.subplot(5, 1, 1)
    plt.plot(
        t,
        [I_stim(ti, stimulus_initial_time, stimulus_duration) for ti in t],
        "k",
        label="Stimulus",
    )
    plt.ylabel("Current\n(μA/cm²)")
    plt.title("Knee-jerk Reflex Simulation")
    plt.grid()
    plt.legend()

    # Sensory neuron
    plt.subplot(5, 1, 2)
    plt.plot(t, model["sensory"], "b", label="Sensory")
    plt.ylabel("Voltage (mV)")
    plt.grid()
    plt.legend()

    # Extensor motor neuron
    plt.subplot(5, 1, 3)
    plt.plot(t, model["extensor"], "g", label="Extensor")
    plt.ylabel("Voltage (mV)")
    plt.grid()
    plt.legend()

    # Inhibitory interneuron
    plt.subplot(5, 1, 4)
    plt.plot(t, model["inhibitory"], "r", label="Inhibitory")
    plt.ylabel("Voltage (mV)")
    plt.grid()
    plt.legend()

    # Flexor motor neuron
    plt.subplot(5, 1, 5)
    plt.plot(t, model["flexor"], "purple", label="Flexor")
    plt.xlabel("Time (ms)")
    plt.grid()
    plt.ylabel("Voltage (mV)")
    plt.legend()
    if(saveGraphs):
        plt.savefig('ExternalData/separated_graphs.png', bbox_inches='tight')

    """
    Plotting overlapping
    """
    # Plot results
    plt.figure(figsize=(12, 10))

    # Stimulus
    plt.subplot(2, 1, 1)
    plt.plot(
        t,
        [I_stim(ti, stimulus_initial_time, stimulus_duration) for ti in t],
        "k",
        label="Stimulus",
    )
    plt.ylabel("Current\n(μA/cm²)")
    plt.title("Knee-jerk Reflex Simulation")
    plt.grid()
    plt.legend()

    # Sensory neuron
    plt.subplot(2, 1, 2)
    plt.plot(t, model["sensory"], "b", label="Sensory")
    plt.plot(t, model["extensor"], "g", label="Extensor")
    plt.plot(t, model["inhibitory"], "r", label="Inhibitory")
    plt.plot(t, model["flexor"], "purple", label="Flexor")
    plt.xlabel("Time (ms)")
    plt.ylabel("Voltage (mV)")
    plt.grid()
    plt.legend()


    plt.tight_layout()
    if(saveGraphs):
        plt.savefig('ExternalData/overlapping_graph.png', bbox_inches='tight')
    else:
        plt.show()