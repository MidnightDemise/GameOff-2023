using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;
namespace Helper
{
    public class SubstateMachine 
    {
        public delegate void MyStates();
      
        private string _currentState;
        private List<MyStates> stateActions;
        private Dictionary<string, int> stateMap;
        public bool lockState = false;
      
        public int currentStateIndex => stateMap[currentState]; // this is giving void sometimes 
        public string currentState
        {
            get 
            {
                if (_currentState == null)
                    return "Void";
                return _currentState; }
            set
            {

                if (!lockState && _currentState != value)
                {

                    if (stateMap.ContainsKey(value))
                    { _currentState = value; }
                    else
                        _currentState = "Void";
                } 
            }
        }

        /// <summary>
        /// Event based state machine, portable and modular
        /// intended to be used in conjunction with controller records 
        /// </summary>
        public SubstateMachine()
        {
            stateActions = new List<MyStates>();
            stateMap = new Dictionary<string, int>();
        }
        
        public void AddState(MyStates state)
        {
            stateActions.Add(state);
            stateMap.Add(state.Method.Name, stateActions.Count-1); 
        }

        public MyStates GetState(string name)
        {
            if (stateMap.ContainsKey(name))
            {
                return stateActions[stateMap[name]];
            }
            else
            {
                Debug.Log("ERROR: State " + name + " not in substate machine");
                return null;
            }
        }

        public void RunCurrent()
        {
            int index = currentStateIndex;
            if (index >= 0 && index < stateActions.Count -1)
            {
                stateActions[index].Invoke();
            }

        }

        public void Run(string name)
        {
            MyStates state = GetState(name);
            if (state != null)
            {
                
                currentState = name;
                state.Invoke();
            }
        }


        public IEnumerator HoldCurrentStateTill(float time)
        {
            lockState = true;
            yield return new WaitForSeconds(time);
            lockState = false;
        }

        public IEnumerator HoldCurrentStateTill(Func<bool> action)
        { 
            lockState = true;
            yield return new WaitUntil(() => action());
            lockState = false; 
        }

        public IEnumerator HoldCurrentStateTill()
        {
            lockState = true;
            yield return new WaitForFixedUpdate();
            lockState = false;
        }


    }
}
