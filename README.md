# PLUX Unity API \[Windows] | Sample APP
The **Sample APP** contained inside this repository provides a practical and intuitive resource to explore the integration of the signal acquisition devices
commercialized by **PLUX** ([**biosignalsplux**](https://www.biosignalsplux.com/en/):warning:, [**BITalino**](https://bitalino.com/en/)...) with the powerful 
development environment of **Unity**.

> :warning: **Note:** Despite being part of the **biosignalsplux** family, the [**fNIRS Explorer**](https://plux.info/biosignalsplux-wearables/497-fnirs-explorer-820201221.html) is not currently supported by the **Unity API**. Compatibility issues will be solved in a short-future!

Some questions can be enlightened, such as:

+	How PLUX acquisition systems can be searched and found on the user's computer?
+	How to pair a computer with a PLUX device?
+	How to customize signal acquisition parameters, such as sampling rate and Analog to Digital Converter (ADC) resolution?
+	How to start a real-time acquisition?
+	How to collect and store the acquired real-time physiological data?
+	How to stop the real-time acquisition?

## Supported Platforms

+	Microsoft Windows \[32 and 64 bits]

## Tested Script Runtime Versions

+	Microsoft .NET Framework 3.5
+	Microsoft .NET Framework 4.0

## Limitations

+	Currently, **Bluetooth Low Energy** (BLE) devices are supported by the **Sample APP**, however, you should use the approved [**BLE dongle D 112**](https://plux.info/accessories/207-bitalino-proven-ble-dongle-810121502.html?search_query=ble+dongle&results=38).

## Auxiliary Packages

**UnityThreadHelper** | https://assetstore.unity.com/packages/tools/unitythreadhelper-3847

**Code Monkey Utils** | https://www.youtube.com/playlist?list=PLzDRvYVwl53v5ur4GluoabyckImZz3TVQ

## How to Use It

The provided project folder is a ready to use Unity solution, you simply need to access the cloned folder through Unity interface.

On the following animation we can quickly show the main functionalities of our **Sample APP**:

![sample-app-animation.gif](https://i.postimg.cc/7YsBQ8jf/sample-app-animation.gif)

## How to Easily Integrate the API in your Unity Project?
1.	Copy the **plux_unity_interface.dll** to the **Plugins** folder of your Unity project. The location of this file will depend on the desired Operating System Architecture (x86 or x86_64): `plux-api-unity-sample/Assets/Plugins/x86/plux_unity_interface.dll` **\[32 bits]** or `plux-api-unity-sample/Assets/Plugins/x86_64/plux_unity_interface.dll` **\[64 bits]**
2.	Copy the **PluxDeviceManager** folder (a C\# interface to PLUX API) to the **Scripts** folder of your Unity project. In this case the directory location is: `plux-api-unity-sample/Assets/Scripts/PluxDeviceManager`

> **WARNING**
>
> While initialising **PluxDeviceManager** instance/object it will be necessary to specify a callback responsible for dealing with the asynchronous results produced during the Bluetooth device scan and another one that flags when a successful Bluetooth connection is established. The callbacks should present the following structure:
>
> **Scanning Results**
>
> `public void ScanResults(List<string> listDevices)`
>
> **Connection Successful**
>
> `public void ConnectionDone()`

After the previous two steps, you will be fully prepared to expand your Unity project and include some interesting functionalities through the use of **PLUX** signal acquisition devices.

For this purpose, you simply need to invoke the available [Methods](#Methods).

# Unity API

## Methods

-	[PluxDeviceManager.PluxDev](#PluxDev)
-	[PluxDeviceManager.DisconnectPluxDev](#DisconnectPluxDev)
-	[PluxDeviceManager.StartAcquisitionUnity](#StartAcquisitionUnity)
-	[PluxDeviceManager.StartAcquisitionBySourcesUnity](#StartAcquisitionBySourcesUnity)
-	[PluxDeviceManager.StartAcquisitionByNbrUnity](#StartAcquisitionByNbrUnity)
-	[PluxDeviceManager.StartAcquisitionMuscleBanUnity](#StartAcquisitionMuscleBanUnity)
-	[PluxDeviceManager.StopAcquisitionUnity](#StopAcquisitionUnity)
-	[PluxDeviceManager.GetPackageOfData [v1]](#GetPackageOfData-v1)
-	[PluxDeviceManager.GetPackageOfData [v2]](#GetPackageOfData-v2)
-	[PluxDeviceManager.GetDetectableDevicesUnity](#GetDetectableDevicesUnity)
-	[PluxDeviceManager.GetNbrChannelsUnity](#GetNbrChannelsUnity)
-	[PluxDeviceManager.GetBatteryUnity](#GetBatteryUnity)
-	[PluxDeviceManager.GetDeviceTypeUnity](#GetDeviceTypeUnity)
-	[PluxDeviceManager.SetParameter](#SetParameter)

## PluxDev

Method used to establish a connection between **PLUX** devices and computer. Behaves like an object constructor.
```csharp
void PluxDev(string macAddress)
```

### Description

With `PluxDev` method it is possible to establish a **Bluetooth** connection between a computer and a **PLUX** Device. To ensure a successful pairing, the user only need to check the device mac-address (attached to the **BITalino** board, **biosignalsplux** hub...).
This mac-address should be inserted in a string format with the following structure: `"00:07:80:4D:2E:AD"` (6 pairs of characters separated by ":").

### Parameters

+	**macAddress** `string`: Device unique identifier, consisting in a string formed by 6 pairs of characters separated by ":" symbol (total of 17 characters).

## DisconnectPluxDev

Opposing to [PluxDev](#PluxDev), this method is very useful when you want to safely cut the connection between **PLUX** devices and computer. Behaves like an object destructor.
```csharp
void DisconnectPluxDev()
```

### Description

Through `DisconnectPluxDev` method the established connection between a **PLUX** device and a personal computer can be securely closed. If a real-time acquisition is yet being executed, a stop command is automatically sent, ensuring the end of communication loop.
Internally, the object created with `PluxDev` is destroyed after invoking `DisconnectPluxDev`.

### Parameters

*Function without input parameters*

## StartAcquisitionUnity

Class method used to start a real-time acquisition at the device paired with the computer through `PluxDev` method.
```csharp
void StartAcquisitionUnity(int samplingRate, List<int> listChannels, int resolution)
```

### Description

The current API version supports real-time acquisitions with analogical sensors, however, to communicate with computer the **PLUX** device needs to execute an **Analogical to Digital Conversion** (ADC), through data sampling.
Taking ADC stage into consideration, while starting a real-time acquisition, selecting a specific sampling rate and resolution is extremely easy through the inputs of `StartAcquisitionUnity`.

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**listChannels** `List<int>`: A list where it can be specified the active channels. Each entry contains a port number of an active channel.
+	**resolution** `int`: Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when compared with the ideal real case scenario.

## StartAcquisitionBySourcesUnity

Class method used to start a real-time acquisition at the device paired with the computer through `PluxDev` method.

```csharp
void void StartAcquisitionBySourcesUnity(int samplingRate, PluxSource[] sourcesArray)
```

### Description

The current methods is responsible to trigger real-time acquisitions with analogical **AND** digital sensors, through the creation of an array containing `PluxSource` elements.
Each source is responsible for the configuration of a specific **port** of the **PLUX** device being used.

To create a new `PluxSource` the following constructor must be used:

```csharp
// port -> Source port (1...8 for analog ports). Default value is zero.
// freqDivisor -> Source frequency divisor from acquisition base frequency (>= 1). Default value is 1.
// nBits -> Source sampling resolution in bits (8 or 16). Default value is 16.
// chMask -> Bitmask of source channels to sample (bit 0 is channel 0, etc). Default value is 1 (channel 0 
//           only). For a digital port if chMask=0x0F it means that channels 1-4 are active (0000 1111).
public PluxSource(int port, int freqDivisor, int nBits, int chMask)
```

Some practical examples are presented in the following code blocks:

**\[bisignalsplux\]**

```csharp
// Starting a real-time acquisition from:
// >>> biosignalsplux [CH1 and CH8 active | resolution=16 bits]
List<PluxDeviceManager.PluxSource> pluxSources = new List<PluxDeviceManager.PluxSource>();
pluxSources.Add(new PluxDeviceManager.PluxSource(1, 1, 16, 0x01));
pluxSources.Add(new PluxDeviceManager.PluxSource(8, 1, 16, 0x01));

// Being PluxDevManager the instance of our PluxDeviceManager class.
// sampling rate = 1000 Hz
PluxDevManager.StartAcquisitionBySourcesUnity(1000, pluxSources.ToArray());
```

**\[bisignalsplux Hybrid-8\]**

```csharp
// Starting a real-time acquisition from:
// >>> biosignalsplux Hybrid-8 [CH1, CH2 and CH8 active | resolution=16 bits]
// >>> CH1 and CH2 will be connected to a digital sensor (fNIRS, SpO2,...).
List<PluxDeviceManager.PluxSource> pluxSources = new List<PluxDeviceManager.PluxSource>();

// Add the sources of the digital channels (CH1 and CH2).
pluxSources.Add(new PluxDeviceManager.PluxSource(1, 1, 16, 0x03));
pluxSources.Add(new PluxDeviceManager.PluxSource(2, 1, 16, 0x03));

// Being PluxDevManager the instance of our PluxDeviceManager class.
// Define the LED Intensities of both sensors as: RED=80% and INFRARED=40%
int[] ledIntensities = new int[2] {80, 40};
PluxDevManager.SetParameter(1, 0x03, ledIntensities);
PluxDevManager.SetParameter(2, 0x03, ledIntensities);

// Add the source of the analog channel (CH8).
pluxSources.Add(new PluxDeviceManager.PluxSource(8, 1, 16, 0x01));

// Being PluxDevManager the instance of our PluxDeviceManager class.
// Start a real-time acquisition at a 300 Hz sampling rate.
PluxDevManager.StartAcquisitionBySourcesUnity(300, pluxSources.ToArray());
```

**\[fNIRS Explorer\]**

```csharp
// Starting a real-time acquisition from:
// >>> fNIRS Explorer [Full set of SpO2 and Accelerometer channels]
List<PluxDeviceManager.PluxSource> pluxSources = new List<PluxDeviceManager.PluxSource>();
// [SpO2 Port (4 channels - 0x0F > 0000 1111)]
pluxSources.Add(new PluxDeviceManager.PluxSource(9, 1, 16, 0x0F));

// [ACC Port (3 channels - 0x07 > 0000 0111)]
pluxSources.Add(new PluxDeviceManager.PluxSource(11, 1, 16, 0x07));

// Being PluxDevManager the instance of our PluxDeviceManager class.
// sampling rate = 1000 Hz
PluxDevManager.StartAcquisitionBySourcesUnity(1000, pluxSources.ToArray());
```

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**sourcesArray** `PluxSource[]`: A list of `PluxSource` elements that define which channels are active and its internal configurations, namely the resolution and frequency divisor.

## StartAcquisitionByNbrUnity

Class method used to start a real-time acquisition at the device paired with `PluxDev` method.
```csharp
void StartAcquisitionByNbrUnity(int samplingRate, int numberOfChannel, int resolution)
```

### Description

The current API version supports real-time acquisitions with analogical sensors, however, to communicate with computer the **PLUX** device needs to execute an **Analogical to Digital Conversion** (ADC), through data sampling.
Taking ADC stage into consideration, while starting a real-time acquisition, selecting a specific sampling rate and resolution is extremely easy through the inputs of `StartAcquisitionByNbrUnity`.
The main difference of this function in comparison with `StartAcquisitionUnity` is the `numberOfChannels` argument, so, instead of specifying a list of active channels you can simply identify the number of channels to be used during the data collection procedure.
Channels are activated in a sequential way, so, if `numberOfChannels` is equal to 3 it means that ports 1 to 3 will collect data.

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**numberOfChannels** `int`: Number of the active channel that will be used during data acquisition. With **BITalino** this value should be between 1 and 6 while for **biosignalsplux** it is possible to collect data from up to 8 channels (simultaneously).
+	**resolution** `int`: Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when compared with the ideal real case scenario.

## StartAcquisitionMuscleBanUnity

Class method used to start a real-time acquisition at the device paired with `PluxDev` method (on **muscleBAN**).
```csharp
void StartAcquisitionMuscleBanUnity(int samplingRate, List<int> listChannels, int resolution, int freqDivisor)
```

### Description

The current API version supports real-time acquisitions with analogical sensors, however, to communicate with computer the **PLUX** device needs to execute an **Analogical to Digital Conversion** (ADC), through data sampling.
Taking ADC stage into consideration, while starting a real-time acquisition, selecting a specific sampling rate and resolution is extremely easy through the inputs of `StartAcquisitionMuscleBanUnity`.
The main difference on the inputs of this function in comparison with `StartAcquisitionUnity`, is the additional `freqDivisor` argument, that transmits the information regarding the subsampling level to be applied during the **muscleBAN** data acquisition.

### Parameters

+	**samplingRate** `int`: Desired sampling rate that will be used during the data acquisition stage. The used units are in Hz (samples/s).
+	**listChannels** `List<int>`: A list where it can be specified the active channels. Each entry contains a port number of an active channel.
+	**resolution** `int`: Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when compared with the ideal real case scenario.
+	**freqDivisor** `int`: Frequency divisor, i.e., acquired data will be subsampled accordingly to this parameter. If freqDivisor = 10, it means that each set of 10 acquired samples will trigger the communication of a single sample (through the communication loop).

## StopAcquisitionUnity

Class method used to interrupt the real-time communication loop triggered by `StartAcquisitionUnity()`, `StartAcquisitionByNbrUnity()` or `StartAcquisitionMuscleBanUnity()` methods.
```csharp
bool StopAcquisitionUnity(int forceStop=0)
```

### Description

After starting a real-time acquisition a communication loop between the PLUX device and the computer starts running on a parallel thread.
With `StopAcquisitionUnity()` function, first of all, the communication loop is interrupted, task followed by the stop of  the real-time acquisition and finally the thread closure.

### Parameters

+	**forceStop** `int`: Integer code identifying the cause of acquisition stop (>= 0 | manually invoked by the user; -1 | device turned off; -2 | communication timeout exceeded).

### Returned Values

A `bool` flag is returned identifying when the stop event was forced (**true**) or manually invoked by the user (**false**).

## GetPackageOfData [v1]

Class method intended to request a package of data collected by the **PLUX** device.

```csharp
int[] GetPackageOfData(int channelNbr, List<int> activeChannelsMask, bool rebootMemory)
```

### Description

After triggering the start of a real-time data acquisition streaming of sample values is continuously guaranteed through the established **Bluetooth** communication.

The **PluxDeviceManager** class ensures the storage of the received packages of data, being this getter the method used to access the all packages of data from a desired channel (**channelNbr**). 

### Parameters

+	**channelNbr** `int`: Number of the channel under analysis (from which the sample will be retrieved).
+	**activeChannelsMask** `List<int>`: List containing set of active channels.
+	**rebootMemory** `bool`: When **true** the stored data inside **BufferedSamples** object is re-initialized.

### Returned Values

An ordered array of integer values, where each entry contains a sample value acquired from the **channelNbr** under analysis.

## GetPackageOfData [v2]

Class method intended to request a package of data collected by the **PLUX** device.

```csharp
int[][] GetPackageOfData(bool rebootMemory)
```

### Description

After triggering the start of a real-time data acquisition streaming of sample values is continuously guaranteed through the established **Bluetooth** communication.

The **PluxDeviceManager** class ensures the storage of the received packages of data, being this getter the method used to access all packages of data from each one of the active channels. 

### Parameters

+	**rebootMemory** `bool`: When **true** the stored data inside **BufferedSamples** object is re-initialized.

### Returned Values

A 2D integer array, where each entry of the first dimension contains an array of integers that store all samples collected from the respective communication channel. 

The size of the first dimension is equal to the number of active channels.

## GetDetectableDevicesUnity

Class method intended to find the list of detectable devices through **Bluetooth** communication.
```csharp
void GetDetectableDevicesUnity(List<string> domains)
```

### Description

Internally the list of detected devices reaches `PluxDeviceManager.cs` in a string format, where each device mac-address is separated from each other by a "&" character.

`GetDetectableDevicesUnity` trigger the start of a Bluetooth scan in a parallel thread.

In `GetDetectableDevicesUnity` an automatic split of the received string took place, ensuring that a list with the available mac-addresses will be returned.

> **WARNING**
>
> To access the results of the previously started asynchronous task, a callback with the following format should be implemented in your application:
>
> `public void ScanResults(List<string> listDevices)`

### Parameters

+	**domains** `List<string>`: A list containing the Bluetooth domains to be searched (**BTH** or **BLE**). Each domain inside the list will be used while searching for PLUX devices \[Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy]

### Returned Values

A list of strings containing the mac-addresses of all **PLUX** devices detected during the Bluetooth scan.

## GetNbrChannelsUnity

"Getting" method used for determination of the number of used channels during the real-time acquisition.
```csharp
int GetNbrChannelsUnity()
```

### Description

A simple method that returns the number of samples being used on the real-time acquisition under execution.

### Parameters

*Function without input parameters*

### Returned Values

An integer identifying the number of active channels in the **PLUX** device connected to the computer through Bluetooth.

## GetBatteryUnity

"Getting" method dedicated to check the battery level of the device.
```csharp
int GetBatteryUnity()
```

### Description

Having a feedback about the energy level of electronic devices is extremely important and useful. With this method an integer with the percentage (0-100%) of battery level is returned.

### Parameters

*Function without input parameters*

### Returned Values

The percentage level of battery.

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

### Returned Values

A string that identifies which **PLUX** device is currently connected to the computer.

## SetParameter

"Setting" method intended to define the value of a specific parameter in the PLUX device connected with the computer or its sensors.

```csharp
void SetParameter(int port, int index, int[] data)
```

### Description

With the `SetParameter` method, the developer has the possibility of customizing system specific variables (in the PLUX device) or sensor parameters, for example, the intensity of the **RED** and **INFRARED** LEDs included in the **fNIRS** sensor. 

**\[Definition of LED Intensity on the fNIRS/SpO2 Sensor\]**

```csharp
// Configure SpO2 channel intensity if dealing with fNIRS Explorer port/chnMask.
int[] ledParam = { 80, 40 }; // Percentage values.
PluxDevManager.SetParameter(0x09, 0x03, ledParam); // update oximeter LED values (Red and IR)
```

### Parameters

+	**port** `int`: Sensor port number for a sensor parameter, or **zero** for a system parameter.
+	**index** `const void`: Index of the parameter to set within the sensor or system.
+	**data** `int[]`: List containing the values to assign to the parameter under analysis.

## Support
If you find any problem during your experience, please, feel free to create a new issue track on our repository.
We will be very glad to guide you in this amazing journey and listen your suggestions!
