using System.Collections.Generic;
using UnityEngine;

public class FSM : MonoBehaviour
{
	//Base class for all states, only the required methods need to be overriden
	public enum StateEnum
	{
	};

	public class State
	{
		[HideInInspector] public FSM fsm;

		public virtual void Enter()
		{
		}

		public virtual void Update(float elapsedTime)
		{
		}

		public virtual void Exit()
		{
		}
	};

	//Current state being handled in this FSM
	public State currentState { private set; get; }

	public Dictionary<int, State> states = new Dictionary<int, State>();

	public void ChangeState(int id)
	{
		State nextState = states[id];
		if (nextState != null)
			ChangeState(nextState);
		else
			Debug.LogError("Undefined State " + id);
	}

	public void ChangeState(State newState)
	{
		//Exit the current state
		if (currentState != null)
		{
			Debug.Log("Exiting " + currentState);
			currentState.Exit();
		}

		//Change to the new state
		currentState = newState;

		//Enter the new state
		if (newState != null)
		{
			newState.fsm = this;
			Debug.Log("Entering " + newState);

			newState.Enter();
		}
	}

	void Update()
	{
		//Update the currentState
		if (currentState != null)
			currentState.Update(Time.deltaTime);
	}
}
