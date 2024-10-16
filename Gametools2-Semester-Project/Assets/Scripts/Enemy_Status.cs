using System;
using UnityEngine;

public class Enemy_Status : MonoBehaviour, ITP_Pistol_Damage, IPlasma_Projectile
{
    private Damage_Controller damage_Control_Script;

    [Header("General")] 
    [SerializeField] private int total_HP;
    public int current_HP;
    
    private void Start()
    {
        GameObject controller = GameObject.FindGameObjectWithTag("Damage Controller");
        damage_Control_Script = controller.GetComponent<Damage_Controller>();
        current_HP = total_HP;
    }

    private void Update()
    {
        if (current_HP <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    private void Take_Damage(int incoming_Damage)
    {
        current_HP -= incoming_Damage;
        Debug.Log($"Damage recieved, current health: {current_HP}");
    }
    
    #region - Damage Types -

    public void Receive_TP_Pistol_Damage()
    {
        Take_Damage(damage_Control_Script.tp_Pistol_Damage);
    }
    
    public void Recieve_Plasma_Damage()
    {
        Take_Damage(damage_Control_Script.plasma_Gun_Damage);
    }
    
    #endregion
    
}