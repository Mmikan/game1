using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

namespace Game1.Network
{
    public sealed class NetworkSessionMenu : MonoBehaviour
    {
        public static bool MenuOpen { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMenu() => MenuOpen = false;
        private void Update()
        {
            if (Application.isBatchMode || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            MenuOpen = !MenuOpen;
            Cursor.lockState = MenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = MenuOpen;
        }
        [SerializeField] private NetworkManager manager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;
        public void Configure(NetworkManager managerValue, UnityTransport transportValue)
        {
            manager = managerValue;
            transport = transportValue;
        }

        private void Awake()
        {
            if (manager == null) manager = GetComponent<NetworkManager>();
            if (transport == null) transport = GetComponent<UnityTransport>();
            manager.OnClientConnectedCallback += id => Debug.Log($"GAME1_NET_CONNECTED client={id} host={manager.IsHost}");
            manager.OnClientDisconnectCallback += id => Debug.Log($"GAME1_NET_DISCONNECTED client={id} host={manager.IsHost}");
            ApplyCommandLine();
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-game1-host") >= 0) StartHost();
            else if (Array.IndexOf(args, "-game1-client") >= 0) StartClient();
        }

        private void OnGUI()
        {
            if (manager == null || transport == null) return;
            if (!MenuOpen) { GUI.Label(new Rect(Screen.width - 250, 18, 230, 30), "[Esc] Menu"); return; }
            GUILayout.BeginArea(new Rect(Screen.width - 250, 18, 230, 150), GUI.skin.box);
            GUILayout.Label(manager.IsListening ? (manager.IsHost ? "Host running" : "Client connected") : "Network offline");
            if (!manager.IsListening)
            {
                address = GUILayout.TextField(address);
                if (GUILayout.Button("Start Host")) StartHost();
                if (GUILayout.Button("Join")) StartClient();
            }
            else if (GUILayout.Button("Leave")) manager.Shutdown();
            GUILayout.EndArea();
        }

        public bool StartHost()
        {
            transport.SetConnectionData(address, port, "0.0.0.0");
            return manager.StartHost();
        }

        public bool StartClient()
        {
            transport.SetConnectionData(address, port);
            return manager.StartClient();
        }

        private void ApplyCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-game1-address") address = args[i + 1];
                if (args[i] == "-game1-port" && ushort.TryParse(args[i + 1], out ushort parsed)) port = parsed;
            }
        }
    }
}
