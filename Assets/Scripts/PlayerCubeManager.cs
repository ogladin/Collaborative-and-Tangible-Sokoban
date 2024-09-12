using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;
using toio.Navigation;
using toio.Simulator;
using Unity.Netcode;
using UnityEngine.Events;
using Random = UnityEngine.Random;

enum State
{
	running,
	stopped
}

class CubeData
{
	public State currentState;
	public Vector2Int target;
	public Player player;
}

public class PlayerCubeManager : NetworkBehaviour
{
	public ConnectType connectType;
	public int cubeNumber = 2;
	CubeManager cubeManager;
	public Mat mat;
	public GameObject cubePrefab;
	public bool showId = false;

	public int MoveToSpeed = 100;

	public Color[] colors;

	public string[] hostBleDeviceNames;
	public string[] clientBleDeviceNames;


	private int GhostLayerId;
	private int GreenPlayerLayerId;
	private int BluePlayerLayerId;


	public float networkFps = 15.0f;
	private float elapsedTime = 0f;

	private Dictionary<Cube, CubeData> cubeDatas;

	public UnityEvent CubeReadyEvent;
	public UnityEvent<int> CubeReadyMovementReached;

	private GameObject[] gameObjects;

	private bool initReady;
	private bool initDone;
	private float initDelay = 0f;
	public float initTime = 2.0f;
	public float minTimeBetweenUndo = 2.0f;
	private float timeBetweenUndo = 0.0f;

	public Vector3 RemoteplayerPosition;

	private Vector2Int UpperRightCornerMatPos;
	private Vector2Int UpperLeftCornerMatPos;
	private Vector2Int LowerLeftCornerMatPos;

	private Vector2Int MeasuredUpperRightCornerMatPos;
	private Vector2Int MeasuredUpperLeftCornerMatPos;
	private Vector2Int MeasuredLowerLeftCornerMatPos;


	async void Start()
	{
		//Set Calibration value
		MeasuredUpperRightCornerMatPos = UpperRightCornerMatPos = new Vector2Int(68, 432);
		MeasuredUpperLeftCornerMatPos = UpperLeftCornerMatPos = new Vector2Int(432, 432);
		MeasuredLowerLeftCornerMatPos = LowerLeftCornerMatPos = new Vector2Int(432, 68);

		if (connectType == ConnectType.Real && PlayerPrefs.HasKey("MeasuredUpperRightCornerMatPosX"))
		{
			MeasuredUpperRightCornerMatPos.x = PlayerPrefs.GetInt("MeasuredUpperRightCornerMatPosX");
			MeasuredUpperLeftCornerMatPos.x = PlayerPrefs.GetInt("MeasuredUpperLeftCornerMatPosX");
			MeasuredLowerLeftCornerMatPos.x = PlayerPrefs.GetInt("MeasuredLowerLeftCornerMatPosX");

			MeasuredUpperRightCornerMatPos.y = PlayerPrefs.GetInt("MeasuredUpperRightCornerMatPosY");
			MeasuredUpperLeftCornerMatPos.y = PlayerPrefs.GetInt("MeasuredUpperLeftCornerMatPosY");
			MeasuredLowerLeftCornerMatPos.y = PlayerPrefs.GetInt("MeasuredLowerLeftCornerMatPosY");
		}

		Vector2Int Test = GetCalibratedMatPos(UpperRightCornerMatPos);
		//Debug.LogError("UpperRightCornerMatPos             -> " + UpperRightCornerMatPos.x + " " + UpperRightCornerMatPos.y);
		//Debug.LogError("UpperRightCornerMatPos transformed -> " + Test.x + " " + Test.y);

		Test = GetCalibratedMatPos(UpperLeftCornerMatPos);
		//Debug.LogError("UpperLeftCornerMatPos             -> " + UpperLeftCornerMatPos.x + " " + UpperLeftCornerMatPos.y);
		//Debug.LogError("UpperLeftCornerMatPos transformed -> " + Test.x + " " + Test.y);

		Test = GetCalibratedMatPos(LowerLeftCornerMatPos);
		//Debug.LogError("LowerLeftCornerMatPos             -> " + LowerLeftCornerMatPos.x + " " + LowerLeftCornerMatPos.y);
		//Debug.LogError("LowerLeftCornerMatPos transformed -> " + Test.x + " " + Test.y);

		//Get Layer Ids
		GhostLayerId = LayerMask.NameToLayer("Ghost");
		BluePlayerLayerId = LayerMask.NameToLayer("BluePlayer");
		GreenPlayerLayerId = LayerMask.NameToLayer("GreenPlayer");

		gameObjects = new GameObject[cubeNumber];
		cubeDatas = new Dictionary<Cube, CubeData>();


		cubeManager = new CubeManager(connectType);
		if (connectType == ConnectType.Real)
		{
			if (LevelManager.Instance.NetworkMode)
			{
				List<string> whiteList;
				if (LevelManager.Instance.isHost)
				{
					whiteList = hostBleDeviceNames.ToList();
				}
				else
				{
					whiteList = clientBleDeviceNames.ToList();

				}

				List<string> FirstElementList = new List<string>();
				FirstElementList.Add(whiteList.First());
				gameObjects[0] = Instantiate(cubePrefab,
						new Vector3(Random.Range(-0.25f, 0.25f), 0.11f, Random.Range(-0.25f, 0.25f)), Quaternion.identity);
				await cubeManager.MultiConnect(1, FirstElementList);
				List<string> SecondElementList = new List<string>();
				SecondElementList.Add(whiteList.Last());
				gameObjects[1] = Instantiate(cubePrefab,
					new Vector3(Random.Range(-0.25f, 0.25f), 0.11f, Random.Range(-0.25f, 0.25f)), Quaternion.identity);
				await cubeManager.MultiConnect(2, SecondElementList);

			}
			else
			{

				for (int i = 0; i < cubeNumber; i++)
				{
					gameObjects[i] = Instantiate(cubePrefab,
						new Vector3(Random.Range(-0.25f, 0.25f), 0.11f, Random.Range(-0.25f, 0.25f)), Quaternion.identity);
				}
				await cubeManager.MultiConnect(cubeNumber);
			}
		}
		else
		{

			for (int i = 0; i < cubeNumber; i++)
			{
				gameObjects[i] = Instantiate(cubePrefab,
					new Vector3(Random.Range(-0.25f, 0.25f), 0.11f, Random.Range(-0.25f, 0.25f)), Quaternion.identity);
			}
			await cubeManager.MultiConnect(cubeNumber);
		}
		initReady = true;

		RemoteplayerPosition = Vector3.zero;
	}


	void Init()
	{
		RectInt matBorders = Mat.GetRectForMatType(mat.matType);

		for (int i = 0; i < cubeManager.cubes.Count; i++)
		{
			CubeData cubeData = new CubeData();
			cubeData.target = Vector2Int.zero;
			cubeData.currentState = State.stopped;
			Cube cube = cubeManager.cubes[i];//use index ?

			int index = i;

			if (connectType == ConnectType.Real)
			{
				if (LevelManager.Instance.NetworkMode)
				{
					List<string> whiteList;
					if (LevelManager.Instance.isHost)
					{
						whiteList = hostBleDeviceNames.ToList();
					}
					else
					{
						whiteList = clientBleDeviceNames.ToList();
					}

					index = whiteList.IndexOf(cube.localName);
					if (index != -1)
					{
						gameObjects[index].GetComponent<Player>().Id = index;
						cubeData.player = gameObjects[index].GetComponent<Player>();
						gameObjects[index].name = cube.localName; //Debug
					}
					else
					{
						index = i;
						Debug.LogError("Cannot find the BLE address in the list");
						gameObjects[i].GetComponent<Player>().Id = i;
						cubeData.player = gameObjects[i].GetComponent<Player>();
					}
				}
				else
				{
					gameObjects[i].GetComponent<Player>().Id = i;
					cubeData.player = gameObjects[i].GetComponent<Player>();
					Debug.Log("TOIO ID: " + cube.localName);
				}
			}
			else
			{
				//Can't find a better way to link unity object and cube that comparing their distance :/
				Vector3 cubePosInUnity = mat.MatCoord2UnityCoord(cube.x, cube.y);
				float distance = float.MaxValue;
				int goodId = -1;
				for (int j = 0; j < cubeNumber; j++)
				{
					float newDistance = Vector3.Distance(gameObjects[j].transform.position, cubePosInUnity);
					if (newDistance < distance)
					{
						goodId = j;
						distance = newDistance;
					}
				}

				gameObjects[goodId].GetComponent<Player>().Id = i;
				cubeData.player = gameObjects[goodId].GetComponent<Player>();
			}

			cubeDatas[cube] = cubeData;

			if (index < colors.Length)
			{
				cube.TurnLedOn((int)(colors[index].r * 255), (int)(colors[index].g * 255),
					(int)(colors[index].b * 255), 0);
			}

			cubeManager.handles[index].SetBorderRect(matBorders);


			cube.buttonCallback.AddListener("EventScene", OnPressButton);
			cube.slopeCallback.AddListener("EventScene", OnSlope);
			cube.collisionCallback.AddListener("EventScene", OnCollision);
			cube.idCallback.AddListener("EventScene", OnUpdateID);
			cube.standardIdCallback.AddListener("EventScene", OnUpdateStandardID);
			cube.idMissedCallback.AddListener("EventScene", OnMissedID);
			cube.standardIdMissedCallback.AddListener("EventScene", OnMissedStandardID);
			cube.poseCallback.AddListener("EventScene", OnPose);
			cube.doubleTapCallback.AddListener("EventScene", OnDoubleTap);
			cube.shakeCallback.AddListener("EventScene", OnShake);
			cube.motorSpeedCallback.AddListener("EventScene", OnMotorSpeed);
			cube.magnetStateCallback.AddListener("EventScene", OnMagnetState);
			cube.magneticForceCallback.AddListener("EventScene", OnMagneticForce);
			cube.attitudeCallback.AddListener("EventScene", OnAttitude);
		}

		CubeReadyEvent.Invoke();


		if (LevelManager.Instance.isProjectorMode)
		{
			LevelManager.Instance.GoToProjectorMode();
		}
	}


	public Player GetPlayer(int id)
	{
		foreach (CubeData cubeData in cubeDatas.Values)
		{
			if (cubeData.player.Id == id)
			{
				return cubeData.player;
			}
		}

		return null;
	}

	public int GetId(Cube c)
	{
		CubeData data;
		if (cubeDatas.TryGetValue(c, out data))
		{
			return data.player.Id;
		}

		return -1;
	}

	private CubeData getCubeData(int id)
	{
		foreach (CubeData cubeData in cubeDatas.Values)
		{
			if (cubeData.player.GetComponent<Player>().Id == id)
				return cubeData;
		}

		return null;
	}

	// Update is called once per frame
	void Update()
	{
		if (initReady)
		{
			initDelay += Time.deltaTime;
			if (!initDone)
			{
				if (initDelay > initTime)
				{
					Init();
					initDone = true;
				}

				return;
			}
		}
		else return;

		timeBetweenUndo += elapsedTime;

		float intervalTime = 1 / networkFps;
		elapsedTime += Time.deltaTime;

		if (!cubeManager.synced) return;
		int id = 0;
		foreach (CubeNavigator handle in cubeManager.navigators)
		{
			if (connectType == ConnectType.Real)
			{
				gameObjects[id].transform.position = mat.MatCoord2UnityCoord(handle.cube.x, handle.cube.y);
				Quaternion q = Quaternion.Euler(0, mat.MatDeg2UnityDeg(handle.cube.angle), 0);
				gameObjects[id].transform.rotation = q;
			}

			CubeData cubeData = cubeDatas[handle.cube];

			if (cubeData.currentState == State.running)
			{
				Movement movement = handle.Navi2Target(cubeData.target.x, cubeData.target.y,MoveToSpeed,250,25).Exec();
				if (movement.reached)
				{
					cubeData.currentState = State.stopped;
					CubeReadyMovementReached.Invoke(cubeData.player.Id);
				}
			}
			else
			{
				//Debug.Log("Pos " + handle.cube.x);
			}

			id++;
		}


		if (intervalTime < elapsedTime)
		{
			//TestRpc("Time elapsed", NetworkObjectId);

			if (LevelManager.Instance.NetworkMode)
			{
				//Forward position
				if (NetworkManager.Singleton.IsHost)
				{
					UpdateGhostPositionRpc(LevelManager.Instance.grid.getUnityPositionFromGridPosition(GetPlayer(0).lastValidPos));
				}
				else
				{
					UpdateGhostPositionRpc(LevelManager.Instance.grid.getUnityPositionFromGridPosition(GetPlayer(1).lastValidPos));
				}

				if (LevelManager.Instance.updateCratePosition)
				{
					//Send crates positions
					UpdateCratesRpc(LevelManager.Instance.GetCratesPositions());
					LevelManager.Instance.updateCratePosition = false;
				}
			}

			elapsedTime = 0.0f;
		}

		LevelManager.Instance.UpdateShakeCube(Time.deltaTime);
	}

	public void Calibrate(Vector2Int UpperRightCornerMatPos, Vector2Int UpperLeftCornerMatPos, Vector2Int LowerLeftCornerMatPos)
	{
		MeasuredUpperRightCornerMatPos = UpperRightCornerMatPos;
		MeasuredUpperLeftCornerMatPos = UpperLeftCornerMatPos;
		MeasuredLowerLeftCornerMatPos = LowerLeftCornerMatPos;


		if (connectType == ConnectType.Real)
		{
			PlayerPrefs.SetInt("MeasuredUpperRightCornerMatPosX", MeasuredUpperRightCornerMatPos.x);
			PlayerPrefs.SetInt("MeasuredUpperLeftCornerMatPosX", MeasuredUpperLeftCornerMatPos.x);
			PlayerPrefs.SetInt("MeasuredLowerLeftCornerMatPosX", MeasuredLowerLeftCornerMatPos.x);

			PlayerPrefs.SetInt("MeasuredUpperRightCornerMatPosY", MeasuredUpperRightCornerMatPos.y);
			PlayerPrefs.SetInt("MeasuredUpperLeftCornerMatPosY", MeasuredUpperLeftCornerMatPos.y);
			PlayerPrefs.SetInt("MeasuredLowerLeftCornerMatPosY", MeasuredLowerLeftCornerMatPos.y);
		}
	}

	private void SetGameLayerRecursive(GameObject gameObject, int layer)
	{
		gameObject.layer = layer;
		foreach (Transform child in gameObject.transform)
		{
			SetGameLayerRecursive(child.gameObject, layer);
		}
	}

	public void CubesToGhostLayer()
	{
		foreach (KeyValuePair<Cube, CubeData> pair in cubeDatas)
		{
			SetGameLayerRecursive(pair.Value.player.getCalibratedCube().gameObject, GhostLayerId);
		}
	}

	public void CubeToGhostLayer(int id)
	{
		foreach (KeyValuePair<Cube, CubeData> pair in cubeDatas)
		{
			if (pair.Value.player.Id == id)
				SetGameLayerRecursive(pair.Value.player.getCalibratedCube().gameObject, GhostLayerId);
		}
	}

	public void PlayVictorySound()
	{
		foreach (CubeNavigator handle in cubeManager.navigators)
		{
			CubeData cubeData = cubeDatas[handle.cube];

			cubeData.currentState = State.stopped;
			CubeReadyMovementReached.Invoke(cubeData.player.Id);
			List<Cube.SoundOperation> sound = new List<Cube.SoundOperation>();
			//byte[] notes = { 72, 72, 72, 72 };
			//int[] duration = { 250, 250, 250, 500 };
			byte[] notes = { 72, 72, 72, 72, 68, 70, 72, 70, 72 };
			int[] duration = { 250, 250, 250, 500, 500, 500, 250, 250, 1000 };
			for (int j = 0; j < notes.Length; j++)
			{
				sound.Add(new Cube.SoundOperation(duration[j], volume: 30, notes[j]));
				sound.Add(new Cube.SoundOperation(50, volume: 30, Cube.NOTE_NUMBER.NO_SOUND));
			}

			handle.cube.PlaySound(1, sound.ToArray());
		}
	}

	public void CubeToPlayerLayer(int id)
	{
		foreach (KeyValuePair<Cube, CubeData> pair in cubeDatas)
		{
			if (pair.Value.player.Id == id)
			{
				if (id == 0)
					SetGameLayerRecursive(pair.Value.player.getCalibratedCube().gameObject, GreenPlayerLayerId);
				else
					SetGameLayerRecursive(pair.Value.player.getCalibratedCube().gameObject, BluePlayerLayerId);
			}
		}
	}

	public Vector2Int GetCalibratedMatPos(Vector2Int UncalibratedMatPos)
	{
		// float DeltaX = (float)(UncalibratedMatPos.x - UpperLeftCornerMatPos.x) /
		//                (float)(UpperRightCornerMatPos.x - UpperLeftCornerMatPos.x);

		// int x = (int)(MeasuredUpperLeftCornerMatPos.x + DeltaX * (float)(MeasuredUpperRightCornerMatPos.x - MeasuredUpperLeftCornerMatPos.x));

		// x -= (x - UncalibratedMatPos.x) * 2;

		// float DeltaY = (float)(UncalibratedMatPos.y - UpperLeftCornerMatPos.y) /
		//                (float)(UpperLeftCornerMatPos.y - LowerLeftCornerMatPos.y);

		// int y = (int)(MeasuredUpperLeftCornerMatPos.y + DeltaY * (float)(MeasuredUpperLeftCornerMatPos.y - MeasuredLowerLeftCornerMatPos.y));

		// y -= (y - UncalibratedMatPos.y) * 2;

		// return new Vector2Int(x, y);

		float DeltaX = (float)(UncalibratedMatPos.x - MeasuredUpperLeftCornerMatPos.x) /
		               (float)(MeasuredUpperRightCornerMatPos.x - MeasuredUpperLeftCornerMatPos.x);

		int x = (int)(UpperLeftCornerMatPos.x + DeltaX * (float)(UpperRightCornerMatPos.x - UpperLeftCornerMatPos.x));


		float DeltaY = (float)(UncalibratedMatPos.y - MeasuredUpperLeftCornerMatPos.y) /
		               (float)(MeasuredUpperLeftCornerMatPos.y - MeasuredLowerLeftCornerMatPos.y);

		int y = (int)(UpperLeftCornerMatPos.y + DeltaY * (float)(UpperLeftCornerMatPos.y - LowerLeftCornerMatPos.y));


		return new Vector2Int(x, y);

	}

	public Vector2Int GetUnCalibratedMatPos(Vector2Int UncalibratedMatPos)
	{
		float DeltaX = (float)(UncalibratedMatPos.x - UpperLeftCornerMatPos.x) /
		               (float)(UpperRightCornerMatPos.x - UpperLeftCornerMatPos.x);

		int x = (int)(MeasuredUpperLeftCornerMatPos.x + DeltaX * (float)(MeasuredUpperRightCornerMatPos.x - MeasuredUpperLeftCornerMatPos.x));


		float DeltaY = (float)(UncalibratedMatPos.y - UpperLeftCornerMatPos.y) /
		               (float)(UpperLeftCornerMatPos.y - LowerLeftCornerMatPos.y);

		int y = (int)(MeasuredUpperLeftCornerMatPos.y + DeltaY * (float)(MeasuredUpperLeftCornerMatPos.y - MeasuredLowerLeftCornerMatPos.y));


		return new Vector2Int(x, y);
	}

	public void MoveTo(int id, Vector3 targetInUnityCoordinate)
	{
		foreach (CubeData cubeData in cubeDatas.Values)
		{
			if (cubeData.player.Id == id)
			{
				cubeData.currentState = State.running;

				Vector2Int matPos = mat.UnityCoord2MatCoord(targetInUnityCoordinate);
				// Debug.LogError("Move to     " + matPos.x + " " + matPos.y);

				Vector2Int correctedMatPos = GetUnCalibratedMatPos(matPos);
				// Debug.LogError("Corrected to " + correctedMatPos.x + " " + correctedMatPos.y);

				cubeData.target = correctedMatPos;
				return;
			}
		}
	}

	public Transform GetCubeTransform(int id)
	{
		foreach (CubeData cubeData in cubeDatas.Values)
		{
			if (cubeData.player.Id == id)
			{
				return cubeData.player.gameObject.transform;
			}
		}

		return null;
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer &&
		    IsOwner) //Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
		{
			TestRpc("OnNetworkSpawn", NetworkObjectId);
		}
	}

	[Rpc(SendTo.NotMe)]
	public void TestRpc(string value, ulong sourceNetworkObjectId)
	{
		Debug.Log($"Client Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
	}

	[Rpc(SendTo.NotMe)]
	public void LoadLevelRpc(int levelId)
	{
		Debug.Log($"RPC load level " + levelId);
		LevelManager.Instance.LoadLevel(LevelManager.Instance.WaitingState.Levels[levelId]);
	}

	[Rpc(SendTo.NotMe)]
	public void LoadLevelRpc(string level)
	{
		Debug.Log($"RPC load level " + level);
		LevelManager.Instance.LoadLevel(level, true);
	}

	[Rpc(SendTo.NotMe)]
	public void GameStateChangeRpc(LevelManager.GameStates state)
	{
		Debug.Log($"RPC Game state change to  " + state);
		LevelManager.Instance.ChangeGameState(state);
	}

	[Rpc(SendTo.NotMe)]
	void UpdateGhostPositionRpc(Vector3 position)
	{
		//Debug.Log($"Client Received the RPC UpdateGhostPositionRpc #{position}");
		RemoteplayerPosition = position;
	}

	[Rpc(SendTo.NotMe)]
	void UpdateCratesRpc(float[] positions)
	{
		LevelManager.Instance.UpdateCratesPositions(positions);
		if (LevelManager.Instance.isHost)
		{
			LevelManager.Instance.IncreaserMoveCounter(1);
		}
		else
		{
			LevelManager.Instance.IncreaserMoveCounter(0);
		}
	}

	[Rpc(SendTo.NotMe)]
	public void ShakeRpc(int id)
	{
		LevelManager.Instance.shakeCube(id);
	}


	//Callbacks
	void OnCollision(Cube c)
	{
		Debug.Log("OnCollision");
	}

	void OnSlope(Cube c)
	{
		Debug.Log("OnSlope");
	}

	void OnPressButton(Cube c)
	{
		Debug.Log("OnPressButton");

		if (c.isPressed)
		{
			//showId = !showId;
			if (timeBetweenUndo > minTimeBetweenUndo)
			{
				Debug.Log("UNDO");
				timeBetweenUndo = 0;

				c.PlayPresetSound(4);

				if (LevelManager.Instance.isHost)
				{
					LevelManager.Instance.shakeCube(0);
				}
				else
				{
					ShakeRpc(1);
				}
			}
		}

		c.PlayPresetSound(0);
	}

	void OnUpdateID(Cube c)
	{
		//Debug.Log("OnUpdateID");

		if (showId)
		{
			Debug.LogFormat("pos=(x:{0}, y:{1}), angle={2}", c.pos.x, c.pos.y, c.angle);
		}
	}

	void OnUpdateStandardID(Cube c)
	{
		Debug.Log("OnUpdateStandardID");

		if (showId)
		{
			Debug.LogFormat("standardId:{0}, angle={1}", c.standardId, c.angle);
		}
	}

	void OnMissedID(Cube cube)
	{
		Debug.LogFormat("Position ID Missed.");
	}

	void OnMissedStandardID(Cube c)
	{
		Debug.LogFormat("Standard ID Missed.");
	}

	void OnPose(Cube c)
	{
		Debug.Log($"pose = {c.pose.ToString()}");
	}

	void OnDoubleTap(Cube c)
	{
		Debug.Log("OnDoubleTap");

		c.PlayPresetSound(3);


	}

	void OnShake(Cube c)
	{
		Debug.Log("OnShake");


	}

	void OnMotorSpeed(Cube c)
	{
		Debug.Log($"motor speed: left={c.leftSpeed}, right={c.rightSpeed}");
	}

	void OnMagnetState(Cube c)
	{
		Debug.Log($"magnet state: {c.magnetState.ToString()}");
	}

	void OnMagneticForce(Cube c)
	{
		Debug.Log($"magnetic force = {c.magneticForce}");
	}

	void OnAttitude(Cube c)
	{
		Debug.Log($"attitude = {c.eulers}");
	}
}
