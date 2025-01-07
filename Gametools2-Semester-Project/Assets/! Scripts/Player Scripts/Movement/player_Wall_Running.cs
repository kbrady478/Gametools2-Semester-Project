// Tutorial used: https://www.youtube.com/watch?v=gNt9wBOrQO4

using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class player_Wall_Running : MonoBehaviour
{
    [Header("General")] 
    [SerializeField] private Transform orientation;
    [SerializeField] private LayerMask wall_Layer;
    [SerializeField] private LayerMask ground_Layer;
    private player_Movement movement_Script;
    private Rigidbody rb;
    
    [Header("Wall Running")] 
    [SerializeField] private float wall_Run_Force;
    [SerializeField] private float max_Wall_Run_Time;
    private float wall_Run_Timer;
    
    [Header("Input")]
    private float horizontal_Input;
    private float vertical_Input;

    [Header("Detection")]
    [SerializeField] private float wall_Check_Distance;
    [SerializeField] private float min_Jump_Height;
    private RaycastHit left_Wall_Hit;
    private RaycastHit right_Wall_Hit;
    private bool wall_On_Left;
    private bool wall_On_Right;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        movement_Script = GetComponent<player_Movement>();
    }// end Start()

    private void Update()
    {
        Check_For_Wall();
        State_Machine();
    }// end Update()

    private void FixedUpdate()
    {
        if (movement_Script.is_Wall_Running)
            Wall_Run_Movement();
    }// end FixedUpdate()


    private void Check_For_Wall()
    {
        wall_On_Left = Physics.Raycast(transform.position, -orientation.right, out left_Wall_Hit, wall_Check_Distance, wall_Layer);
        wall_On_Right = Physics.Raycast(transform.position, orientation.right, out right_Wall_Hit, wall_Check_Distance, wall_Layer);
    }// end Check_For_Wall()

    private bool Is_Grounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, min_Jump_Height, ground_Layer);
    }// end Is_Grounded()

    private void State_Machine()
    {
        // Get inputs
        horizontal_Input = Input.GetAxis("Horizontal");
        vertical_Input = Input.GetAxis("Vertical");
        
        //  State 1 - Is Wall Running
        if ((wall_On_Left || wall_On_Right) && vertical_Input > 0 && !Is_Grounded())
        {
            if (!movement_Script.is_Wall_Running)
                Start_Wall_Run();
        }

        // State 3 - Not on Wall
        else
        {
            if (movement_Script.is_Wall_Running)
                Stop_Wall_Run();
        }

    }// end State_Machine()


    private void Start_Wall_Run()
    {
        movement_Script.is_Wall_Running = true;
    }// end Start_Wall_Run()

    private void Wall_Run_Movement()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        Vector3 wall_Normal = wall_On_Right ? right_Wall_Hit.normal : left_Wall_Hit.normal;

        Vector3 wall_Forward = Vector3.Cross(wall_Normal, transform.up);

        rb.AddForce(wall_Forward * wall_Run_Force, ForceMode.Force);
    }// end Wall_Run_Movement()

    private void Stop_Wall_Run()
    {
        rb.useGravity = true;
        movement_Script.is_Wall_Running = false;
    }// end Stop_Wall_Run()
    
    
    
}// end player_Wall_Running
