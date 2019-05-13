using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class PluxDeviceManagerTests
    {
        string deviceMacAddr = null; // To speed up running individual tests, replace this with a valid device address:
        PluxDeviceManager pluxManager;

        [SetUp]
        public void OneTimeSetup()
        {
            pluxManager = new PluxDeviceManager();

            if (string.IsNullOrEmpty(deviceMacAddr))
            {
                var devices = pluxManager.GetDetectableDevicesUnity("BTH");
                if (devices.Count <= 0)
                    Assert.Fail("Can't run tests without a device to connect to");

                deviceMacAddr = devices[0].Remove(0, 3);
            }
        }

        // Ensure testing fixture works
        [Test]
        public void Passes()
        {
            Assert.Pass();
        }

        [Test]
        public void DestroyIsSafe()
        {
            pluxManager.DisconnectPluxDev();
            Assert.Pass();
        }

        [Test]
        public void CanInitAndDestroy()
        {
            pluxManager.PluxDev(deviceMacAddr);
            pluxManager.DisconnectPluxDev();
            Assert.Pass();
        }

        [Test]
        public void CanInitTwice()
        {
            pluxManager.PluxDev(deviceMacAddr);
            pluxManager.DisconnectPluxDev();

            pluxManager.PluxDev(deviceMacAddr);
            pluxManager.DisconnectPluxDev();
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator CanInitTwiceWithDelay()
        {
            pluxManager.PluxDev(deviceMacAddr);
            yield return new WaitForSecondsRealtime(0.25f);
            pluxManager.DisconnectPluxDev();

            yield return new WaitForSecondsRealtime(1.0f);

            pluxManager.PluxDev(deviceMacAddr);
            yield return new WaitForSecondsRealtime(0.25f);
            pluxManager.DisconnectPluxDev();

            Assert.Pass();
        }
    }
}
