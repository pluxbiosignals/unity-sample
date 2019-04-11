# PLUX Unity API \[Windows] | Sample APP
The Sample APP contained inside this repository provides a practical and intuitive resource to explore the integration of the signal acquisition devices
commercialized by PLUX ([**biosignalsplux**](https://www.biosignalsplux.com/en/), [**BITalino**](https://bitalino.com/en/)...) with the powerful 
development environment of **Unity**.

Some questions can be enlightened, such as:

+	How PLUX acquisition systems can be searched and found on the user's computer?
+	How to pair a computer with a PLUX device?
+	How to customize signal acquisition parameters, such as sampling rate and Analog to Digital Converter (ADC) resolution?
+	How to start a real-time acquisition?
+	How to collect and store the acquired real-time physiological data?
+	How to stop the real-time acquisition?

## Supported Platforms

+	Microsoft Windows \[32 and 64 bits]

## Limitations

+	Currently, Bluetooth Low Energy (BLE) devices are not yet supported by the Sample APP, however, they can be accessed through the API.

## How to Use It

The provided project folder is a ready to use Unity solution, you simply need to access the cloned folder through Unity interface.

On the following animation we can quickly show the main functionalities of our Sample APP:

![sample-app-animation.gif](https://i.postimg.cc/7YsBQ8jf/sample-app-animation.gif)

## How to Easily Integrate the API in your Unity Project?
1.	Copy the plux_unity_interface.dll to the **Plugins** folder of your Unity project. The location of this file will depend on the desired Operating System Architecture (x86 or x86_64): plux-api-unity-sample/Assets/Plugins/x86/plux_unity_interface.dll **\[32 bits]** or plux-api-unity-sample/Assets/Plugins/x86_64/plux_unity_interface.dll **\[64 bits]**
2.	Copy the PluxDeviceManager.cs script (a C\# interface to PLUX API) to the **Scripts** folder of your Unity project. In this case the file location is: plux-api-unity-sample/Assets/Scripts/PluxDeviceManager.cs

After the previous two steps, you will be fully prepared to expand your Unity project and include some interesting functionalities through the use of PLUX signal acquisition devices.

For this purpose, you simply need to invoke the available [Methods](#Methods).

# Unity API

## Methods

-	[PluxDeviceManager.PluxDev](#PluxDev)
-	[PluxDeviceManager.DisconnectPluxDev](#DisconnectPluxDev)
-	[PluxDeviceManager.StartAcquisitionUnity](#StartAcquisitionUnity)
-	[PluxDeviceManager.StartAcquisitionByNbrUnity](#StartAcquisitionByNbrUnity)
-	[PluxDeviceManager.StartAcquisitionMuscleBanUnity](#StartAcquisitionMuscleBanUnity)
-	[PluxDeviceManager.StopAcquisitionUnity](#StopAcquisitionUnity)
-	[PluxDeviceManager.GetDetectableDevicesUnity](#GetDetectableDevicesUnity)
-	[PluxDeviceManager.SetCallbackHandler](#SetCallbackHandler)
-	[PluxDeviceManager.GetNbrChannelsUnity](#GetNbrChannelsUnity)
-	[PluxDeviceManager.GetBatteryUnity](#GetBatteryUnity)
-	[PluxDeviceManager.GetDeviceTypeUnity](#GetDeviceTypeUnity)

## PluxDev

Method used to establish a connection between PLUX devices and computer. Behaves like an object constructor.
```csharp
void PluxDev(string macAddress)
```

### Description

With `PluxDev` method it is possible to establish a Bluetooth connection between a computer and a PLUX Device. To ensure a successful pairing, the user only need to check the device mac-address (attached to the **BITalino** board, **biosignalsplux** hub...).
This mac-address should be inserted in a string format with the following structure: `"00:07:80:4D:2E:AD"` (6 pairs of characters separated by ":").

### Parameters

+	**macAddress** `string`: Device unique identifier, consisting in a string formed by 6 pairs of characters separated by ":" symbol (total of 17 characters).

## DisconnectPluxDev

Opposing to [PluxDev](#PluxDev), this method is very useful when you want to safely cut the connection between PLUX devices and computer. Behaves like an object destructor.
```csharp
void DisconnectPluxDev(string macAddress)
```

### Description

Through `DisconnectPluxDev` method the established connection between a PLUX device and a personal computer can be securely closed. If a real-time acquisition is yet being executed, a stop command is automatically sent, ensuring the end of communication loop.
Internally, the object created with `PluxDev` is destroyed after invoking `DisconnectPluxDev`.

### Parameters

*Function without input parameters*

## StartAcquisitionUnity

Class method used to Start a Real-Time acquisition at the device paired with the computer through `PluxDev` method.
```csharp
void StartAcquisitionUnity(int samplingRate, List<int> listChannels, int resolution)
```

### Description

The current API version supports real-time acquisitions with analogical sensors, however, to communicate with computer the PLUX device needs to execute an Analogical to Digital Conversion, through data sampling.
Taking ADC stage into consideration, while starting a real-time acquisition, selecting a specific sampling rate and resolution is extremely easy through the inputs of `StartAcquisitionUnity`.

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**listChannels** `List\<int>`: A list where it can be specified the active channels. Each entry contains a port number of an active channel.
+	**resolution** `int`: Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when compared with the ideal real case scenario.

## StartAcquisitionByNbrUnity

Class method used to Start a Real-Time acquisition at the device paired with `PluxDev` method.
```csharp
void StartAcquisitionByNbrUnity(int samplingRate, int numberOfChannel, int resolution)
```

### Description

The current API version supports real-time acquisitions with analogical sensors, however, to communicate with computer the PLUX device needs to execute an Analogical to Digital Conversion, through data sampling.
Taking ADC stage into consideration, while starting a real-time acquisition, selecting a specific sampling rate and resolution is extremely easy through the inputs of `StartAcquisitionByNbrUnity`.
The main difference of this function in comparison with `StartAcquisitionUnity` is the `numberOfChannels` argument, so, instead of specifying a list of active channels you can simply identify the number of channels to be used during the data collection procedure.
Channels are activated in a sequential way, so, if `numberOfChannels` is equal to 3 it means that ports 1 to 3 will collect data.

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**numberOfChannels** `int`: Number of the active channel that will be used during data acquisition. With bitalino this value should be between 1 and 6 while for biosignalsplux it is possible to collect data from up to 8 channels (simultaneously).
+	**resolution** `int`: Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when compared with the ideal real case scenario.

## StartAcquisitionMuscleBanUnity

Class method used to Start a Real-Time acquisition at the device paired with `PluxDev` method (on **muscleBAN**).
```csharp
void StartAcquisitionMuscleBanUnity(int samplingRate, List<int> listChannels, int resolution, int freqDivisor)
```

### Description

The current API version supports real-time acquisitions with analogical sensors, however, to communicate with computer the PLUX device needs to execute an Analogical to Digital Conversion, through data sampling.
Taking ADC stage into consideration, while starting a real-time acquisition, selecting a specific sampling rate and resolution is extremely easy through the inputs of `StartAcquisitionMuscleBanUnity`.
The main difference on the inputs of this function in comparison with `StartAcquisitionUnity`, is the additional `freqDivisor` argument, that transmits the information regarding the subsampling level to be applied during the **muscleBAN** data acquisition.

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**listChannels** `List\<int>`: A list where it can be specified the active channels. Each entry contains a port number of an active channel.
+	**resolution** `int`: Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when compared with the ideal real case scenario.
+	**freqDivisor** `int`: Frequency divisor, i.e., acquired data will be subsampled accordingly to this parameter. If freqDivisor = 10, it means that each set of 10 acquired samples will trigger the communication of a single sample (through the communication loop).

## StopAcquisitionUnity

Class method used to interrupt the real-time communication loop triggered by `StartAcquisitionUnity()`, `StartAcquisitionByNbrUnity()` or `StartAcquisitionMuscleBanUnity()` methods.
```csharp
void StopAcquisitionUnity()
```

### Description

After starting a real-time acquisition a communication loop between the PLUX device and the computer starts running on a parallel thread.
With `StopAcquisitionUnity()` function, first of all, the communication loop is interrupted, task followed by the stop of  the real-time acquisition and finally the thread closure.

### Parameters

*Function without input parameters*

## GetDetectableDevicesUnity

Class method intended to find the list of detectable devices through Bluetooth communication.
```csharp
List<string> GetDetectableDevicesUnity(string domain)
```

### Description

Internally the list of detected devices reaches `PluxDeviceManager.cs` in a string format, where each device mac-address is separated from each other by a "&" character.
In `GetDetectableDevicesUnity` an automatic split of the received string took place, ensuring that a list with the available mac-addresses will be returned.

### Parameters

+	**domain** `string`: String that defines which domain will be used while searching for PLUX devices \[Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy; "USB" -> Through USB connection cable]

## SetCallbackHandler

"Set" method which ensures the update of an internal class variable. Essentially this variable (`callbackPointer`) will be a pointer to the callback function that will deliver the acquired data samples to the Unity project.
```csharp
bool SetCallbackHandler(FPtr callbackHandler)
```

### Description

After defining a callback function on your project, respecting the original specifications (inputs and outputs: `bool <callbackName>(int data, int channel)`), the user should invoke `SetCallbackHandler` method to update the function pointer inside `PluxDeviceManager` class.
Inside the callback function data will be successively reaching in packages containing a sample value (`data`) and the respective channel to which it belongs (`channel`).

### Parameters

+	**callbackHandler** `FPtr`: A function pointer to the callback that will receive the acquired data samples on the Unity side.

## GetNbrChannelsUnity

"Getting" method used for determination of the number of used channels during the real-time acquisition.
```csharp
int GetNbrChannelsUnity()
```

### Description

A simple method that returns the number of samples being used on the real-time acquisition under execution.

### Parameters

*Function without input parameters*

## GetBatteryUnity

"Getting" method dedicated to check the battery level of the device.
```csharp
int GetBatteryUnity()
```

### Description

Having a feedback about the energy level of electronic devices is extremely important and useful. With this method an integer with the percentual (0-100%) battery level is returned.

### Parameters

*Function without input parameters*

## GetDeviceTypeUnity

"Getting" method intended to check the type of the PLUX device connected with computer.
```csharp
string GetDeviceTypeUnity()
```

### Description

With this method a string is retrieved, identifying which type of PLUX device is connected with the computer.
The possible values are `"biosignalsplux"`, `"BITalino"` or `"MuscleBAN BE Plux"`

### Parameters

*Function without input parameters*

## Support
If you find any problem during your experience, please, feel free to create a new issue track on our repository.
We will be very glad to guide you in this amazing journey and listen your suggestions!