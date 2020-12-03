using System;
using UnityEngine;

namespace Runtime.GameSystem
{
	public abstract class PhotonStateMachineSingleton<T1, T2> : PhotonStateMachine<T1> where T1 : IConvertible, IComparable where T2 : PhotonStateMachineSingleton<T1, T2>
	{
		#region Static Stuff

		protected static PhotonStateMachineSingleton<T1, T2> _instance;
		private static bool _searchedForInstance;
		public static T2 Instance => _instance as T2;

		#endregion

		#region Unity methods

		protected virtual void Awake()
		{
			if (_instance != null)
			{
				Debug.Log(string.Format("Multiple instances of script {0}!", GetType()), gameObject);
				Debug.Log("Other instance: " + _instance, _instance);
			}
			else
			{
				_instance = this;
			}
		}

		protected virtual void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}

		#endregion
	}
}