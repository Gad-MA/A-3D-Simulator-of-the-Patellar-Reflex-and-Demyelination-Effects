from HH_Computational_Model import computional_model, dt
from helpers import toCSV, myelinToggles, plot

myelinToggles = myelinToggles()

simulation_duration = 50
stimulus_initial_time = 0
stimulus_duration = 1
model = computional_model(
    stimulus_initial_time=stimulus_initial_time,
    stimulus_duration=stimulus_duration,
    simulation_duration=simulation_duration,
    isSensoryMylinated=myelinToggles["isSensoryMyelinated"],
    isExtensorMylinated=myelinToggles["isExtensorMyelinated"],
    isInhibitorMylinated=myelinToggles["isInhibitoryMyelinated"],
    isFlexorMylinated=myelinToggles["isFlexorMyelinated"],
)

peaks = {
    "sensory": -65,
    "extensor": -65,
    "inhibitory": -65,
    "flexor": -65,
}

for i in range(int(simulation_duration / dt)):
    for neuron in model:
        if model[neuron][i] > peaks[neuron]:
            peaks[neuron] = model[neuron][i]

toCSV(
    "neurons_volages.csv",
    model["sensory"],
    model["extensor"],
    model["inhibitory"],
    model["flexor"],
)

toCSV(
    "peaks.csv",
    [peaks["sensory"]],
    [peaks["extensor"]],
    [peaks["inhibitory"]],
    [peaks["flexor"]],
)

plot(stimulus_initial_time, stimulus_duration, simulation_duration,model, True)