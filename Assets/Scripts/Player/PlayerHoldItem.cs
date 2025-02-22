using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Script responsável por instanciar objeto na mão do jogador quando ele é selecionado no inventário.
/// Também é responsável por tirar o item da mão do jogador caso ele seja consumido e removido do inventário.
/// </summary>
public class PlayerHoldItem : MonoBehaviour
{
    private bool isHoldingItem = false;
    private InventoryItem holdedItem = null;
    public Transform spawnPositon;
    private GameObject spawnedItem;
    
    void Start()
    {
        InventorySystem.Instance.onInventorySelectItem += EquipItem;
        InventorySystem.Instance.onInventoryRemoveSelectedItem += ConsumeItem;
    }

    public void EquipItem()
    {
        Destroy(spawnedItem);
        holdedItem = InventorySystem.Instance.selectedItem;
        isHoldingItem = true;
        // Fazer item aparecer na mão do player e nao castar shadows
        spawnedItem = Instantiate(InventorySystem.Instance.selectedItem.data.prefab, spawnPositon);
        MeshRenderer spawnedItemMeshRenderer = spawnedItem.GetComponent<MeshRenderer>();
        if (spawnedItemMeshRenderer != null)
        {
            spawnedItemMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        int LayerIgnoreRaycast = LayerMask.NameToLayer("Top Layer");
        spawnedItem.layer = LayerIgnoreRaycast;
    }

    public void ConsumeItem()
    {
        if (InventorySystem.Instance.selectedItem != null) return;
        Destroy(spawnedItem);
        isHoldingItem = false;
    }

    public void HandlePlayerMovementUseItem(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!isHoldingItem) return;
        
        switch (InventorySystem.Instance.selectedItem.data.type)
        {
            case ObjectType.Consumable:
                Ray consumableRay = new Ray(transform.position, transform.forward);
                RaycastHit consumableHit;
                if (Physics.Raycast(consumableRay, out consumableHit, 3f))
                {
                    if (!consumableHit.transform) return;
                    if (!consumableHit.collider.CompareTag("Interactee")) return;
                    IConsumable consumableItem = spawnedItem.GetComponent<IConsumable>();
                    consumableItem?.HandleUseItem();
                    IInteractee interactee = consumableHit.collider.gameObject.GetComponent<IInteractee>();
                    interactee?.HandleItemInteraction(InventorySystem.Instance.selectedItem.data.type);
                }
                break;
            
            case ObjectType.Durable:
                Ray durableRay = new Ray(transform.position, transform.forward);
                RaycastHit durableHit;
                if (Physics.Raycast(durableRay, out durableHit, 3f))
                {
                    if (!durableHit.transform) return;
                    if (!durableHit.collider.CompareTag("Interactee")) return;
                    IDurable durableItem = spawnedItem.GetComponent<IDurable>();
                    durableItem?.HandleUseItem();
                    IInteractee interactee = durableHit.collider.gameObject.GetComponent<IInteractee>();
                    interactee?.HandleItemInteraction(InventorySystem.Instance.selectedItem.data.type);
                }
                break;
            
            case ObjectType.IndependentDurable:
                IIndependentDurable independentDurableItem = spawnedItem.GetComponent<IIndependentDurable>();
                independentDurableItem?.HandleUseItem();
                break;
            
            
        }
    }
}