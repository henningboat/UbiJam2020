using System;
using UnityEngine;

namespace Runtime.Utils
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                throw new Exception($"Duplicated Singleton of type {typeof(T)}");
            }

            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }
    }
}