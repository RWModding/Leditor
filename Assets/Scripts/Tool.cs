using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Tool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract bool SupportsDrag { get; }
    public abstract GameObject Button { get; set; }

    public abstract void OnClick(Vector2 position);

    public abstract bool OnDragStart(Vector2 position);

    public abstract void OnDragUpdate(Vector2 position);

    public abstract void OnDragEnd(Vector2 position);
}