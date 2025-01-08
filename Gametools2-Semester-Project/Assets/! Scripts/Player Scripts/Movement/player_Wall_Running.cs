// Tutorial used: https://www.youtube.com/watch?v=gNt9wBOrQO4

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

public class player_Wall_Running : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private LayerMask wall_Layer;
    [SerializeField] private LayerMask ground_Layer;
    [SerializeField] private Transform orientation;
    [SerializeField] private Camera camera_Component;
    private player_Movement player_Movement_Script;
    private Rigidbody rb;  
    private float horizontal_Input;
    private float vertical_Input;    
    
    [Header("Keybinds")]
    [SerializeField] private KeyCode jump_Key = KeyCode.Space;
    [SerializeField] private KeyCode upwards_Run_Key = KeyCode.LeftShift;
    [SerializeField] private KeyCode downwards_Run_Key = KeyCode.LeftControl;    
    
    [Header("Wallrunning")]
    [SerializeField] private float wall_Run_Force;
    [SerializeField] private float wall_Jump_Upward_Force;
    [SerializeField] private float wall_Jump_Sideway_Force;
    [SerializeField] private float wall_Climb_Speed;
    private bool upwards_Running;
    private bool downwards_Running;
    
    [SerializeField] private float max_Wall_Run_Time;
    private float wall_Run_Timer;
    
    [SerializeField] private float exit_Wall_Time;
    private bool exiting_Wall;
    private float exit_Wall_Timer;
    
    [Header("Camera Tilt")]
    [SerializeField] private first_Person_Cam camera_Script;
    [SerializeField] private float target_Tilt_Angle;
    
    [Header("Detection")]
    [SerializeField] private float wall_Check_Distance;
    [SerializeField] private float min_Jump_Height;
    private RaycastHit left_Wall_Hit;
    private RaycastHit right_Wall_Hit;
    private bool wall_On_Left;
    private bool wall_On_Right;
    
    [Header("Gravity")]
    [SerializeField] private bool use_Gravity;
    [SerializeField] private float gravity_Counter_Force;

    
    #region --- Unity Updates---
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        player_Movement_Script = GetComponent<player_Movement>();

    }// end Start()

    private void Update()
    {
        Check_For_Wall();
        State_Machine();

        if (player_Movement_Script.is_Wall_Running == false)
            camera_Script.z_Rotation = 0;
    }// end Update()

    private void FixedUpdate()
    {
        if (player_Movement_Script.is_Wall_Running)
            Wall_Running_Movement();
        
    }// end FixedUpdate()

    #endregion
    

    private void State_Machine()
    {
        // Getting Inputs
        horizontal_Input = Input.GetAxisRaw("Horizontal");
        vertical_Input = Input.GetAxisRaw("Vertical");

        upwards_Running = Input.GetKey(upwards_Run_Key);
        downwards_Running = Input.GetKey(downwards_Run_Key);

        // State - Wallrunning
        if((wall_On_Left || wall_On_Right) && vertical_Input > 0 && Above_Ground() && !exiting_Wall)
        {
            if (!player_Movement_Script.is_Wall_Running)
                Start_Wall_Run();

            // Wallrun timer
            if (wall_Run_Timer > 0)
                wall_Run_Timer -= Time.deltaTime;

            if(wall_Run_Timer <= 0 && player_Movement_Script.is_Wall_Running)
            {
                exiting_Wall = true;
                exit_Wall_Timer = exit_Wall_Time;
            }

            // Wall jump
            if (Input.GetKeyDown(jump_Key)) Wall_Jump();
        }

        // State - Exiting
        else if (exiting_Wall)
        {
            if (player_Movement_Script.is_Wall_Running)
                Stop_Wall_Run();

            if (exit_Wall_Timer > 0)
                exit_Wall_Timer -= Time.deltaTime;

            if (exit_Wall_Timer <= 0)
                exiting_Wall = false;
        }

        // State - None
        else
        {
            if (player_Movement_Script.is_Wall_Running)
                Stop_Wall_Run();
        }
        
    }// end State_Machine()

    private void Start_Wall_Run()
    {
        player_Movement_Script.is_Wall_Running = true;
        wall_Run_Timer = max_Wall_Run_Time;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        player_Movement_Script.has_Air_Jumped = false;
        player_Movement_Script.can_Air_Jump = true;

        camera_Component.fieldOfView = 65;
        
    }// end Start_Wall_Run()

    private void Wall_Running_Movement()
    {
        Handle_Camera_Tilt();
        
        rb.useGravity = use_Gravity;
        Vector3 wall_Normal = wall_On_Right ? right_Wall_Hit.normal : left_Wall_Hit.normal;
        Vector3 wall_Forward = Vector3.Cross(wall_Normal, transform.up);

        if ((orientation.forward - wall_Forward).magnitude > (orientation.forward - -wall_Forward).magnitude)
            wall_Forward = -wall_Forward;

        // Forward force
        rb.AddForce(wall_Forward * wall_Run_Force, ForceMode.Force);

        // Upwards/downwards force
        if (upwards_Running)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wall_Climb_Speed, rb.linearVelocity.z);
        if (downwards_Running)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wall_Climb_Speed, rb.linearVelocity.z);

        // Push to wall force
        if (!(wall_On_Left && horizontal_Input > 0) && !(wall_On_Right && horizontal_Input < 0))
            rb.AddForce(-wall_Normal * 100, ForceMode.Force);

        // Weaken gravity
        if (use_Gravity)
            rb.AddForce(transform.up * gravity_Counter_Force, ForceMode.Force);
    }// end Wall_Running_Movement()

    private void Stop_Wall_Run()
    {
        player_Movement_Script.is_Wall_Running = false;

        camera_Component.fieldOfView = 60;
        /*
        tilt_Complete = false;
        
        StopAllCoroutines();
        if (last_Camera_Tilt == 1) // Is tilted right
            StartCoroutine(nameof(Tilt_Camera_Left));
        if (last_Camera_Tilt == 2)
            StartCoroutine(nameof(Tilt_Camera_Right)); // Is tilted left

        camera_Tilt_Left_Started = false;
        camera_Tilt_Right_Started = false;
        tilt_Complete = false;
        last_Camera_Tilt = 0;
        */
        camera_Script.z_Rotation = 0;
    }// end Stop_Wall_Run()

    private void Wall_Jump()
    {
        
        // Enter exiting wall state
        exiting_Wall = true;
        exit_Wall_Timer = exit_Wall_Time;

        Vector3 wall_Normal = wall_On_Right ? right_Wall_Hit.normal : left_Wall_Hit.normal;

        Vector3 force_To_Apply = transform.up * wall_Jump_Upward_Force + wall_Normal * wall_Jump_Sideway_Force;

        // Reset y velocity and add force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(force_To_Apply, ForceMode.Impulse);
        
    }// end Wall_Jump()
    
    private void Handle_Camera_Tilt()
    {
        // Tilt camera opposite to the wall
        // Negative angle is towards the right and vice versa
        
        if (wall_On_Left == true)
        {
            camera_Script.z_Rotation = -target_Tilt_Angle;
            //StartCoroutine(nameof(Tilt_Camera_Right));
        }
        
        if (wall_On_Right == true)
        {
            camera_Script.z_Rotation = target_Tilt_Angle;
            //StartCoroutine(nameof(Tilt_Camera_Left));
        }
    }// end Handle_Camera_Tilt()
/*
    private IEnumerator Tilt_Camera_Right()
    {
        print("beginning tilt");
        camera_Tilt_Right_Started = true;
        camera_Tilt_Left_Started = false;
        last_Camera_Tilt = 1;
        float starting_Angle = camera_Script.z_Rotation;
        float target_Angle;

        // If the camera is tilted towards the right already, center it
        if (starting_Angle < 0)
            target_Angle = 0;
        // Else go towards the right
        else
            target_Angle = -target_Tilt_Angle;

        float time_Elapsed = 0f;

        while (time_Elapsed < camera_Tilt_Duration)
        {
            print("tilting");
            camera_Script.z_Rotation = Mathf.Lerp(starting_Angle, target_Angle, time_Elapsed / camera_Tilt_Duration);
            time_Elapsed += Time.deltaTime;
            yield return null;
        }
        
        print("tilted");
        
        camera_Script.z_Rotation = target_Angle;

        tilt_Complete = true;
        camera_Tilt_Right_Started = false;

    }// end Tilt_Camera_Negative()
    
    private IEnumerator Tilt_Camera_Left()
    {
        print("beginning tilt");
        camera_Tilt_Left_Started = true;
        camera_Tilt_Right_Started = false;
        last_Camera_Tilt = 2;
        float starting_Angle = camera_Script.z_Rotation;
        float target_Angle;

        // If the camera is tilted towards the left already, center it
        if (starting_Angle > 0)
            target_Angle = 0;
        // Else go towards the right
        else
            target_Angle = target_Tilt_Angle;

        float time_Elapsed = 0f;

        while (time_Elapsed < camera_Tilt_Duration)
        {
            print("tilting");
            camera_Script.z_Rotation = Mathf.Lerp(starting_Angle, target_Angle, time_Elapsed / camera_Tilt_Duration);
            time_Elapsed += Time.deltaTime;
            yield return null;
        }
        print("tilted");
        
        camera_Script.z_Rotation = target_Angle;

        tilt_Complete = true;
        camera_Tilt_Left_Started = false;

    }// end Tilt_Camera_Negative()
*/
    
    #region --- Detections ---
    
    private bool Above_Ground()
    {
        return !Physics.Raycast(transform.position, Vector3.down, min_Jump_Height, ground_Layer);
    }// end Above_Ground()
    
    private void Check_For_Wall()
    {
        wall_On_Right = Physics.Raycast(transform.position, orientation.right, out right_Wall_Hit, wall_Check_Distance, wall_Layer);
        wall_On_Left = Physics.Raycast(transform.position, -orientation.right, out left_Wall_Hit, wall_Check_Distance, wall_Layer);
    }// end Check_For_Wall()
    
    #endregion
    
    
}// end player_Wall_Running
