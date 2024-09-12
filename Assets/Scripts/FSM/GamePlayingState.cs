#define DEBUG_LOGGER

using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class GamePlayingState : FSM.State
{
	private float elapsedTime;
	private bool isReloading = false;
	private int greenUndoNumber = 0;
	private int blueUndoNumber = 0;
	private int greenMovesBeforeReloading;
	private int blueMovesBeforeReloading;
	private string gameStateToReload = "";

	override public void Enter()
	{
		FileLogger.Logger.LogString("Start Level");
		if (!isReloading)
		{
			elapsedTime = 0.0f;
			greenUndoNumber = 0;
			blueUndoNumber = 0;
		}

		isReloading = false;
	}

	override public void Update(float elapsedTime)
	{
		this.elapsedTime += elapsedTime;
		bool GotoCongratState = false;
		bool ReloadLevel = false;
		bool FullReloadLevel = false;

		if ((LevelManager.Instance.NetworkMode && LevelManager.Instance.isHost) || !LevelManager.Instance.NetworkMode)
		{
			if (LevelManager.Instance.CheckIfLevelCompleted())
				GotoCongratState = true;

			if (Input.GetKeyDown("space"))
				GotoCongratState = true;
			if (Input.GetKeyDown("return"))
				FullReloadLevel = true;
			if (Input.GetKeyDown("s"))
			{
				LevelManager.Instance.shakeCube(0);
			}

			if (Input.GetKeyDown("d"))
			{
				LevelManager.Instance.shakeCube(1);
			}

			if (LevelManager.Instance.CubeWasShaken(0))
			{
				gameStateToReload = LevelManager.Instance.PopLastGameState(0);
				if (gameStateToReload.Length > 0)
				{
					ReloadLevel = true;
					greenUndoNumber++;
				}
			}

			if (LevelManager.Instance.CubeWasShaken(1))
			{
				gameStateToReload = LevelManager.Instance.PopLastGameState(1);
				if (gameStateToReload.Length > 0)
				{
					ReloadLevel = true;
					blueUndoNumber++;

				}
			}

			// if (LevelManager.Instance.CubesWereShaken())
			// {
			// 	ReloadLevel = true;
			// 	gameStateToReload = "";
			// }
		}

		if (GotoCongratState)
		{
			if (LevelManager.Instance.NetworkMode && LevelManager.Instance.isHost)
				LevelManager.Instance.playercubeManager.GameStateChangeRpc(LevelManager.GameStates.Congrat);
			LevelManager.Instance.ChangeGameState(LevelManager.GameStates.Congrat);
			LevelManager.Instance.ResetGameStates();
		}
		else if (FullReloadLevel)
		{
			greenUndoNumber = 0;
			blueUndoNumber = 0;
			LevelManager.Instance.ResetGameStates();
			LevelManager.Instance.WaitingState.FullReloadLevel();
			LevelManager.Instance.ResetMoveNumber();
		}
		else if (ReloadLevel)
		{
			isReloading = true;
			greenMovesBeforeReloading = LevelManager.Instance.GetGreenMoveNumber();
			blueMovesBeforeReloading = LevelManager.Instance.GetBlueMoveNumber();
			LevelManager.Instance.WaitingState.ReloadLevel(gameStateToReload);
		}
	}

	override public void Exit()
	{
		if (!isReloading)
		{
			FileLogger.Logger.LogString("Complete Level in " + elapsedTime + " second(s) and " +
			                            LevelManager.Instance.GetGreenMoveNumber() + " green move(s) and " +
			                            LevelManager.Instance.GetBlueMoveNumber() + " blue move(s) and " +
			                            " with " + greenUndoNumber + " green undo(s) and " +
			                            blueUndoNumber + " blue undo(s)"
			                            );
		}
		else
		{
			FileLogger.Logger.LogString("Undo at " + elapsedTime + " second(s) and " +
			                            greenMovesBeforeReloading + "  green move(s) and " +
										blueMovesBeforeReloading + " blue move(s), " +
										greenUndoNumber + " green undo(s) and " +
			                            blueUndoNumber + " blue undo(s)");
		}
	}
}
