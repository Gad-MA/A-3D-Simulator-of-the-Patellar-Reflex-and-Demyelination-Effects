import numpy as np
from scipy.integrate import odeint

# Hodgkin-Huxley Parameters
Cm = 1  # in microF/cm
Cm_demylinated = 7  # in microF/cm
g_Na, g_K, g_L = 120, 36, 0.3
E_Na, E_K, E_L = 60, -88, -54.387

# Synaptic parameters
g_syn_ex = 0.5  # excitatory synaptic conductance
g_syn_in = 1.0  # inhibitory synaptic conductance
E_syn_ex = 0  # excitatory reversal potential
E_syn_in = -80  # inhibitory reversal potential
tau_syn = 2.0  # synaptic time constant
alpha_r = 0.5  # Rate constant for channel opening
beta_r = 0.1  # Rate constant for channel closing
threshold = -20  # Presynaptic spike threshold


# Gate dynamics
def alpha_n(V):
    return 0.01 * (V + 55) / (1 - np.exp(-(V + 55) / 10))


def beta_n(V):
    return 0.125 * np.exp(-(V + 65) / 80)


def alpha_m(V):
    return 0.1 * (V + 40) / (1 - np.exp(-(V + 40) / 10))


def beta_m(V):
    return 4 * np.exp(-(V + 65) / 18)


def alpha_h(V):
    return 0.07 * np.exp(-(V + 65) / 20)


def beta_h(V):
    return 1 / (1 + np.exp(-(V + 35) / 10))


# Ionic currents
def I_Na(V, m, h):
    return g_Na * m**3 * h * (V - E_Na)


def I_K(V, n):
    return g_K * n**4 * (V - E_K)


def I_L(V):
    return g_L * (V - E_L)


def T(V_pre):
    return 1 if V_pre > threshold else 0


def synaptic_current(V_pre, V_post, g_syn, E_syn, r):
    return g_syn * r * (E_syn - V_post)


def dr_dt(r, V_pre):
    return alpha_r * T(V_pre) * (1 - r) - beta_r * r


# Stimulus function (tap to the knee)
def I_stim(t, stimulus_initial_time, stimulus_duration):
    return (
        40
        if stimulus_initial_time <= t < (stimulus_initial_time + stimulus_duration)
        else 0
    )


def noise_current(V_sens):
    return 0.3 * g_L * (E_L - V_sens)

dt = 0.01

def computional_model(stimulus_initial_time, stimulus_duration, simulation_duration, isSensoryMylinated, isExtensorMylinated, isInhibitorMylinated, isFlexorMylinated):

    # Combined dynamics for all 4 neurons
    def dSystem_dt(X, t):
        # Unpack state variables
        V_sens, m_sens, h_sens, n_sens = X[0:4]
        V_ext, m_ext, h_ext, n_ext = X[4:8]
        V_inh, m_inh, h_inh, n_inh = X[8:12]
        V_flex, m_flex, h_flex, n_flex = X[12:16]
        r_sens_ext, r_sens_inh, r_inh_flex = X[16:19]  # Gating variables

        # Synaptic currents
        I_sens_ext = synaptic_current(V_sens, V_ext, g_syn_ex, E_syn_ex, r_sens_ext)
        I_sens_inh = synaptic_current(V_sens, V_inh, g_syn_ex, E_syn_ex, r_sens_inh)
        I_inh_flex = synaptic_current(V_inh, V_flex, g_syn_in, E_syn_in, r_inh_flex)

        # Dynamics of gating variables
        dr_sens_ext = dr_dt(r_sens_ext, V_sens)
        dr_sens_inh = dr_dt(r_sens_inh, V_sens)
        dr_inh_flex = dr_dt(r_inh_flex, V_inh)

        # Sensory neuron dynamics
        dV_sens = (
            I_stim(t, stimulus_initial_time, stimulus_duration)
            - I_Na(V_sens, m_sens, h_sens)
            - I_K(V_sens, n_sens)
            - I_L(V_sens)
        ) / (Cm if isSensoryMylinated else Cm_demylinated)
        dm_sens = alpha_m(V_sens) * (1 - m_sens) - beta_m(V_sens) * m_sens
        dh_sens = alpha_h(V_sens) * (1 - h_sens) - beta_h(V_sens) * h_sens
        dn_sens = alpha_n(V_sens) * (1 - n_sens) - beta_n(V_sens) * n_sens

        # Extensor motor neuron dynamics
        dV_ext = (
            noise_current(V_sens)
            + I_sens_ext
            - I_Na(V_ext, m_ext, h_ext)
            - I_K(V_ext, n_ext)
            - I_L(V_ext)
        ) / (Cm if isExtensorMylinated else Cm_demylinated)
        dm_ext = alpha_m(V_ext) * (1 - m_ext) - beta_m(V_ext) * m_ext
        dh_ext = alpha_h(V_ext) * (1 - h_ext) - beta_h(V_ext) * h_ext
        dn_ext = alpha_n(V_ext) * (1 - n_ext) - beta_n(V_ext) * n_ext

        # Inhibitory interneuron dynamics
        dV_inh = (
            noise_current(V_sens)
            + I_sens_inh
            - I_Na(V_inh, m_inh, h_inh)
            - I_K(V_inh, n_inh)
            - I_L(V_inh)
        ) / (Cm if isInhibitorMylinated else Cm_demylinated)
        dm_inh = alpha_m(V_inh) * (1 - m_inh) - beta_m(V_inh) * m_inh
        dh_inh = alpha_h(V_inh) * (1 - h_inh) - beta_h(V_inh) * h_inh
        dn_inh = alpha_n(V_inh) * (1 - n_inh) - beta_n(V_inh) * n_inh

        # Flexor motor neuron dynamics
        dV_flex = (
            noise_current(V_sens)
            + I_inh_flex
            - I_Na(V_flex, m_flex, h_flex)
            - I_K(V_flex, n_flex)
            - I_L(V_flex)
        ) / (Cm if isFlexorMylinated else Cm_demylinated)
        dm_flex = alpha_m(V_flex) * (1 - m_flex) - beta_m(V_flex) * m_flex
        dh_flex = alpha_h(V_flex) * (1 - h_flex) - beta_h(V_flex) * h_flex
        dn_flex = alpha_n(V_flex) * (1 - n_flex) - beta_n(V_flex) * n_flex

        return [
            dV_sens,
            dm_sens,
            dh_sens,
            dn_sens,
            dV_ext,
            dm_ext,
            dh_ext,
            dn_ext,
            dV_inh,
            dm_inh,
            dh_inh,
            dn_inh,
            dV_flex,
            dm_flex,
            dh_flex,
            dn_flex,
            dr_sens_ext,
            dr_sens_inh,
            dr_inh_flex,
        ]

    # Time vector
    t = np.arange(0, simulation_duration, dt)

    # Initial conditions for all neurons (resting state)
    X0 = np.array([-65, 0.05, 0.6, 0.32] * 4 + [0.0, 0.0, 0.0])

    # Solve the system
    X = odeint(dSystem_dt, X0, t)

    return {
        "sensory": X[:, 0],
        "extensor": X[:, 4],
        "inhibitory": X[:, 8],
        "flexor": X[:, 12],
    }

