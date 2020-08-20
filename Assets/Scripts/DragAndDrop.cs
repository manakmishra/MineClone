using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{

    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster raycaster = null;
    private PointerEventData pointerEventData;
    [SerializeField] private EventSystem eventSystem = null;

    World world;

    private void Start()
    {

        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }


    private void Update()
    {

        if (!world.uiActive)
            return;

        cursorSlot.transform.position = Input.mousePosition;
        
        if(Input.GetMouseButtonDown(0))
        {

            HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {

        if (clickedSlot == null)
            return;

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
            return;

        if(clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertItemStack(clickedSlot.itemSlot.stack);
        }

        if(!cursorSlot.HasItem && clickedSlot.HasItem)
        {

            cursorItemSlot.InsertItemStack(clickedSlot.itemSlot.GrabItemStack());
            return;
        }

        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {

            clickedSlot.itemSlot.InsertItemStack(cursorItemSlot.GrabItemStack());
            return;
        }

        if(cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if(cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.GrabItemStack();
                ItemStack oldSlot = clickedSlot.itemSlot.GrabItemStack();

                clickedSlot.itemSlot.InsertItemStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertItemStack(oldSlot); 
            }

            //add for same item
        }
    }

    private UIItemSlot CheckForSlot()
    {

        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach(RaycastResult result in results)
        {

            if (result.gameObject.tag == "UIItemSlot")
                return result.gameObject.GetComponent<UIItemSlot>();
        }

        return null;
    }
} 
