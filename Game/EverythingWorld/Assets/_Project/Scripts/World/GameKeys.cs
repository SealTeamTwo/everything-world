using UnityEngine;

namespace EverythingWorld.World
{
    /// Whether the game's shortcut keys (G, M, J, L, - and =, H and the numbers at towers, F1) should work right now:
    /// not while the menu is open, while you're typing at the hub's terminal or a note (T), or while a text box has the keyboard.
    public static class GameKeys
    {
        public static bool Off => EverythingWorld.Player.MainMenu.Showing || HubTerminal.Active || PlayNotes.Active || EverythingWorld.Activities.GolfGame.Playing || GUIUtility.keyboardControl != 0;
    }
}
