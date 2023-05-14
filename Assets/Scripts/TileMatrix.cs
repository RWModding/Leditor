using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class gTEprops
{
    /* composed of:
     *  lastKeys <-- default this
     *  Keys <-- default this
     * workLayer <--default this
     * lstMsPs <--... WHY SAVE THIS???? default it.
     * tlMatrix <-- actual information on tiles!!!
     * defaultMaterial <-- important to save.
     * toolType <-- probably default this.
     * toolData <-- probably default this too.
     * tmPos <-- position in the tile menu.
     * tmSavPosL <-- no idea yet either, its a big array? was 75 long and all 1s in my testing project.
     * specialEdit <--also not sure.
     */
}

public class tlMatrix
{
    //formatted pretty much identical to the geo matrix, just with #tp and #data instead of the geo type and feature type.
    //tp is the type, either default, material, tileHead or tileBody?
    //data... is obviously data. for example, default has data as 0, material has the material name
    //tilehead data is first location of the tile in the tiles list?, then the tile name
    //tilebody data is first the location of the tilehead,  and then what i think is the layer of the tilehead. (since rarely tiles are multilayered)

    public tlColumn[] columns;

    public tlMatrix (int height,int width)
    {
        columns = new tlColumn[width];
        for (int i = 0; i < width; i++)
        {
            columns[i] = new tlColumn(height);
        }
    }
    public tlMatrix (string matrixstring) {
        var desermatrix = JsonConvert.DeserializeObject < List < List < List<object> >>> (matrixstring);
        columns = new tlColumn[desermatrix.Count];
        for (int i = 0;i < desermatrix.Count; i++)
        {
            var col = desermatrix[i];
            var column = new LColumn(col.Count);
            for (int j = 0; j < col.Count; j++)
            {
                var savedcell = col[j];
            }
        }
    }


    public void AddTile(LETile tile, Vector2 pos,int layer)
    {
        int col = (int)pos.x;
        int row = (int)pos.y;
        tileCell selectedCell = columns[col].cells[row];
        selectedCell.placedTileOrMat[layer] = new PlacedTile(tile);
        
    }
    
    public void AddMat(Mat material, Vector2 pos, int layer)
    {
        int col = (int)pos.x;
        int row = (int)pos.y;
        tileCell selectedCell = columns[col].cells[row];
        selectedCell.placedTileOrMat[layer] = new PlacedMaterial(material);
    }

}

public class tlColumn
{
    public tileCell[] cells;

    public tlColumn(int height)
    {
        cells = new tileCell[height];
        for(int i = 0; i < height; i++)
        {
            cells[i] = new tileCell();
        }
    }

}
/// <summary>
/// tile cell.
/// </summary>
public class tileCell
{
    //format like:
    // [#tp: "default",#data: 0]
    // [#tp: "tileBody",#data: [point(2,16), 1]]
    public TEPlaced[] placedTileOrMat;

    /// <summary>
    /// Creates a default tile cell.
    /// </summary>
    public tileCell()
    {
        placedTileOrMat = null;
    }

    public void PlaceTile(LETile tile,int layer) {
        placedTileOrMat[layer] = new PlacedTile(tile);
    }

    public void PlaceMaterial(Mat material,int layer)
    {
        placedTileOrMat[layer] = new PlacedMaterial(material);
    }

}

public abstract class TEPlaced
{
}
public class PlacedMaterial : TEPlaced
{
    public Mat material;
    public PlacedMaterial(Mat material)
    {
        this.material = material;
    }
}

public class Mat
{
    /*these are usually built into the leditor, 
     * but i think it'd be better if we allowed users to import in their own.
     * just for compatibility with private custom materials.
     */
    public string MatName;
    public Color MatColor;
    public string RenderType;

    public Mat(string matName, Color matColor, string renderType)
    {
        MatName = matName;
        MatColor = matColor;
        RenderType = renderType;
    }
}

public class PlacedTile : TEPlaced
{
    LETile tile;
    public TileCategory category;
    public Vector2? ChainPos = null; //only used for chainholders as special data guh.
    /// <summary>
    /// Creates a tile at a location.
    /// </summary>
    /// <param name="tile">Chosen tile</param>
    /// <param name="pos">Tile Head Position</param>
    public PlacedTile(LETile tile)
    {
        this.tile = tile;
        this.ChainPos = null;
    }
}

public class LETile
{
    public string TileName; //#nm in init
    public Vector2 TileSize; //#sz in init
    public RenderType renderType; //this wont be too useful until we do renderin but its there i guess!
    public GeoType[] Spec;
    public GeoType[]? Spec2;
    public int[] repeatL;
    public int bfTiles;
    public int rnd;
    public List<string> tags;
    public TileImageStuff imageData;
    /* for the images, theres first a grid of 20x20 tiles
     * theres the buffer tiles around the actual geo tiles like so:
     *  B|B|B
     *  B|A|B 
     *  B|B|B
     *  ^^ with B being buffer tiles, and A being actual tile.
     *  this is repeated for however many different layers needed
     *  we'll probably only need this for flattening into a preview for props, since we aren't doing rendering
     *  then theres a 16x16 grid section right after thats for the tile editor images
     *  you can get the amount of layers from how long the #repeatL is.
     *  and buffer tile amount from #bfTiles
     *  #rnd is for how many random variations, their images are side by side.
     *  #specs is the geometry array, and #specs2 is the second layer geo (0 by default)
     *  #tags is used for tags like notTrashProp etc.
     *  don't need to have most of these since no rendering.
     *  only one i can see use of is `notProp` which disallows the tile from tile as prop
     *  
     */



    public LETile(string TileName, Vector2 TileSize, GeoType[] Spec, GeoType[]? Spec2, RenderType renderType,int[]? repeatL,int bfTiles,int rnd, List<string> tags, TileImageStuff image)
    {
        this.TileName = TileName;
        this.TileSize = TileSize;
        this.Spec = Spec;
        this.Spec2 = Spec2;
        this.renderType = renderType;
        this.repeatL = repeatL;
        this.bfTiles = bfTiles;
        this.rnd = rnd;
        this.tags = tags;
        this.imageData = image;
    }
}

public class TileImageStuff
{
    Texture2D rawImage;
    public Vector2 size;
    public int layers;
    public Sprite[] splitUpLayers;
    public Sprite Icon;

    public TileImageStuff(Texture2D rawImage, int bftiles, Vector2 origsize, int layers, RenderType rendertype)
    {
        this.rawImage = rawImage;
        this.size = new(origsize.x + (2 * bftiles), origsize.y + (2 * bftiles));
        this.layers = layers;
        splitUpLayers = new Sprite[layers];
        for (int i = 0; i < layers; i++)
        {
            splitUpLayers[i] = Sprite.Create(rawImage, new Rect(0, i * (size.y * 20) + (rendertype == RenderType.voxelStruct?1:0), size.x * 20, size.y * 20), new Vector2(0f, 0f));
        }
        Rect iconplace = new Rect(0,(size.y *20)* layers + (rendertype == RenderType.voxelStruct ? 1 : 0), origsize.x*16,origsize.y*16);
        while(iconplace.y + iconplace.height > rawImage.height)
        {
            iconplace.height--;
        }
        Icon = Sprite.Create(rawImage, iconplace, new Vector2(0f, 0f));
    }
}

public enum RenderType
{
    voxelStruct,
    voxelStructRockType,
    voxelStructRandomDisplaceVertical,
    voxelStructRandomDisplaceHorizontal,
    box,

}

public class TileCategory
{
    public List<LETile> tiles;
    public Color categoryColor;
    public string categoryName;
    public TileCategory(string Name, Color Color)
    {
        tiles = new List<LETile>();
        categoryColor = Color;
        categoryName = Name;
    }
}

public class MaterialCategory
{
    public string Name;
    public List<Mat> materials;
    public MaterialCategory(string name)
    {
        materials = new List<Mat>();
        Name = name;
    }
}