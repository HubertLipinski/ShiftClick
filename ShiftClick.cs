using PugMod;
using System.Linq;
using UnityEngine;

public class ShiftClick : IMod
{

    private ObjectType[] ignoredTypes = new ObjectType[]
    {
        ObjectType.Helm,
        ObjectType.BreastArmor,
        ObjectType.PantsArmor,
        ObjectType.Necklace,
        ObjectType.Ring,
        ObjectType.Bag,
        ObjectType.Lantern,
        ObjectType.Offhand,
        ObjectType.Pet,
    };

    public void EarlyInit() { }

    public void Init()
    {
        Debug.Log("[ShiftClick] initialized!");
    }

    public void ModObjectLoaded(Object obj) { }

    public void Shutdown() { }

    public void Update()
    {
        if (API.Server.World == null || !Manager.ui.isPlayerInventoryShowing)
            return;

        PlayerController player = Manager.main.player;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Mouse0))
        {
            HandleInventoryChange(player);
        }
    }

    private void HandleInventoryChange(PlayerController player)
    {
        InventorySlotUI inventorySlotUI = Manager.ui.currentSelectedUIElement as InventorySlotUI;

        int index = inventorySlotUI == null ? -1 : inventorySlotUI.inventorySlotIndex;

        InventoryHandler inventoryHandler = player.playerInventoryHandler;
        InventoryHandler chestInventoryHandler = player.activeInventoryHandler;

        ObjectDataCD itemData = inventoryHandler.GetObjectData(index);
        ObjectInfo objectInfo = PugDatabase.GetObjectInfo(itemData.objectID);

        // clicked on non inventory object
        if (itemData.objectID == ObjectID.None || index == -1)
            return;

        if (inventorySlotUI.slotType == ItemSlotsUIType.ChestSlot)
        {
            ObjectDataCD itemDataChest = chestInventoryHandler.GetObjectData(index);
            ObjectInfo objectInfoChest = PugDatabase.GetObjectInfo(itemDataChest.objectID);

            int emptySlot = GetEmptyInventoryIndex(inventoryHandler, objectInfoChest, -1);

            if (emptySlot == -1)
                return;

            chestInventoryHandler.TryMoveTo(player, index, inventoryHandler, emptySlot);

            return;
        }

        if (inventorySlotUI.slotType == ItemSlotsUIType.PlayerInventorySlot)
        {
            if (Manager.ui.isChestInventoryUIShowing)
            {
                int emptySlot = GetIndexOfItemInInventory(chestInventoryHandler, objectInfo.isStackable ? objectInfo.objectID : ObjectID.None);

                if (emptySlot == -1)
                    return;

                inventoryHandler.TryMoveTo(player, index, chestInventoryHandler, emptySlot);
            }
            else
            {
                // ignore inventory items - default behavior is expected
                if (ignoredTypes.Contains(objectInfo.objectType))
                    return;

                int inventorySlot = GetEmptyInventoryIndex(inventoryHandler, objectInfo, index);

                if (inventorySlot == -1)
                    return;

                inventoryHandler.TryMoveTo(player, index, inventoryHandler, inventorySlot);
            }
        }
    }

    private int GetEmptyInventoryIndex(InventoryHandler inventoryHandler, ObjectInfo objectInfo, int startingIndex = 0)
    {
        bool isItemStackable = objectInfo.isStackable;
        ObjectID objectID = isItemStackable ? objectInfo.objectID : ObjectID.None;

        int index = startingIndex != -1 && startingIndex < 10 ? 10 : 0;

        var firstFound = GetIndexOfItemInInventory(inventoryHandler, objectID, index);

        if (!isItemStackable)
            return firstFound;

        var nextItemKind = GetIndexOfItemInInventory(inventoryHandler, objectID, 0, firstFound);
        var firstStackableSlot = FindFirstStackbleSlot(startingIndex, firstFound, nextItemKind);

        return firstStackableSlot ?? GetIndexOfItemInInventory(inventoryHandler, ObjectID.None, index);
    }

    private int GetIndexOfItemInInventory(InventoryHandler inventoryHandler, ObjectID objectID, int index = 0, int skipIndex = -1)
    {
        for (int i = index; i < inventoryHandler.size; i++)
        {
            ObjectDataCD ObjData = inventoryHandler.GetObjectData(i);

            if (ObjData.objectID == objectID && i != skipIndex)
                return i;
        }

        return -1;
    }

    private int? FindFirstStackbleSlot(int initialValue, int first, int second)
    {
        if (first == initialValue && second != -1)
            return second;

        if (second == initialValue && first != -1)
            return first;

        // egde case - when inventory is filled with stackable items of the same kind
        if (first != second && first != initialValue && first != -1)
            return first;

        return null;
    }
}
