using System;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Cube : MonoBehaviour
{
    public enum TestMode { Button, Acceleration, WMP, Nunchuck, Shake, Twist, Pointer }

    public TestMode testMode = (TestMode)Enum.GetValues(typeof(TestMode)).GetValue(0);

    private Rigidbody rb;

    [Header("CubeRespawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelayInSeconds = 1.0f;

    [Header("Movement")]
    [SerializeField] private float speed = 5.0f;
    private Vector3 movementVector = Vector3.zero;

    [Header("Jumping")]
    [SerializeField] private float jumpStrength = 5.0f;

    [Header("Nunchuck")]
    [Tooltip("Move speed while using the nunchuck.")]
    [SerializeField] private float nunchuckSpeed = 3.0f;

    [Header("Shake/Twist")]
    [SerializeField] private float shakeJumpStrength = 7.0f;
    [SerializeField] private float shakeSpin = 20.0f;
    [SerializeField] private float shakeCooldown = 1.0f;
    [SerializeField] private float shakeTimer; // Timer for calculating shake cooldown. 0 is resting, and counts down from shakeCooldown.

    [Header("Pointer")]
    [Tooltip("Speed multiplier for when object is being dragged by the pointer.")]
    [SerializeField] private float dragSpeedMultiplier = 3.0f;
    private bool isHeld = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        ReadKeyboardInputs();

        if (InputManager.wiimote == null)
            return;
        
        // Set variables
        if (testMode != TestMode.Pointer)
            isHeld = false;

        // Switch testing modes
        switch (testMode)
        {
            case TestMode.Button:
                if (InputManager.inputs.GetWiimoteButtonDown(Button.B))
                    Jump();
                break;

            // ------------------------------------------------

            case TestMode.Acceleration:
                Vector3 accelVector = InputManager.inputs.GetAccelVector();

                // Vertical position of controler.
                movementVector = accelVector;

                // Horizontal position of controler.
                //movementVector.x = -accelVector[2];
                //movementVector.z = accelVector[0];

                movementVector.y = 0; // Ignore vertical movement.

                rb.velocity = speed * movementVector;

                break;

            // ------------------------------------------------

            case TestMode.WMP:
                Vector3 newVelocity = InputManager.inputs.WMPVectorStandardized();
                /*if(newVelocity.magnitude > 0)
                    Debug.Log(newVelocity);*/

                if (newVelocity.y == 0)
                    newVelocity.y = rb.velocity.y;

                if (newVelocity.magnitude > 1f)
                    rb.velocity = newVelocity;
                break;

            // ------------------------------------------------

            case TestMode.Nunchuck:
                try
                {
                    Vector3 axis = new Vector3(InputManager.inputs.GetNunchuckAxis("Horizontal"), 0f, InputManager.inputs.GetNunchuckAxis("Vertical"));
                    Debug.Log(axis);
                    rb.velocity = axis * nunchuckSpeed;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e + InputManager.wiimote.current_ext.ToString());
                }
                break;

            // ------------------------------------------------

            case TestMode.Shake:
                if (shakeTimer == 0)
                {
                    if (InputManager.inputs.Shake)
                    {
                        rb.velocity = new Vector3(0f, shakeJumpStrength, 0f);
                        rb.angularVelocity = new Vector3(0f, 1f, 1f).normalized * shakeSpin;
                        shakeTimer = shakeCooldown;
                    }
                }
                else
                {
                    shakeTimer = Mathf.Clamp(shakeTimer - Time.deltaTime, 0, shakeCooldown);
                }
                break;

            // ------------------------------------------------

            case TestMode.Twist:
                if (shakeTimer == 0)
                {
                    if (InputManager.inputs.Twisting)
                    {
                        rb.velocity = new Vector3(0f, shakeJumpStrength, 0f);
                        rb.angularVelocity = new Vector3(0f, 0f, 1f).normalized * shakeSpin;
                        shakeTimer = shakeCooldown;
                    }
                }
                else
                {
                    shakeTimer = Mathf.Clamp(shakeTimer - Time.deltaTime, 0, shakeCooldown);
                }
                break;

            // ------------------------------------------------

            case TestMode.Pointer:
                // Pick up
                if (!isHeld && (InputManager.wiimote.Button.b && InputManager.wiimote.Button.a) && InputManager.inputs.AimingAtObject(gameObject))
                {
                    isHeld = true;
                    InputManager.inputs.RumbleWiimoteForSeconds(0.1f);
                }
                else if (!(InputManager.wiimote.Button.b && InputManager.wiimote.Button.a) || InputManager.inputs.pointer.anchorMin[0] == -1f) // Button released or pointer offscreen
                    isHeld = false;
                if (!isHeld)
                    break;

                // Move
                float offsetFromCamera = 10;
                Vector3 direction = InputManager.inputs.PointerToWorldPos(offsetFromCamera) - transform.position;
                rb.velocity = dragSpeedMultiplier * direction;

                break;

            // ------------------------------------------------

            default:
                break;
        }
    }

    // These Keyboard Inputs control the Testing Scene (manual cube respawning, testing mode, etc.)
    private void ReadKeyboardInputs()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RespawnCube();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            //if (testMode == TestMode.Pointer)
            //    testMode = 0;
            //else
            //    testMode++; 
            if (testMode == (TestMode)Enum.GetValues(typeof(TestMode)).GetValue(Enum.GetValues(typeof(TestMode)).Length - 1))
                testMode = (TestMode)Enum.GetValues(typeof(TestMode)).GetValue(0);
            else
                testMode++;

            Debug.Log("<color=green>Cube test mode set to: </color>" + testMode.ToString());
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (testMode == (TestMode)Enum.GetValues(typeof(TestMode)).GetValue(0))
                testMode = (TestMode)Enum.GetValues(typeof(TestMode)).GetValue(Enum.GetValues(typeof(TestMode)).Length - 1);
            else
                testMode--;

            Debug.Log("<color=green>Cube test mode set to: </color>" + testMode.ToString());
        }
    }

    private void Jump()
    {
        rb.velocity = Vector3.up * jumpStrength;
    }

    private void OnTriggerExit(Collider other)
    {
        // Automatically respawning cube after it gets out of the world's bounds.
        if (other.CompareTag("WorldBounds"))
        {
            InputManager.inputs.RumbleWiimoteForSeconds(respawnDelayInSeconds);
            InputManager.inputs.PlayLoadingLEDEffect(respawnDelayInSeconds);
            Invoke(nameof(RespawnCube), respawnDelayInSeconds);
        }
    }

    private void RespawnCube()
    {
        ResetCubePosition();
        ResetCubeRotation();
        ResetCubeVelocities();
    }

    private void ResetCubePosition()
    {
        transform.position = respawnPoint.position;
    }

    private void ResetCubeRotation()
    {
        transform.rotation = Quaternion.identity;
    }

    // Velocity and angular velocity of a rigidbody are properties that determine its movement and rotation in the physics simulation.
    // If we don’t reset these values, the rigidbody will continue to move or rotate from its previous state, which can lead to unexpected behavior.
    private void ResetCubeVelocities()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
    }
}
