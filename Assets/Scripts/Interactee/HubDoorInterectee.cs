using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubDoorInterectee : MonoBehaviour, IInteractee
{
    [SerializeField] private Requirement requirement;
    [SerializeField] private static Animator anim;
    private static string _estado;

    public static void ChangeDoorState(string novoest) //static permite que a porta seja manipulada externamente
                                                       //por eventos estáticos (e. g. OnTriggerExit)
    {
        if (_estado == novoest) return; //impede uma animação de interromper a si mesma
        
        anim.Play(novoest, -1, 0);

        _estado = novoest;
    }
    
    public void HandleItemInteraction(ObjectType objectType)
    {
        anim = GetComponent<Animator>();
        if (requirement.CheckRequirements())
        {
            if (objectType == ObjectType.Consumable)
            {
                requirement.RemoveRequirementsFromInventory();
            }
            InteractionSuccess();
        }
        else
        {
            InteractionFailed();
        }
    }

    public void InteractionSuccess()
    {
        SoundManager.Instance.PlaySound(requirement.successClip);
        Debug.Log("You met the requirements: " + requirement);
        ChangeDoorState("door-open");
    }

    public void InteractionFailed()
    {
        SoundManager.Instance.PlaySound(requirement.failureClip);
        Debug.Log("You don't meet the requirements: " + requirement);
    }
}
