using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItemSlot : MonoBehaviour
{

    public bool isLinked;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public TextMeshProUGUI slotAmount;

    World world;

    private void Awake()
    {
        world = GameObject.Find("World").GetComponent<World>();
    }

    public bool HasItem
    {
        get
        {
            if (itemSlot == null)
                return false;
            else return itemSlot.HasItem;
        }
    }

    public void Link (ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void Unlink()
    {
        itemSlot.UnlinkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (itemSlot != null && itemSlot.HasItem)
        {

            slotIcon.sprite = world.blockTypes[itemSlot.stack.id].icon;
            slotAmount.SetText(itemSlot.stack.amount.ToString());
            slotIcon.enabled = true;
            slotAmount.enabled = true;
        }
        else
            Clear();
    }

    public void Clear()
    {

        slotIcon.sprite = null;
        slotAmount.SetText("");
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy()
    {

        if (itemSlot != null)
            itemSlot.UnlinkUISlot();
    }
}

public class ItemSlot
{

    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;

    public bool isCreative;

    public ItemSlot(UIItemSlot _uiItemSlot)
    {
        stack = null;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot _uiItemSlot, ItemStack _stack)
    {

        stack = _stack;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public void LinkUISlot (UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }

    public void UnlinkUISlot ()
    {
        uiItemSlot = null;
    }

    public void EmptySlot()
    {

        stack = null;
        if (uiItemSlot != null)
            uiItemSlot.UpdateSlot();
    }

    public int UpdateAmount(int amt)
    {

        if(amt > stack.amount)
        {
            int _amt = stack.amount;
            EmptySlot();
            return _amt;
        } else if(amt < stack.amount)
        {
            stack.amount -= amt;
            uiItemSlot.UpdateSlot(); 
            return amt;
        } else
        {
            EmptySlot();
            return amt;
        }
    }

    public ItemStack GrabItemStack()
    {

        ItemStack handOver = new ItemStack(stack.id, stack.amount);
        EmptySlot();
        return handOver;
    }

    public void InsertItemStack(ItemStack _itemStack)
    {
        stack = _itemStack;
        uiItemSlot.UpdateSlot();
    }

    public bool HasItem
    {
        get
        {
            if (stack != null)
                return true;
            else return false;
        }
    }
}
