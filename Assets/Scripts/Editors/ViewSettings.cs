using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewSettings : MonoBehaviour
{
    public Dropdown viewModeDropdown;
    public GeoView geoView;

    private void Start()
    {
        viewModeDropdown.OnSelect.AddListener(ChangeViewMode);
        viewModeDropdown.Select(geoView.Mode switch
        {
            GeoView.ViewMode.GeoOnly => "Geo",
            GeoView.ViewMode.GeoAndTiles => "Geo + Tiles",
            GeoView.ViewMode.Tiled or _ => "Tiled"
        });
    }

    private void ChangeViewMode(string newMode)
    {
        geoView.Mode = newMode switch
        {
            "Geo" => GeoView.ViewMode.GeoOnly,
            "Geo + Tiles" => GeoView.ViewMode.GeoAndTiles,
            "Tiled" or _ => GeoView.ViewMode.Tiled
        };
    }
}
