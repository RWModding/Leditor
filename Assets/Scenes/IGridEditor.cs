using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGridEditor: IEditor
{
    public int SelectedLayer { get; set; }

    public bool TryPlace<T>(int obj, Vector3Int pos) where T : Enum;

    public void Clear(Vector3Int pos);

    public void ClearAll();
}
