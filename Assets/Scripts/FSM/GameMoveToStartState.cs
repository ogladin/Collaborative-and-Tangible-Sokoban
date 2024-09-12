#define DEBUG_LOGGER


using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class GameMoveToStartState : FSM.State
{
	// Start is called before the first frame update
	private bool []arrived ;

	override public void Enter()
	{
		arrived = new bool [2];
		arrived[0] = false;
		arrived[1] = true;
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.AddListener(CubeHasArrived);
		LevelManager.Instance.playercubeManager.CubesToGhostLayer();


		if (LevelManager.Instance.NetworkMode)
		{
			if (LevelManager.Instance.isHost)
			{
				//Network Host
				//Local player
				Player player = LevelManager.Instance.playercubeManager.GetPlayer(0).GetComponent<Player>();
				player.ChangeState(Player.PlayerStates.GotoSpawn);
			}
			else
			{
				//Network client
				//Local player
				Player player = LevelManager.Instance.playercubeManager.GetPlayer(1).GetComponent<Player>();
				player.ChangeState(Player.PlayerStates.GotoSpawn);
			}
		}
		else
		{
			for (int i = 0; i < LevelManager.Instance.playercubeManager.cubeNumber; i++)
			{
				LevelManager.Instance.playercubeManager.GetPlayer(i).GetComponent<Player>()
					.ChangeState(Player.PlayerStates.GotoSpawn);
			}
		}
	}

	public void CubeHasArrived(int id)
	{

		arrived[id] = true;
		Debug.Log("Cube " + id + " HasArrived");
	}

	override public void Update(float elapsedTime)
	{
		if (arrived[0] && arrived[1])
			LevelManager.Instance.ChangeGameState(LevelManager.GameStates.Playing);
	}

	public override void Exit()
	{
		FileLogger.Logger.LogString("Toios have reached their starting positions");


		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.RemoveListener(CubeHasArrived);
		LevelManager.Instance.InitValidPosition();
		LevelManager.Instance.CheckGridPosition(0);
		LevelManager.Instance.CheckGridPosition(1);

	}
}
