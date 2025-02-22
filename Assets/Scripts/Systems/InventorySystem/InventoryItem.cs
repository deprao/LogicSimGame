using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encapsula um item de inventario adicionando a quantidade disponível no inventário
/// </summary>
public class InventoryItem
{
    public InventoryItemData data { get; private set; }
    public int stackSize { get; private set; }

    public InventoryItem(InventoryItemData source)
    {
        data = source;
        AddToStack();
    }

    public void AddToStack()
    {
        stackSize++;
    }

    public void RemoveFromStack()
    {
        stackSize--;
    }
}
