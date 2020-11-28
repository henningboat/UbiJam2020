using System;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public abstract class StateMachineBase<T> : MonoBehaviour where T : IComparable, IConvertible
{
	#region Private Fields

#if ODIN_INSPECTOR
	[ShowInInspector] [DebugGroup]
#endif
	private T _state;

	#endregion

	#region Properties

	protected abstract T InitialState { get; }
	public T State => _state;

	#endregion

	#region Unity methods

	protected virtual void Update()
	{
		ReevaluateState();
	}

	protected virtual void OnEnable()
	{
		_state = InitialState;
		OnStateChange(State, State);
	}

	#endregion

	#region Protected methods

	protected void ReevaluateState()
	{
		T newState = GetNextState();
		if (_state.CompareTo(newState) != 0)
		{
			T oldState = _state;
			_state = newState;
			OnStateChange(oldState, newState);
		}
	}

	protected abstract T GetNextState();
	protected abstract void OnStateChange(T oldState, T newState);

	#endregion
}