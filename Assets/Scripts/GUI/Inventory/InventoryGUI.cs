using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InventoryGUI : GUIControl
{
    public override string key { get { return "InventoryGUI"; } }
    public GameObject grid;
    public GameObject gridElementPrefab;
    protected override void Start()
    {
        InventorySystem.Instance.onInventoryChangedEvent += OnUpdateInventory;
        base.Start();
    }

    public void OnDestroy()
    {
        InventorySystem.Instance.onInventoryChangedEvent -= OnUpdateInventory;
    }

    public void OnUpdateInventory()
    {
        foreach (Transform t in grid.transform)
        {
            Destroy(t.gameObject);
        }
        DrawInventory();
    }
    
    public void DrawInventory()
    {
        int itemIndex = 0;
        foreach (InventoryItem item in InventorySystem.Instance.inventory)
        {
            AddGridElement(item, itemIndex);
            itemIndex++;
        }
    }

    public void AddGridElement(InventoryItem item, int itemIndex)
    {
        GameObject gridElementObj = Instantiate(gridElementPrefab, grid.transform);
        GridElement gridElement = gridElementObj.GetComponent<GridElement>();
        gridElement.Set(item);
        if (InventorySystem.Instance.selectedItem != null &&
            item.data.id != InventorySystem.Instance.selectedItem.data.id)
        {
            EventSystem.current.firstSelectedGameObject = gridElementObj;
            EventSystem.current.SetSelectedGameObject(gridElementObj);
        }
        else if (itemIndex == 0)
        {
            EventSystem.current.firstSelectedGameObject = gridElementObj;
            EventSystem.current.SetSelectedGameObject(gridElementObj);
        }
    }
}
