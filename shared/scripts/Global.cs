using Godot;
using Steamworks;


public partial class Global : Node
{
    //state bools
	public static bool isServerStarted = false;
	public static bool isOffline = false;
	public static bool enableDebugOutput = true;
	public static bool isSteamRunning = false;
	public static bool isJoinable = false;

    //Pointers to scenetree nodes (set in _ready)
	public static Global instance;
	public static Main main;
    public static NetworkInterface networkInterface;

    //Steam vars
	public static uint appID = 480;
	public static ulong steamID = 0;
	public static string steamName = string.Empty;

    //Steam Callbacks
	protected Callback<GameRichPresenceJoinRequested_t> joinRequest;

    public override void _Ready()
	{
        LinkNodes();
		SteamInit(true);
        CheckLaunchOptions();
        networkInterface = new NetworkInterface();
        //networkInterface.StartServer();
        AddChild(networkInterface);
	}

    private void CheckLaunchOptions()
    {
        SteamApps.GetLaunchCommandLine(out string cmdLine, 260);
        if (cmdLine == string.Empty) { Global.PrintDebug("No Steam URL Launch Options"); }
        Global.PrintDebug("Checking Steam Launch URL cmdline: " + cmdLine);

    }

    public override void _Process(double delta)
    {
		if (isSteamRunning)
		{
            SteamAPI.RunCallbacks();
        }
	
    }

	private void SteamInit(bool useSteam)
	{
		if (useSteam) 
		{
            if (appID != 480)
            {
                SteamAPI.RestartAppIfNecessary(new AppId_t(appID));
            }
			if (SteamAPI.Init())
			{
                steamID = SteamUser.GetSteamID().m_SteamID;
                steamName = SteamFriends.GetPersonaName();
                SteamNetworkingUtils.InitRelayNetworkAccess();
				isSteamRunning = true;
				linkCallbacks();
            }
			else
			{
                Global.PrintError("Steam failed to init.");
            }

        }
		else
		{
			steamID = 0;
			steamName = "OFFLINE";
			isOffline = true;
		}
    }

    private void linkCallbacks()
    {
		joinRequest = Callback<GameRichPresenceJoinRequested_t>.Create(onSteamJoinRequest);
    }

    private void onSteamJoinRequest(GameRichPresenceJoinRequested_t param)
    {
		if (true) // TODO
		{
            PrintDebug("Requesting to join: " + param.m_steamIDFriend);
        }
		else
		{
			PrintError("Error", true);
		}
		
    }

    private void LinkNodes()
	{
		instance = this;
		main = GetNode<Main>("../Main");
	}

    public override void _ExitTree()
    {

    }

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationCrash)
		{

		}
    }
    public static void PrintError(string msg, bool asServer = false, bool addTimestamp = true)
    {
        string instanceTag = "";
        string timestamp = "";
        if (asServer)
        {
            instanceTag = "[SERVER] ";
        }
        else
        {
            instanceTag = "[CLIENT] ";
        }
        if (addTimestamp)
        {
            timestamp = "[" + Time.GetTimeStringFromSystem(true) + "] ";
        }
        GD.PrintErr(instanceTag + timestamp + msg);
    }

    public static void PrintCriticalError(string msg, bool asServer = false, bool addTimestamp = true)
    {
        string instanceTag = "";
        string timestamp = "";
        if (asServer)
        {
            instanceTag = "[SERVER]";
        }
        else
        {
            instanceTag = "[CLIENT]";
        }
        if (addTimestamp)
        {
            timestamp = "[" + Time.GetTimeStringFromSystem(true) + "] ";
        }
        GD.PrintErr("CRITICAL ERROR: " + instanceTag + timestamp + msg);
    }

    public static void Print(string msg, bool asServer = false, bool addTimestamp = true)
    {
        string instanceTag = "";
        string timestamp = "";
        if (asServer)
        {
            instanceTag = "[SERVER]";
        }
        else
        {
            instanceTag = "[CLIENT]";
        }
        if (addTimestamp)
        {
            timestamp = "[" + Time.GetTimeStringFromSystem(true) + "] ";
        }
        GD.Print(instanceTag + timestamp + msg);
    }

    public static void PrintDebug(string msg, bool asServer = false, bool addTimestamp = true)
    {
        if (!enableDebugOutput) { return; }
        string instanceTag = "";
        string timestamp = "";
        if (asServer)
        {
            instanceTag = "[SERVER]";
        }
        else
        {
            instanceTag = "[CLIENT]";
        }
        if (addTimestamp)
        {
            timestamp = "[" + Time.GetTimeStringFromSystem(true) + "] ";
        }
        GD.Print(instanceTag + timestamp + msg);
    }
}