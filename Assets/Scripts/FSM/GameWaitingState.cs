#define DEBUG_LOGGER

using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class GameWaitingState : FSM.State
{
	public List<TextAsset> Levels;
	public int levelId = -1;
	public GameObject greenReadyPosition;
	public GameObject blueReadyPosition;
	public float readyDelta = 0.03f;
	public TextMeshPro Text;
	public string WaitingText;

	override public void Enter()
	{
		LevelManager.Instance.HideMarkers();

		LevelManager.Instance.DestroyLevel();

		Text.SetText(WaitingText);
		Text.gameObject.SetActive(true);
		if (LevelManager.Instance.NetworkMode)
		{
			if (LevelManager.Instance.isHost)
			{
				//Network Host
				//Remote player
				Player player = LevelManager.Instance.playercubeManager.GetPlayer(1).GetComponent<Player>();
				player.ChangeState(Player.PlayerStates.NetworkGhost);
				greenReadyPosition.SetActive(true);

			}
			else
			{
				//Network client
				//Remote player
				Player player = LevelManager.Instance.playercubeManager.GetPlayer(0).GetComponent<Player>();
				player.ChangeState(Player.PlayerStates.NetworkGhost);
				blueReadyPosition.SetActive(true);

			}
		}
		else
		{
			greenReadyPosition.SetActive(true);
			blueReadyPosition.SetActive(true);
		}
	}

	public override void Exit()
	{
		LevelManager.Instance.ShowMarkers();
		Text.gameObject.SetActive(false);
		greenReadyPosition.SetActive(false);
		blueReadyPosition.SetActive(false);
	}

	public void GoToNextLevel()
	{
		levelId++;
		if (levelId >= Levels.Count) levelId = 0;
		FileLogger.Logger.LogString("Go to next Level: " + Levels[levelId].name);
		LoadLevel();
	}

	public void ReloadLevel(string level="")
	{
		LoadLevel(level);

	}

	public void FullReloadLevel()
	{
		FileLogger.Logger.LogString("Completely reload current Level "+ Levels[levelId].name);

		LoadLevel(Levels[levelId].text,false);

	}

	private void LoadLevel(string level="",bool rewind=true)
	{

		if (level.Length == 0)
		{
			LevelManager.Instance.LoadLevel(Levels[levelId]);
			if (LevelManager.Instance.NetworkMode && LevelManager.Instance.isHost)
			{
				LevelManager.Instance.playercubeManager.LoadLevelRpc(levelId);
				LevelManager.Instance.playercubeManager.GameStateChangeRpc(LevelManager.GameStates.MovingToStart);
			}
		}
		else
		{
			LevelManager.Instance.LoadLevel(level,rewind);
			if (LevelManager.Instance.NetworkMode && LevelManager.Instance.isHost)
			{
				LevelManager.Instance.playercubeManager.LoadLevelRpc(level);
				LevelManager.Instance.playercubeManager.GameStateChangeRpc(LevelManager.GameStates.MovingToStart);
			}
		}



		LevelManager.Instance.ChangeGameState(LevelManager.GameStates.MovingToStart);

	}

	override public void Update(float elapsedTime)
	{
		if ((LevelManager.Instance.NetworkMode && LevelManager.Instance.isHost) || !LevelManager.Instance.NetworkMode)
		{

			if (Input.GetKeyDown("space"))
			{
				GoToNextLevel();
			}

			float greenDistanceToReady =
				Vector3.Distance(LevelManager.Instance.playercubeManager.GetPlayer(0).getCalibratedCube().transform.position,
					greenReadyPosition.transform.position);
			if (greenDistanceToReady < readyDelta)
			{
				float blueDistanceToReady =
					Vector3.Distance(LevelManager.Instance.playercubeManager.GetPlayer(1).getCalibratedCube().transform.position,
						blueReadyPosition.transform.position);
				if (blueDistanceToReady < readyDelta)
				{
					GoToNextLevel();
				}
			}
		}

		if (LevelManager.Instance.NetworkMode)
		{

			Player player;
			if (LevelManager.Instance.isHost)
			{
				player = LevelManager.Instance.playercubeManager.GetPlayer(0);
			}
			else
			{
				player = LevelManager.Instance.playercubeManager.GetPlayer(1);
			}

			player.lastValidPos =
				LevelManager.Instance.grid.getGridPositionFromUnityPosition(player.getCalibratedCube().transform.position);
		}

	}
}
