using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;

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
    private static extern void StartAcquisitionByNbr(int samplingRate, int numberOfChannel, int resolution, FPtr callbackFunction);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisition(int samplingRate, string activeChannels, int resolution, FPtr callbackFunction);
    [DllImport("plux_unity_interface")]
    private static extern void StartAcquisitionMuscleBan(int samplingRate, string activeChannels, int resolution, int freqDivisor, FPtr callbackFunction);
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
    private static extern System.IntPtr GetDeviceType();

    // Delegates (needed for callback purposes).
    public delegate bool FPtr(int nSeq, IntPtr dataIn, int dataInSize);
    public delegate bool FPtrUnity(int nSeq, int[] dataIn, int dataInSize);

    // [Generic Variables]
    public Thread AcquisitionThread;
    public int SamplingRate;
    public string ActiveChannelsStr = "";
    public bool AcquisitionStopped = true;
    private static CallbackManager callbackPointer;

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
        PluxDevUnity(macAddress);
    }

    public void DisconnectPluxDev()
    {
        if (AcquisitionThread != null)
        {
            Console.WriteLine("Thread Unity Forced to Close");
            if (AcquisitionStopped == false)
            {
                StopAcquisitionUnity();
            }
            DisconnectPluxDevUnity();
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
        // callbackPointer -> Pointer to the callback function that will be used to send/communicate the data acquired by PLUX devices, i.e., this callback 
        //                    function will be invoked during the data acquisition process and through his inputs the acquired data will become accessible.
        //                    So, for receiving data on a third-party application it will only be necessary to redefine this callbackFunction.
        FPtr callbackPointerUnity = new FPtr(CallbackHandlerUnity);
        if (callbackPointerUnity == null)
        {
            return;
        }

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
        StartAcquisition(samplingRate, ActiveChannelsStr, resolution, callbackPointerUnity);

        // Start Communication Loop.
        StartLoopUnity();

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
        // callbackPointer -> Pointer to the callback function that will be used to send/communicate the data acquired by PLUX devices, i.e., this callback 
        //                    function will be invoked during the data acquisition process and through his inputs the acquired data will become accessible.
        //                    So, for receiving data on a third-party application it will only be necessary to redefine this callbackFunction.
        FPtr callbackPointerUnity = new FPtr(CallbackHandlerUnity);
        if (callbackPointerUnity == null)
        {
            return;
        }

        // Start of the real-time acquisition.
        StartAcquisitionByNbr(samplingRate, numberOfChannels, resolution, callbackPointerUnity);

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
        // callbackPointer -> Pointer to the callback function that will be used to send/communicate the data acquired by PLUX devices, i.e., this callback 
        //                    function will be invoked during the data acquisition process and through his inputs the acquired data will become accessible.
        //                    So, for receiving data on a third-party application it will only be necessary to redefine this callbackFunction.
        FPtr callbackPointerUnity = new FPtr(CallbackHandlerUnity);
        if (callbackPointerUnity == null)
        {
            return;
        }

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
        StartAcquisitionMuscleBan(samplingRate, ActiveChannelsStr, resolution, freqDivisor, callbackPointerUnity);
        
        // Start Communication Loop.
        StartLoopUnity();

        // Update global flag.
        AcquisitionStopped = false;
    }

    // Trigger the start of the communication loop (between PLUX device and computer).
    private void StartLoopUnity()
    {
        // Creation of new thread to manage the communication loop.
        AcquisitionThread = new Thread(StartLoop);
        AcquisitionThread.Name = "ACQUISITION";
        AcquisitionThread.Start();
        Debug.Log("Acquisition Thread Started with Success !");
    }

    // Callback function responsible for receiving the acquired data samples from the communication loop started by StartLoopUnity().
    private bool CallbackHandlerUnity(int nSeq, IntPtr dataIn, int dataInSize)
    {
        lock (callbackPointer)
        {
            // Convert our data pointer to an array format.
            int[] dataArray = new int[dataInSize];
            Marshal.Copy(dataIn, dataArray, 0, dataInSize);

            // DEBUG
            //Console.WriteLine("nSeq: " + nSeq.ToString() + " data: " + dataArray[0].ToString());

            callbackPointer.GetCallbackRef()(nSeq, dataArray, dataInSize);

            return true;
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
            Console.WriteLine("Thread State: " + AcquisitionThread.ThreadState);
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

        return forceFlag;
    }

    // Class method intended to find the list of detectable devices through Bluetooth communication.
    // domain -> String that defines which domain will be used while searching for PLUX devices 
    //           [Valid Options: "BTH" -> classic Bluetooth; "BLE" -> Bluetooth Low Energy; "USB" -> Through USB connection cable]
    public List<string> GetDetectableDevicesUnity(string domain)
    {
        // List of available Devices.
        System.IntPtr listDevicesBTH = GetDetectableDevices(domain);
        List<System.IntPtr> listDevicesByType = new List<IntPtr>() { listDevicesBTH };
        List<string> listDevices = new List<string>();

        for (int k = 0; k < listDevicesByType.Count; k++)
        {
            // Convert listDevices (in a String format) to an array.
            string[] tempListDevices = Marshal.PtrToStringAnsi(listDevicesByType[k]).Split('&');

            // Add elements to the returnable list.
            for (int i = 0; i < tempListDevices.Length; i++)
            {
                listDevices.Add(tempListDevices[i]);
            }
        }
        return listDevices;
    }

    // Definition of the callback function responsible for managing the acquired data (which is defined on users Unity script).
    // callbackHandler -> A function pointer to the callback that will receive the acquired data samples on the Unity side.
    public bool SetCallbackHandler(FPtrUnity callbackHandler)
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
        public FPtrUnity callbackReference;
        public CallbackManager(FPtrUnity callbackPointer)
        {
            callbackReference = callbackPointer;
        }

        public FPtrUnity GetCallbackRef()
        {
            return callbackReference;
        }
    }
}
