using System;
using UnityEngine;

public abstract class StateMachineSingleton<T1, T2> : StateMachineBase<T1> where T1 : IConvertible,
	IComparable
	where T2 : StateMachineSingleton<T1, T2>
{
	#region Static Stuff

	protected static StateMachineSingleton<T1, T2> _instance;
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