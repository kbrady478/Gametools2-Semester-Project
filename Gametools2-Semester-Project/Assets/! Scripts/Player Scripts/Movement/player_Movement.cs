using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class player_Movement : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Transform orientation;
    private Rigidbody rb;
    private Vector3 move_Direction;
    private float horizontal_Input;
    private float vertical_Input;
    
    [Header("Movement")]
    private float move_Speed;
    [SerializeField] private float walk_Speed;
    [SerializeField] private float sprint_Speed;
    [SerializeField] private float wall_Run_Speed;
    [HideInInspector] public bool is_Wall_Running;
    
    [FormerlySerializedAs("terrain_Layer")]
    [Header("Grounded")] 
    [SerializeField] private LayerMask ground_Layer;
    [SerializeField] private float player_Height;
    [SerializeField] private float player_Drag;
    [SerializeField] private bool is_Grounded;
    
    [Header("Jumping")]
    [SerializeField] private float jump_Force;
    [SerializeField] private float jump_Cooldown;
    [SerializeField] private float air_Multiplier;
    [SerializeField] private bool can_Jump = true;

    [Header("Crouching")] 
    [SerializeField] private float crouch_Speed;
    [SerializeField] private float crouch_Y_Scale;
    private float start_crouch_Y_Scale;

    /*
    [Header("Slope Handling")] 
    [SerializeField] private float max_Slope_Angle;
    private RaycastHit slope_Hit;
    */
    
    [Header("Current State")]
    public Movement_State current_State;
    
    public enum Movement_State
    {
        walking,
        sprinting,
        wall_Running,
        crouching,
        in_Air
    }// end enum Movement_State
    
    #region --- Base Functions ---
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        start_crouch_Y_Scale = transform.localScale.y;
    }// end Start()

    private void Update()
    {
        // Ground check
        is_Grounded = Physics.Raycast(transform.position, Vector3.down, (player_Height * 0.5f) + 0.2f, ground_Layer);
        
        Player_Input();
        State_Handler();
        Speed_Limiter();
        
        // Change drag
        if (is_Grounded)
            rb.linearDamping = player_Drag;
        else
            rb.linearDamping = 0;
    }// end Update()

    private void FixedUpdate()
    {
        Move_Player();
    }// end FixedUpdate()

    #endregion

    #region --- General Functions ---
    
    private void Player_Input()
    {
        horizontal_Input = Input.GetAxis("Horizontal");
        vertical_Input = Input.GetAxis("Vertical");

        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && can_Jump && is_Grounded)
        {
            can_Jump = false;
            Jump();
            Invoke("Reset_Jump", jump_Cooldown);
        }
        
        // Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouch_Y_Scale, transform.localScale.z);
            // Correct height to avoid floating
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        
        if (Input.GetKeyUp(KeyCode.LeftControl))
            transform.localScale = new Vector3(transform.localScale.x, start_crouch_Y_Scale, transform.localScale.z);
        
    }// end Player_Input()

    private void State_Handler()
    {
        // State = Wall running
        if (is_Wall_Running)
        {
            current_State = Movement_State.wall_Running;
            move_Speed = walk_Speed;
        }
            
        // State = Crouching
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            current_State = Movement_State.crouching;
            move_Speed = crouch_Speed;
        }
        
        // State = Sprinting
        else if (is_Grounded && Input.GetKey(KeyCode.LeftShift))
        {
            current_State = Movement_State.sprinting;
            move_Speed = sprint_Speed;
        }
        
        // State = Walking
        else if (is_Grounded)
        {
            current_State = Movement_State.walking;
            move_Speed = walk_Speed;
        }

        else
            current_State = Movement_State.in_Air;
        
    }// end State_Handler()
    
    private void Move_Player()
    {
        // Calculate movement direction
        move_Direction = orientation.forward * vertical_Input + orientation.right * horizontal_Input;
       
        /*
        if (On_Slope())
            rb.AddForce(Get_Slope_Move_Direction() * move_Speed * 20f, ForceMode.Force);
        */
            
        if (is_Grounded)
            rb.AddForce(move_Direction.normalized * move_Speed * 10f, ForceMode.Force);
        else if (!is_Grounded)
            rb.AddForce(move_Direction.normalized * move_Speed * air_Multiplier * 10f , ForceMode.Force);
    }// end Move_Player()

    private void Speed_Limiter()
    {
        Vector3 flat_Velocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        // Limit velocity when needed
        if (flat_Velocity.magnitude > move_Speed)
        {
            Vector3 limited_Velocity = flat_Velocity.normalized * move_Speed;
            rb.linearVelocity = new Vector3(limited_Velocity.x, rb.linearVelocity.y, limited_Velocity.z);
        }
        
    }// end Speed_Limiter()

    #endregion
    
    #region --- Jumping ---
    
    private void Jump()
    {
        // Reset Y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        rb.AddForce(transform.up * jump_Force, ForceMode.Impulse);
    }

    private void Reset_Jump()
    {
        can_Jump = true;
    }
    
    #endregion
    
/*    
    
    #region --- Slope Handling ---
    
    private bool On_Slope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slope_Hit, (player_Height * 0.5f) + 0.3f, ground_Layer));
        {
            float angle = Vector3.Angle(Vector3.up, slope_Hit.normal);
            return angle > max_Slope_Angle && angle != 0;
        }
        
        return false;
    }// end On_Slop()

    private Vector3 Get_Slope_Move_Direction()
    {
        return Vector3.ProjectOnPlane(move_Direction, slope_Hit.normal).normalized;
    }// end Get_Slope_Move_Direction()
    
    #endregion
    
*/
    
}// end player_Movement
