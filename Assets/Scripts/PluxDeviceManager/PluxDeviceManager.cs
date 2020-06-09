using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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
    private static extern System.IntPtr GetDetectableDevices(string domain);
    [DllImport("plux_unity_interface")]
    private static extern System.IntPtr GetAllDetectableDevices();
    [DllImport("plux_unity_interface")]
    private static extern System.IntPtr GetDeviceType();
    [DllImport("plux_unity_interface")]
    private static extern void SetCommunicationHandler(FPtrUnity handlerFunction);

    // Delegates (needed for callback purposes).
    public delegate bool FPtr(int nSeq, int[] dataIn, int dataInSize);
    public delegate bool FPtrUnity(int exceptionCode, string exceptionDescription, int nSeq, IntPtr dataIn, int dataInSize);
    public delegate void ScanResults(List<string> listDevices);
    public delegate void ConnectionDone();
    //public delegate bool FPtrExceptions(int exceptionCode, string exceptionDescription);

    // [Generic Variables]
    private Thread ScanningThread;
    private Thread ConnectionThread;
    private Thread AcquisitionThread;
    private Thread MainThread;
    private ScanResults ScanResultsCallback;
    private ConnectionDone ConnectionDoneCallback;
    private List<String> PluxDevsFound;
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
    public PluxDeviceManager(ScanResults scanResultsCallback, ConnectionDone connectionDoneCallback)
    {
        LazyObject = new Lazy<BufferAcqSamples>(InitBufferedSamplesObject);

        // exceptionPointer -> Pointer to the callback function that will be used to send/communicate information about exceptions generated inside this .dll
        //                     The exception code and description will be sent to Unity where an appropriate action can take place.
        FPtrUnity dllCommunicationHandler = new FPtrUnity(DllCommunicationHandler);
        SetCommunicationHandler(dllCommunicationHandler);

        // Scan callback.
        this.ScanResultsCallback = new ScanResults(scanResultsCallback);

        // On connection successful callback.
        this.ConnectionDoneCallback = new ConnectionDone(connectionDoneCallback);

        // Initialise helper object that manages threads creating during the scanning and connection processes.
        var unitDispatcher = UnityThreadHelper.Dispatcher;

        // Specification of the callback function (defined on this/the user Unity script) which will receive the acquired data
        // samples as inputs.
        SetCallbackHandler(CallbackHandler);
    }

    // [Redefinition of the imported methods ensuring that they are acessible on other scripts]

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
        Debug.Log("Connection Thread Started with Success !");
    }

    // Auxiliary method intended to establish a Bluetooth connection between the computer and PLUX device.
    // macAddress -> Device unique identifier, i.e., mac-address.
    private void ConnectToPluxDev(string macAddress)
    {
        PluxDevUnity(macAddress);
        DeviceConnected = true;

        // Send data (list of devices found) to the MAIN THREAD.
        UnityThreadHelper.Dispatcher.Dispatch(() => ConnectionDoneCallback());
    }

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

    // Class method used to Start a Real-Time acquisition:
    // samplingRate -> Desired sampling rate that will be used during the data acquisition stage.
    //                 The used units are in Hz (samples/s)
    // listChannels -> A list where there are specified the active channels. Each entry contains a port number of an active channel.
    // resolution -> Analog-to-Digital Converter (ADC) resolution. This parameter defines how precise are the digital sampled values when
    //               compared with the ideal real case scenario.
    // callbackFunction -> Pointer to the callback function that will be used to send/communicate the data acquired by PLUX devices, i.e., this callback 
    //                     function will be invoked during the data acquisition process and through his inputs the acquired data will become accessible.
    //                     So, for receiving data on a third-party application it will only be necessary to redefine this callbackFunction.
    public void StartAcquisitionUnity(int samplingRate, List<int> listChannels, int resolution)
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
        // Storage of a reference to the main thread.
        if (MainThread == null)
        {
            MainThread = Thread.CurrentThread;
            MainThread.Name = "MAIN";
        }

        // Creation of new thread to manage the communication loop.
        AcquisitionThread = new Thread(StartLoop);
        AcquisitionThread.Name = "ACQUISITION_" + currThreadNumber;
        currThreadNumber++;
        AcquisitionThread.Start();
        Debug.Log("Acquisition Thread Started with Success !");
    }

    // Getter method used to request a new package of data.
    // rebootMemory -> When true the stored data inside BufferedSamples object is re-initialized.
    public int[][] GetPackageOfData(bool rebootMemory)
    {
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            if (!bufferedSamples.getUncaughtExceptionState())
            {
                return bufferedSamples.getPackages(rebootMemory);
            }
            else
            {
                bufferedSamples.deactUncaughtException();
                throw new ExternalException("An exception with unknown origin was raised, but it is not fatal. It is probable that the device connection was lost...");
            }
        }
    }

    // Getter method used to request a new package of data.
    // channelNbr -> Number of the channel under analysis.
    // activeChannelsMask -> List containing set of active channels.
    // rebootMemory -> When true the stored data inside BufferedSamples object is re-initialized.
    public int[] GetPackageOfData(int channelNbr, List<int> activeChannelsMask, bool rebootMemory)
    {
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            if (!bufferedSamples.getUncaughtExceptionState())
            {
                // Identification of the current channel index.
                int auxCounter = activeChannelsMask.IndexOf(channelNbr);

                // Initialisation of local variables.
                int[][] fullData = bufferedSamples.getPackages(rebootMemory);

                // Selection of data inside the desired channel.
                if (fullData != null && fullData[0] != null)
                {
                    int[] dataInChannel = new int[fullData.Length];
                    for (int i = 0; i < dataInChannel.Length; i++)
                    {
                        try
                        {
                            dataInChannel[i] = fullData[i][auxCounter];
                        }
                        catch (Exception exc)
                        {
                            bufferedSamples.reboot();
                            DoubleCheckLock = null;
                            return new int[0];
                        }
                    }

                    return dataInChannel;
                }

                return new int[0];
            }
            else
            {
                bufferedSamples.deactUncaughtException();
                throw new ExternalException("An exception with unknown origin was raised, but it is not fatal. It is probable that the device connection was lost...");
            }
        }
    }

    // Callback function responsible for receiving the acquired data samples from the communication loop started by StartLoopUnity().
    private bool DllCommunicationHandler(int exceptionCode, string exceptionDescription, int nSeq, IntPtr data, int dataInSize)
    {
        lock (callbackPointer)
        {
            try
            {
                // DEBUG
                //Console.WriteLine("Monitoring: " + nSeq + "|" + data + "|" + dataInSize);

                // Check if no exception were raised.
                if (exceptionCode == 0)
                {
                    try
                    {
                        // Convert our data pointer to an array format.
                        int[] dataArray = new int[dataInSize];
                        Marshal.Copy(data, dataArray, 0, dataInSize);

                        callbackPointer.GetCallbackRef()(nSeq , dataArray, dataInSize);
                    }
                    catch (OutOfMemoryException exception)
                    {
                        BufferAcqSamples bufferedSamples = LazyObject.Value;
                        lock (bufferedSamples)
                        {
                            bufferedSamples.actUncaughtException();
                            Debug.Log("Executing preventive approaches to deal with a potential OutOfMemoryException:\n" + exception.StackTrace);
                        }
                    }
                }
                else
                {
                    Debug.Log("A new C++ exception was found...");
                    // Check if the current exception could be an uncaught one.
                    BufferAcqSamples bufferedSamples = LazyObject.Value;
                    lock (bufferedSamples)
                    {
                        // Activate flag in BufferedSamples object stating that an uncaught exception exists.
                        bufferedSamples.actUncaughtException();
                    }

                    throw new Exception(exceptionCode.ToString() + " | " + exceptionDescription);
                }
            }
            catch (OutOfMemoryException exception)
            {
                BufferAcqSamples bufferedSamples = LazyObject.Value;
                lock (bufferedSamples)
                {
                    bufferedSamples.actUncaughtException();
                    Debug.Log("Executing preventive approaches to deal with a potential OutOfMemoryException:\n" + exception.StackTrace);
                }
            }

            return true;
        }
    }

    // Callback Handler (function invoked during signal acquisition, being essential to ensure the 
    // communication between our C++ API and the Unity project.
    bool CallbackHandler(int nSeq, int[] data, int dataLength)
    {
        // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
        BufferAcqSamples bufferedSamples = LazyObject.Value;
        lock (bufferedSamples)
        {
            // Storage of the received data samples.
            // Samples are organized in a sequential way, so if channels 1 and 4 are active it means that
            // data[0] will contain the sample value of channel 1 while data[1] is the sample collected on channel 4.
            bufferedSamples.addSamples(nSeq, data);
        }
        return true;
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
                Debug.Log("Real-Time Data Acquisition stopped due to the lost of connection with PLUX Device.");
            }

            // Reboot variables.
            ActiveChannelsStr = "";

            // Update global flag.
            AcquisitionStopped = true;

            // Reboot AcquisitionThread
            AcquisitionThread = null;
        }

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
        Debug.Log("Scanning Thread Started with Success !");
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
            for (int domainNbr = 0; domainNbr < domains.Count; domainNbr++)
            {
                // List of available Devices.
                System.IntPtr listDevicesByDomain = GetDetectableDevices(domains[domainNbr]);
                List<System.IntPtr> listDevicesByType = new List<IntPtr>() {listDevicesByDomain};

                for (int k = 0; k < listDevicesByType.Count; k++)
                {
                    // Convert listDevices (in a String format) to an array.
                    string[] tempListDevices = Marshal.PtrToStringAnsi(listDevicesByType[k]).Split('&');

                    // Add elements to the returnable list.
                    for (int i = 0; i < tempListDevices.Length - 1; i++)
                    {
                        listDevices.Add(tempListDevices[i]);
                    }
                }
            }

            // Store list of found devices in a global variable shared between threads.
            this.PluxDevsFound = listDevices;

            // Send data (list of devices found) to the MAIN THREAD.
            UnityThreadHelper.Dispatcher.Dispatch(() => ScanResultsCallback(this.PluxDevsFound));
        }
        catch (ExecutionEngineException exc)
        {
            BufferAcqSamples bufferedSamples = LazyObject.Value;
            lock (bufferedSamples)
            {
                bufferedSamples.actUncaughtException();
            }
        }
    }

    // Definition of the callback function responsible for managing the acquired data (which is defined on users Unity script).
    // callbackHandler -> A function pointer to the callback that will receive the acquired data samples on the Unity side.
    public bool SetCallbackHandler(FPtr callbackHandler)
    {
        callbackPointer = new CallbackManager(callbackHandler);
        return true;
    }

    // "Getting" method for determination of the number of used channels during the acquisition.
    public int GetNbrChannelsUnity()
    {
        return GetNbrChannels();
    }

    // "Getting" method for checking the state of the communication flag.
    public bool GetCommunicationFlagUnity()
    {
        return GetCommunicationFlag();
    }

    // "Getting" method dedicated to check the battery level of the device.
    public int GetBatteryUnity()
    {
        return GetBattery();
    }

    // "Getting" method intended to check the type of the connected device.
    public string GetDeviceTypeUnity()
    {
        return Marshal.PtrToStringAnsi(GetDeviceType());
    }

    // Class that manages the reference to callbackPointer.
    public class CallbackManager
    {
        public FPtr callbackReference;
        public CallbackManager(FPtr callbackPointer)
        {
            callbackReference = callbackPointer;
        }

        public FPtr GetCallbackRef()
        {
            return callbackReference;
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
}
