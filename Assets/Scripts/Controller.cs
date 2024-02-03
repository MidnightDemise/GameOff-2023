using UnityEngine;

namespace game
{
    public abstract class Controller : ScriptableObject
    {
        public Character character { get; set; }
        public abstract void Init(Character character);
        public abstract void OnCharacterUpdate();
        public abstract void OnCharacterFixedUpdate();
    }
}
