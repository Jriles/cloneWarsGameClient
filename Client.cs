using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using GlobalHelpers;
using GlobalData;
//multiplayer testing
#if UNITY_EDITOR
using ParrelSync;
#endif

public class ClientConstants {
  public const string lobbyClientContext = "lobbyClient";
  public const string gameClientContext = "gameClient";
}

public class Client : MonoBehaviour
{
    public UdpClient gameClient { get; private set; }
    public UdpClient lobbyClient { get; private set; }
    public GameManager gameManager;
    Thread receiveThread;
    private ThreadManager threadManager;
    private string playerApiIdKey = "playerApiId";

    void Start()
    {
        threadManager = GetComponent<ThreadManager>();
    }

    // init
    private void CreateReceiveThread (UdpClient client, string context)
    {
        receiveThread = new Thread(
            new ThreadStart(() => ReceiveData(client, context)));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    // receive thread
    private void ReceiveData (UdpClient client, string receiveContext)
    {
        while (true)
        {
            try
            {
              if (client.Available > 0)
              {
                  IPEndPoint remote = null;
                  byte[] rbytes = client.Receive(ref remote);
                  string received = Encoding.UTF8.GetString(rbytes);
                  JObject msgJSON = JObject.Parse(received);

                  switch (receiveContext)
                  {
                    case ClientConstants.gameClientContext:
                      ThreadManager.ExecuteOnMainThread(
                        new Action(
                            () => {
                                Debug.Log("forwarding message");
                                gameManager.RouteClientMessage(msgJSON);
                            }
                        ));
                      break;
                  }
                  
              }
            }
            catch (Exception err)
            {
                Debug.Log(err.ToString());
            }
        }
    }

    public void ConnectToGameServer (
      string weapon, 
      string gamerTag, 
      string playerApiId, 
      string sessionToken,
      string serverAddress,
      int serverPort
    )
    {
      gameClient = new UdpClient(FindAvailableUDPPort());
      gameClient.Connect(serverAddress, serverPort);

      CreateReceiveThread(gameClient, ClientConstants.gameClientContext);

      JObject connectMsg = new JObject(
        new JProperty(GlobalConstants.GameMsgOpCodeKey, 0),
        //what weapon they picked in the menus
        new JProperty(GlobalConstants.GameMsgContentKey, new JObject(
          new JProperty(GlobalConstants.GameMsgWeaponKey, weapon),
          new JProperty(GlobalConstants.GamerTagNameKey, gamerTag),
          new JProperty(playerApiIdKey, playerApiId),
          new JProperty(GlobalConstants.SessionTokenKey, sessionToken)
        ))
      );
      SendMessageToServer(connectMsg, gameClient);
    }
    public void SendMessageToServer (JObject message, UdpClient client)
    {
      string msgStr = message.ToString();
      byte[] bytes = Encoding.UTF8.GetBytes(msgStr);

      client.Send(bytes, bytes.Length);
    }

    private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(0, port: 0);
    private static int FindAvailableUDPPort ()
    {
      using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
      {
          socket.Bind(DefaultLoopbackEndpoint);
          return ((IPEndPoint)socket.LocalEndPoint).Port;
      }
    }
}
