using System.Linq;
using CoreLib;
using CoreLib.RewiredExtension;
using PugMod;
using Rewired;
using UnityEngine;

public class ShiftClick : IMod
{
    private const string Version = "1.2.0";
    private const string Name = "ShiftClick";
    private const string Author = "Trolxu";

    private const string KeyBindName = "ShiftClickModifier";
    private const string KeyBindDescription = "[ShitClick] Key modifier";

    private readonly ObjectType[] _ignoredItemTypes =
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

    public void EarlyInit()
    {
        Debug.Log($"[{Name}]: Version: {Version}, Author: {Author}");
        CoreLibMod.LoadModules(typeof(RewiredExtensionModule));
        RewiredExtensionModule.AddKeybind(KeyBindName, KeyBindDescription, KeyboardKeyCode.LeftShift);
        RewiredExtensionModule.SetDefaultControllerBinding(KeyBindName, GamepadTemplate.elementId_rightTrigger);
    }

    public void Init()
    {
        Debug.Log($"[{Name}]: initialized!");
    }

    public void ModObjectLoaded(Object obj)
    {
    }

    public void Shutdown()
    {
    }

    public void Update()
    {
        if (!Manager.ui.isPlayerInventoryShowing || IsAnyIgnoredUIOpen())
            return;

        PlayerController player = Manager.main.player;
        PlayerInput input = Manager.input.singleplayerInputModule;

        bool modifierKeyHeldDown = input.IsButtonCurrentlyDown((PlayerInput.InputType)RewiredExtensionModule.GetKeybindId(KeyBindName));
        bool interactedWithUI = input.rewiredPlayer.GetButtonDown((int)PlayerInput.InputType.UI_INTERACT);

        if (modifierKeyHeldDown && interactedWithUI)
        {
            HandleInventoryChange(player);
        }
    }

    private static bool IsAnyIgnoredUIOpen()
    {
        bool[] ignoredUIElementsAreOpened =
        {
            Manager.ui.cookingCraftingUI.isShowing,
            Manager.ui.processResourcesCraftingUI.isShowing,
            Manager.ui.isSalvageAndRepairUIShowing,
            Manager.ui.bossStatueUI.isShowing,
            Manager.ui.isBuyUIShowing,
            Manager.ui.isSellUIShowing,
        };

        return ignoredUIElementsAreOpened.Any(element => element);
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
        if (itemData.objectID == ObjectID.None || index == -1 || inventorySlotUI == null)
            return;

        if (inventorySlotUI.slotType == ItemSlotsUIType.ChestSlot)
        {
            ObjectDataCD itemDataChest = chestInventoryHandler.GetObjectData(index);
            ObjectInfo objectInfoChest = PugDatabase.GetObjectInfo(itemDataChest.objectID);

            int emptySlot = GetEmptyInventoryIndex(inventoryHandler, objectInfoChest, -1);
            MoveInventoryItem(player, chestInventoryHandler, inventoryHandler, index, emptySlot);

            return;
        }

        if (inventorySlotUI.slotType != ItemSlotsUIType.PlayerInventorySlot)
            return;

        if (Manager.ui.isChestInventoryUIShowing)
        {
            int emptySlot = GetIndexOfItemInInventory(chestInventoryHandler, objectInfo.isStackable ? objectInfo.objectID : ObjectID.None);
            MoveInventoryItem(player, inventoryHandler, chestInventoryHandler, index, emptySlot);

            return;
        }

        // default game behavior is expected
        if (_ignoredItemTypes.Contains(objectInfo.objectType))
            return;

        int inventorySlot = GetEmptyInventoryIndex(inventoryHandler, objectInfo, index);
        MoveInventoryItem(player, inventoryHandler, inventoryHandler, index, inventorySlot);
    }

    private static int GetEmptyInventoryIndex(InventoryHandler inventoryHandler, ObjectInfo objectInfo, int startingIndex = 0)
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

    private static int GetIndexOfItemInInventory(InventoryHandler inventoryHandler, ObjectID objectID, int index = 0, int skipIndex = -1)
    {
        for (int i = index; i < inventoryHandler.size; i++)
        {
            ObjectDataCD objData = inventoryHandler.GetObjectData(i);

            if (objData.objectID == objectID && i != skipIndex)
                return i;
        }

        return -1;
    }

    private static int? FindFirstStackbleSlot(int initialValue, int first, int second)
    {
        if (first == initialValue && second != -1)
            return second;

        if (second == initialValue && first != -1)
            return first;

        // edge case - when inventory is filled with stackable items of the same kind
        if (first != second && first != initialValue && first != -1)
            return first;

        return null;
    }

    private static void MoveInventoryItem(PlayerController player, InventoryHandler primaryInventoryHandler, InventoryHandler secondaryInventoryHandler, int index, int emptySlot)
    {
        if (emptySlot == -1)
            return;

        primaryInventoryHandler.TryMoveTo(player, index, secondaryInventoryHandler, emptySlot);
    }
}