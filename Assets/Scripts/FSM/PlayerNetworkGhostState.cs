using UnityEngine;

[System.Serializable]
public class PlayerNetworkGhostState : FSM.State
{

	private Player player;
	Vector3 target = Vector3.positiveInfinity;
	Vector3 pos = Vector3.positiveInfinity;

	override public void Enter()
	{
		player = fsm.GetComponent<Player>();
		LevelManager.Instance.playercubeManager.CubeToGhostLayer(player.Id);
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.AddListener(CubeHasArrived);

	}

	override public void Update(float elapsedTime)
	{
		if (target != LevelManager.Instance.playercubeManager.RemoteplayerPosition || pos != player.transform.position)
		{
			target = LevelManager.Instance.playercubeManager.RemoteplayerPosition;
			LevelManager.Instance.playercubeManager.MoveTo(player.Id,target);
			LevelManager.Instance.CheckGridPosition(player.Id);
			LevelManager.Instance.SetMarkerPosition(player.Id,target);
		}

	}

	public void CubeHasArrived(int id)
	{
		if (id == player.Id)
		{
			LevelManager.Instance.CheckGridPosition(player.Id);
			pos = player.transform.position;
		}
	}

	override public void Exit()
	{
		LevelManager.Instance.playercubeManager.CubeReadyMovementReached.RemoveListener(CubeHasArrived);
		LevelManager.Instance.playercubeManager.CubeToPlayerLayer(player.Id);
	}

}
