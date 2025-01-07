using UnityEngine;
using UnityEngine.Serialization;

public class player_Sliding : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform player_Object;
    private Rigidbody rb;
    private player_Movement movement_Script;

    [Header("Sliding")]
    [SerializeField] private float max_Slide_Time;
    private float slide_Timer;
    [SerializeField] private float slide_Force;
    [SerializeField] private float slide_Y_Scale;
    private float start_Y_Scale;

    [Header("Keybinds")]
    [SerializeField] private KeyCode slide_Key = KeyCode.LeftControl;
    private float horizontal_Input;
    private float vertical_Input;
    
    #region --- Unity Updates ---
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        movement_Script = GetComponent<player_Movement>();

        start_Y_Scale = player_Object.localScale.y;
    }

    private void Update()
    {
        horizontal_Input = Input.GetAxisRaw("Horizontal");
        vertical_Input = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slide_Key) && (horizontal_Input != 0 || vertical_Input != 0))
            Start_Slide();

        if (Input.GetKeyUp(slide_Key) && movement_Script.is_Sliding)
            Stop_Slide();
    }

    private void FixedUpdate()
    {
        if (movement_Script.is_Sliding)
            Sliding_Movement();
    }

    #endregion
    
    private void Start_Slide()
    {
        if (movement_Script.is_Wall_Running) return;

        movement_Script.is_Sliding = true;

        player_Object.localScale = new Vector3(player_Object.localScale.x, slide_Y_Scale, player_Object.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slide_Timer = max_Slide_Time;
    }

    private void Sliding_Movement()
    {
        Vector3 input_Direction = orientation.forward * vertical_Input + orientation.right * horizontal_Input;

        // is_Sliding normal
        if(!movement_Script.On_Slope() || rb.linearVelocity.y > -0.1f)
        {
            rb.AddForce(input_Direction.normalized * slide_Force, ForceMode.Force);

            slide_Timer -= Time.deltaTime;
        }

        // is_Sliding down a slope
        else
        {
            rb.AddForce(movement_Script.Get_Slope_Move_Direction(input_Direction) * slide_Force, ForceMode.Force);
        }

        if (slide_Timer <= 0)
            Stop_Slide();
    }

    private void Stop_Slide()
    {
        movement_Script.is_Sliding = false;

        player_Object.localScale = new Vector3(player_Object.localScale.x, start_Y_Scale, player_Object.localScale.z);
    }
    
}// end player_Sliding
