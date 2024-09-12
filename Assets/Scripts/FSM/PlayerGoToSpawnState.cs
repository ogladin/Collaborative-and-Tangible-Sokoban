using UnityEngine;

[System.Serializable]
public class PlayerGoToSpawnState : FSM.State
{
	private Player player;

	override public void Enter()
	{
		player = fsm.GetComponent<Player>();
		int id = player.Id;
		LevelManager.Instance.MoveToSpawnPoint(id);

		Debug.Log("PlayerGoToSpawnState ENTER");
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.AddListener(CubeHasArrived);
	}

	public void CubeHasArrived(int id)
	{
		if (id == player.Id)
		{
			player.lastValidPos = LevelManager.Instance.GetGridPosition(id);
			player.ChangeState(Player.PlayerStates.Idle);
		}
	}

	override public void Update(float elapsedTime)
	{
	}

	public override void Exit()
	{
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.RemoveListener(CubeHasArrived);
		LevelManager.Instance.playercubeManager.CubeToPlayerLayer(player.Id);
	}
}
