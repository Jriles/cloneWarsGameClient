namespace GlobalData
{
  public enum PlayerState
  {
    Disabled,
    Idle,
    Attacking,
    Jumping,
    Aiming,
    Walking,
    Dead
  }

  static class GlobalConstants
  {
    public const float PlayerSpeed = 10f;
    public const string PlayerCanvasName = "PlayerCanvas";
    public const string UserIdKey = "UserId";
    public const string SessionTokenKey = "sessionToken";
    public const string GamerTagNameKey = "gamerTag";
    public const string HostIpAddress = "hostIp";
    //scene names
    public const string LobbySceneName = "Start Menu";
    public const string GameMsgWeaponKey = "weapon";
    public const string GameMsgContentKey = "content";
    public const string WalkDirKey = "walkDirKey";
    public const string NewPosKey = "newPosKey";
    public const string PlayerRotationKey = "playerDirKey";
    public const string GameMsgOpCodeKey = "opCode";
    public const string GameMsgPlayerIdxKey = "playerIdx";
    public const string AimDirXKey = "aimDirX";
    public const string AimDirYKey = "aimDirY";
    public const string AimDirZKey = "aimDirZ";
    public const string XVector3Key = "x";
    public const string YVector3Key = "y";
    public const string ZVector3Key = "z";
    public const string PlayerYRotationKey = "yRotation";
    public const string HitDirXKey = "hitDirX";
    public const string HitDirYKey = "hitDirY";
    public const string CanvasName = "Canvas";
    //team names
    public const string DroidsTeamName = "droids";
    public const string ClonesTeamName = "clones";
    public const int GameServerPort = 7654;
  }
}
