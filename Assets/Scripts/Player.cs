using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Player : MonoBehaviour
{
	public Vector2Int lastValidPos = new Vector2Int();
	public int id_;

	FSM fsm;
	public PlayerIdleState IdleState;
	public PlayerDraggedState DraggedState;
	public PlayerGoToGoodPositionState GoToGoodPositionState;
	public PlayerGoToSpawnState GoToSpawnState;
	public PlayerNetworkGhostState NetworkGhostState;

	public CalibratedCube CalibratedCubePrefab;
	private CalibratedCube calibratedCube;

	public int Id
	{
		get => id_;
		set
		{
			id_ = value;
		}
	}

	public void ChangeState(PlayerStates stateId)
	{
		fsm.ChangeState((int)stateId);
	}

	public enum PlayerStates
	{
		Idle = 0,
		GotoSpawn,
		Dragged,
		GotoGoodPosition,
		NetworkGhost
	}

	void Start()
	{
		fsm = gameObject.AddComponent<FSM>();
		fsm.states.Add((int)PlayerStates.Dragged, DraggedState);
		fsm.states.Add((int)PlayerStates.GotoGoodPosition, GoToGoodPositionState);
		fsm.states.Add((int)PlayerStates.GotoSpawn, GoToSpawnState);
		fsm.states.Add((int)PlayerStates.Idle, IdleState);
		fsm.states.Add((int)PlayerStates.NetworkGhost, NetworkGhostState);

		calibratedCube = Instantiate(CalibratedCubePrefab);

	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.D))
		{
			if (fsm.currentState != null)
				Debug.Log("ID " + id_ + " current state " + fsm.currentState);
			else
				Debug.Log("ID " + id_ + " no current state ");
		}

		//calibratedCube.transform.rotation = transform.rotation;

		Vector2Int matPos = LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(transform.position);
		matPos = LevelManager.Instance.playercubeManager.GetCalibratedMatPos(matPos);
		Vector3 calibratedPos = toio.Simulator.Mat.MatCoord2UnityCoord(matPos);
		calibratedPos.y -= 0.005f;

		calibratedCube.transform.position = calibratedPos;
	}

	public CalibratedCube getCalibratedCube()
	{
		return calibratedCube;
	}
}
