using LevelModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tool : MonoBehaviour
{
    public bool ShowLayerSelector;
    public string[] KeybindIds = new string[0];
    public Texture2D CursorOverride;
    public Vector2 CursorHotspot;
    private ToolGroup _group;

    protected LevelLoader LevelLoader;
    protected LevelData Level => LevelLoader.LevelData;
    protected int Layer => _group.LayerSelector.Layer;

    public void Awake()
    {
        _group = GetComponentInParent<ToolGroup>();
        LevelLoader = GetComponentInParent<LevelLoader>();
    }

    public void Toggle()
    {
        _group.CurrentTool = this;
    }

    public void OnBeginDrag(PointerEventData eventData) => OnBeginDrag(new MouseData(this, eventData));
    public void OnDrag(PointerEventData eventData) => OnDrag(new MouseData(this, eventData));
    public void OnEndDrag(PointerEventData eventData) => OnEndDrag(new MouseData(this, eventData));
    public void OnPointerClick(PointerEventData eventData) => OnPointerClick(new MouseData(this, eventData));
    public void OnPointerUp(PointerEventData eventData) => OnPointerUp(new MouseData(this, eventData));
    public void OnPointerDown(PointerEventData eventData) => OnPointerDown(new MouseData(this, eventData));

    protected virtual void OnBeginDrag(MouseData mouse) {}
    protected virtual void OnDrag(MouseData mouse) {}
    protected virtual void OnEndDrag(MouseData mouse) {}
    protected virtual void OnPointerClick(MouseData mouse) {}
    protected virtual void OnPointerUp(MouseData mouse) {}
    protected virtual void OnPointerDown(MouseData mouse) {}

    protected static Vector2 GetLevelPos(Vector2 screenPos)
    {
        var cam = Camera.main;
        var mousePos = (Vector3)screenPos;
        mousePos.z = 10f;
        return cam.ScreenToWorldPoint(mousePos) * new Vector2(1f, -1f);
    }

    protected class MouseData
    {
        public readonly Vector2 LevelPos;
        public readonly Vector2Int LevelTile;
        public readonly PointerEventData EventData;

        public MouseData(Tool tool, PointerEventData eventData)
        {
            EventData = eventData;

            LevelPos = GetLevelPos(eventData.position);
            LevelTile = Vector2Int.FloorToInt(LevelPos);
        }
    }
}
