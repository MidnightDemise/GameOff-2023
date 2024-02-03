using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Helper;
using System.Runtime.CompilerServices;

namespace game
{
   
    [CreateAssetMenu(fileName = "PlayerController", menuName = "Controller/PlayerController")]
    public class PlayerController : Controller
    {
        public float ControlRotationSensitivity = 1.0f;

        public InputHandler playerInput;
        public PlayerCamera playerCam;
        public CoroutineLauncher launcher;
        private string surfaceState;
        private string movementState;
        private string gravityState;
        private string rotationState;

       
        public override void Init(Character character)
        {
            this.character = character;

            playerInput = InputHandler.Instance;
            playerCam = PlayerCamera.Instance;
            launcher = this.character.GetComponent<CoroutineLauncher>();
            SetSurfaceCollisions();
            this.character.movementSettings.substates = new SubstateMachine();
            this.character.movementSettings.substates.AddState(Void);
            this.character.movementSettings.substates.AddState(Running);

        }

        public override void OnCharacterUpdate()
        {
            playerInput.TickInput(Time.deltaTime); 
            UpdateControlRotation();
            character.SetMovementInput(GetMovementInput());
            character.SetJumpInput(playerInput.jumpInput > 0f);
            UpdateSurfaceStates();
            UpdateMovementStates();
        }

        public override void OnCharacterFixedUpdate()
        {
            playerCam.SetPosition(character.transform.position);
            playerCam.SetControlRotation(character.GetControlRotation());
        }

        private void UpdateControlRotation()
        {
            Vector2 camInput = playerInput.mouseInput;
            Vector2 controlRotation = character.GetControlRotation();

            // Adjust the pitch angle (X Rotation)
            float pitchAngle = controlRotation.x;
            pitchAngle -= camInput.y * ControlRotationSensitivity;

            // Adjust the yaw angle (Y Rotation)
            float yawAngle = controlRotation.y;
            yawAngle += camInput.x * ControlRotationSensitivity;

            controlRotation = new Vector2(pitchAngle, yawAngle);
            character.SetControlRotation(controlRotation);
        }

        private Vector3 GetMovementInput()
        {
            // Calculate the move direction relative to the character's yaw rotation
            Quaternion yawRotation = Quaternion.Euler(0.0f, character.GetControlRotation().y, 0.0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            Vector3 movementInput = (forward * playerInput.movementInput.y + right * playerInput.movementInput.x);

            if (movementInput.sqrMagnitude > 1f)
            {
                movementInput.Normalize();
            }

            return movementInput;
        }

        /// <summary>
        /// Parameters for Character BoxCast
        /// index: 0 Must be a ground check
        /// </summary>
        private void SetSurfaceCollisions()
        {
            character.surfaceCollisions.numOfBoxes = 4;
            int boxes = character.surfaceCollisions.numOfBoxes;
            float scaleWeight = .3f;

            // 0 = ground check 
            // 1 = left check | 2 = right check | 3 = forward check 
            character.surfaceCollisions.surfaceLayers = 1 << 8;
            Debug.Log(character.surfaceCollisions.boxCastScale);
            Vector2 scale = character.characterColliderScale; 
            character.surfaceCollisions.boxCastPositions = new Vector3[] 
                { Vector3.down, Vector3.left * .5f, Vector3.right * .5f, Vector3.forward * .5f};
            character.surfaceCollisions.boxCastScale = new Vector3[]
                { new Vector3(scale.x * 2, scale.x * .7f, scale.x * 2), 
                    new Vector3(scale.x * scaleWeight,scale.y * .6f,scale.x * 2),
                    new Vector3(scale.x * scaleWeight, scale.y * .6f, scale.x * 2),
                    new Vector3(scale.x * 2, scale.y * .6f, scale.x * scaleWeight)};
            character.surfaceCollisions.boxRotations = new Vector3[boxes]; 
            character.surfaceCollisions.surfaceNormals = new Vector3[boxes];

            // set substates 
            character.surfaceCollisions.substates = new SubstateMachine();
            character.surfaceCollisions.substates.AddState(Void);
            character.surfaceCollisions.substates.AddState(JustWalkedOffLedge);

        }

        #region player state machines
        private void UpdateSurfaceStates()
        { 
            surfaceState = character.surfaceCollisions.substates.currentState;
            if (character.surfaceCollisions.surfaceNormals[0].y > 0)
            {
                character.surfaceCollisions.surfaceState = SurfaceState.OnGround; 
            }
            else if (MovingIntoSurface())
            { 
                character.surfaceCollisions.surfaceState = SurfaceState.OnSurface;
            }
            else
            {
                if (character.surfaceCollisions.surfaceState != SurfaceState.InAir)
                {
                    character.surfaceCollisions.substates.Run("JustWalkedOffLedge");
                }
                else
                {
                    character.surfaceCollisions.substates.currentState = "Void";
                }
                character.surfaceCollisions.surfaceState = SurfaceState.InAir;
                if (surfaceState == "JustWalkedOffLedge")
                {
                    character.surfaceCollisions.justWalkedOffEdge = true;
                }
                else
                    character.surfaceCollisions.justWalkedOffEdge = false;
            }
        }

        private void UpdateMovementStates()
        {
            movementState = character.movementSettings.substates.currentState;

            switch(movementState)
            {
                case "Void":
                    if (playerInput.sprintInput > 0f)
                    {
                        character.movementSettings.substates.Run("Running");
                        Debug.Log("started running");
                    }
                    break;
                case "Running":
                    break;
            }
        }

        private bool MovingIntoSurface()
        {
            Vector3[] normals = character.surfaceCollisions.surfaceNormals;
            for (int i = 1; i < normals.Length; i++)
            {
                if (Mathf.Abs(normals[i].x) > .6f || Mathf.Abs(normals[i].z) > .6f)
                    return true;
            }
            return false;
        }
        #endregion

        #region substate Actions
        private void JustWalkedOffLedge() 
        {
            launcher.Launch(character.surfaceCollisions.substates.HoldCurrentStateTill()); 
            
        }

        private void Running()
        {
            if(playerInput.sprintInput > 0f)
            {
                character.movementSettings.maxHorizontalSpeed = 16f;
                Debug.Log("running");
            }
            else
            {
                character.movementSettings.maxHorizontalSpeed = 8f;
                character.movementSettings.substates.currentState = "Void";
                Debug.Log("stopped running");
            }
        }

        private void Void() { }
        #endregion
    }

}
