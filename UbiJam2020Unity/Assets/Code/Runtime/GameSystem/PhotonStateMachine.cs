using System;
using Photon.Pun;
using UnityEngine;

namespace Runtime.GameSystem
{
	[RequireComponent(typeof(PhotonView)),]
	public abstract class PhotonStateMachine<T> : MonoBehaviour where T : IConvertible, IComparable
	{
		#region Private Fields

#if ODIN_INSPECTOR
	[ShowInInspector] [DebugGroup]
#endif
		private T _state;
		public PhotonView PhotonView { get; private set; }

		#endregion

		#region Properties

		protected abstract T InitialState { get; }
		public T State => _state;

		#endregion

		#region Unity methods

		private bool _initialized;

		protected virtual void Update()
		{
			if (PhotonView.IsMine)
			{
				if (!_initialized)
				{
					_initialized = true;
					CallSwitchStateRPC(InitialState, InitialState);
				}
				else
				{
					ReevaluateState();
				}
			}
		}

		protected virtual void OnEnable()
		{
			_state = InitialState;
			PhotonView = GetComponent<PhotonView>();
		}

		#endregion

		#region Protected methods

		protected void ReevaluateState()
		{
			T newState = GetNextState();
			if (_state.CompareTo(newState) != 0)
			{
				T oldState = _state;

				CallSwitchStateRPC(oldState, newState);
			}
		}

		private void CallSwitchStateRPC(T oldState, T newState)
		{
			PhotonView.RPC("RPCOnStateChange", RpcTarget.All, StateToByte(oldState), StateToByte(newState));
		}

		protected abstract byte StateToByte(T state);
		protected abstract T ByteToState(byte state);

		protected virtual void RPCOnStateChange(byte oldStateByte, byte newStateByte)
		{
			T newState = ByteToState(newStateByte);
			T oldState = ByteToState(oldStateByte);
			_state = newState;
			OnStateChange(oldState, newState);
		}

		protected abstract T GetNextState();
		protected abstract void OnStateChange(T oldState, T newState);

		#endregion
	}
}