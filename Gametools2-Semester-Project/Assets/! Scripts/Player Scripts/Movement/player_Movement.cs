using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class player_Movement : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Transform orientation;
    private Rigidbody rb;    
    private float horizontal_Input;
    private float vertical_Input;
    
    [Header("Keybinds")]
    [SerializeField] private KeyCode jump_Key = KeyCode.Space;
    [SerializeField] private KeyCode sprint_Key = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouch_Key = KeyCode.LeftControl;    
    
    [Header("Movement")]
    private Vector3 moveDirection;
    private float move_Speed;
    private float target_Move_Speed;
    private float last_Target_Move_Speed;
    [SerializeField] private float walk_Speed;
    [SerializeField] private float sprint_Speed;
    [SerializeField] private float slide_Speed;
    [SerializeField] private float wall_Run_Speed;
    [SerializeField] private float speed_Increase_Multiplier;
    [SerializeField] private float ground_Drag;

    [Header("Jumping")]
    [SerializeField] private float jump_Force;
    [SerializeField] private float jump_Cooldown;
    [SerializeField] private float air_Multiplier;

    [Header(("Double Jump"))] 
    //[SerializeField] private float double_Jump_Cooldown; // Timer between jump and double jump
    
    [Header("Crouching")]
    [SerializeField] private float crouch_Move_Speed;
    [SerializeField] private float crouch_Y_Scale;
    private float start_Y_Scale;

    [Header("Ground Check")]
    [SerializeField] private float player_Height;
    [SerializeField] private LayerMask ground_Layer;

    [Header("Slope Handling")]
    [SerializeField] private float max_Slope_Angle;
    [SerializeField] private float slope_Increase_Multiplier;
    private RaycastHit slope_Hit;
    private bool exiting_Slope;
    
    
    public enum Movement_State
    {
        walking,
        sprinting,
        wallrunning,
        crouching,
        sliding,
        in_Air
    }// end Movement_State

    [Header("State Bools - Do not change")]
    public Movement_State current_State;
    [SerializeField] private bool can_Jump;
    public bool can_Air_Jump = false;
    public bool has_Air_Jumped = false;
    private bool start_Double_Jump_Cooldown;
    public bool is_Grounded;
    public bool is_Crouching;
    public bool is_Sliding;
    public bool is_Wall_Running;
    
    
    #region --- Unity Updates ---
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        can_Jump = true;
        start_Y_Scale = transform.localScale.y;
    }// end Start()

    private void Update()
    {
        // Ground check
        is_Grounded = Physics.Raycast(transform.position, Vector3.down, player_Height * 0.5f + 0.2f, ground_Layer);

        Player_Input();
        Speed_Control();
        State_Handler();

        // Handle drag
        if (is_Grounded)
        {
            can_Air_Jump = false;
            rb.linearDamping = ground_Drag;
        }
        
        else
        {
            if (has_Air_Jumped == false)
                can_Air_Jump = true;
                
            rb.linearDamping = 0;
        }
        
    }// end Update()

    private void FixedUpdate()
    {
        Move_Player();
    }// end FixedUpdate()

    #endregion
    
    #region --- General Functionality ---
    
    private void Player_Input()
    {
        horizontal_Input = Input.GetAxisRaw("Horizontal");
        vertical_Input = Input.GetAxisRaw("Vertical");

        // Regular Jump
        if (Input.GetKeyDown(jump_Key) && can_Jump && is_Grounded)
        {
            can_Jump = false;
            Jump();
            Invoke(nameof(Reset_Jump), jump_Cooldown);
        }
        // Air Jump
        else if (Input.GetKeyDown(jump_Key) && can_Air_Jump == true && has_Air_Jumped == false && is_Grounded == false && is_Wall_Running == false)
        {
            can_Air_Jump = false;
            has_Air_Jumped = true;
            Jump();
        }

        // Start crouch
        if (Input.GetKeyDown(crouch_Key) && horizontal_Input == 0 && vertical_Input == 0)
        {
            transform.localScale = new Vector3(transform.localScale.x, crouch_Y_Scale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

            is_Crouching = true;
        }

        // Stop crouch
        if (Input.GetKeyUp(crouch_Key))
        {
            transform.localScale = new Vector3(transform.localScale.x, start_Y_Scale, transform.localScale.z);

            is_Crouching = false;
        }
    } // end Player_Input()

    private void State_Handler()
    {
        // State - Wallrunning
        if (is_Wall_Running)
        {
            current_State = Movement_State.wallrunning;
            target_Move_Speed = wall_Run_Speed;
        }

        // State - Sliding
        else if (is_Sliding)
        {
            current_State = Movement_State.sliding;

            // increase speed by one every second
            if (On_Slope() && rb.linearVelocity.y < 0.1f)
                target_Move_Speed = slide_Speed;

            else
                target_Move_Speed = sprint_Speed;
        }

        // State - Crouching
        else if (is_Crouching)
        {
            current_State = Movement_State.crouching;
            target_Move_Speed = crouch_Move_Speed;
        }

        // State - Sprinting
        else if (is_Grounded && Input.GetKey(sprint_Key))
        {
            current_State = Movement_State.sprinting;
            target_Move_Speed = sprint_Speed;
        }

        // State - Walking
        else if (is_Grounded)
        {
            current_State = Movement_State.walking;
            target_Move_Speed = walk_Speed;
        }

        // State - Air
        else
        {
            current_State = Movement_State.in_Air;
        }

        // Check if desired move speed has changed drastically
        if (Mathf.Abs(target_Move_Speed - last_Target_Move_Speed) > 4f && move_Speed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(Lerp_Move_Speed());
        }
        else
        {
            move_Speed = target_Move_Speed;
        }

        last_Target_Move_Speed = target_Move_Speed;
    }// end State_Handler()

    private IEnumerator Lerp_Move_Speed()
    {
        // Smoothly transition movementSpeed to target value
        float time = 0;
        float difference = Mathf.Abs(target_Move_Speed - move_Speed);
        float start_Value = move_Speed;

        while (time < difference)
        {
            move_Speed = Mathf.Lerp(start_Value, target_Move_Speed, time / difference);

            if (On_Slope())
            {
                float slope_Angle = Vector3.Angle(Vector3.up, slope_Hit.normal);
                float slope_Angle_Increase = 1 + (slope_Angle / 90f);

                time += Time.deltaTime * speed_Increase_Multiplier * slope_Increase_Multiplier * slope_Angle_Increase;
            }
            else
                time += Time.deltaTime * speed_Increase_Multiplier;

            yield return null;
        }

        move_Speed = target_Move_Speed;
    }// end Lerp_Move_Speed)(

    private void Move_Player()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * vertical_Input + orientation.right * horizontal_Input;

        // Movement on slope
        if (On_Slope() && !exiting_Slope)
        {
            rb.AddForce(Get_Slope_Move_Direction(moveDirection) * move_Speed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // Movement on ground
        else if (is_Grounded)
            rb.AddForce(moveDirection.normalized * move_Speed * 10f, ForceMode.Force);

        // Movement in air
        else if (!is_Grounded)
            rb.AddForce(moveDirection.normalized * move_Speed * 10f * air_Multiplier, ForceMode.Force);

        // Turn gravity off while on slope
        if(!is_Wall_Running) rb.useGravity = !On_Slope();
    }// end Move_Player()

    private void Speed_Control()
    {
        // Limits speed on slope
        if (On_Slope() && !exiting_Slope)
        {
            if (rb.linearVelocity.magnitude > move_Speed)
                rb.linearVelocity = rb.linearVelocity.normalized * move_Speed;
        }

        // Limits speed on ground or in air
        else
        {
            Vector3 flat_Velocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // Limit velocity if needed
            if (flat_Velocity.magnitude > move_Speed)
            {
                Vector3 limited_Velocity = flat_Velocity.normalized * move_Speed;
                rb.linearVelocity = new Vector3(limited_Velocity.x, rb.linearVelocity.y, limited_Velocity.z);
            }
        }
        
    }// end Speed_Control()

    #endregion
    
    #region --- Jumping ---
    
    private void Jump()
    {
        exiting_Slope = true;

        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jump_Force, ForceMode.Impulse);
    }// end Jump()
    
    private void Reset_Jump()
    {
        can_Jump = true;
        exiting_Slope = false;
    }// end Reset_Jump()
    
    
    #endregion
    
    #region --- Slope Handling ---
    
    public bool On_Slope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slope_Hit, player_Height * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slope_Hit.normal);
            return angle < max_Slope_Angle && angle != 0;
        }

        return false;
    }// end On_Slope()

    public Vector3 Get_Slope_Move_Direction(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slope_Hit.normal).normalized;
    }// end Get_Slope_Move_Direction

    #endregion
    

}// end player_Movement
