using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerDraggedState : FSM.State
{
	private Vector3 currentGroundPos;
	public float deltaTimeInSecond = 2;
	private float timeStoppedMoving;
	public float deltaMove = 0.1f;
	private Player player;

	override public void Enter()
	{
		currentGroundPos = fsm.transform.position;
		currentGroundPos.y = 0;
		timeStoppedMoving = 0;
		player = fsm.GetComponent<Player>();
	}

	override public void Update(float elapsedTime)
	{
		Vector3 groundPosition = fsm.transform.position;
		groundPosition.y = 0;
		float distance = Vector3.Distance(groundPosition, currentGroundPos);

		timeStoppedMoving += Time.deltaTime;

		bool isValidPosition = LevelManager.Instance.CheckGridPosition(player.Id);

		if (distance > deltaMove)
		{
			//Still moving
			timeStoppedMoving = 0;
			currentGroundPos = fsm.transform.position;
			currentGroundPos.y = 0;
			//Check if position valid
			if (isValidPosition)
			{
				//Go to player layer
				LevelManager.Instance.playercubeManager.CubeToPlayerLayer(player.Id);
			}
			else
			{
				//Go to Ghost layer
				LevelManager.Instance.playercubeManager.CubeToGhostLayer(player.Id);
			}
		}
		else
		{
			if (timeStoppedMoving > deltaTimeInSecond)
			{
				//Didn't move for deltaTimeInSecond, is the current position valid ?
				if (isValidPosition)
				{
					player.ChangeState(Player.PlayerStates.Idle);
					LevelManager.Instance.playercubeManager.CubeToPlayerLayer(player.Id);
				}
				else
				{
					player.ChangeState(Player.PlayerStates.GotoGoodPosition);
				}
			}
		}
	}
}
