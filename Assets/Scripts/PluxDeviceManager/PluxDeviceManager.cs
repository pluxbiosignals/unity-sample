using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;

//using Boo.Lang.Runtime;

public class PluxDeviceManager
{
    // Declaration of DllImport statements for accessing the functions inside our native PLUX .dll
    [DllImport("plux_unity_interface")]
    private static extern int WelcomeFunction();
    [DllImport("plux_unity_interface")]
    private static extern void PluxDevUnity(string macAddress);
    [DllImport("plux_unity_interface")]
    private static extern void DisconnectPluxDevUnity();
    [DllImport("plux_unity_interface", CallingConvention = CallingConvention.Cdecl)]
    private static extern void StartAcquisitionBySources(int samplingRate, [In] IntPtr sourcesArray, int nbrSources);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisitionByNbr(int samplingRate, int numberOfChannel, int resolution);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisition(int samplingRate, string activeChannels, int resolution);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisitionMuscleBan(int samplingRate, string activeChannels, int resolution, int freqDivisor);
    [DllImport("plux_unity_interface")]
    private static extern void StartLoop();
    [DllImport("plux_unity_interface")]
    private static extern void StopAcquisition();
    [DllImport("plux_unity_interface")]
    private static extern void InterruptAcquisition();
    [DllImport("plux_unity_interface")]
    private static extern int SendDataTo(IntPtr dataIn);
    [DllImport("plux_unity_interface")]
    private static extern int GetNbrChannels();
    [DllImport("plux_unity_interface")]
    private static extern bool GetCommunicationFlag();
    [DllImport("plux_unity_interface")]
    private static extern int GetBattery();
    [DllImport("plux_unity_interface")]
    private static extern void GetDetectableDevices(string domain);
    [DllImport("plux_unity_interface")]
    private static extern void GetAllDetectableDevices();
    [DllImport("plux_unity_interface")]
    private static extern int GetProductId();
    [DllImport("plux_unity_interface")]
    private static extern System.IntPtr GetDeviceType();
    [DllImport("plux_unity_interface")]
    private static extern void SetNewDeviceFoundHandler(IntPtr handlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetOnRawDataHandler(OnRawFrameReceived handlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetOnExceptionRaisedHandler(OnExceptionRaised handlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetOnEventDetectedHandlers(OnDisconnectEventRaised disconnectEventHandlerFunction, OnDigInUpdateEventRaised digInUpdateEventHandlerFunction);
    [DllImport("plux_unity_interface")]
    private static extern void SetParameter(int port, int index, [In] IntPtr data, int dataLen);

    // Declaration of a Plux::Source structure shared with the .dll.
    [StructLayout(LayoutKind.Sequential)]
    public struct PluxSource
    {
        public int port;
        public int freqDivisor;
        public int nBits;
        public int chMask;

        // Constructor responsible for the creation of a Plux::Source.
        // port -> Source port (1...8 for analog ports). Default value is zero.
        // freqDivisor -> Source frequency divisor from acquisition base frequency (>= 1). Default value is 1.
        // nBits -> Source sampling resolution in bits (8 or 16). Default value is 16.
        // chMask -> Bitmask of source channels to sample (bit 0 is channel 0, etc). Default value is 1 (channel 0 only).
        public PluxSource(int port = 0, int freqDivisor = 1, int nBits = 16, int chMask = 1)
        {
            this.port = port;
            this.freqDivisor = freqDivisor;
            this.nBits = nBits;
            this.chMask = chMask;
        }
    }

    // Declaration of the Plux::Event class.
    public class PluxEvent
    {
        // Enumerator defining the types of events that can be raised by the PLUX API.
        public enum PluxEvents
        {
            DigInUpdate = 3, // Digital Input Updated
            Disconnect = 8 // Disconnect Event
        }

        public PluxEvents type;

        // Constructor responsible for the creation of a Plux::Event.
        // type -> PluxEvents enumerator key that identifies the type of event under analysis.
        public PluxEvent(PluxEvents type)
        {
            this.type = type;
        }

    }

    // Declaration of a Plux::DigInUpdateEvent structure shared with the .dll.
    public class PluxDigInUpdateEvent : PluxEvent
    {
        // Event timestamp class.
        public struct PluxClock
        {
            // Enumerator defining the available clock sources used in the PluxDigInUpdateEvent.
            public enum ClockSources
            {
                None,
                RTC,
                FrameCount,
                Bluetooth
            }

            public ClockSources source;
            public int value;

            // Constructor responsible for the creation of a Plux::Clock.
            // source -> Clock source for the current timestamp.
            // value -> Timestamp value.
            public PluxClock(ClockSources source = ClockSources.None, int value = 0)
            {
                this.source = source;
                this.value = value;
            }
        }

        public PluxClock timestamp;
        public int channel;
        public bool state;


        // Constructor responsible for the creation of a Plux::EvtDigInUpdate.
        // timestamp -> Event timestamp.
        // channel -> The digital input which changed state, starting at zero.
        // state -> New state of digital port input. If true, new state is High, otherwise it is Low.
        public PluxDigInUpdateEvent(PluxClock timestamp, int channel, bool state) : base(PluxEvents.DigInUpdate)
        {
            this.timestamp = timestamp;
            this.channel = channel;
            this.state = state;
        }
    }

    // Declaration of a Plux::EvtDisconnect structure shared with the .dll.
    public class PluxDisconnectEvent : PluxEvent
    {
        /// Disconnect reason enumeration.
        public enum PluxDisconnectReason
        {
            Timeout = 1,         // Connection timeout has elapsed.
            ButtonPressed = 2,   // Device button was pressed.
            BatDischarged = 4,   // Device battery is discharged.
        };

        public PluxDisconnectReason reason;


        // Constructor responsible for the creation of a Plux::EvtDigInUpdate.
        // reason -> Reason for the device disconnection.
        public PluxDisconnectEvent(PluxDisconnectReason reason) : base(PluxEvents.Disconnect)
        {
            this.reason = reason;
        }
    }

    // Delegates (needed for callback purposes).
    public delegate void OnRawFrame(int nSeq, int[] dataIn);
    public delegate void OnRawFrameReceived(int nSeq, IntPtr dataIn, int dataInSize);
    public delegate void OnNewDeviceFound(string newDevice);
    public delegate void ScanResults(List<string> listDevices);
    public delegate void ConnectionDone(bool connectionStatus);
    public delegate void AcquisitionStarted(bool acquisitionStatus, bool exceptionRaised = false, string exceptioDescription = "");
    public delegate void OnExceptionRaised(int exceptionCode, string exceptionDescription);
    public delegate void OnEventDetected(PluxEvent pluxEvent);
    public delegate void OnDisconnectEventRaised(PluxDisconnectEvent.PluxDisconnectReason reason);
    public delegate void OnDigInUpdateEventRaised(PluxDigInUpdateEvent.PluxClock.ClockSources clockSource, int clockValue, int channel, bool state);

    // [Generic Variables]
    private Thread ScanningThread;
    private Thread ConnectionThread;
    private Thread AcquisitionThread;
    private ScanResults ScanResultsCallback;
    private ConnectionDone ConnectionDoneCallback;
    private AcquisitionStarted AcquisitionStartedCallback;
    private static Lazy<List<String>> PluxDevsFound = null;
    private bool DeviceConnected = false;
    private int SamplingRate;
    private string ActiveChannelsStr = "";
    private bool AcquisitionStopped = true;
    private static CallbackManager callbackPointer;
    //private BufferAcqSamples BufferedSamples = new BufferAcqSamples();
    private static Lazy<BufferAcqSamples> LazyObject = null;
    private BufferAcqSamples BufferedSamples;
    private volatile object DoubleCheckLock = null;
    private int currThreadNumber = 0;

    // Contructor.
    // scanResultsCallback -> Callback function that will be invoked once the Bluetooth scan for PLUX devices ends.
    // connectionDoneCallback -> Callback function that will be invoked once a connection with a PLUX device is established.
    // acquisitionStartedCallback -> Callback function that will be invoked when the acqusition start request attempt was completed with success or not.
    // onDataReceivedCallback -> Callback function invoked every time a new package of RAW data samples is transmitted by the API.
    // onEventDetectedCallback -> Callback invoked when an event is raised by the PLUX API.
    // onExceptionRaisedCallback -> Callback invoked when an exception is raised by the PLUX API.
    public PluxDeviceManager(ScanResults scanResultsCallback, ConnectionDone connectionDoneCallback, AcquisitionStarted acquisitionStartedCallback, OnRawFrame onDataReceivedCallback, OnEventDetected onEventDetectedCallback, OnExceptionRaised onExceptionRaisedCallback)
    {
        LazyObject = new Lazy<BufferAcqSamples>(InitBufferedSamplesObject);
        PluxDevsFound = new Lazy<List<String>>(InitiListDevFound);

        // Scan callback.
        this.ScanResultsCallback = new ScanResults(scanResultsCallback);

        // On connection successful callback.
        this.ConnectionDoneCallback = new ConnectionDone(connectionDoneCallback);

        // Storage of the AcquisitionStarted callback.
        this.AcquisitionStartedCallback = new AcquisitionStarted(acquisitionStartedCallback);

        // Initialization of the variable storing the callback responsible for receiving the devices found during the scan.
        OnNewDeviceFound onNewDeviceFoundHandler = new OnNewDeviceFound(OnNewDeviceFoundHandler);
        GCHandle onNewDeviceFoundGCHandler = GCHandle.Alloc(onNewDeviceFoundHandler);
        SetNewDeviceFoundHandler(Marshal.GetFunctionPointerForDelegate(onNewDeviceFoundHandler));

        // Initialization of the variable storing the callback responsible for receiving the streamed data.
        OnRawFrameReceived onRawDataHandler = new OnRawFrameReceived(OnRawFrameHandler);
        GCHandle onRawDataGCHandler = GCHandle.Alloc(onRawDataHandler);
        SetOnRawDataHandler(onRawDataHandler);

        // Initialization of the variable storing the callback responsible for receiving the exceptions raised in the PLUX API .dll.
        OnExceptionRaised onExceptionRaisedHandler = new OnExceptionRaised(OnExceptionRaisedHandler);
        GCHandle onExceptionRaisedGCHandler = GCHandle.Alloc(onExceptionRaisedHandler);
        SetOnExceptionRaisedHandler(onExceptionRaisedHandler);

        // Initialization of the variables storing the callbacks responsible for receiving the events raised in the PLUX API .dll.
        OnDisconnectEventRaised onDisconnectEventHandler = new OnDisconnectEventRaised(OnDisconnectEventHandler);
        GCHandle onDisconnectEventGCHandler = GCHandle.Alloc(onDisconnectEventHandler);
        OnDigInUpdateEventRaised onDigInEventHandler = new OnDigInUpdateEventRaised(OnDigInEventHandler);
        GCHandle onDigInEventGCHandler = GCHandle.Alloc(onDigInEventHandler);
        SetOnEventDetectedHandlers(onDisconnectEventHandler, onDigInEventHandler);

        // Initialise helper object that manages threads creating during the scanning and connection processes.
        var unitDispatcher = UnityThreadHelper.Dispatcher;

        // Specification of the callback function (defined on this/the user Unity script) which will receive the acquired data
        // samples as inputs.
        GCHandle onDataReceivedGCHandler = GCHandle.Alloc(onDataReceivedCallback);
        GCHandle onEventDetectedGCHandler = GCHandle.Alloc(onEventDetectedCallback);
        SetCallbackHandler(onDataReceivedCallback, onEventDetectedCallback, onExceptionRaisedCallback);
    }

    // [Redefinition of the imported methods ensuring that they are accessible on other scripts]

    // A simple function used to check if the .dll generated during the build process was successfully imported by Unity.
    public int WelcomeFunctionUnity()
    {
        return WelcomeFunction();
    }

    // Method used to establish a connection between PLUX devices and computer.
    // Behaves like an object constructor.
    // macAddress -> Device unique identifier, i.e., mac-address.
    public void PluxDev(string macAddress)
    {
        Console.WriteLine("Scanning Thread State: " + ScanningThread.ThreadState);
        Console.WriteLine("Selected Device being received: " + macAddress);
        
        // Creation of new thread to manage the connection stage.
        ConnectionThread = new Thread(() => ConnectToPluxDev(macAddress)); ;
        ConnectionThread.Name = "CONNECTION_" + currThreadNumber;
        currThreadNumber++;
        ConnectionThread.Start();
        Console.WriteLine("Connection Thread Started with Success !");
    }

    // Auxiliary method intended to establish a Bluetooth connection between the computer and PLUX device.
    // macAddress -> Device unique identifier, i.e., mac-address.
    private void ConnectToPluxDev(string macAddress)
    {
        try
        {
            PluxDevUnity(macAddress);

            // Check if the connection was established with success.
            DeviceConnected = !IsExceptionInBuffer() ? true : false;
            
            // Send data (connection status) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => ConnectionDoneCallback(DeviceConnected));
        }
        catch (Exception exc)
        {
            Debug.Log("Exception being raised: " + exc.StackTrace);
        }
    }

    // In this method a disconnect attempt between the computer and the PLUX device will be executed.
    // If a real-time acquisition is in progress, then, the API will try to stop it before the disconnect command.
    public void DisconnectPluxDev()
    {
        if (AcquisitionThread != null)
        {
            Console.WriteLine("Thread Unity Forced to Close");
            if (AcquisitionStopped == false)
            {
                Console.WriteLine("Forcing the acquisition stop");
                StopAcquisitionUnity();
            }
        }

        // Check if the device was previously been connected.
        if (DeviceConnected)
        {
            DisconnectPluxDevUnity();
            DeviceConnected = false;
        }
    }

    // Class method used to Start a Real-Time acquisition through Plux::Source configuration:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // sourcesArray -> List of Sources that define which channels are active and its internal configurations, 
    //				   namely the resolution and frequency divisor.
    public void StartAcquisitionBySourcesUnity(int samplingRate, PluxSource[] sourcesArray)
    {
        // Reboot BufferedSamples object.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            bufferedSamples.reinitialise();
        }

        if (!bufferedSamples.getUncaughtExceptionState())
        {
            // >>> Garbage collector memory management.
            GCHandle pinnedArray = GCHandle.Alloc(sourcesArray, GCHandleType.Pinned);
            // >>> Convert to a memory address.
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();
            // >>> Call correspondent .dll method to start the real-time acquisition.
            StartAcquisitionBySources(samplingRate, ptr, sourcesArray.Length);
            // >>> Releasing memory.
            pinnedArray.Free();

            // Start Communication Loop.
            StartLoopUnity();
        }
        else
        {
            throw new Exception("Unable to start a real-time acquisition. It is probable that the connection between the computer and the PLUX device was broke");
        }

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Class method used to Start a Real-Time acquisition:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // listChannels -> A list where there are specified the active channels. Each entry contains a port number of an active channel.
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    public void StartAcquisitionUnity(int samplingRate, List<int> listChannels, int resolution)
    {
        // Conversion of List of active channels to a string format.
        for (int i = 0; i < 11; i++)
        {
            if (listChannels.Contains(i + 1))
            {
                ActiveChannelsStr += "1";
            }
            else
            {
                ActiveChannelsStr += "0";
            }
        }

        // Reboot BufferedSamples object.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            bufferedSamples.reinitialise();
        }

        if (!bufferedSamples.getUncaughtExceptionState())
        {
            // Start of acquisition.
            StartAcquisition(samplingRate, ActiveChannelsStr, resolution);

            // Start Communication Loop.
            StartLoopUnity();
        }
        else
        {
            throw new Exception("Unable to start a real-time acquisition. It is probable that the connection between the computer and the PLUX device was broke");
        }

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Class method used to Start a Real-Time acquisition:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // numberOfChannels -> Number of the active channel that will be used during data acquisition.
    //                    With bitalino this value should be between 1 and 6 while for biosignalsplux it is possible to collect data
    //                    from up to 8 channels (simultaneously).
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    public void StartAcquisitionByNbrUnity(int samplingRate, int numberOfChannels, int resolution)
    {
        // Start of the real-time acquisition.
        StartAcquisitionByNbr(samplingRate, numberOfChannels, resolution);

        // Start Communication Loop.
        StartLoopUnity();

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Class method used to Start a Real-Time acquisition (on muscleBAN):
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // listChannels -> A list where there are specified the active channels. Each entry contains a port number of an active channel.
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    // freqDivisor -> Frequency divisor, i.e., acquired data will be subsampled accordingly to this parameter. If freqDivisor = 10, it means that each set of 10 acquired samples
    //                will trigger the communication of a single sample (through the communication loop).
    public void StartAcquisitionMuscleBanUnity(int samplingRate, List<int> listChannels, int resolution, int freqDivisor)
    {
        // Conversion of List of active channels to a string format.
        for (int i = 0; i < 8; i++)
        {
            if (listChannels.Contains(i + 1))
            {
                ActiveChannelsStr += "1";
            }
            else
            {
                ActiveChannelsStr += "0";
            }
        }

        // Start of acquisition.
        StartAcquisitionMuscleBan(samplingRate, ActiveChannelsStr, resolution, freqDivisor);
        
        // Start Communication Loop.
        StartLoopUnity();

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Trigger the start of the communication loop (between PLUX device and computer).
    private void StartLoopUnity()
    {
        if(!IsExceptionInBuffer()) { 
            // Creation of new thread to manage the communication loop.
            AcquisitionThread = new Thread(StartLoop);
            AcquisitionThread.Name = "ACQUISITION_" + currThreadNumber;
            currThreadNumber++;
            AcquisitionThread.Start();
            Console.WriteLine("Acquisition Thread Started with Success !");

            // Inform the frontend about the successful start of the real-time acquisition.
            AcquisitionStartedCallback(true);
        }
        else
        {
            // Inform the frontend about the failure in the start of the real-time acquisition.
            AcquisitionStartedCallback(false);
        }
    }

    // Method used to check if any unhandled exception was raised until the moment.
    // raiseException -> A Boolean flag stating if an exception should be explicitly raised (true) or silently flagged (false).
    private bool IsExceptionInBuffer(bool raiseException = false)
    {
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            if (bufferedSamples.getUncaughtExceptionState())
            {
                bufferedSamples.deactUncaughtException();
                if (raiseException)
                {
                    throw new ExternalException(
                        "An exception with unknown origin was raised, but it is not fatal. It is probable that the device connection was lost...");
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Callback function responsible for receiving the devices found during the Bluetooth scan.
    // newDeviceFound -> MAC-Address of the device found during the scan.
    private void OnNewDeviceFoundHandler(string newDeviceFound)
    {
        try
        {
            // Store the device into a class variable.
            PluxDevsFound.Value.Add(newDeviceFound);
        }
        catch (OutOfMemoryException exception)
        {
            Debug.Log("OutOfMemory Exception raised: " + exception);
        }
        catch (Exception exception)
        {
            Debug.Log("Unexpected Exception raised: " + exception);
        }
    }

    // Callback function responsible for receiving the acquired data samples from the communication loop started by StartLoopUnity().
    // nSeq -> Sequence number, i.e., the number of the acquired data sample.
    // data -> Pointer to an array containing the sample value for each active channel, for example, if we are conducting an acquisition with 3 channels, the first three entries of "data" will contain the values that we desired.
    // dataInSize -> Size of the array referenced in the data pointer.
    private void OnRawFrameHandler(int nSeq, IntPtr data, int dataInSize)
    {
        lock (callbackPointer)
        {
            // Convert our data pointer to an array format.
            int[] dataArray = new int[dataInSize];
            Marshal.Copy(data, dataArray, 0, dataInSize);

            // Check if an exception was raised.
            if (!IsExceptionInBuffer()) { 
                // Send data (RAW frames) to the MAIN THREAD.
                UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.onRawFrameReference(nSeq, dataArray));
            }
        }
    }

    // Callback function responsible for receiving the info about the exceptions raised in the PLUX API .dll file.
    // exceptionCode -> ID number of the exception to be raised.
    // exceptionDescription -> Descriptive message about the exception.
    private void OnExceptionRaisedHandler(int exceptionCode, string exceptionDescription)
    {
        lock (callbackPointer)
        {
            BufferAcqSamples bufferedSamples = LazyObject.Value;
            lock (bufferedSamples)
            {
                bufferedSamples.actUncaughtException();
                Debug.Log("Exception being raised in the PLUX C++ API Wrapper:\n" + exceptionCode + " | " + exceptionDescription);

                // Inform the GUI about the raise of an exception.
                UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.OnExceptionRaisedReference(exceptionCode, exceptionDescription));
            }
        }
    }

    // Callback intended to communicate information about a "Disconnect" event detected in this wrapper.
    // reason -> Reason for the device disconnection.
    private void OnDisconnectEventHandler(PluxDisconnectEvent.PluxDisconnectReason reason)
    {
        lock (callbackPointer)
        {
            // Send data (event) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.onEventDetectedReference(new PluxDisconnectEvent(reason)));
        }
    }

    // Callback intended to communicate information about a "Digital Input Update" event detected in this wrapper.
    // clockSource -> Clock source for the current timestamp.
    // clockValue -> Timestamp value.
    // channel -> The digital input which changed state, starting at zero.
    // state -> New state of digital port input. If true, new state is High, otherwise it is Low.
    private void OnDigInEventHandler(PluxDigInUpdateEvent.PluxClock.ClockSources clockSource, int clockValue, int channel, bool state)
    {
        lock (callbackPointer)
        {
            // Send data (event) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => callbackPointer.onEventDetectedReference(new PluxDigInUpdateEvent(new PluxDigInUpdateEvent.PluxClock(clockSource, clockValue), channel, state)));
        }
    }

    // Class method used to interrupt the real-time communication loop.
    private void InterruptAcquisitionUnity()
    {
        InterruptAcquisition();
    }

    // Method dedicated to stop the real-time acquisition.
    // forceStop -> An identifier that specify when the stop command was voluntarily sent by the user (>=0) or forced  by an event or exception (-1, -2...).
    // RETURN (bool): A flag identifying when the acquisition was stopped in a forced way (true) or triggered by the user (false).
    public bool StopAcquisitionUnity(int forceStop=0)
    {
        // Returned variable.
        bool forceFlag = false;

        // Check if the StopButtonFunction was invoked by the user (button click) or after a Disconnect Event was triggered.
        if (AcquisitionThread != null)
        {
            if (forceStop >= 0)
            {
                // Interrupt real-time communication loop.
                Console.WriteLine("Communication Flag (Before Interrupt): " + GetCommunicationFlag());
                InterruptAcquisition();

                // Wait for the communication of the flag stating the end of the communication loop.
                bool communicationFlag = GetCommunicationFlag();
                Console.WriteLine("Communication Flag (After Interrupt): " + GetCommunicationFlag());
                while (communicationFlag == true)
                {
                    communicationFlag = GetCommunicationFlag();
                }

                Console.WriteLine("Communication Flag (After Loop): " + GetCommunicationFlag());

                // Stop acquisition.
                StopAcquisition();
                Console.WriteLine("Thread State: " + AcquisitionThread.ThreadState);
                AcquisitionThread.Abort();
                Console.WriteLine("Thread State (After Aborting): " + AcquisitionThread.ThreadState);
            }
            else
            {
                // Close Thread.
                Console.WriteLine("Thread State: " + AcquisitionThread.ThreadState + "_" + Thread.CurrentThread.Name);
                AcquisitionThread.Abort();
                Console.WriteLine("Thread State (After Aborting): " + AcquisitionThread.ThreadState);

                // Disconnect device if a forced stop occurred.
                if (forceStop == -1)
                {
                    //DisconnectPluxDev();
                }

                // Update forceFlag.
                forceFlag = true;

                // Debug Message.
                Console.WriteLine("Real-Time Data Acquisition stopped due to the lost of connection with PLUX Device.");
            }

            // Reboot variables.
            ActiveChannelsStr = "";

            // Update global flag.
            AcquisitionStopped = true;

            // Reboot AcquisitionThread
            AcquisitionThread = null;
        }

        // Clear the buffer containing the packages of collected data.
        RebootDataBuffer();

        return forceFlag;
    }

    // Class method intended to find the list of detectable devices through Bluetooth communication.
    // domains -> Array of strings that defines which domains will be used while searching for PLUX devices 
    //            [Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy; "USB" -> Through USB connection cable]
    public void GetDetectableDevicesUnity(List<string> domains)
    {
        // Creation of new thread to manage the scanning stage.
        ScanningThread = new Thread(() => ScanPluxDevs(domains)); ;
        ScanningThread.Name = "SCANNING_" + currThreadNumber;
        currThreadNumber++;
        ScanningThread.Start();
        Console.WriteLine("Scanning Thread Started with Success !");
    }

    // Auxiliary function that manages the scanning process.
    // domains -> Array of strings that defines which domains will be used while searching for PLUX devices 
    //            [Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy; "USB" -> Through USB connection cable]
    private void ScanPluxDevs(List<string> domains)
    {
        try
        {
            // Search for BLE and BTH devices.
            List<string> listDevices = new List<string>();
            List<String> devicesFound = PluxDevsFound.Value;

            // Clear previous content of the device list.
            devicesFound.Clear();
            for (int domainNbr = 0; domainNbr < domains.Count; domainNbr++)
            {
                try
                {
                    // List of available Devices.
                    GetDetectableDevices(domains[domainNbr]);
                }
                catch (OutOfMemoryException exception)
                {
                    Debug.Log("OutOfMemory Exception raised [Point 1]: " + exception);
                }
                catch (Exception exception)
                {
                    Debug.Log("Unexpected Exception raised: " + exception);
                }
            }

            // Send data (list of devices found) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => ScanResultsCallback(devicesFound));
        }
        catch (ExecutionEngineException exc)
        {
            Debug.Log("Exception found while scanning: \n" + exc.Message + "\n" + exc.StackTrace);
            BufferAcqSamples bufferedSamples = LazyObject.Value;
            lock (bufferedSamples)
            {
                bufferedSamples.actUncaughtException();
            }
        }
    }

    // Definition of the callback function responsible for managing the acquired data (which is defined on users Unity script).
    // onRawFrameHandler -> Callback function invoked every time a new package of RAW data samples is transmitted by the API.
    // onEventDetectedHandler -> Callback invoked when an event is raised by the PLUX API.
    // onExceptionRaisedHandler -> Callback invoked when an exception is raised by the PLUX API.
    private bool SetCallbackHandler(OnRawFrame onRawFrameHandler, OnEventDetected onEventDetectedHandler, OnExceptionRaised onExceptionRaisedHandler)
    {
        callbackPointer = new CallbackManager(onRawFrameHandler, onEventDetectedHandler, onExceptionRaisedHandler);
        return true;
    }

    // "Setting" method intended to define the value of a specific parameter. the type of the connected device.
    // port -> Sensor port number for a sensor parameter, or zero for a system parameter.
    // index -> Index of the parameter to set within the sensor or system.
    // data -> List containing the values to assign to the parameter under analysis.
    public void SetParameter(int port, int index, int[] data)
    {
        // >>> Garbage collector memory management.
        GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
        // >>> Convert to a memory address.
        IntPtr ptr = pinnedArray.AddrOfPinnedObject();
        // >>> Call correspondent .dll method to set the parameter value.
        SetParameter(port, index, ptr, data.Length);
        // >>> Releasing memory.
        pinnedArray.Free();
    }

    // "Getter" method for determination of the number of used channels during the acquisition.
    public int GetNbrChannelsUnity()
    {
        return GetNbrChannels();
    }

    // "Getter" method for checking the state of the communication flag.
    public bool GetCommunicationFlagUnity()
    {
        return GetCommunicationFlag();
    }

    // "Getter" method dedicated to check the battery level of the device.
    public int GetBatteryUnity()
    {
        return GetBattery();
    }

    // "Getter" method intended to check the product ID of the connected device.
    public int GetProductIdUnity()
    {
        return GetProductId();
    }

    // "Getter" method intended to check if a real-time acquisition is currently running.
    public bool IsAcquisitionInProgress()
    {
        return AcquisitionThread != null;
    }

    // "Getter" method intended to check the type of the connected device.
    public string GetDeviceTypeUnity()
    {
        return Marshal.PtrToStringAnsi(GetDeviceType());
    }

    // Class that manages the reference to callbackPointer.
    public class CallbackManager
    {
        public OnRawFrame onRawFrameReference;
        public OnEventDetected onEventDetectedReference;
        public OnExceptionRaised OnExceptionRaisedReference;
        public CallbackManager(OnRawFrame onRawFrameHandler, OnEventDetected onEventDetectedHandler, OnExceptionRaised onExceptionRaisedHandler)
        {
            onRawFrameReference = onRawFrameHandler;
            onEventDetectedReference = onEventDetectedHandler;
            OnExceptionRaisedReference = onExceptionRaisedHandler;
        }
    }

    // Auxiliary subclass that works as a buffer of received samples.
    public class BufferAcqSamples
    {
        private int comCounter = 0;
        private int[][] packagesOfData;
        private int maxNbrSamples = 10000;
        private bool rebootOnNextPackage = false;
        private bool uncaugthException = false;
        private int lastNSeq = -1;

        // Class constructor.
        public BufferAcqSamples()
        {
            packagesOfData = new int[maxNbrSamples][]; // Stores 10 seconds of data in data acquisitions of 1000 Hz sampling rate.
        }

        // An important method that ensures the reinitialisation of the class variables.
        public void reinitialise()
        {
            comCounter = 0;
            packagesOfData = new int[maxNbrSamples][];
            rebootOnNextPackage = false;
            uncaugthException = false;
            lastNSeq = -1;
        }

        // Add samples to the buffer.
        // nSeq -> Sequence number that univocally identifies the package.
        // newPackage -> Package of data to be added to the memory data structure of this object.
        public void addSamples(int nSeq, int[] newPackage)
        {
            // Check if the new package of data is the valid one, i.e., if it is the one immediately after the last received package.
            if (nSeq <= lastNSeq || nSeq > lastNSeq + 1)
            {
                actUncaughtException();
            }
            else
            {
                lastNSeq = nSeq;
            }
            
            // Reboot buffer if the controlling flag is true.
            if (rebootOnNextPackage)
            {
                // Reboot flag.
                rebootOnNextPackage = false;

                // Re-Initialise array.
                packagesOfData = new int[maxNbrSamples][];
                restart();
            }

            // Check if the maximum capacity of the buffer was reached.
            if (comCounter == maxNbrSamples)
            {
                // Shift data.
                Array.Copy(packagesOfData, 1, packagesOfData, 0, packagesOfData.Length - 1);

                // Decrement counter.
                decrement();
            }
            packagesOfData[comCounter] = newPackage;

            // Update counter.
            increment();
        }

        // Activate UncaughtException flag.
        public void actUncaughtException()
        {
            uncaugthException = true;
        }

        // Deactivate UncaughtException flag.
        public void deactUncaughtException()
        {
            uncaugthException = false;
        }

        // Increment the counter value.
        private void increment()
        {
            comCounter++;
        }

        // Decrement the counter value.
        private void decrement()
        {
            comCounter--;
        }
           
        // Method used to reboot the object memory.
        public void reboot()
        {
            // Reboot flag.
            rebootOnNextPackage = false;

            // Re-Initialise array.
            packagesOfData = new int[maxNbrSamples][];
        }

        // Restart counter.
        private void restart()
        {
            comCounter = 0;
        }

        // Get the counter value.
        public int getCounter()
        {
            return comCounter;
        }

        // Get available packages of data.
        // rebootMemory -> When true the stored data inside BufferedSamples object is re-initialized.
        public int[][] getPackages(bool rebootMemory)
        {
            // Check if the array is not empty.
            if (packagesOfData[0] == null)
            {
                return null;
            }
            // Send the filled section of the array.
            else if (comCounter != maxNbrSamples)
            {
                int[][] tempArray = new int[comCounter][];
                Array.Copy(packagesOfData, 0, tempArray, 0, comCounter);

                // Update flag.
                rebootOnNextPackage = rebootMemory;

                return tempArray;
            }
            else
            {
                // Update flag.
                rebootOnNextPackage = rebootMemory;

                return packagesOfData;
            }
        }

        // Get Uncaught Exception state.
        public bool getUncaughtExceptionState()
        {
            return uncaugthException;
        }
    }

    /**
     * Auxiliary method used to reboot the buffer responsible for storing the packages of collected data.
     */
    public void RebootDataBuffer()
    {
        // Clear the buffer containing the packages of collected data.
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            bufferedSamples.reboot();
        }
    }

    // Auxiliary method that ensures a secure lock.
    // https://www.pluralsight.com/guides/lock-statement-best-practices
    private object InitializeIfNeeded()
    {
        if (DoubleCheckLock == null)
        {
            lock (BufferedSamples)
            {
                if (DoubleCheckLock == null)
                {
                    DoubleCheckLock = true;
                }
            }
        }

        return DoubleCheckLock;
    }

    // Factory for our Multi-Thread lazy object.
    static BufferAcqSamples InitBufferedSamplesObject()
    {
        BufferAcqSamples lazyComponent = new BufferAcqSamples();
        return lazyComponent;
    }

    static List<String> InitiListDevFound()
    {
        List<String> devFound = new List<string>();
        return devFound;
    }
}
