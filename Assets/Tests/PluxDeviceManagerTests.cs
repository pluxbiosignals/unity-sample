using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class PluxDeviceManagerTests : MonoBehaviour
    {
        string deviceMacAddr = null; // To speed up running individual tests, replace this with a valid device address:
        PluxDeviceManager pluxManager;

        public void OneTimeSetup()
        {
            pluxManager = new PluxDeviceManager();

            if (string.IsNullOrEmpty(deviceMacAddr))
            {
                var devices = pluxManager.GetDetectableDevicesUnity(new List<string>{"BTH", "BLE"});
                if (devices.Count <= 0)
                    Console.WriteLine("Can't run tests without a device to connect to");

                deviceMacAddr = "BTH00:07:80:4D:2E:AD";
            }
        }

        public void DestroyIsSafe()
        {
            pluxManager.DisconnectPluxDev();
            Console.WriteLine("Disconnected with success!");
        }

        public void CanInitAndDestroy()
        {
            pluxManager.PluxDev(deviceMacAddr);
            Console.WriteLine("Initiated with success!");
            pluxManager.DisconnectPluxDev();
            Console.WriteLine("Initiated and Destroyed with success!");
        }

        public void CanInitTwice()
        {
            pluxManager.PluxDev(deviceMacAddr);
            pluxManager.DisconnectPluxDev();

            pluxManager.PluxDev(deviceMacAddr);
            pluxManager.DisconnectPluxDev();
            Console.WriteLine("Initiated and Destroyed with success twice!");
        }

        public IEnumerator CanInitTwiceWithDelay()
        {
            pluxManager.PluxDev(deviceMacAddr);
            yield return new WaitForSecondsRealtime(0.25f);
            pluxManager.DisconnectPluxDev();

            yield return new WaitForSecondsRealtime(1.0f);

            pluxManager.PluxDev(deviceMacAddr);
            yield return new WaitForSecondsRealtime(0.25f);
            pluxManager.DisconnectPluxDev();

            Console.WriteLine("Initiated and Destroyed with success with delay!");
        }
    }
}
