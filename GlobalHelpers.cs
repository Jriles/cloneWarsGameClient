using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Threading.Tasks;
using GlobalData;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GlobalHelpers
{
  class GlobalHelpersClass : MonoBehaviour
  {
    private static readonly HttpClient client = new HttpClient();

    public static async Task<int> WaitForSecondsTask(int time)
    {
      await Task.Delay(time);
      return 0;
    }

    public static void SetGamerTagUI (GameObject player, string gamerTag)
    {
      Text gamerTagText = player
        .transform.Find(GlobalConstants.PlayerCanvasName)
        .transform.Find(GlobalConstants.GamerTagNameKey)
        .gameObject.GetComponent<Text>();
      gamerTagText.text = gamerTag;
    }

    public static void SetCameraOnPlayerCanvas (GameObject player, GameObject camera) {
        PlayerCanvasLogic canvasLogic = player.transform.Find(GlobalConstants.PlayerCanvasName)
        .gameObject.GetComponent<PlayerCanvasLogic>();
        canvasLogic.SetTargetCamera(camera.transform);
    }

    public static void DestroyChildren(GameObject gameObject)
    {
      foreach(Transform child in gameObject.transform)
      {
        Destroy(child.gameObject);
      }
    }

    public static Texture2D GetTextureFromBytes (byte[] bytes)
    {
      //height and width (2, 2) get replaced by load image
      Texture2D tex = new Texture2D(2, 2, TextureFormat.PVRTC_RGBA4, false);
      tex.LoadImage(bytes);
      return tex;
    }

    //returns true or false for if valid input
    //used for logging in, account creation and friend requests
    public static bool ValidFormEntry (string formInput)
    {
      if (String.IsNullOrWhiteSpace(formInput) || formInput == "")
      {
        return false;
      }
      return true;
    }

    public static void RemoveErrorMessagePanel (GameObject originalPanel, GameObject errorMsgPanel)
    {
      originalPanel.SetActive(true);
      Destroy(errorMsgPanel);
    }

    public static void DisplayFeedbackPanel (
                                                   GameObject feedbackPanelPrefab,
                                                   GameObject panelToHide,
                                                   string msg,
                                                   GameObject canvas
                                                 )
    {
      GameObject feedbackPanel = Instantiate(feedbackPanelPrefab);
      feedbackPanel.transform.SetParent(canvas.transform, false);
      Button closeFeedbackPanelBtn = feedbackPanel.transform.Find("SubmitBtn").GetComponent<Button>();
      closeFeedbackPanelBtn.onClick.AddListener(() => RemoveErrorMessagePanel(panelToHide, feedbackPanel));
      feedbackPanel.transform.Find("Message").GetComponent<Text>().text = msg;
      panelToHide.SetActive(false);
    }

    public static string[] ParseCloneArgs (string args)
    {
      return args.Split(
          new[] { "\r\n", "\r", "\n" },
          StringSplitOptions.None
      );
    }

    public static Vector2 RotateVec2(Vector2 v, float degrees) {
      float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
      float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
      
      float tx = v.x;
      float ty = v.y;
      v.x = (cos * tx) - (sin * ty);
      v.y = (sin * tx) + (cos * ty);
      return v;
    }

    //send post request
    public static async Task<JObject> SendPostRequest (string url, JObject json)
    {
      var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
      content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var response = await client.PostAsync(url, content);

      return JObject.Parse(await response.Content.ReadAsStringAsync());
    }
  }
}
