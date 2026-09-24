using System;
using System.Collections;
using Game1.Gameplay;
using Game1.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game1.Enemies
{
    public sealed class MenuInputScenario : MonoBehaviour
    {
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-menu-input-smoke") < 0) yield break;
            // Synthetic game input only; no desktop keyboard events are sent.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            yield return new WaitForSeconds(1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return new WaitForSeconds(0.2f);
            bool opened = NetworkSessionMenu.MenuOpen && Cursor.visible;
            var player = FindFirstObjectByType<LocalPlayerController>();
            Vector3 before = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.6f);
            bool stopped = Vector3.Distance(before, player.transform.position) < 0.05f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return new WaitForSeconds(0.2f);
            bool closed = !NetworkSessionMenu.MenuOpen;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.4f);
            bool resumed = Vector3.Distance(before, player.transform.position) > 0.5f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Debug.Log($"GAME1_MENU_CHECK opens={opened} blocks_movement={stopped} closes={closed} resumes_movement={resumed}");
            Debug.Log($"GAME1_MENU_COMPLETE success={opened && stopped && closed && resumed}");
            Application.Quit(opened && stopped && closed && resumed ? 0 : 1);
        }
    }
}
