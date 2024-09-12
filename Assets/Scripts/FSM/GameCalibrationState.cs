using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

enum CalibrationStep
{
	UpperRightCornerMeasurement,
	UpperLeftCornerMeasurement,
	LowerLeftCornerMeasurement,
	Complete,
	GotoUpperRightCorner,
	GotoUpperLeftCorner,
	GotoLowerLeftCorner
}

[System.Serializable]
public class GameCalibrationState : FSM.State
{
	public TextMeshPro Text;


	private CalibrationStep currentStep;

	public string UpperRightCornerText =
		"Place the green TOIO on the green target and press space or esc to cancel calibration";

	public string UpperLeftCornerText =
		"Place the green TOIO on the green target and press space or esc to cancel calibration";

	public string LowerLeftCornerText =
		"Place the green TOIO on the green target and press space or esc to cancel calibration";

	public string CalibrationCompleteText = "Calibration complete ! Press space";


	public string CalibrationTestText = "Testing calibration ! ";


	private Vector2Int expectedUpperRightCornerMatPos;
	private Vector2Int expectedUpperLeftCornerMatPos;
	private Vector2Int expectedLowerLeftCornerMatPos;

	private Vector2Int measuredUpperRightCornerMatPos;
	private Vector2Int measuredUpperLeftCornerMatPos;
	private Vector2Int measuredLowerLeftCornerMatPos;

	override public void Enter()
	{
		currentStep = CalibrationStep.UpperRightCornerMeasurement;
		Vector3 unityPos = LevelManager.Instance.grid.getUnityPositionFromGridPosition(new Vector2Int( 1, 1 ));
		expectedUpperRightCornerMatPos = LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityPos);
		//Debug.LogError("expectedUpperRightCornerMatPos Position x: " + expectedUpperRightCornerMatPos.x + " y:" + expectedUpperRightCornerMatPos.y);
		LevelManager.Instance.SetMarkerPosition(0, unityPos);
		Text.SetText(UpperRightCornerText);
		Text.gameObject.SetActive(true);
		LevelManager.Instance.HideMarker(1);
	}

	Vector2Int GetCubeMatPos()
	{
		Vector3 unityPos = LevelManager.Instance.playercubeManager.GetCubeTransform(0).position;
		Vector2Int matPos = LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityPos);
		//Debug.LogError("Position x: " + matPos.x + " y:" + matPos.y);
		return matPos;
	}

	override public void Update(float elapsedTime)
	{
		if (Input.GetKeyDown("escape"))
		{
			LevelManager.Instance.ChangeGameState(LevelManager.GameStates.Waiting);
		}
		else if (Input.GetKeyDown("space"))
		{
			switch (currentStep)
			{
				case CalibrationStep.UpperRightCornerMeasurement:
					measuredUpperRightCornerMatPos = GetCubeMatPos();
					Vector3 unityUpperLeftCornerPos =
						LevelManager.Instance.grid.getUnityPositionFromGridPosition(new Vector2Int(
							LevelManager.Instance.grid.GetGridWidth() - 2, 1 ));
					expectedUpperLeftCornerMatPos =
						LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityUpperLeftCornerPos);
					//Debug.LogError("expectedUpperLeftCornerMatPos Position x: " + expectedUpperLeftCornerMatPos.x + " y:" + expectedUpperLeftCornerMatPos.y);

					LevelManager.Instance.SetMarkerPosition(0, unityUpperLeftCornerPos);
					currentStep = CalibrationStep.UpperLeftCornerMeasurement;
					Text.SetText(UpperLeftCornerText);
					break;
				case CalibrationStep.UpperLeftCornerMeasurement:
					measuredUpperLeftCornerMatPos = GetCubeMatPos();
					Vector3 unityLowerLeftCornerPos = LevelManager.Instance.grid.getUnityPositionFromGridPosition(new Vector2Int(
						LevelManager.Instance.grid.GetGridWidth() - 2, LevelManager.Instance.grid.GetGridHeight() - 2));

					expectedLowerLeftCornerMatPos =
						LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityLowerLeftCornerPos);
					//Debug.LogError("expectedLowerLeftCornerMatPos Position x: " + expectedLowerLeftCornerMatPos.x + " y:" + expectedLowerLeftCornerMatPos.y);

					LevelManager.Instance.SetMarkerPosition(0, unityLowerLeftCornerPos);
					currentStep = CalibrationStep.LowerLeftCornerMeasurement;
					Text.SetText(LowerLeftCornerText);
					break;
				case CalibrationStep.LowerLeftCornerMeasurement:
					measuredLowerLeftCornerMatPos = GetCubeMatPos();
					LevelManager.Instance.playercubeManager.Calibrate(measuredUpperRightCornerMatPos,
						measuredUpperLeftCornerMatPos, measuredLowerLeftCornerMatPos);
					currentStep = CalibrationStep.Complete;
					LevelManager.Instance.HideMarker(0);
					Text.SetText(CalibrationCompleteText);
					break;
				case CalibrationStep.Complete:
					LevelManager.Instance.ChangeGameState(LevelManager.GameStates.Waiting);
					break;
			}
		}
		else if (Input.GetKeyDown("return"))
		{
			Vector3 target;
			Vector3 unityPos = LevelManager.Instance.grid.getUnityPositionFromGridPosition(new Vector2Int(1, 1 ));
			LevelManager.Instance.SetMarkerPosition(0, unityPos);

			expectedUpperRightCornerMatPos = LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityPos);

			LevelManager.Instance.playercubeManager.CubeReadyMovementReached.AddListener(CubeHasArrived);
			Text.SetText(CalibrationTestText);
			currentStep = CalibrationStep.GotoUpperRightCorner;
			target = LevelManager.Instance.playercubeManager.mat.MatCoord2UnityCoord(
				expectedUpperRightCornerMatPos.x, expectedUpperRightCornerMatPos.y);
			LevelManager.Instance.playercubeManager.MoveTo(0, target);
		}
	}

	public void CubeHasArrived(int id)
	{
		if (id != 0)
		{
			Debug.LogError("GameCalibration check wrong id");
			return;
		}

		Vector3 target;

		switch (currentStep)
		{
			case CalibrationStep.GotoUpperRightCorner:
				currentStep = CalibrationStep.GotoUpperLeftCorner;
				Vector3 unityUpperLeftCornerPos =
					LevelManager.Instance.grid.getUnityPositionFromGridPosition(new Vector2Int(
						LevelManager.Instance.grid.GetGridWidth() - 2, 1 ));
				LevelManager.Instance.SetMarkerPosition(0, unityUpperLeftCornerPos);

				expectedUpperLeftCornerMatPos =
					LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityUpperLeftCornerPos);

				target = LevelManager.Instance.playercubeManager.mat.MatCoord2UnityCoord(
					expectedUpperLeftCornerMatPos.x, expectedUpperLeftCornerMatPos.y);
				LevelManager.Instance.playercubeManager.MoveTo(0, target);
				break;
			case CalibrationStep.GotoUpperLeftCorner:
				currentStep = CalibrationStep.GotoLowerLeftCorner;
				Vector3 unityLowerLeftCornerPos = LevelManager.Instance.grid.getUnityPositionFromGridPosition(new Vector2Int(
					LevelManager.Instance.grid.GetGridWidth() - 2, LevelManager.Instance.grid.GetGridHeight() - 2
				));
				LevelManager.Instance.SetMarkerPosition(0, unityLowerLeftCornerPos);

				expectedLowerLeftCornerMatPos =
					LevelManager.Instance.playercubeManager.mat.UnityCoord2MatCoord(unityLowerLeftCornerPos);
				target = LevelManager.Instance.playercubeManager.mat.MatCoord2UnityCoord(
					expectedLowerLeftCornerMatPos.x, expectedLowerLeftCornerMatPos.y);
				LevelManager.Instance.playercubeManager.MoveTo(0, target);
				break;
			case CalibrationStep.GotoLowerLeftCorner:
				LevelManager.Instance.playercubeManager.CubeReadyMovementReached.RemoveListener(CubeHasArrived);
				LevelManager.Instance.ChangeGameState(LevelManager.GameStates.Waiting);
				break;
		}
	}


	override public void Exit()
	{
		Text.gameObject.SetActive(false);
	}
}
