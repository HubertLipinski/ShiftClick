using PugMod;
using UnityEngine;

public class ShiftClick : IMod
{
    public void EarlyInit() {}

    public void Init()
    {
        UnityEngine.Debug.Log("[ShiftClick] initialized!");
    }

    public void ModObjectLoaded(Object obj) {}

    public void Shutdown() {}

    public void Update()
    {
        if (API.Server.World == null || !Manager.ui.isPlayerInventoryShowing) 
            return;

        PlayerController player = Manager.main.player;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Mouse0))
        {
            InventorySlotUI inventorySlotUI = Manager.ui.currentSelectedUIElement as InventorySlotUI;
            int index = inventorySlotUI == null ? -1 : inventorySlotUI.inventorySlotIndex;

            int freeSpace = GetEmptyInventoryIndex(player, index);

            if (freeSpace == -1) 
                return;

            InventoryHandler inventoryHandler = player.playerInventoryHandler;
            inventoryHandler.Swap(player, index, inventoryHandler, freeSpace);
        } 
    }

    private int GetEmptyInventoryIndex(PlayerController player, int itemIndex)
    {
        if (itemIndex == -1)
            return -1;

        int inventorySize = player.playerInventoryHandler.size;

        int index = itemIndex < 10 ? 10 : 0; // skip hotbar indexes

        for (int i = index; i < inventorySize; i++)
        {
            ObjectDataCD ObjData = player.playerInventoryHandler.GetObjectData(i);

            if (ObjData.objectID == 0) 
                return i;
        }

        return -1;
    }
}
