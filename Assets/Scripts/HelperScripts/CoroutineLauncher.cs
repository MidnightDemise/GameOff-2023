using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helper
{

    public class CoroutineLauncher : MonoBehaviour
    {
    
        public void Launch(IEnumerator routine) { StartCoroutine(routine); }
    }

}