
# Touchscreen Chamber Unity Project

This Unity-based project, developed by the Mohajerani Lab, is designed for operant conditioning experiments with touchscreen-enabled behavioral studies in rodents. The Unity application interfaces with a Raspberry Pi running the [PiController](https://github.com/AmirHoseinMazrooei/PiController) project to enable seamless control of connected hardware, including stimuli presentation and reward dispensing.

The experimental results for the paper _Assessing Cognitive Flexibility in Mice Using a Custom-Built Touchscreen Chamber (Pais et al., 2024)_ can be found in the [ExperimentResults](/ExperimentResults/) folder.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [PiController Integration](#picontroller-integration)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

## Overview

The **Touchscreen Chamber Unity Project** operates as the central software interface for conducting experiments. This project leverages a PI controller on a Raspberry Pi for hardware control, allowing the Unity application to control and monitor various peripherals connected to the Raspberry Pi (e.g., touchscreens, reward dispensers). When deployed together on the same network, this Unity project can communicate with the **PiController** to execute experimental protocols with precise control over all hardware components.

## Features

- **Experimental Protocols**: Configurable task protocols with Unity-based interfaces.
- **Real-Time Hardware Control**: Integrates with Raspberry Pi to control peripherals.
- **Data Collection**: Records data from experiments, including timestamps and task-specific metrics.
- **Remote Control**: Unity app communicates with PiController over a network for real-time feedback and control.
- **Modular Design**: Extendable structure for implementing custom tasks and hardware components.

## Requirements

- **Unity Version**: 2021.3.8f1
- **Hardware**: Raspberry Pi with connected peripherals (e.g., touchscreen, sensors, reward dispensers).
- **Network**: Both the Unity application and Raspberry Pi should be connected to the same network for communication.
- **Tablet**: Tested on Samsung Galaxy Tab A 8.0 SM-T350.
- **PiController**: Set up the [PiController repository](https://github.com/AmirHoseinMazrooei/PiController) on a Raspberry Pi.

## Installation

You have two options to set up the application:

### Option 1: Clone and Build

1. Clone this repository:
   ```bash
   git clone https://github.com/Mohajerani-Lab/touchscreen-chamber-unity.git
   ```

2. Open this project in Unity 2021.3.8f1.

3. Make any necessary changes to the project to suit your experimental requirements.

4. Build the project as an APK file for Android deployment. Go to **File > Build Settings**, select **Android**, and then **Build**.

5. Deploy the APK to a compatible tablet. This project has been tested on a **Samsung Galaxy Tab A 8.0 SM-T350**.

### Option 2: Download the Pre-built APK

1. Download the latest APK from the [releases page](https://github.com/Mohajerani-Lab/touchscreen-chamber-unity/releases/latest).

2. Transfer the APK to your tablet and install it.

3. Download the configurations and sample date from the [latest release](https://github.com/Mohajerani-Lab/touchscreen-chamber-unity/releases/latest/download/touchscreen-chamber-data.zip).

4. Put the content of the extracted file of the previous step at `/storage/emulated/0/TouchScreen-Trial-Game` path of the tablet.

5. Ensure the Unity app and PiController are both connected to the same network to enable communication.

## PiController Integration

The Unity application communicates with the **PiController** on the Raspberry Pi to manage hardware components. This setup enables the Unity app to issue commands to the Pi for real-time control of devices like reward dispensers or sensors. The PiController project serves as a bridge to control and monitor hardware directly.

## Usage

1. **Setup the Chamber Configuration**: Adjust experiment parameters in Unity.

2. **Connect to PiController**: Ensure the Unity app can connect to the PiController over the network.

3. **Run Experiment**: Press **Play** in Unity to initiate the experiment. Unity will send commands to the PiController for real-time control of peripherals.

4. **Data Collection and Export**: Experimental data is saved within Unity’s designated output folder.

## Project Structure

- `Assets/`
  - All Unity assets, scripts, scenes, prefabs, and UI components.
- `Scripts/`
  - C# scripts for experiment logic, hardware communication, and data handling.
- `Scenes/`
  - Contains Unity scenes for different experimental setups.
- `Resources/`
  - Stimuli images and sound files.

## Contributing

Contributions are welcome. Follow these steps:

1. Fork this repository.
2. Create a branch (`git checkout -b feature-name`).
3. Commit changes (`git push origin feature-name`).
4. Open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

For more information, please contact the [Mohajerani Lab](mailto:info@mohajeranilab.org).
