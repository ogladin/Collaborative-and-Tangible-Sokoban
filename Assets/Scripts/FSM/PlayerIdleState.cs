using UnityEngine;

[System.Serializable]
public class PlayerIdleState : FSM.State
{
	private Vector3 currentGroundPos;
	public float positionDelta = 0.01f;
	private Player player;

	override public void Enter()
	{
		player = fsm.GetComponent<Player>();
		currentGroundPos = fsm.transform.position;
		currentGroundPos.y = 0;
		LevelManager.Instance.CheckGridPosition(player.Id);
	}

	override public void Update(float elapsedTime)
	{
		Vector3 groundPosition = fsm.transform.position;
		groundPosition.y = 0;
		float distance = Vector3.Distance(groundPosition, currentGroundPos);
		if (distance > positionDelta)
		{
			player.ChangeState(Player.PlayerStates.Dragged);
		}
	}
}
