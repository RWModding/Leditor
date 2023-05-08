using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static Networking;

public static class Operation
{
    public static Bundle Current;
    public static List<Bundle> Recent = new();

    public static void Start(string tabName)
    {
        Current ??= new() { TabName = tabName };
    }

    public static void Commit()
    {
        if (Current == null) return;

        if (Current.Actions.Count > 0)
        {
            //-- TODO: Implement undo/redo
            //Recent.Add(Current);
        }

        var current = Current;
        Current = null;

        if (NetworkManager.Singleton.IsServer)
        {
            Networking.Instance.SendOperation(current, false);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Networking.Instance.SendOperation(current, true);
        }
        else
        {
            EditorManager.Instance.CurrentEditor.CommitOperation(current);
        }
    }

    public static void NewAction(ActionType type, int layer, Vector3Int position, int oldValue, int newValue)
    {
        Current.Actions.Add(new (type, layer, position, oldValue, newValue));
    }

    public class Bundle
    {
        public string TabName;
        public List<EditorAction> Actions = new();
    }

    public struct EditorAction : INetworkSerializable
    {
        public ActionType Type;
        public Vector3Int Position;
        public int Layer;
        //public int OldValue;
        public int NewValue;

        public EditorAction(ActionType type, int layer, Vector3Int position, int oldValue, int newValue)
        {
            Type = type;
            Layer = layer;
            Position = position;
            //OldValue = oldValue;
            NewValue = newValue;
        }

        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Type);
            writer.WriteValueSafe(Position);
            writer.WriteValueSafe(Layer);
            //writer.WriteValueSafe(OldValue);
            writer.WriteValueSafe(NewValue);
        }

        public void Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out Type);
            reader.ReadValueSafe(out Position);
            reader.ReadValueSafe(out Layer);
            //reader.ReadValueSafe(out OldValue);
            reader.ReadValueSafe(out NewValue);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                Deserialize(serializer.GetFastBufferReader());
            }
            else
            {
                Serialize(serializer.GetFastBufferWriter());
            }
        }
    }

    public enum ActionType
    {
        SetGeoType,
        AddFeature,
        RemoveFeature,
    }
}