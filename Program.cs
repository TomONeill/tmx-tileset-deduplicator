﻿using Spectre.Console;

AnsiConsole.WriteLine("Welcome to TMX merger by Tom O'Neill.");
AnsiConsole.WriteLine("Tilesets will always be merged into the first tileset. Remember, you can always fix the image source of the tileset later!");

var tmxPath = AnsiConsole.Ask<string>("Where is the [purple].tmx[/] located?");
var map = MapLoader.TryLoadMap(tmxPath);

if (map == null)
{
    throw new FileLoadException($"Could not load '{tmxPath}'");
}

// TODO: improve selector:
// - only list tilesets which have equal counts of other tileset
// - only allow selection of tilesets that have equal counts
// - prevent selecting only one tileset
// - deal with unparsable ints? Apparently that's a thing
// - write unit test for example tmx
// - add --force option to ignore layers that contain data that can't be processed (other data types than csv)

var selectedTilesets = AnsiConsole.Prompt(
    new MultiSelectionPrompt<Tileset>()
        .Title("What [purple]tilesets[/] do you want to merge?")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more tilesets)[/]")
        .InstructionsText(
            "[grey](Press [blue]<space>[/] to toggle a tileset, " +
            "[green]<enter>[/] to accept)[/]")
        .AddChoices(map.Tilesets.ToArray())
        .UseConverter((tileset) => $"{tileset.Name} ([grey]{tileset.Image.Source}[/]) (tiles: {tileset.TileCount})")
);


var tileGrids = selectedTilesets.Select(tileset =>
{
    var index = 0;
    return Enumerable.Range(tileset.FirstGid, tileset.TileCount).ToDictionary(i => index++);
    // var tilesetDictionary = new Dictionary<int, int>();
    // var gid = tileset.FirstGid;
    // for (var index = 0; index < tileset.TileCount; index++)
    // {
    //     tilesetDictionary[index] = gid++;
    // }
});

// tileset A  gid = 1, length = 10
// tileset B  gid = 11, length = 10
// A gid 3 = B gid 13
// A[position] = gid

var sourceTileset = tileGrids.ElementAt(0);
var otherTilesets = tileGrids.Skip(1);
AnsiConsole.WriteLine($"Using {selectedTilesets.ElementAt(0).Name} as source.");
map.Layers.ForEach((layer) =>
{
    AnsiConsole.WriteLine($"Processing layer '{layer.Name}'...");
    if (layer.Data.Encoding != "csv")
    {
        throw new NotSupportedException($"Layer '{layer.Name}' had data encoding '{layer.Data.Encoding}'. Only 'csv' is supported. Please open an issue on GitHub.");
    }
    AnsiConsole.WriteLine("original");
    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine(layer.Data.Text);
    var layerTiles = layer.Data.Text.Split(',').Select(x => x.Replace("\n", "")).Select(x =>
    {
        // AnsiConsole.WriteLine($"parsing '{x}'");
        return int.Parse(x);
    }).ToList();
    var newLayerTiles = layerTiles.Select(layerTile =>
    {
        if (layerTile == 0 || !otherTilesets.Any(tileset => tileset.Values.Contains(layerTile)))
        {
            return layerTile;
        }

        var tilesetContainingLayerTile = otherTilesets.Single(tileset => tileset.Values.Contains(layerTile));
        var tileSetIndex = tilesetContainingLayerTile.FirstOrDefault(x => x.Value == layerTile).Key;
        var target = sourceTileset[tileSetIndex];
        // Console.WriteLine($"{layerTile} = {target}");

        return target;
    });
    var newLayerTilesStringified = string.Join(',', newLayerTiles);
    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine("new");
    AnsiConsole.WriteLine(newLayerTilesStringified);
    // layerTiles.Where(layerTile => layerTile > 0 && otherTilesets.Any(tileset => tileset.Values.Contains(layerTile))).Select(layerTile =>
    // {
    //     var tilesetContainingLayerTile = otherTilesets.Single(tileset => tileset.Values.Contains(layerTile));
    //     var tileSetIndex = tilesetContainingLayerTile.FirstOrDefault(x => x.Value == layerTile).Key;
    //     var target = sourceTileset[tileSetIndex];
    //     Console.WriteLine($"{layerTile} = {target}");

    //     return otherTilesets;
    // });
});

// put back the newlines \n?