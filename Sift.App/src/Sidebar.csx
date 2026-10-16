using Sift.App;

UINode Sidebar(Props _) {
  var host = Sift.App.SiftHost.Current;
  var node = host.SelectedNode;
  var content = new List<UINode>();

  if (node != null) {
    var position = node.Position;
    var nodeId = node.Id;

    content.Add(Widgets.TextOf("NODE", "heading"));
    content.Add(Widgets.BoxOf("card", new[] {
      Widgets.TextOf(node.Name ?? "Untitled", "title"),
      Widgets.Row("Position", position.X.ToString() + ", " + position.Y.ToString() + "  (layer " + position.Z.ToString() + ")"),
      Widgets.Row("Starts on play", node.EmitOnPlay ? "Yes" : "No"),
      Widgets.Row("Active", node.IsActive ? "Yes" : "No"),
    }));

    var ports = new List<UINode>();
    var order = new[] { Sift.Core.Enums.Direction.Up, Sift.Core.Enums.Direction.Right, Sift.Core.Enums.Direction.Down, Sift.Core.Enums.Direction.Left, Sift.Core.Enums.Direction.Forwards, Sift.Core.Enums.Direction.Backwards };

    foreach (var direction in order) {
      if (!node.Ports.TryGetValue(direction, out var components) || components.Count == 0)
        continue;

      var chips = components.Select(c => Widgets.Chip(c.Type)).ToList();
      ports.Add(Widgets.BoxOf("row", new[] { Widgets.TextOf(direction.ToString(), "row-label"), Widgets.BoxOf("chips", chips) }));
    }

    if (ports.Count == 0)
      ports.Add(Widgets.TextOf("Nothing on any port", "note"));

    content.Add(Widgets.TextOf("PORTS", "heading"));
    content.Add(Widgets.BoxOf("card", ports));
    content.Add(Widgets.BoxOf("row", new[] {
      Widgets.Pill("Delete node", () => host.DeleteNode(nodeId), Sift.App.Theme.Error),
    }));
  }
  else if (host.SelectedCell is { } cell) {
    content.Add(Widgets.TextOf("EMPTY CELL", "heading"));
    content.Add(Widgets.BoxOf("card", new[] {
      Widgets.Row("Position", cell.X.ToString() + ", " + cell.Y.ToString() + "  (layer " + cell.Z.ToString() + ")"),
      Widgets.TextOf("Adding nodes here is coming next.", "note"),
    }));
  }
  else {
    var (minLayer, maxLayer) = host.Layers;

    content.Add(Widgets.TextOf("SEQUENCE", "heading"));
    content.Add(Widgets.BoxOf("card", new[] {
      Widgets.Row("Tempo", host.Tempo.ToString() + " BPM"),
      Widgets.Row("Grid", host.QuantisationLabel + " per cell"),
      Widgets.Row("Nodes", host.Scene.Nodes.Count.ToString()),
      Widgets.Row("Layers", (maxLayer - minLayer + 1).ToString()),
    }));

    var layerRows = new List<UINode>();

    for (int layer = maxLayer; layer >= minLayer; layer--) {
      int captured = layer;
      int count = host.Scene.Nodes.Count(n => n.Position.Z == captured);
      layerRows.Add(Widgets.LayerRow(captured, count, captured == host.Layer, () => host.SetLayer(captured)));
    }

    content.Add(Widgets.TextOf("LAYERS", "heading"));
    content.Add(Widgets.BoxOf("card", layerRows));

    content.Add(Widgets.TextOf("KEYS", "heading"));
    content.Add(Widgets.BoxOf("card", new[] {
      Widgets.Row("Play / stop", "Space"),
      Widgets.Row("2D / layers", "Tab"),
      Widgets.Row("Layer up / down", "] / ["),
      Widgets.Row("Delete node", "Delete"),
      Widgets.Row("Undo / redo", "Cmd Z / Cmd Shift Z"),
    }));
  }

  return Widgets.BoxOf("sidebar", content);
}
