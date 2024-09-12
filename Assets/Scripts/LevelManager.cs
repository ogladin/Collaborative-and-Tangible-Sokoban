#define DEBUG_LOGGER


using System;
using System.Collections.Generic;
using ParrelSync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;

struct GameState
{
	public int id;
	public string state;
}

public class LevelManager : MonoBehaviour
{
	public static LevelManager Instance { get; private set; }

	public int width;
	public int height;

	public bool NetworkMode = false;
	public 	bool isHost = true;

	[Tooltip("In projector mode TOIOs and mar are hidden")]
	public bool isProjectorMode = false;

	[Tooltip("Never display remote marker")]
	public bool hideRemotePlayerMarker = false;

	public string ipAddress = "127.0.0.1";


	[FormerlySerializedAs("CratePrefab")] public GameObject GreenCratePrefab;
	public GameObject BlueCratePrefab;
	public GameObject WallPrefab;
	[FormerlySerializedAs("DotPrefab")] public GameObject GreenDotPrefab;
	public GameObject BlueDotPrefab;
	public GameObject SpawnPointPrefab;

	public Transform LevelRoot;
	public Transform ToioMat;

	public Grid grid;
	public Pathfinding pathfinding;
	public PlayerCubeManager playercubeManager;

	private float squareSide = 1.0f;

	private List<Dot> dots;
	private List<Crate> crates;
	public bool updateCratePosition;

	public List<GameObject> markerPrefabs;
	public Dictionary<int, GameObject> markers;

	private List<Player> players;
	private Vector3[] playerStartPosition;
	private float[] timeSinceLastShakeEvent;
	public float shakeDeltaInSeconds = 2;
	private int greenMoveNumber;
	private int blueMoveNumber;
	private bool logLevel = false;
	private Stack<GameState> positionStack;

	FSM fsm;
	public GameCalibrationState CalibrationState;
	public GameMoveToStartState MoveToStartState;
	public GamePlayingState PlayingState;
	public GameWaitingState WaitingState;
	public GameCongratState CongratState;


	public void ChangeGameState(GameStates stateId)
	{
		fsm.ChangeState((int)stateId);
	}

	public enum GameStates
	{
		Waiting = 0,
		Playing,
		MovingToStart,
		Congrat,
		Calibration
	}

	private void Awake()
	{
		crates = new List<Crate>();

		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}

		Instance = this;
		markers = new Dictionary<int, GameObject>();
		positionStack = new Stack<GameState>();

		if (LevelManager.Instance.NetworkMode)
		{

			UnityTransport transport = GetComponent<UnityTransport>();

			//Is this unity editor instance opening a clone project?
			if (ClonesManager.IsClone())
			{
				Debug.Log("This is a clone project.");
				// Get the custom argument for this clone project.
				string customArgument = ClonesManager.GetArgument();
				// Do what ever you need with the argument string.
				Debug.Log("The custom argument of this clone project is: " + customArgument);
				isHost = false;
				ipAddress= customArgument;
			}
			else
			{
				Debug.Log("This is the original project.");
			}

			string[] args = Environment.GetCommandLineArgs();

			Debug.Log("Usage isHost (0/1) ipAddress (string)");

			if (args.Length >= 5 && !Application.isEditor)
			{
				isHost = int.Parse(args[1]) != 0;
				ipAddress = args[2];
				Debug.Log("Parameters isHost " + isHost + " ip address " + ipAddress);
			}
		}



	}

	private void Start()
	{
		fsm = gameObject.AddComponent<FSM>();

		fsm.states.Add((int)GameStates.Calibration, CalibrationState);
		fsm.states.Add((int)GameStates.Waiting, WaitingState);
		fsm.states.Add((int)GameStates.Playing, PlayingState);
		fsm.states.Add((int)GameStates.MovingToStart, MoveToStartState);
		fsm.states.Add((int)GameStates.Congrat, CongratState);

		GameObject marker1 = GameObject.Instantiate(markerPrefabs[0]);
		markers[0] = marker1;
		GameObject marker2 = GameObject.Instantiate(markerPrefabs[1]);
		markers[1] = marker2;

		NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().ConnectionData.Address = ipAddress;


		if (NetworkMode)
		{
			if (isHost)
			{
				NetworkManager.Singleton.StartHost();
			}
			else
			{
				NetworkManager.Singleton.StartClient();
			}
		}

		timeSinceLastShakeEvent = new float[playercubeManager.cubeNumber];


	}

	public void GoToProjectorMode()
	{
		foreach (Player player in players)
		{
			foreach (MeshRenderer renderer in player.transform.GetComponentsInChildren<MeshRenderer>())
			{
				renderer.enabled = false;
			}


			player.getCalibratedCube().transform.GetComponent<Renderer>().enabled = false;

		}

		GameObject background = GameObject.Find("Background");
		if (background)
		{
			foreach (MeshRenderer renderer in background.transform.GetComponentsInChildren<MeshRenderer>())
			{
				renderer.enabled = false;
			}
		}
		else
		{
			Debug.LogError("Can't hide background objects");
		}
	}

	public void LeaveProjectorMode()
	{
		foreach (Player player in players)
		{
			foreach (MeshRenderer renderer in player.transform.GetComponentsInChildren<MeshRenderer>())
			{
				renderer.enabled = true;
			}
			player.getCalibratedCube().transform.GetComponent<Renderer>().enabled = true;

		}
		GameObject background = GameObject.Find("Background");
		if (background)
		{
			foreach (MeshRenderer renderer in background.transform.GetComponentsInChildren<MeshRenderer>())
			{
				renderer.enabled = true;
			}
		}
		else
		{
			Debug.LogError("Can't hide background objects");
		}
	}

	private void ResetShakeTime()
	{
		for (int i = 0; i < playercubeManager.cubeNumber; i++)
		{
			timeSinceLastShakeEvent[i] = shakeDeltaInSeconds;
		}
	}

	public void UpdateShakeCube(float deltaTime)
	{
		for (int i = 0; i < playercubeManager.cubeNumber; i++)
		{
			timeSinceLastShakeEvent[i] += deltaTime;
		}
	}

	public void CubeAreReady()
	{
		players = new List<Player>(FindObjectsOfType<Player>());

		ChangeGameState(GameStates.Calibration);
	}

	public void shakeCube(int id)
	{
		timeSinceLastShakeEvent[id] = 0f;

	}

	// public bool CubesWereShaken()
	// {
	// 	for (int i = 0; i < playercubeManager.cubeNumber; i++)
	// 	{
	// 		if (timeSinceLastShakeEvent[i] > shakeDeltaInSeconds)
	// 			return false;
	// 	}
	// 	return true;
	// }

	//Will only return true the first time called after cube was shaken
	public bool CubeWasShaken(int id)
	{
		if (id >= timeSinceLastShakeEvent.Length)
		{
			Debug.LogError("CubeWasShaken wrong id " + id);
			return false;
		}

		if (timeSinceLastShakeEvent[id] > shakeDeltaInSeconds)
			return false;

		timeSinceLastShakeEvent[id] = shakeDeltaInSeconds;
		return true;
	}


	public void LoadLevel(TextAsset LevelPath)
	{
		LoadLevel(LevelPath.text);
	}

	public void LoadLevel(string level,bool rewind=false)
	{
		FileLogger.Logger.LogString("Loading Level: \n" + level);

		DestroyLevel(rewind);
		if (!rewind)
		{
			dots = new List<Dot>();
			greenMoveNumber = 0;
			blueMoveNumber = 0;
		}
		crates = new List<Crate>();
		updateCratePosition = false;
		ResetShakeTime();
		logLevel = false;

		height = 0;
		width = 0;
		foreach (string line in level.Split(
			         new string[] { "\r\n", "\r", "\n" },
			         StringSplitOptions.None
		         ))
		{
			width = Math.Max(width, line.Length);
			height++;
		}


		Debug.Log("Level width :" + width + " height :" + height);

		squareSide = LevelRoot.transform.localScale.x / Math.Max(height, width);

		Debug.Log("Square side is:" + squareSide);

		float y = -Math.Max(width,height) / 2.0f * squareSide + squareSide / 2.0f;

		playerStartPosition = new Vector3[2];

		int crateIdCounter = 0;

		foreach (string line in level.Split(
			         new string[] { "\r\n", "\r", "\n" },
			         StringSplitOptions.None
		         ))
		{
			float x = Math.Max(width,height) / 2.0f * squareSide - squareSide / 2.0f;

			foreach (char c in line.ToCharArray())
			{
				switch (c)
				{
					case '#': //WALL
						GameObject wall = GameObject.Instantiate(WallPrefab, LevelRoot);
						wall.transform.localPosition = new Vector3(x, y, 0);
						wall.transform.localScale = new Vector3(squareSide, squareSide, squareSide);
						break;
					case '$': //GREEN CRATE
						GameObject greenCrate = GameObject.Instantiate(GreenCratePrefab, LevelRoot);
						greenCrate.transform.localPosition = new Vector3(x, y, 0);
						greenCrate.transform.localScale = new Vector3(squareSide, squareSide, squareSide);
						Crate greenCrateBehaviour = greenCrate.GetComponent<Crate>();
						greenCrateBehaviour.NetworkId = crateIdCounter++;
						crates.Add(greenCrateBehaviour);
						break;
					case '£': //BLUE CRATE
						GameObject blueCrate = GameObject.Instantiate(BlueCratePrefab, LevelRoot);
						blueCrate.transform.localPosition = new Vector3(x, y, 0);
						blueCrate.transform.localScale = new Vector3(squareSide, squareSide, squareSide);
						Crate blueCrateBehaviour = blueCrate.GetComponent<Crate>();
						blueCrateBehaviour.NetworkId = crateIdCounter++;
						crates.Add(blueCrateBehaviour);
						break;
					case '.': //GREEN DOT
						GameObject greenDot = GameObject.Instantiate(GreenDotPrefab, LevelRoot);
						greenDot.transform.localPosition = new Vector3(x, y, 0);
						greenDot.transform.localScale = new Vector3(squareSide, squareSide, squareSide);
						dots.Add(greenDot.transform.GetChild(0).GetComponent<Dot>());
						break;
					case ':': //BLUE DOT
						GameObject blueDot = GameObject.Instantiate(BlueDotPrefab, LevelRoot);
						blueDot.transform.localPosition = new Vector3(x, y, 0);
						blueDot.transform.localScale = new Vector3(squareSide, squareSide, squareSide);
						dots.Add(blueDot.transform.GetChild(0).GetComponent<Dot>());
						break;
					case '1': //Player 1
						GameObject spawnPoint1 = GameObject.Instantiate(SpawnPointPrefab, LevelRoot);
						spawnPoint1.transform.localPosition = new Vector3(x, y, 0);
						playerStartPosition[0] = spawnPoint1.transform.position;

						break;
					case '2': //Player 2
						GameObject spawnPoint2 = GameObject.Instantiate(SpawnPointPrefab, LevelRoot);
						spawnPoint2.transform.localPosition = new Vector3(x, y, 0);
						playerStartPosition[1] = spawnPoint2.transform.position;
						break;
					case ' ': //Empty space
						break;
					default:
						Debug.Log("Unknown character " + c);
						break;
				}

				x -= squareSide;
			}

			y += squareSide;
		}

		//Ajust path finding grid size
		grid.vGridWorldSize = new Vector2(ToioMat.localScale.x, ToioMat.localScale.y);
		grid.fNodeRadius = ToioMat.localScale.x * squareSide / 2;
		grid.Init();

		// pathfinding.StartPosition = playercubeManager.GetCubeTransform(0);
		// pathfinding.TargetPosition = playercubeManager.GetCubeTransform(1);
	}

	public void DestroyLevel(bool rewind=false)
	{
		//Remove previous objects
		foreach (Transform child in LevelRoot)
		{
			if (rewind)
			{
				if (child.gameObject.layer != BlueDotPrefab.layer && child.gameObject.layer != GreenDotPrefab.layer)
				{
					Destroy(child.gameObject);
				}
			}
			else
				Destroy(child.gameObject);
		}

		crates = new List<Crate>();

	}


	//Called after loading level once cubes have reached their start position
	public void InitValidPosition()
	{
		foreach (Player player in players)
		{
			player.lastValidPos = grid.getGridPositionFromUnityPosition(player.getCalibratedCube().transform.position);
		}
	}


	private Vector2Int[] getPlayerGridPosition()
	{
		Vector2Int[] playerpositions = new Vector2Int[players.Count];
		foreach (Player player in players)
		{
			int i = player.Id; //Ids start at 0 and are continuous
			playerpositions[i].x = player.lastValidPos.x;
			playerpositions[i].y = player.lastValidPos.y;
		}

		return playerpositions;
	}

	public void LateUpdate()
	{

		//Check if crates are well positionned
		foreach (Crate crate in crates)
		{
			crate.CheckPosition();
		}

		if (logLevel)
		{
			Vector2Int[] playerpositions = getPlayerGridPosition();
			FileLogger.Logger.LogString("New level state\n" + grid.LogNodeArray(playerpositions));
			logLevel = false;
		}
	}

	public void MoveToSpawnPoint(int id)
	{
		playercubeManager.MoveTo(id, playerStartPosition[id]);
	}

	public void HideMarker(int id)
	{
		markers[id].transform.position = new Vector3(0, -10, 0);
	}

	public void SetMarkerPosition(int id, Vector3 worldPos)
	{
		if (NetworkMode && hideRemotePlayerMarker)
		{
			if ((isHost && id==1) || (!isHost && id==0) )
			{
				HideMarker(id);
			}
			else
			{
				markers[id].transform.position = worldPos;
			}
		}
		else
		{
			markers[id].transform.position = worldPos;
		}
	}

	public void HideMarkers()
	{
		foreach (Player player in players)
		{
			markers[player.Id].GetComponent<MeshRenderer>().enabled = false;
		}
	}

	public void ShowMarkers()
	{
		foreach (Player player in players)
		{
			markers[player.Id].GetComponent<MeshRenderer>().enabled = true;
		}
	}

	public bool CheckIfLevelCompleted()
	{
		return grid.isWon();
	}

	public bool CheckGridPosition(int id)
	{
		if (!playercubeManager.GetPlayer(id)) return false;
		Player player = playercubeManager.GetPlayer(id).GetComponent<Player>();
		if (!player) return false;
		Vector3 unityCurrentPos = grid.getUnityPositionFromGridPosition(player.lastValidPos);
		unityCurrentPos.y = 0; //Compute path on the ground
		Vector3 unityTargetPos =
			grid.getUnityPositionFromGridPosition(grid.getGridPositionFromUnityPosition(player.getCalibratedCube().transform.position));
		unityTargetPos.y = 0;
		List<Node> path = checkPath(unityCurrentPos, unityTargetPos);
		if (path != null)
		{

			Vector2Int currentValidPos = grid.getGridPositionFromUnityPosition(unityTargetPos);
			if (currentValidPos[0] != player.lastValidPos[0] || currentValidPos[1] != player.lastValidPos[1])
			{
				//Grid position of the player has changed
				logLevel = true;
				player.lastValidPos = currentValidPos;
			}
			SetMarkerPosition(id, grid.getUnityPositionFromGridPosition(player.lastValidPos));
			return true;
		}
		else
		{
			return false;
		}
	}


	public Vector2Int GetGridPosition(int id)
	{
		return grid.getGridPositionFromUnityPosition(playercubeManager.GetPlayer(id).getCalibratedCube().transform.position);
	}

	public List<Node> checkPath(Vector3 unityStartPos, Vector3 unityTargetPos)
	{
		if (grid.Pathfinding)
			return grid.Pathfinding.FindPath(unityStartPos, unityTargetPos);
		else return null;
	}

	public void UpdateCratesPositions(float [] positions)
	{
		for (int i = 0; i+2 < positions.Length; i+=3)
		{
			crates[i/3].transform.position = new Vector3(positions[i],positions[i+1],positions[i+2]);
		}
	}

	public float[] GetCratesPositions()
	{
		float[] positions = new float[crates.Count * 3];
		int i = 0;
		foreach (Crate crate in crates)
		{
			if (crate)
			{
				positions[i++] = crate.transform.position.x;
				positions[i++] = crate.transform.position.y;
				positions[i++] = crate.transform.position.z;
			}
		}

		return positions;
	}

	public void IncreaserMoveCounter(int id)
	{
		Vector2Int[] playerpositions = getPlayerGridPosition();
		GameState gameState = new GameState();
		gameState.id = id;
		gameState.state = grid.LogNodeArray(playerpositions);
		positionStack.Push(gameState);
		if (id == 0)
			greenMoveNumber++;
		else
			blueMoveNumber++;
	}

	public int GetGreenMoveNumber()
	{
		return greenMoveNumber;
	}

	public int GetBlueMoveNumber()
	{
		return blueMoveNumber;
	}

	public void ResetMoveNumber()
	{
		greenMoveNumber = 0;
		blueMoveNumber = 0;
	}

	//Pop until a gameState for this player id id found or return empty string
	public string PopLastGameState(int id)
	{
		bool isThereAValidState = false;
		foreach (GameState gameState in positionStack)
		{
			if (gameState.id == id)
			{
				isThereAValidState = true;
				break;
			}
		}

		if (!isThereAValidState)
			return string.Empty;

		while (positionStack.Count > 0)
		{
			GameState gameState = positionStack.Pop();
			if (gameState.id == id)
			{
				return gameState.state;
			}
		}
		return string.Empty;

	}

	public void ResetGameStates()
	{
		positionStack.Clear();
	}


}
