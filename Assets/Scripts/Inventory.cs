using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{

    public GameObject slotPrefab;
    World world;

    List<ItemSlot> slots = new List<ItemSlot>();

    private void Start()
    {

        world = GameObject.Find("World").GetComponent<World>();

        for(int i=1; i<world.blockTypes.Length; i++)
        {

            GameObject newSlot = Instantiate(slotPrefab, transform );

            ItemStack stack = new ItemStack((byte)i, 64);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack);
            slot.isCreative = false;

        }
    }
}
