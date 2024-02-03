using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helper
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T> 
    {
        
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                { 
                    instance = FindObjectOfType<T>();
                    if(instance == null)
                    {
                        GameObject singletonObj = new GameObject();
                        instance = singletonObj.AddComponent<T>(); 
                        DontDestroyOnLoad(singletonObj); 
                    }
                    else
                        instance.Init();
                }
                return instance;
            }
        }

       protected virtual void Init() { } 
    }


}
