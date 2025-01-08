using UnityEngine;

public class Mobo : MonoBehaviour, IInteractable
{
    [SerializeField] private Victory_Manager victory_Manager;

    public void Interact()
    {
        print("interacted");
        victory_Manager.Next_Scene();
        //victory_Manager.Victory_Condition_Met();
    }
    
}
