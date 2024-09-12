using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

[System.Serializable]
public class PlayerGoToGoodPositionState : FSM.State
{
	private Player player;

	override public void Enter()
	{
		player = fsm.GetComponent<Player>();
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.AddListener(CubeHasArrived);
		LevelManager.Instance.playercubeManager.MoveTo(player.Id,
			LevelManager.Instance.grid.getUnityPositionFromGridPosition(player.lastValidPos));
		LevelManager.Instance.playercubeManager.CubeToGhostLayer(player.Id);
	}

	public void CubeHasArrived(int id)
	{
		if (id == fsm.GetComponent<Player>().Id)
		{
			player.lastValidPos = LevelManager.Instance.GetGridPosition(id);
			player.ChangeState(Player.PlayerStates.Idle);
		}
	}

	override public void Update(float elapsedTime)
	{
	}

	override public void Exit()
	{
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.RemoveListener(CubeHasArrived);
		LevelManager.Instance.playercubeManager.CubeToPlayerLayer(player.Id);
	}
}
