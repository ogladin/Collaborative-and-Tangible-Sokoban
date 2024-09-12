
using TMPro;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class GameCongratState : FSM.State
{
	private float elapsedTime;
	public float ShowTime = 3.0f;
	public TextMeshPro Text;
	public string CongratText;



	override public void Enter()
	{
		elapsedTime = 0;
		Text.SetText(CongratText);
		Text.gameObject.SetActive(true);
		LevelManager.Instance.playercubeManager.PlayVictorySound();
	}

	override public void Update(float elapsedTime)
	{
		this.elapsedTime += elapsedTime;
		if (this.elapsedTime > ShowTime)
		{
			if (LevelManager.Instance.NetworkMode && LevelManager.Instance.isHost)
				LevelManager.Instance.playercubeManager.GameStateChangeRpc(LevelManager.GameStates.Waiting);
			LevelManager.Instance.ChangeGameState(LevelManager.GameStates.Waiting);

		}

	}

	override public void Exit()
	{
		Text.gameObject.SetActive(false);
	}
}
