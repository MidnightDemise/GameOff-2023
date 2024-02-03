using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System;
using Helper;
 namespace game
{ 
    public enum ERotationBehavior
    {
        OrientRotationToMovement,
        UseControlRotation
    }
    public enum SurfaceState
    {
        OnGround,
        OnSurface,
        InAir
    }

    [System.Serializable]
    public record RotationSettings
    {
        [HideInInspector]
        public SubstateMachine substates;

        [Header("Control Rotation")]
        public float minPitchAngle = -45.0f;
        public float maxPitchAngle = 75.0f;

        [Header("Character Orientation")]
        public ERotationBehavior rotationBehavior = ERotationBehavior.OrientRotationToMovement;
        public float minRotationSpeed = 600.0f; // The turn speed when the player is at max speed (in degrees/second)
        public float maxRotationSpeed = 1200.0f; // The 

    }

    [System.Serializable]
    public record MovementSettings
    {
        [HideInInspector]
        public SubstateMachine substates;

        public float acceleration = 25.0f;
        public float decceleration = 25.0f;
        public float maxHorizontalSpeed = 8.0f;
        public float jumpSpeed = 10.0f;
        public float jumpAbortSpeed = 10.0f;
        public float coyoteTime = .005f;
    }

    [System.Serializable]
    public record GravitySettings
    {
        public float gravity = 20.0f;
        public float groundedGravity = 5.0f;
        public float maxFallSpeed = 40.0f;
    }

    // works really well with the substate system
    // this way the set of variables can be easily managed by the substatemachine
    // You can easily package and add state/ functions to modifiy these packaged variable sets 
    // this organizes variable manipulation and stays modular
    // flexible system to allow lots of behaviour 
    [System.Serializable]
    public record SurfaceCollisions
    {
        [HideInInspector]
        public SubstateMachine substates;
        public SurfaceState surfaceState;
        public Vector2 characterColliderScale;
        public bool justWalkedOffEdge;
       
        public LayerMask surfaceLayers;
        public int numOfBoxes = 1; // arr max
        public Vector3[] boxCastPositions = new Vector3[] { Vector3.zero };
        public Vector3[] boxCastScale = new Vector3[] { Vector3.zero }; 
        public Vector3[] boxRotations = new Vector3[] { Vector3.zero }; 
        public Vector3[] surfaceNormals = new Vector3[] { Vector3.zero };

    }

    // ^can likely do: similiar set up for hit box set up^

    public class Character : MonoBehaviour
    {
        public Controller controller;
        public MovementSettings movementSettings;
        public GravitySettings gravitySettings;
        public SurfaceCollisions surfaceCollisions;
        public RotationSettings rotationSettings;


        private CharacterController characterController;
        //private CharacterAnimator characterAnimator;

        private float targetHorizontalSpeed;
        private float horizontalSpeed;
        private float verticalSpeed;

        private Vector2 controlRotation; //X(pitch), Y(yaw)
        private Vector3 movementInput;
        private Vector3 lastMovementInput; 
        private bool hasMovementInput;
        private bool jumpInput;

        private RaycastHit hit;
        private (int, int) currentSurface;
        private (int, int) lastSurface;

        public Vector3 velocity => characterController.velocity;
        public Vector3 horizontalVelocity => characterController.velocity.SetY(0.0f);
        public Vector3 verticalVelocity => characterController.velocity.Multiply(0.0f, 1.0f, 0.0f);
        public Vector2 characterColliderScale => new Vector2(characterController.radius, characterController.height); 
        // should turn this into a static var

        private void Awake()
        {
            characterController = gameObject.GetComponent<CharacterController>();
            controller.Init(this); 
            //characterAnimator = gameObject.GetComponent<CharacterAnimator>();

        }
 
        private void Update()
        {
            controller.OnCharacterUpdate();
        }

        private void FixedUpdate()
        {
            controller.OnCharacterFixedUpdate();

            Tick(Time.deltaTime);
            
           
        }

        private void OnDrawGizmos()
        {
            for(int i = 0; i < surfaceCollisions.numOfBoxes; i++)
            {
                Gizmos.DrawRay(transform.position, surfaceCollisions.boxRotations[i].normalized * 1f); 
            } 
        } 

        // All functions to local functions to run on ficked update, used to pass delta time to all these functions 
        private void Tick(float deltaTime)
        {
            CheckSurfaces();

            UpdateHorizontalSpeed(deltaTime);
            UpdateVerticalSpeed(deltaTime); 
            
            Vector3 movement = horizontalSpeed * GetMovementInput() + verticalSpeed * Vector3.up;
            characterController.Move(movement * deltaTime);

            OrientToTargetRotation(movement.SetY(0.0f), deltaTime);
//            _characterAnimator.UpdateState();
        }

        public void SetMovementInput(Vector3 movementInput)
        {
            bool hasMovementInput = movementInput.sqrMagnitude > 0.0f;

            if (!this.hasMovementInput && hasMovementInput ) 
            {
                lastMovementInput = this.movementInput;
            }

            this.movementInput = movementInput;
            this.hasMovementInput = hasMovementInput;
        }

        
        public Vector3 GetMovementInput()
        {
            Vector3 movementInput = hasMovementInput ? this.movementInput : lastMovementInput;
            if(movementInput.sqrMagnitude > 1f)
            {
                movementInput.Normalize();
            }
            return movementInput; 
        }
        
        public void SetJumpInput(bool jumpInput)
        {
            this.jumpInput = jumpInput;
        }

        public Vector2 GetControlRotation()
        {
            return controlRotation;
        }

        public void SetControlRotation(Vector2 controlRotation)
        {
            float pitchAngle = controlRotation.x;
            pitchAngle %= 360.0f;
            pitchAngle = Mathf.Clamp(pitchAngle, rotationSettings.minPitchAngle, rotationSettings.maxPitchAngle);

            float yawAngle = controlRotation.y;
            yawAngle %= 360.0f;

            this.controlRotation = new Vector2(pitchAngle, yawAngle); 
        }

        private bool CheckSurfaces() 
        { 
            Vector3 direction;
            Vector3 position;
            Quaternion localRotation;
            bool gotHit = false;
            for (int i = 0; i < surfaceCollisions.numOfBoxes; i++)
            { 
                // quaternion point rotation
                position = surfaceCollisions.boxCastPositions[i];
                localRotation = new Quaternion(position.x, position.y, position.z, 0);
                localRotation = transform.rotation * localRotation * Quaternion.Inverse(transform.rotation); 
                direction = new Vector3(localRotation.x, localRotation.y, localRotation.z); 
                surfaceCollisions.boxRotations[i] = direction;

                // BoxCast with SurfaceCollision Settings based on Character local rotation
                gotHit = true == Physics.BoxCast(transform.position, surfaceCollisions.boxCastScale[i], direction, out hit, transform.rotation, 1f, surfaceCollisions.surfaceLayers); 
                surfaceCollisions.surfaceNormals[i] = hit.normal;

                // update surface info
                if (gotHit)
                {
                    if (currentSurface.Item2 != lastSurface.Item2)
                        lastSurface = currentSurface;
                    else
                    {
                        currentSurface.Item1 = i;
                        currentSurface.Item2 = hit.collider.GetHashCode();
                    }
                }
            }

            return gotHit;
        }


        // State machine needs a way to inject custom substates 
            // need to just have to change select weight variables likely
            // the controller can have its unique local state machine which changes the weights 
        private void UpdateVerticalSpeed(float deltaTime) 
        {
            // states updated by controller settings 
            switch (surfaceCollisions.surfaceState)
            {
                // in order for the substates to be modular and work correctly 
                case (SurfaceState.OnGround): 
                    surfaceCollisions.substates.RunCurrent();
                    verticalSpeed = -gravitySettings.groundedGravity; 
                    if (jumpInput)
                    {
                        verticalSpeed = movementSettings.jumpSpeed;
                    }
                    break;

                case (SurfaceState.OnSurface):

                    // have to move changing vert speed out of here 
                    surfaceCollisions.substates.RunCurrent(); 
                    verticalSpeed = 0f;
                    break;

                case (SurfaceState.InAir): 
                    surfaceCollisions.substates.RunCurrent();
                    if(!jumpInput && verticalSpeed > 0.0f)
                    {
                        verticalSpeed = Mathf.MoveTowards(verticalSpeed, -gravitySettings.maxFallSpeed, movementSettings.jumpAbortSpeed * deltaTime); 
                    }
                    else if(surfaceCollisions.justWalkedOffEdge && verticalSpeed <= 0)
                    {
                        if(jumpInput)
                            verticalSpeed = movementSettings.jumpSpeed;
                    } 
                    verticalSpeed = Mathf.MoveTowards(verticalSpeed, -gravitySettings.maxFallSpeed, gravitySettings.gravity *  deltaTime); 
                    break;
            } 
        }
        private void UpdateHorizontalSpeed(float deltaTIme)
        {
            Vector3 movementInput = this.movementInput; 
            switch (surfaceCollisions.surfaceState)
            {
                case SurfaceState.OnGround:
                    if(movementInput.sqrMagnitude > 1.0f)
                    {
                        movementInput.Normalize();
                    }

                    targetHorizontalSpeed = movementInput.magnitude * movementSettings.maxHorizontalSpeed;
                    float acceleration = hasMovementInput ? movementSettings.acceleration : movementSettings.decceleration;

                    horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, targetHorizontalSpeed, acceleration * deltaTIme);

                    break;
                case SurfaceState.OnSurface: 
                    break;
                case SurfaceState.InAir:
                    break;
            }
                  }

        private void OrientToTargetRotation(Vector3 horizontalMovement, float deltaTime) 
        {

            // might have to add the SurfaceState switch to change a variable for this function 

            if (rotationSettings.rotationBehavior == ERotationBehavior.OrientRotationToMovement && horizontalMovement.sqrMagnitude > 0.0f)
            {
                float rotationSpeed = Mathf.Lerp(
                    rotationSettings.maxRotationSpeed, rotationSettings.minRotationSpeed, horizontalSpeed / targetHorizontalSpeed);

                Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
            else if (rotationSettings.rotationBehavior == ERotationBehavior.UseControlRotation)
            {
                Quaternion targetRotation = Quaternion.Euler(0.0f, controlRotation.y, 0.0f);
                transform.rotation = targetRotation;
            }
        }




    }
}
