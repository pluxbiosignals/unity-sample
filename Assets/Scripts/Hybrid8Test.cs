using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Hybrid8Test : MonoBehaviour
{
    // Class Variables
    private PluxDeviceManager pluxDevManager;

    // GUI Objects.
    public Button ScanButton;
    public Button ConnectButton;
    public Button DisconnectButton;
    public Button StartAcqButton;
    public Button StopAcqButton;
    public Dropdown DeviceDropdown;
    public Dropdown SamplingRateDropdown;
    public Dropdown ResolutionDropdown;
    public Dropdown RedIntensityDropdown;
    public Dropdown InfraredIntensityDropdown;
    public Text OutputMsgText;

    // Class constants (CAN BE EDITED BY IN ACCORDANCE TO THE DESIRED DEVICE CONFIGURATIONS)
    [System.NonSerialized]
    public List<string> domains = new List<string>() { "BTH" };
    public int samplingRate = 100;
    public int nbrAcqSamples = 0;

    private int Hybrid8PID = 517;
    private int BiosignalspluxPID = 513;
    private int BitalinoPID = 1538;
    private int MaxLedIntensity = 255;

    // Start is called before the first frame update
    void Start()
    {
        // Initialise object
        pluxDevManager = new PluxDeviceManager(ScanResults, ConnectionDone);

        // Important call for debug purposes by creating a log file in the root directory of the project.
        pluxDevManager.WelcomeFunctionUnity();
    }

    // Update function, being constantly invoked by Unity.
    void Update()
    {
        try
        {
            // Check if there are any exception in the buffer that should be handled.
            if (!pluxDevManager.IsExceptionInBuffer())
            {
                int[][] packageOfData = pluxDevManager.GetPackageOfData(true);

                // Protection against null packages.
                if (packageOfData != null)
                {
                    // Increment our auxiliary counter.
                    nbrAcqSamples += packageOfData.Length;

                    // Show samples with a 2s interval.
                    if (nbrAcqSamples % (2 * samplingRate) == 0)
                    {
                        // Show the first sample (index 0) of the current package of data.
                        string outputString = "Acquired Data:\n";
                        for (int j = 0; j < packageOfData[0].Length; j++)
                        {
                            outputString += packageOfData[0][j] + "\t";
                        }

                        // Show the values in the GUI.
                        OutputMsgText.text = outputString;
                    }
                }
            }
        }
        catch (ExternalException exc)
        {
            ExceptionTreatmentProc(exc.Message);
        }
        catch (Exception exc)
        {
            ExceptionTreatmentProc(exc.Message);
        }
    }

    // Method invoked when the application was closed.
    void OnApplicationQuit()
    {
        try
        {
            // Disconnect from device.
            if (pluxDevManager != null)
            {
                pluxDevManager.DisconnectPluxDev();
                Debug.Log("Application ending after " + Time.time + " seconds");
            }
        }
        catch (Exception exc)
        {
            Debug.Log("Device already disconnected when the Application Quit.");
        }
    }

    /**
     * =================================================================================
     * ============================= GUI Events ========================================
     * =================================================================================
     */

    // Method called when the "Scan for Devices" button is pressed.
    public void ScanButtonFunction()
    {
        // Search for PLUX devices
        pluxDevManager.GetDetectableDevicesUnity(domains);

        // Disable the "Scan for Devices" button.
        ScanButton.interactable = false;
    }

    // Method called when the "Connect to Device" button is pressed.
    public void ConnectButtonFunction()
    {
        // Disable Connect button.
        ConnectButton.interactable = false;

        // Connect to the device selected in the Dropdown list.
        pluxDevManager.PluxDev(DeviceDropdown.options[DeviceDropdown.value].text);
    }

    // Method called when the "Disconnect Device" button is pressed.
    public void DisconnectButtonFunction()
    {
        // Disconnect from the device.
        pluxDevManager.DisconnectPluxDev();

        // Reboot GUI elements state.
        RebootGUI();
    }

    // Method called when the "Start Acquisition" button is pressed.
    public void StartButtonFunction()
    {
        // Get the Sampling Rate and Resolution values.
        samplingRate = int.Parse(SamplingRateDropdown.options[SamplingRateDropdown.value].text);
        int resolution = int.Parse(ResolutionDropdown.options[ResolutionDropdown.value].text);

        // Initializing the sources array.
        List<PluxDeviceManager.PluxSource> pluxSources = new List<PluxDeviceManager.PluxSource>();

        // biosignalsplux Hybrid-8 device (3 sensors >>> 1 Analog + 2 Digital SpO2/fNIRS)
        if (pluxDevManager.GetProductIdUnity() == Hybrid8PID)
        {
            // Add the sources of the digital channels (CH1 and CH2).
            pluxSources.Add(new PluxDeviceManager.PluxSource(1, 1, resolution, 0x03));
            pluxSources.Add(new PluxDeviceManager.PluxSource(2, 1, resolution, 0x03));

            // Define the LED Intensities of both sensors (CH1 and CH2) as: {RED, INFRARED}
            int redLedIntensity = (int) (int.Parse(RedIntensityDropdown.options[RedIntensityDropdown.value].text) * (MaxLedIntensity / 100f)); // A 8-bit value (0-255)
            int infraredLedIntensity = (int)(int.Parse(InfraredIntensityDropdown.options[InfraredIntensityDropdown.value].text) * (MaxLedIntensity / 100f)); // A 8-bit value (0-255)
            int[] ledIntensities = new int[2] { redLedIntensity, infraredLedIntensity };
            pluxDevManager.SetParameter(1, 0x03, ledIntensities);
            pluxDevManager.SetParameter(2, 0x03, ledIntensities);

            // Add the source of the analog channel (CH8).
            pluxSources.Add(new PluxDeviceManager.PluxSource(8, 1, resolution, 0x01));
        }
        // biosignalsplux (2 Analog sensors)
        else if(pluxDevManager.GetProductIdUnity() == BiosignalspluxPID)
        {
            // Starting a real-time acquisition from:
            // >>> biosignalsplux [CH1 and CH8 active]
            pluxSources.Add(new PluxDeviceManager.PluxSource(1, 1, resolution, 0x01));
            pluxSources.Add(new PluxDeviceManager.PluxSource(8, 1, resolution, 0x01));
        }

        // BITalino (2 Analog sensors)
        if (pluxDevManager.GetProductIdUnity() == BitalinoPID)
        {
            // Starting a real-time acquisition from:
            // >>> BITalino [Channels A2 and A5 active]
            pluxDevManager.StartAcquisitionUnity(samplingRate, new List<int>{2, 5}, 10);
        }
        else
        {
            // Start a real-time acquisition with the created sources.
            pluxDevManager.StartAcquisitionBySourcesUnity(samplingRate, pluxSources.ToArray());
        }

        // Enable the "Stop Acquisition" button and disable the "Start Acquisition" button.
        StartAcqButton.interactable = false;
        StopAcqButton.interactable = true;
    }

    // Method called when the "Stop Acquisition" button is pressed.
    public void StopButtonFunction()
    {
        // Stop the real-time acquisition.
        pluxDevManager.StopAcquisitionUnity();

        // Enable the "Start Acquisition" button and disable the "Stop Acquisition" button.
        StartAcqButton.interactable = true;
        StopAcqButton.interactable = false;
    }

    /**
     * =================================================================================
     * ============================= Callbacks =========================================
     * =================================================================================
     */

    // Callback that receives the list of PLUX devices found during the Bluetooth scan.
    public void ScanResults(List<string> listDevices)
    {
        // Enable the "Scan for Devices" button.
        ScanButton.interactable = true;

        if (listDevices.Count > 0)
        {
            // Update list of devices.
            DeviceDropdown.ClearOptions();
            DeviceDropdown.AddOptions(listDevices);

            // Enable the Dropdown and the Connect button.
            DeviceDropdown.interactable = true;
            ConnectButton.interactable = true;

            // Show an informative message about the number of detected devices.
            OutputMsgText.text = "Scan completed.\nNumber of devices found: " + listDevices.Count;
        }
        else
        {
            // Show an informative message stating the none devices were found.
            OutputMsgText.text = "Bluetooth device scan didn't found any valid devices.";
        }
    }

    // Callback invoked once the connection with a PLUX device was established.
    public void ConnectionDone()
    {
        // Disable some GUI elements.
        ScanButton.interactable = false;
        DeviceDropdown.interactable = false;
        ConnectButton.interactable = false;

        // Enable some generic GUI elements.
        if (pluxDevManager.GetProductIdUnity() != BitalinoPID)
        {
            ResolutionDropdown.interactable = true;
        }
        SamplingRateDropdown.interactable = true;
        StartAcqButton.interactable = true;
        DisconnectButton.interactable = true;

        // Enable some biosignalsplux Hybrid-8 specific GUI elements.
        if (pluxDevManager.GetProductIdUnity() == Hybrid8PID)
        {
            RedIntensityDropdown.interactable = true;
            InfraredIntensityDropdown.interactable = true;
        }
    }

    /**
     * =================================================================================
     * ========================== Auxiliary Methods ====================================
     * =================================================================================
     */

    // Method invoked when an exception is raised (in order to stop a real-time acquisition securely).
    // excMsg -> Message describing the raised exception.
    public void ExceptionTreatmentProc(string excMsg)
    {
        // Present the error message to the user.
        OutputMsgText.text = excMsg;

        // Clear packages in memory.
        pluxDevManager.RebootDataBuffer();

        // Stop Acquisition in a secure way.
        pluxDevManager.StopAcquisitionUnity(-1);

        // Reboot GUI elements state.
        RebootGUI();
    }

    /**
     * Auxiliary method used to reboot the GUI elements.
     */
    public void RebootGUI()
    {
        ScanButton.interactable = true;
        ConnectButton.interactable = false;
        DisconnectButton.interactable = false;
        StartAcqButton.interactable = false;
        StopAcqButton.interactable = false;
        DeviceDropdown.interactable = false;
        SamplingRateDropdown.interactable = false;
        ResolutionDropdown.interactable = false;
        RedIntensityDropdown.interactable = false;
        InfraredIntensityDropdown.interactable = false;
    }
}
