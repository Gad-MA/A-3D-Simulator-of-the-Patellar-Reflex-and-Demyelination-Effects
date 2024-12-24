from helpers import plot
from HH_Computational_Model import computional_model

stimulus_initial_time = 10
stimulus_duration = 1
simulation_duration = 50
model = computional_model(
    stimulus_initial_time,
    stimulus_duration,
    simulation_duration,
    isSensoryMylinated=1,
    isExtensorMylinated=1,
    isInhibitorMylinated=1,
    isFlexorMylinated=1,
)

plot(stimulus_initial_time, stimulus_duration, simulation_duration,model)