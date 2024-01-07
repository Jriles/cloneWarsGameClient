using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using GlobalData;
using GlobalHelpers;

//multiplayer testing
#if UNITY_EDITOR
using ParrelSync;
#endif

static class GameManagerConstants
{
    public const int FriendlyPlayerLayer = 3;
    public const int UnfriendlyPlayerLayer = 7;
    public const string DefaultWeapon = "assaultRifle";
    public const string VictoryMessage = "victory";
    public const string DefeatMessage = "defeat";
    public const string DroidScoreBoardStr = "Droid kills: ";
    public const string CloneScoreBoardStr = "Clone kills: ";
    public const string TeamKey = "team";
    public const int DeathAnimationTime = 3000;
    public const int TDMMatchSize = 2;
    //receiver opCodes
    public const int RecPlayerDeetsOp = 0;
    public const int RenderJumpOp = 1;
    public const int RenderWalkingOp = 2;
    public const int NewRotationOp = 4;
    public const int CreatePlayersOp = 5;
    public const int RenderFireWeaponOp = 6;
    public const int RecTookDamageOp = 7;
    public const int RecPlayerDiedOp = 8;
    public const int RespawnPlayerOp = 9;
    public const int ScoreUpdateOp = 10;
    public const int GameOverOp = 11;
    public const int PlayerIdleOp = 12;
    public const int NewPositionOp = 13;
    public const int StopAttackingOp = 14;
}

public class GameManager : MonoBehaviour
{
    private Vector3 team1Spawn = new Vector3(61, 10, -14);
    private Vector3 team2Spawn = new Vector3(95.6f, 7.5f, -14f);
    public GameObject clonePlayerPrefab;
    public GameObject droidPlayerPrefab;
    [SerializeField]
    public GameObject cameraPrefab;
    public GameObject loadingPanel;
    public GameObject gameMsgPanel;
    public GameObject endGamePanel;
    public Text gameMsgText;
    public GameObject clientGameObject;
    Client client;
    private int localPlayerIdx;
    private string localPlayerTeam;
    List<PlayerController> players;
    //weapons
    public GameObject assaultRifle;
    public Text droidScoreBoard;
    public Text cloneScoreBoard;
    private GameObject camera;
    [SerializeField]
    public GameObject quitButton;

    void Start ()
    {
        players = new List<PlayerController>();
        camera = Instantiate(
            cameraPrefab,
            new Vector3(0,0,0),
            Quaternion.identity
        );
        client = (Client) clientGameObject.GetComponent(typeof(Client));
        string gamerTag = PlayerPrefs.GetString(GlobalConstants.GamerTagNameKey);
        string playerApiId = PlayerPrefs.GetString(GlobalConstants.UserIdKey);
        string sessionToken = PlayerPrefs.GetString(GlobalConstants.SessionTokenKey);
        string hostIp = PlayerPrefs.GetString(GlobalConstants.HostIpAddress);
        client.ConnectToGameServer(
            GameManagerConstants.DefaultWeapon, 
            gamerTag, 
            playerApiId, 
            sessionToken, 
            hostIp,
            GlobalConstants.GameServerPort
        );
    }


    //take in a JObject of message received by client from server
    //then use a switch to decide what to do to the game
    // ************************************** //
    // Messages have the following structure:
    // - opCode: an integer that tells the switch what sort of action event has occured
    // - content: a string used to capture any other relevant data
    // - playerId: a string used to hold the relevant players host
    // ************************************** //
    public async void RouteClientMessage (JObject message)
    {
      switch ((int) message[GlobalConstants.GameMsgOpCodeKey])
      {
        case GameManagerConstants.RecPlayerDeetsOp:
            // local client connection response
            AssignLocalPlayerIdx(GetPlayerIdx(message));
            AssignLocalPlayerTeam((string) message[GlobalConstants.GameMsgContentKey]);
            break;
        case GameManagerConstants.RenderJumpOp:
            //render jump case
            PlayerController jumpingPlayer = players[GetPlayerIdx(message)];
            jumpingPlayer.SetPlayerState(PlayerState.Jumping);
            jumpingPlayer.RenderJump();
            break;
        case GameManagerConstants.RenderWalkingOp:
            //render walk right case
            PlayerController player = players[GetPlayerIdx(message)];
            player.SetPlayerState(PlayerState.Walking);
            JToken msgContent = message[GlobalConstants.GameMsgContentKey];
            Vector3 movementDirection = new Vector3(
                Convert.ToSingle((double) msgContent[GlobalConstants.WalkDirKey][GlobalConstants.XVector3Key]),
                Convert.ToSingle((double) msgContent[GlobalConstants.WalkDirKey][GlobalConstants.YVector3Key]),
                Convert.ToSingle((double) msgContent[GlobalConstants.WalkDirKey][GlobalConstants.ZVector3Key])
            );
            float playerYRotation = Convert.ToSingle((double) msgContent[GlobalConstants.PlayerYRotationKey]);

            player.RenderWalk(movementDirection, playerYRotation);
            break;
        case GameManagerConstants.NewRotationOp:
            //render aim weapon case
            PlayerController currentPlayer = players[GetPlayerIdx(message)];
            float newPlayerYRotation = Convert.ToSingle((double) message[GlobalConstants.GameMsgContentKey]);
            currentPlayer.RenderRotationChange(newPlayerYRotation);
            break;
        case GameManagerConstants.CreatePlayersOp:
            //create players case
            CreatePlayers(message[GlobalConstants.GameMsgContentKey] as JArray);
            //HideLoadingPanel();
            //ShowGameMsgPanel();
            //await MatchCountdown();
            //HideGameMsgPanel();
            ActivatePlayers();
            break;
        case GameManagerConstants.RenderFireWeaponOp:
            //render fire weapon case
            PlayerController attackingPlayer = players[GetPlayerIdx(message)];
            Vector3 aimDir = new Vector3(
                Convert.ToSingle((double) message[GlobalConstants.GameMsgContentKey][GlobalConstants.XVector3Key]),
                Convert.ToSingle((double) message[GlobalConstants.GameMsgContentKey][GlobalConstants.YVector3Key]),
                Convert.ToSingle((double) message[GlobalConstants.GameMsgContentKey][GlobalConstants.ZVector3Key])
            );
            attackingPlayer.RenderAttack(aimDir);
            attackingPlayer.SetPlayerState(PlayerState.Attacking);
            break;
        case GameManagerConstants.RecTookDamageOp:
            //player took damage
            float changePercentage = (float) message[GlobalConstants.GameMsgContentKey];
            PlayerController damagedPlayer = players[GetPlayerIdx(message)];
            damagedPlayer.RenderNewHealth(changePercentage);
            damagedPlayer.RenderTookDamage();
            break;
        case GameManagerConstants.RecPlayerDiedOp:
            //player died
            int deadPlayerIdx = GetPlayerIdx(message);
            PlayerController deadPlayer = players[GetPlayerIdx(message)];
            deadPlayer.SetPlayerState(PlayerState.Dead);
            deadPlayer.Die();
            break;
        case GameManagerConstants.RespawnPlayerOp:
            await GlobalHelpersClass.WaitForSecondsTask(GameManagerConstants.DeathAnimationTime);
            //first we want to destroy the player who died
            int respawnPlayerIdx = GetPlayerIdx(message);
            GameObject respawnPlayer = players[respawnPlayerIdx].gameObject;
            //GlobalHelpersClass.DestroyChildren(respawnPlayer);
            Destroy(respawnPlayer);

            //then respawn that same player
            PlayerController playerController = InstantiatePlayer((JObject) message[GlobalConstants.GameMsgContentKey], respawnPlayerIdx);
            // "activate" the player
            playerController.SetPlayerState(PlayerState.Idle);
            break;
        case GameManagerConstants.ScoreUpdateOp:
            //updating scores
            JObject scores = (JObject) message[GlobalConstants.GameMsgContentKey];
            droidScoreBoard.text = (string)scores[GlobalConstants.DroidsTeamName];
            cloneScoreBoard.text = (string)scores[GlobalConstants.ClonesTeamName];
            break;
        case GameManagerConstants.GameOverOp:
            //game over!
            string winningTeam = (string) message[GlobalConstants.GameMsgContentKey];

            DisplayVictoryMsg(winningTeam);
            ShowEndGamePanel();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            break;
        case GameManagerConstants.PlayerIdleOp:
            PlayerController idlePlayer = players[GetPlayerIdx(message)];
            idlePlayer.RenderIdle();
            break;
        case GameManagerConstants.NewPositionOp:
            PlayerController playerWNewPos = players[GetPlayerIdx(message)];
            JToken newPosMsgContent = message[GlobalConstants.GameMsgContentKey];
            Vector3 newPosition = new Vector3(
                Convert.ToSingle((double) newPosMsgContent[GlobalConstants.NewPosKey][GlobalConstants.XVector3Key]),
                Convert.ToSingle((double) newPosMsgContent[GlobalConstants.NewPosKey][GlobalConstants.YVector3Key]),
                Convert.ToSingle((double) newPosMsgContent[GlobalConstants.NewPosKey][GlobalConstants.ZVector3Key])
            );
            float newYRotation = Convert.ToSingle((double) newPosMsgContent[GlobalConstants.PlayerYRotationKey]);
            playerWNewPos.RenderNewPosition(newPosition);
            playerWNewPos.RenderRotationChange(newYRotation);
            playerWNewPos.SetPlayerState(PlayerState.Idle);
            break;
        case GameManagerConstants.StopAttackingOp:
            PlayerController playerThatStoppedAttacking = players[GetPlayerIdx(message)];
            playerThatStoppedAttacking.RenderStopAttacking();
            break;
      }
    }

    public void LoadLobby () 
    {
        StartCoroutine(LoadGameScene(GlobalConstants.LobbySceneName));
    }

    //players pass in from server is an array of Objects that store the team and host value for each player
    //here we associate the hostnames with player controllers to route messages to specific players from the server
    void CreatePlayers (JArray playersArr)
    {
        //passing an array of Jobjects with team, we will want that later with regard to placing people.
        for(int i = 0; i < playersArr.Count; i++)
        {
            InstantiatePlayer ((JObject) playersArr[i], i);
        }
    }

    PlayerController InstantiatePlayer (JObject playerInfo, int thisPlayerIdx)
    {
        string team = (string) playerInfo[GameManagerConstants.TeamKey];
        Vector3 playerLocation = GetPlayerLocation(team);
        GameObject playerObject = clonePlayerPrefab;
        if (team == GlobalConstants.DroidsTeamName) {
            playerObject = droidPlayerPrefab;
        }

        GameObject player = Instantiate(playerObject, playerLocation, Quaternion.identity);
        GameObject weapon = InstantiateWeapon((string) playerInfo[GlobalConstants.GameMsgWeaponKey],
                                                playerLocation);

        GlobalHelpersClass.SetCameraOnPlayerCanvas(player, camera);
        GlobalHelpersClass.SetGamerTagUI(player, (string) playerInfo[GlobalConstants.GamerTagNameKey]);

        //if this player is not on our team (whatever that may be) they are an enemy.
        if (team != localPlayerTeam)
        {
            SetLayerRecursively(player, GameManagerConstants.UnfriendlyPlayerLayer);
            SetLayerRecursively(weapon, GameManagerConstants.UnfriendlyPlayerLayer);
        } else {
            SetLayerRecursively(player, GameManagerConstants.FriendlyPlayerLayer);
            SetLayerRecursively(weapon, GameManagerConstants.FriendlyPlayerLayer);
        }

        PlayerController controller = (PlayerController) player.GetComponent(typeof(PlayerController));
        controller.SetPlayerIdx(thisPlayerIdx);
        controller.SetGameManager(this);
        controller.SetMuzzlePoint(player.transform.Find("gun").transform.Find("muzzlePoint"));

        GameObject healthBar = GetHealthBar(player, team);
        healthBar.SetActive(true);
        controller.SetHealthBar(healthBar.GetComponent<Slider>());
        // if the current player is the one playing on this machine, give them a camera
        if (IsLocalPlayer(localPlayerIdx, thisPlayerIdx))
        {
            MouseFollow cameraLogic = camera.GetComponent<MouseFollow>();
            cameraLogic.SetTarget(player.transform);
            controller.SetCameraTransform(camera.transform);
            controller.SetCamera(camera.GetComponent<Camera>());
            controller.SetIsLocalClient();
        }
        controller.SetClient(client);

        players.Add(controller);
        return controller;
    }



    // ************************************** //
    // Instantiate Player helpers             //
    // ************************************** //

    private Vector3 GetPlayerLocation (string team)
    {
        if (team == GlobalConstants.DroidsTeamName)
        {
            return team1Spawn;
        }
        return team2Spawn;
    }

    private GameObject GetHealthBar (GameObject currentPlayer, string currentPlayerTeam) {
        if (currentPlayerTeam != localPlayerTeam)
        {
            return currentPlayer.transform.Find(GlobalConstants.PlayerCanvasName)
            .transform.Find("EnemyHealthBar").gameObject;
        }
        return currentPlayer.transform.Find(GlobalConstants.PlayerCanvasName)
            .transform.Find("FriendlyHealthBar").gameObject;
    }

    private bool IsLocalPlayer (int localPlayerIdx, int thisPlayerIdx)
    {
      if (localPlayerIdx == thisPlayerIdx)
      {
        return true;
      }
      return false;
    }

    private GameObject InstantiateWeapon (string selectedWeapon, Vector3 location)
    {
        return Instantiate(assaultRifle,
                         new Vector2(location.x, location.y),
                         Quaternion.identity);
    }

    // ************************************** //
    // Game Message helpers                   //
    // ************************************** //
    void HideLoadingPanel()
    {
      loadingPanel.SetActive(false);
    }

    void ShowGameMsgPanel()
    {
      gameMsgPanel.SetActive(true);
    }

    void HideGameMsgPanel ()
    {
      gameMsgPanel.SetActive(false);
    }

    void ShowEndGamePanel ()
    {
      endGamePanel.SetActive(true);
    }

    //localPlayerteam is a global here
    private void DisplayVictoryMsg (string winningTeam)
    {
      string gameOverMsg;
      if (winningTeam == localPlayerTeam)
      {
        gameOverMsg = GameManagerConstants.VictoryMessage;
      }
      else
      {
        gameOverMsg = GameManagerConstants.DefeatMessage;
      }
      ShowGameMsgPanel();
      gameMsgText.text = gameOverMsg;
    }

    private void DisplayQuitButton ()
    {
        quitButton.SetActive(true);
    }

    // ************************************** //
    // General purpose helpers                //
    // ************************************** //
    void ActivatePlayers ()
    {
      //c# is dumb and doesnt have a great map function
      foreach (PlayerController player in players)
      {
        player.SetPlayerState(PlayerState.Idle);
      }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
      if (null == obj)
      {
        return;
      }

      obj.layer = newLayer;

      foreach (Transform child in obj.transform)
      {
        if (null == child)
        {
          continue;
        }
        SetLayerRecursively(child.gameObject, newLayer);
      }
    }

    private IEnumerator LoadGameScene(string sceneName)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // ************************************** //
    // Getters and Setters                    //
    // ************************************** //
    void AssignLocalPlayerIdx (int playerIdx)
    {
      localPlayerIdx = playerIdx;
    }

    void AssignLocalPlayerTeam (string inputTeam)
    {
      localPlayerTeam = inputTeam;
    }

    public int GetLocalPlayerIdx ()
    {
      return localPlayerIdx;
    }

    public int GetPlayerIdx (JObject message)
    {
      return (int) message[GlobalConstants.GameMsgPlayerIdxKey];
    }
}
