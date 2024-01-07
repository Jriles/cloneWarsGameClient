using UnityEngine;
using UnityEngine.SceneManagement;
using GlobalData;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using System.Net;
using System;
using System.Net.Sockets;
using GlobalData;



public class SceneLoader : MonoBehaviour
{
    public string singlePlayerSceneName;
    public string multiPlayerSceneName;
    [SerializeField]
    public InputField gamerTagInput;
    [SerializeField]
    public InputField hostIpAddressInput;
    [SerializeField]
    public Text localIpAddress;
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        string localIp = GetLocalIpAddress();
        localIpAddress.text = "Local ip address " + localIp;
        if (PlayerPrefs.GetString(GlobalConstants.HostIpAddress) != "") {
            localIp = PlayerPrefs.GetString(GlobalConstants.HostIpAddress);
        }
        
        hostIpAddressInput.text = localIp;
    }
    void Update()
    {

    }

    public void LoadMultiPlayerScene() {
        // Check if the scene name is not empty or null
        if (!string.IsNullOrEmpty(multiPlayerSceneName))
        {
            // Load the scene
            SceneManager.LoadScene(multiPlayerSceneName);
        }
        else
        {
            Debug.LogError("Scene name is not specified in the inspector!");
        }
    }

    public void LoadSinglePlayerScene() {
        // Check if the scene name is not empty or null
        if (!string.IsNullOrEmpty(singlePlayerSceneName))
        {
            // Load the scene
            SceneManager.LoadScene(singlePlayerSceneName);
        }
        else
        {
            Debug.LogError("Scene name is not specified in the inspector!");
        }
    }

    public void SetPlayerPrefsAndLoadScene()
    {
        PlayerPrefs.SetString(GlobalConstants.GamerTagNameKey, gamerTagInput.text);
        PlayerPrefs.SetString(GlobalConstants.HostIpAddress, hostIpAddressInput.text);
        LoadMultiPlayerScene();
    }

    string GetLocalIpAddress()
    {
        // Get the host name of the local machine
        string hostName = Dns.GetHostName();

        // Get the IP addresses associated with the host
        IPAddress[] localIPAddresses = Dns.GetHostAddresses(hostName);

        // Try to find a private IPv4 address
        foreach (IPAddress ipAddress in localIPAddresses)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // Check if the address is a private IPv4 address
                if (IsPrivateIPv4Address(ipAddress))
                {
                    return ipAddress.ToString();
                }
            }
        }

        // If no private IPv4 address is found, try to find a link-local address
        foreach (IPAddress ipAddress in localIPAddresses)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // Check if the address is a link-local address
                if (IsLinkLocalIPv4Address(ipAddress))
                {
                    return ipAddress.ToString();
                }
            }
        }

        // If no suitable address is found, return an empty string
        return string.Empty;
    }

    bool IsPrivateIPv4Address(IPAddress ipAddress)
    {
        byte[] addressBytes = ipAddress.GetAddressBytes();
        // Check if the address is in the private IPv4 address range
        return addressBytes[0] == 10 ||
            (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31) ||
            (addressBytes[0] == 192 && addressBytes[1] == 168);
    }

    bool IsLinkLocalIPv4Address(IPAddress ipAddress)
    {
        byte[] addressBytes = ipAddress.GetAddressBytes();
        // Check if the address is in the link-local IPv4 address range
        return addressBytes[0] == 169 && addressBytes[1] == 254;
    }
}
