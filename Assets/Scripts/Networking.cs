using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Xml.Schema;
using System.IO.Compression;
using System.IO;
using System.Text;
using Unity.Networking.Transport;
using UnityEditor;

public class Networking : NetworkBehaviour
{
    private const int MaxConnections = 3;
    private const int MaxBufferSize = 60000;

    public static Networking Instance;
    public static string UnityPlayerID;
    public static string JoinCode;
    public static List<Player> Players = new();

    private new Camera camera;

    void Awake()
    {
        Instance = this;
        Players = new();
        camera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }
        if (IsClient)
        {

        }
    }

    private void OnClientDisconnect(ulong clientID)
    {
        var index = -1;
        for (var i = 0; i < Players.Count; i++)
        {
            if (Players[i].PlayerID == clientID)
            {
                index = i;
                break;
            }
        }

        if (index != -1)
        {
            Players.RemoveAt(index);
        }
    }

    #region Operation

    public void SendOperation(Operation.Bundle operation, bool toServer)
    {
        var writer = new FastBufferWriter(MaxBufferSize * 50, Allocator.Temp);

        foreach (var action in operation.Actions)
        {
            action.Serialize(writer);
        }

        var data = Zip(writer.ToArray());
        writer.Dispose();

        var uuid = Guid.NewGuid().ToString();

        for (int i = 0; i < data.Length; i += MaxBufferSize)
        {
            var buffer = new byte[Mathf.Min(MaxBufferSize, data.Length - i)];
            Array.Copy(data, i, buffer, 0, buffer.Length);

            if (toServer)
            {
                SendOperationServerRPC(operation.TabName, buffer, operation.Actions.Count, uuid, i + MaxBufferSize >= data.Length);
            }
            else
            {
                SendOperationClientRPC(operation.TabName, buffer, operation.Actions.Count, uuid, i + MaxBufferSize >= data.Length);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendOperationServerRPC(string tabName, byte[] data, int length, string uuid, bool done)
    {
        SendOperationClientRPC(tabName, data, length, uuid, done);
    }

    private static Dictionary<string, List<byte[]>> OperationBuffers = new();

    [ClientRpc]
    public void SendOperationClientRPC(string tabName, byte[] data, int length, string uuid, bool done)
    {
        if (!OperationBuffers.TryGetValue(uuid, out var buffers))
        {
            buffers = new();
            OperationBuffers.Add(uuid, buffers);
        }

        buffers.Add(data);

        if (done)
        {
            var bufferSize = 0;
            for (var i = 0; i < buffers.Count; i++)
            {
                bufferSize += buffers[i].Length;
            }
            var buffer = new byte[bufferSize];
            for (var i = 0; i < buffers.Count; i++)
            {
                Array.Copy(buffers[i], 0, buffer, i * MaxBufferSize, buffers[i].Length);
            }

            OperationBuffers.Remove(uuid);

            var reader = new FastBufferReader(Unzip(buffer), Allocator.Temp);

            var actions = new Operation.EditorAction[length];

            for (int i = 0; i < length; i++)
            {
                actions[i].Deserialize(reader);
            }

            var tab = EditorManager.Instance.Tabs.FirstOrDefault(x => x.File.Name == tabName);
            if (tab != null)
            {
                tab.Editor.CommitOperation(new Operation.Bundle { TabName = tabName, Actions = actions.ToList() });
            }
        }
    }

    #endregion

    #region Player Data Sync
    private float syncCounter;
    private Player lastPlayerPackage;
    void Update()
    {
        syncCounter += Time.deltaTime;
        if (syncCounter >= 0.05f)
        {
            var cursorPos = camera.ScreenToWorldPoint(Input.mousePosition);
            var playerPackage = new Player {
                PlayerID = NetworkManager.Singleton.LocalClientId,
                CurrentTabName = EditorManager.Instance.CurrentTab?.File.Name ?? "",
                CursorColor = Color.red,
                CursorPos = new Vector2Int(Mathf.FloorToInt(cursorPos.x), Mathf.CeilToInt(cursorPos.y))
            };

            if (!playerPackage.Equals(lastPlayerPackage))
            {
                UpdatePlayerDataServerRPC(playerPackage);
                lastPlayerPackage = playerPackage;
            }
            syncCounter = 0;
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
    void UpdatePlayerDataServerRPC(Player player)
    {
        UpdatePlayerDataClientRPC(player);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void UpdatePlayerDataClientRPC(Player player)
    {
        var replaced = false;
        for (var i = 0; i < Players.Count; i++)
        {
            if (Players[i].PlayerID == player.PlayerID)
            {
                Players[i] = player;
                replaced = true;
                break;
            }
        }

        if (!replaced)
        {
            Players.Add(player);
        }
    }
    #endregion

    #region OpenTab
    private Dictionary<string, List<byte[]>> OpenTabBuffers = new();

    public void SendOpenTab(string levelName, string levelString, bool toServer)
    {
        var data = Zip(Encoding.UTF8.GetBytes(levelString));

        for (int i = 0; i < data.Length; i += MaxBufferSize)
        {
            var buffer = new byte[Mathf.Min(MaxBufferSize, data.Length - i)];
            Array.Copy(data, i, buffer, 0, buffer.Length);
            
            if (toServer)
            {
                OpenTabServerRPC(levelName, buffer, i + MaxBufferSize >= data.Length);
            }
            else
            {
                OpenTabClientRPC(levelName, buffer, i + MaxBufferSize >= data.Length);
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void OpenTabServerRPC(string levelName, byte[] levelData, bool done)
    {

        while (EditorManager.Instance.Tabs.Any(x => x.File.Name == levelName))
        {
            levelName += "_";
        }

        OpenTabClientRPC(levelName, levelData, done);
    }

    [ClientRpc]
    public void OpenTabClientRPC(string levelName, byte[] levelData, bool done)
    {
        if (!OpenTabBuffers.TryGetValue(levelName, out var buffers))
        {
            buffers = new();
            OpenTabBuffers.Add(levelName, buffers);
        }

        buffers.Add(levelData);

        if (done)
        {
            var bufferSize = 0;
            for (var i = 0; i < buffers.Count; i++)
            {
                bufferSize += buffers[i].Length;
            }
            var buffer = new byte[bufferSize];
            for (var i = 0; i < buffers.Count; i++)
            {
                Array.Copy(buffers[i], 0, buffer, i * MaxBufferSize, buffers[i].Length);
            }

            OpenTabBuffers.Remove(levelName);
            EditorManager.Instance.OpenTab(levelName, Encoding.UTF8.GetString(Unzip(buffer)).Trim());
        }
    }

    #endregion

    #region CloseTab

    public void SendCloseTab(string tabName, bool toServer)
    {
        if (toServer)
        {
            CloseTabServerRPC(tabName);
        }
        else
        {
            CloseTabClientRPC(tabName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CloseTabServerRPC(string tabName)
    {
        if (EditorManager.Instance.Tabs.Any(x => x.File.Name == tabName))
        {
            CloseTabClientRPC(tabName);
        }
    }

    [ClientRpc]
    public void CloseTabClientRPC(string tabName)
    {
        EditorManager.Instance.CloseTab(tabName);
    }

    #endregion

    #region Relay Connection

    public static async Task Authenticate()
    {
        if (!string.IsNullOrEmpty(UnityPlayerID)) return;

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        UnityPlayerID = AuthenticationService.Instance.PlayerId;
    }

    public static IEnumerator ConfigureTransportAndStartNgoAsHost(TextMeshProUGUI inviteCodeText)
    {
        if (string.IsNullOrEmpty(JoinCode))
        {
            inviteCodeText.text = "Authenticating...";
            var authenticateTask = Authenticate();

            while (!authenticateTask.IsCompleted)
            {
                yield return null;
            }

            if (authenticateTask.IsFaulted)
            {
                inviteCodeText.text = "Failed to authenticate!";
                Debug.LogError("Failed to authenticate with unity services: " + authenticateTask.Exception.Message);
                yield break;
            }

            inviteCodeText.text = "Allocating relay...";
            var allocationTask = RelayService.Instance.CreateAllocationAsync(MaxConnections);

            while (!allocationTask.IsCompleted)
            {
                yield return null;
            }

            if (allocationTask.IsFaulted) 
            {
                inviteCodeText.text = "Failed to allocate relay!";
                Debug.LogError("Relay create allocation request failed: " + allocationTask.Exception.Message);
                yield break;
            }

            var allocation = allocationTask.Result;

            Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"server: {allocation.AllocationId}");

            inviteCodeText.text = "Requesting invite code...";
            var createJoinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            while (!createJoinCodeTask.IsCompleted)
            {
                yield return null;
            }

            if (createJoinCodeTask.IsFaulted)
            {
                inviteCodeText.text = "Failed to create invite code!";
                Debug.LogError("Relay create join code request failed: " + createJoinCodeTask.Exception.Message);
                yield break;
            }

            JoinCode = createJoinCodeTask.Result;

            var relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
        }

        inviteCodeText.text = JoinCode;
        yield return null;
    }

    public static IEnumerator ConfigureTransportAndStartNgoAsConnectingPlayer(TextMeshProUGUI inviteCodeText, string inviteCode)
    {
        inviteCodeText.text = "Authenticating...";
        var authenticateTask = Authenticate();

        while (!authenticateTask.IsCompleted)
        {
            yield return null;
        }

        inviteCodeText.text = "Joining...";
        var allocationTask = RelayService.Instance.JoinAllocationAsync(inviteCode);

        while (!allocationTask.IsCompleted)
        {

            yield return null;
        }

        if (allocationTask.IsFaulted)
        {
            inviteCodeText.text = "Failed to join relay!";
            Debug.LogError("Relay create join code request failed: " + allocationTask.Exception.Message);

            yield return new WaitForSeconds(3);
            inviteCodeText.text = "";
            yield break;
        }

        var allocation = allocationTask.Result;

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        var relayServerData = new RelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();

        inviteCodeText.text = inviteCode;
        yield return null;
    }

    #endregion

    #region Zip
    public static byte[] Zip(byte[] bytes)
    {
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                CopyTo(msi, gs);
            }

            return mso.ToArray();
        }
    }

    public static byte[] Unzip(byte[] bytes)
    {
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                CopyTo(gs, mso);
            }

            return mso.ToArray();
        }
    }

    public static void CopyTo(Stream src, Stream dest)
    {
        byte[] bytes = new byte[4096];

        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, cnt);
        }
    }
    #endregion

    public struct Player : INetworkSerializable, IEquatable<Player>
    {
        public ulong PlayerID;
        public FixedString512Bytes CurrentTabName;
        public Vector2Int CursorPos;
        public Color CursorColor;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out PlayerID);
                reader.ReadValueSafe(out CurrentTabName);
                reader.ReadValueSafe(out CursorPos);
                reader.ReadValueSafe(out CursorColor);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(PlayerID);
                writer.WriteValueSafe(CurrentTabName);
                writer.WriteValueSafe(CursorPos);
                writer.WriteValueSafe(CursorColor);
            }
        }

        public bool Equals(Player other)
        {
            return PlayerID == other.PlayerID && CurrentTabName == other.CurrentTabName && CursorPos == other.CursorPos && CursorColor == other.CursorColor;
        }
    }
}
