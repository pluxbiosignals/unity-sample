using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using DllExportTest;
//using unity_api;

namespace Assets.Scripts
{
    public class PluxUnityInterface : MonoBehaviour
    {
        // Declaration of public variables.
        // [Graphical User Interface Objects]
        public Dropdown DeviceDropdown;
        public Button StartButton;
        public Button StopButton;
        public Button ScanButton;
        public Button AboutButton;
        public Button ConnectButton;
        public Button ChangeChannel;
        public InputField SamplingRateInput;
        public InputField ResolutionInput;
        public Dropdown ResolutionDropdown;
        public Toggle CH1Toggle;
        public Toggle CH2Toggle;
        public Toggle CH3Toggle;
        public Toggle CH4Toggle;
        public Toggle CH5Toggle;
        public Toggle CH6Toggle;
        public Toggle CH7Toggle;
        public Toggle CH8Toggle;
        public GameObject SamplingRateInfoPanel;
        public GameObject SelectedChannelPanel;
        public GameObject ChannelSelectioInfoPanel;
        public GameObject ConnectInfoPanel;
        public GameObject BatteryIconUnknown;
        public GameObject BatteryIcon0;
        public GameObject BatteryIcon10;
        public GameObject BatteryIcon50;
        public GameObject BatteryIcon100;
        public GameObject PlotIcon;
        public GameObject AcquiringIcon;
        public GameObject GreenFlag;
        public GameObject RedFlag;
        public GameObject TransparencyLevel;
        public Text ConnectText;
        public Text BatteryLevel;
        public Text CurrentChannel;
        public RectTransform GraphContainer;
        [SerializeField] public Sprite DotSprite;
        public WindowGraph GraphZone;

        // [Delegate References]
        // Delegates (needed for callback purposes).
        public delegate bool FPtr(int nSeq, IntPtr dataIn, int dataInSize);

        // [Generic Variables]
        public PluxDeviceManager PluxDevManager;
        private static string MultiThreadString = "";
        public List<List<int>> MultiThreadList = null;
        public List<int> MultiThreadSubList = null;
        public List<int> ActiveChannels;
        public List<string> ListDevices;
        public int tempInt;
        public int LastLenMultiThreadString = 0;
        public int GraphWindSize = -1;
        public bool FirstPlot = true;
        public List<string> ResolutionDropDownOptions = new List<string>() {"8", "16"};
        public int VisualizationChannel = -1;
        public int SamplingRate;


        // Awake is called when the script instance is being loaded.
        void Awake()
        {
            // Find references to graphical objects.
            GraphContainer = transform.Find("WindowGraph/GraphContainer").GetComponent<RectTransform>();  // User interface zone where the acquired data will be plotted using the "WindowGraph.cs" script.
        }

        // Start is called before the first frame update
        void Start()
        {
            // Welcome Message, showing that the communication between C++ dll and Unity was established correctly.
            Debug.Log("Connection between C++ Interface and Unity established with success !\n");
            PluxDevManager = new PluxDeviceManager();
            int welcomeNumber = PluxDevManager.WelcomeFunctionUnity();
            Debug.Log("Welcome Number: " + welcomeNumber);

            // Initialization of Variables.      
            MultiThreadList = new List<List<int>>();
            ActiveChannels = new List<int>();

            // Initialization of graphical zone.
            WindowGraph.IGraphVisual graphVisual = new WindowGraph.LineGraphVisual(GraphContainer, DotSprite, new Color(0, 158, 227, 0), new Color(0, 158, 227));
            GraphContainer = graphVisual.GetGraphContainer();
            GraphZone = new WindowGraph(GraphContainer, graphVisual);
            GraphZone.ShowGraph(new List<int>() { 0 }, graphVisual, -1, (int _i) => "" + (_i), (float _f) => Mathf.RoundToInt(_f) + "k");
        }

        // Update function, being constantly invoked by Unity.
        void Update()
        {
            // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
            lock (MultiThreadString)
            {
                if (MultiThreadString.Length > 0)
                {
                    // Update counter, preparing for the next iterations.
                    LastLenMultiThreadString = MultiThreadString.Length;

                    // Split our string, taking into consideration our separator.
                    string[] MultiThreadArray = MultiThreadString.Split('#');
                    // Each entry of the MultiThreadArray will contain a package of data from the different active channels.
                    for (int i = 0; i < MultiThreadArray.Length - 1; i++)
                    {
                        string[] MultiThreadSubArray = MultiThreadArray[i].Split('&');
                        for (int j = 0; j < MultiThreadSubArray.Length - 1; j++)
                        {
                            // Inclusion of acquired data in our global data structure.
                            if (MultiThreadSubArray[j] != "")
                            {
                                tempInt = Int32.Parse(MultiThreadSubArray[j]);
                                MultiThreadList[ActiveChannels[j]].Add(tempInt);
                            }
                        }
                    }

                    // Reboot string.
                    MultiThreadString = "";

                    // Check if list contains more than 20 seconds of data [Memory Release].
                    if (MultiThreadList[VisualizationChannel].Count > 2 * GraphWindSize)
                    {
                        for (int k = 1; k < MultiThreadList.Count; k++)
                        {
                            // Check if the current channel is an active channel.
                            if (ActiveChannels.Contains(k))
                            {
                                MultiThreadList[k] = MultiThreadList[k].GetRange(MultiThreadList[k].Count - 2 * GraphWindSize, 2 * GraphWindSize);
                            }
                        }
                    }

                    // Creation of the first graphical representation of the results.
                    if (MultiThreadList[VisualizationChannel].Count >= 0)
                    {
                        if (MultiThreadList[VisualizationChannel].Count != 0 && MultiThreadList[VisualizationChannel].Count > GraphWindSize && FirstPlot == true)
                        {
                            // Update flag (after this step we won't enter again on this statement).
                            FirstPlot = false;

                            // Hide Acquiring Icon Image.
                            //AcquiringIcon.SetActive(false);

                            // Plot first set of data.
                            // Subsampling if sampling rate is bigger than 100 Hz.
                            List<int> subSamplingList = GetSubSampleList(MultiThreadList[VisualizationChannel], SamplingRate, GraphWindSize);
                            GraphZone.ShowGraph(subSamplingList, null, -1, (int _i) => "-" + (GraphWindSize - _i), (float _f) => Mathf.RoundToInt(_f / 1000) + "k");
                        }
                        // Update plot.
                        else if (FirstPlot == false)
                        {
                            // Get the values linked with the last 10 seconds of information.
                            MultiThreadSubList = GetSubSampleList(MultiThreadList[VisualizationChannel], SamplingRate, GraphWindSize);
                            for (int j = 0; j < MultiThreadSubList.Count; j++)
                            {
                                GraphZone.UpdateValue(j, MultiThreadSubList[j]);
                            }
                        }
                    }
                }
            }
        }

        // Callback Handler (function invoked during signal acquisition, being essential to ensure the 
        // communication between our C++ API and the Unity project.
        bool CallbackHandler(int nSeq, int[] data, int dataLength)
        {
            // Lock is an essential step to ensure that variables shared by the same thread will not be accessed at the same time.
            lock (MultiThreadString)
            {
                //Console.WriteLine("nSeq_: " + nSeq.ToString() + " data_: " + data[0].ToString());
                // Store data in a string format (sharable variable).
                MultiThreadString += "#";

                // The resulting dataArray will have in each entry the sampled value of the respective channel.
                // Samples are organized in a sequential way, so if channels 1 and 4 are active it means that
                // data[0] will contain the sample value of channel 1 while data[1] is the sample collected on channel 4.
                for (int i = 0; i < data.Length; i++)
                {
                    MultiThreadString += data[i].ToString() + "&";
                }
            }
            return true;
        }

        // Method invoked when the application was closed.
        void OnApplicationQuit()
        {
            // Disconnect from device.
            PluxDevManager.DisconnectPluxDev();
            Debug.Log("Application ending after " + Time.time + " seconds");
        }

        // ===========================================================================================================================================
        // ================================ Functions invoked when after a specific interaction with GUI elements ====================================
        // ===========================================================================================================================================
        // Function invoked during the onClick event of "ScanButton".
        public void ScanButtonFunction()
        {
            try
            {
                // List of available Devices.
                this.ListDevices = PluxDevManager.GetDetectableDevicesUnity("BTH");
                
                // Enable Dropdown if the list of devices is not empty.
                if (this.ListDevices.Count != 0)
                {
                    // Add the new options to the drop-down box included in our GUI.
                    //Create a List of new Dropdown options
                    List<string> dropDevices = new List<string>();

                    // Convert array to list format.
                    dropDevices.AddRange(this.ListDevices);

                    // A check into the list of devices.
                    dropDevices = dropDevices.GetRange(0, dropDevices.Count - 1);
                    for (int i = dropDevices.Count - 1; i >= 0; i--)
                    {
                        // Accept only strings containing "BTH" or "BLE" substrings "flagging" a PLUX Bluetooth device.
                        if (!dropDevices[i].Contains("BTH") && !dropDevices[i].Contains("BLE"))
                        {
                            dropDevices.RemoveAt(i);
                        }
                    }

                    // Raise an exception if none device was detected.
                    if (dropDevices.Count == 0)
                    {
                        throw new ArgumentException();
                    }

                    //Clear the old options of the Dropdown menu
                    DeviceDropdown.ClearOptions();

                    //Add the options created in the List above
                    DeviceDropdown.AddOptions(dropDevices);

                    // Enable drop-down and Connect button if a PLUX Device was detected .
                    DeviceDropdown.interactable = true;
                    ConnectButton.interactable = true;

                    // Hide info message.
                    ConnectInfoPanel.SetActive(false);

                }
            }
            catch (Exception e)
            {
                if (e is ExecutionEngineException || e is ArgumentException)
                {
                    // Show info message.
                    ConnectInfoPanel.SetActive(true);

                    // Hide object after 5 seconds.
                    StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));

                    // Disable Drop-down.
                    DeviceDropdown.interactable = false;
                }
            }
        }

        // Function invoked during the onClick event of "ConnectButton".
        public void ConnectButtonFunction()
        {
            try
            {
                // Change the color and text of "Connect" button.
                if (ConnectText.text == "Connect")
                {
                    // Specification of the callback function (defined on this/the user Unity script) which will receive the acquired data
                    // samples as inputs.
                    PluxDevManager.SetCallbackHandler(CallbackHandler);

                    // Get the selected device.
                    string selectedDevice = this.ListDevices[DeviceDropdown.value];

                    // Connection with the device.
                    Debug.Log("Trying to establish a connection with device " + selectedDevice);
                    PluxDevManager.PluxDev(selectedDevice);
                    Debug.Log("Connection with device " + selectedDevice + " established with success!");

                    ConnectText.text = "Disconnect";
                    GreenFlag.SetActive(true);
                    RedFlag.SetActive(false);

                    // Enable "Device Configuration" panel options.
                    SamplingRateInput.interactable = true;
                    ResolutionInput.interactable = true;
                    ResolutionDropdown.interactable = true;

                    // Enable channel selection buttons accordingly to the type of device.
                    string devType = PluxDevManager.GetDeviceTypeUnity();
                    if (devType == "MuscleBAN BE Plux")
                    {
                        CH1Toggle.interactable = true;
                    }
                    else if (devType == "BITalino")
                    {
                        CH1Toggle.interactable = true;
                        CH2Toggle.interactable = true;
                        CH3Toggle.interactable = true;
                        CH4Toggle.interactable = true;
                        CH5Toggle.interactable = true;
                        CH6Toggle.interactable = true;

                        //Clear the old options of the Dropdown menu
                        ResolutionDropdown.ClearOptions();

                        //Add the options created in the List above
                        ResolutionDropdown.AddOptions(new List<string>() { "8" });
                    }
                    else if (devType == "biosignalsplux")
                    {
                        CH1Toggle.interactable = true;
                        CH2Toggle.interactable = true;
                        CH3Toggle.interactable = true;
                        CH4Toggle.interactable = true;
                        CH5Toggle.interactable = true;
                        CH6Toggle.interactable = true;
                        CH7Toggle.interactable = true;
                        CH8Toggle.interactable = true;
                    }
                    else if (devType == "OpenBANPlux")
                    {
                        CH1Toggle.interactable = true;
                        CH2Toggle.interactable = true;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    // Enable Start and Device Configuration buttons.
                    StartButton.interactable = true;

                    // Disable Connect Button.
                    //ConnectButton.interactable = false;

                    // Hide show Info message if it is active.
                    ConnectInfoPanel.SetActive(false);

                    // Update Battery Level.
                    int batteryLevel;
                    batteryLevel = PluxDevManager.GetBatteryUnity();

                    // Battery icon accordingly to the battery level.
                    List<GameObject> ListBatteryIcons = new List<GameObject>() { BatteryIcon0, BatteryIcon10, BatteryIcon50, BatteryIcon100, BatteryIconUnknown };
                    GameObject currImage;
                    if (batteryLevel > 50)
                    {
                        BatteryIcon100.SetActive(true);
                        currImage = BatteryIcon100;
                    }
                    else if (batteryLevel <= 50 && batteryLevel > 10)
                    {
                        BatteryIcon50.SetActive(true);
                        currImage = BatteryIcon50;
                    }
                    else if (batteryLevel <= 10 && batteryLevel > 1)
                    {
                        BatteryIcon10.SetActive(true);
                        currImage = BatteryIcon10;
                    }
                    else
                    {
                        BatteryIcon0.SetActive(true);
                        currImage = BatteryIcon0;
                    }

                    // Disable the remaining images.
                    foreach (var batImg in ListBatteryIcons)
                    {
                        if (batImg != currImage)
                        {
                            batImg.SetActive(false);
                        }
                    }

                    // Show the quantitative battery value.
                    BatteryLevel.text = batteryLevel.ToString() + "%";
                }
                else if (ConnectText.text == "Disconnect")
                {
                
                    // Disconnect from device.
                    PluxDevManager.DisconnectPluxDev();

                    ConnectText.text = "Connect";
                    GreenFlag.SetActive(false);
                    RedFlag.SetActive(true);

                    // Disable "Device Configuration" panel options.
                    SamplingRateInput.interactable = false;
                    SamplingRateInput.text = "1000";
                    ResolutionInput.interactable = false;
                    ResolutionDropdown.interactable = false;

                    // Disable channel selection buttons.
                    CH1Toggle.interactable = false;
                    CH2Toggle.interactable = false;
                    CH3Toggle.interactable = false;
                    CH4Toggle.interactable = false;
                    CH5Toggle.interactable = false;
                    CH6Toggle.interactable = false;
                    CH7Toggle.interactable = false;
                    CH8Toggle.interactable = false;

                    // Disable Start and Device Configuration buttons.
                    StartButton.interactable = false;

                    // Disable the battery icons.
                    List<GameObject> ListBatteryIcons = new List<GameObject>() { BatteryIcon0, BatteryIcon10, BatteryIcon50, BatteryIcon100 };
                    foreach (var batImg in ListBatteryIcons)
                    {
                        batImg.SetActive(false);
                    }
                    BatteryIconUnknown.SetActive(true);

                    // Show the quantitative battery value.
                    BatteryLevel.text = "";

                    // Disable Drop-down options.
                    DeviceDropdown.ClearOptions();

                    //Add the options created in the List above
                    DeviceDropdown.AddOptions(new List<string>(){"Select Device"});

                    // Disable drop-down and Connect button if a PLUX Device was disconnected.
                    DeviceDropdown.interactable = false;
                    ConnectButton.interactable = false;

                    // Show PlotIcon.
                    PlotIcon.SetActive(true);
                    TransparencyLevel.SetActive(true);

                    // Reboot of global variables.
                    RebootVariables();
                }
            }
            catch(Exception e)
            {
                // Print information about the exception.
                Debug.Log(e);

                // Show info message.
                ConnectInfoPanel.SetActive(true);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));
            }
        }

        // Function invoked during the onClick event of "StartButton".
        public void StartButtonFunction()
        {
            // Get Device Configuration input values.
            SamplingRate = Int32.Parse(SamplingRateInput.text);
            int resolution = Int32.Parse(ResolutionDropDownOptions[ResolutionDropdown.value]);

            // Update graphical window size variable (the plotting zone should contain 10 seconds of data).
            GraphWindSize = SamplingRate * 10;

            // Number of Active Channels.
            int nbrChannels = 0;
            Toggle[] toggleArray = new Toggle[] { CH1Toggle, CH2Toggle, CH3Toggle, CH4Toggle, CH5Toggle, CH6Toggle, CH7Toggle, CH8Toggle };
            MultiThreadList.Add(new List<int>(Enumerable.Repeat(0, GraphWindSize).ToList()));
            for (int i = 0; i < toggleArray.Length; i++)
            {
                if (toggleArray[i].isOn == true)
                {
                    // Preparation of a string that will be communicated to our .dll
                    // This string will be formed by "1" or "0" characters, identifying sequentially which channels are active or not.
                    ActiveChannels.Add(i + 1);

                    // Definition of the first active channel.
                    if (VisualizationChannel == -1)
                    {
                        VisualizationChannel = i + 1;

                        // Update the label with the Current Channel Number.
                        CurrentChannel.text = "CH" + VisualizationChannel;
                    }

                    nbrChannels++;
                }

                // Dictionary that stores all the data received from .dll API.
                MultiThreadList.Add(new List<int>(Enumerable.Repeat(0, GraphWindSize).ToList()));
            }

            // Check if at least one channel is active.
            if (ActiveChannels.Count != 0)
            {
                // Start of Acquisition.
                //Thread.CurrentThread.Name = "MAIN_THREAD";
                if (PluxDevManager.GetDeviceTypeUnity() != "MuscleBAN BE Plux")
                {
                    PluxDevManager.StartAcquisitionUnity(SamplingRate, ActiveChannels, resolution);
                }
                else
                {
                    // Definition of the frequency divisor (subsampling ratio).
                    int freqDivisor = 10;
                    PluxDevManager.StartAcquisitionMuscleBanUnity(SamplingRate, ActiveChannels, resolution, freqDivisor);
                }
              
                // Enable StopButton.
                StopButton.interactable = true;

                // Disable ConnectButton.
                ConnectButton.interactable = false;

                // Disable Start Button.
                StartButton.interactable = false;

                // Hide PlotIcon and show AcquiringIcon.
                PlotIcon.SetActive(false);
                TransparencyLevel.SetActive(false);
                //AcquiringIcon.SetActive(true);

                // Hide panel with the "Change Channel" button.
                SelectedChannelPanel.SetActive(true);
                if (ActiveChannels.Count == 1)
                {
                    ChangeChannel.interactable = false;
                }
                else
                {
                    ChangeChannel.interactable = true;
                }

                // Disable About Button to avoid entering a new scene during the acquisition.
                AboutButton.interactable = false;
            }
            else
            {
                // Show Info Message.
                ChannelSelectioInfoPanel.SetActive(true);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, ChannelSelectioInfoPanel));
            }
        
        }

        // Function invoked during the onClick event of "StopButton".
        public void StopButtonFunction()
        {
            // Invoke stop function from PluxDeviceManager.
            PluxDevManager.StopAcquisitionUnity();

            // Disable StopButton.
            StopButton.interactable = false;

            // Enable About Button.
            AboutButton.interactable = true;

            // Enable ConnectButton.
            ConnectButton.interactable = true;

            // Stop Message.
            Debug.Log("Acquisition was Stopped :D");

        }

        // Function invoked during the onClick event of the About Button.
        public void AboutButtonFunction()
        {
            // Load About Scene.
            SceneManager.LoadScene("AboutWindow");

            // Reboot variables.
            if (ConnectText.text == "Disconnect")
            {
                ConnectButton.onClick.Invoke();
            }
            RebootVariables();
        }

        // Function invoked during the onValueChanged event of the Sampling Rate Input.
        public void SamplingRateOnChangeFunction()
        {
            // Check if "-" symbol is being introduced.
            if (SamplingRateInput.text != "")
            {
                if (SamplingRateInput.text.Contains("-") || SamplingRateInput.text.Length > 4 || Int32.Parse(SamplingRateInput.text) > 4000)
                {
                    // Show info message.
                    SamplingRateInfoPanel.SetActive(true);

                    // Place the standard Sampling rate.
                    SamplingRateInput.text = "1000";

                    // Hide object after 5 seconds.
                    StartCoroutine(RemoveAfterSeconds(5, SamplingRateInfoPanel));
                }
            }
        }

        // Function invoked during the onValueChanged event of the Sampling Rate Input.
        public void SamplingRateOnEndEditFunction()
        {
            if (SamplingRateInput.text == "")
            {
                SamplingRateInput.text = "1000";
            }
        }

        // Function invoked during the onClick event of the "Change Channel" button.
        public void ChangeChannelClickFunction()
        {
            int indexOfVisualizationChn = ActiveChannels.IndexOf(VisualizationChannel);
            if (indexOfVisualizationChn == ActiveChannels.Count - 1)
            {
                VisualizationChannel = ActiveChannels[0];
            }
            else
            {
                VisualizationChannel = ActiveChannels[indexOfVisualizationChn + 1];
            }

            // Update the label with the Current Channel Number.
            CurrentChannel.text = "CH" + VisualizationChannel;
        }

        // Coroutine used to fade out info messages after x seconds.
        IEnumerator RemoveAfterSeconds(int seconds, GameObject obj)
        {
            yield return new WaitForSeconds(seconds);
            obj.SetActive(false);
        }

        // Function used to subsample acquired data.
        public List<int> GetSubSampleList(List<int> originalList, int samplingRate, int graphWindowSize)
        {
            // Subsampling if sampling rate is bigger than 100 Hz.
            List<int> subSamplingList = new List<int>();
            int subSamplingLevel = 1;
            if (samplingRate > 100)
            {
                // Subsampling Level.
                subSamplingLevel = samplingRate / 100;
                for (int i = 0; i < originalList.Count; i++)
                {
                    if (i % subSamplingLevel == 0)
                    {
                        subSamplingList.Add(originalList[i]);
                    }
                }
            }
            else
            {
                subSamplingList = originalList;
            }

            return subSamplingList.GetRange(subSamplingList.Count - (graphWindowSize / subSamplingLevel), (graphWindowSize / subSamplingLevel));
        }

        public void RebootVariables()
        {
            MultiThreadList = new List<List<int>>();
            ActiveChannels = new List<int>();
            lock (MultiThreadString)
            {
                MultiThreadString = "";
            }
            MultiThreadSubList = null;
            LastLenMultiThreadString = 0;
            GraphWindSize = -1;
            VisualizationChannel = -1;
        }

    }
}
