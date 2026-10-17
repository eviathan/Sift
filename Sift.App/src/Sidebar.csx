using Sift.App;
using Sift.App.Editing;

UINode Sidebar(Props _) {
  var host = Sift.App.SiftHost.Current;
  var node = host.SelectedNode;
  var content = new List<UINode>();

  if (host.LastError is { } error)
    content.Add(Widgets.Warning(error));

  if (node != null) {
    content.AddRange(Inspector.Node(host, node));
  }
  else if (host.SelectedCell is { } cell) {
    content.AddRange(Inspector.EmptyCell(host, cell));
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
      Widgets.Row("Add node", "Double-click"),
      Widgets.Row("Move node", "Drag"),
      Widgets.Row("Delete node", "Delete"),
      Widgets.Row("Undo / redo", "Cmd Z / Cmd Shift Z"),
    }));
  }

  // NOTE: Scrolls when a node's ports run past the bottom
  var scroll = UI.Scroll(new Paper.Core.Styles.StyleSheet {
    FlexGrow = 1f,
    Display = Paper.Core.Styles.Display.Flex,
    FlexDirection = Paper.Core.Styles.FlexDirection.Column,
    Gap = Paper.Core.Styles.Length.Px(10),
    Padding = new Paper.Core.Styles.Thickness(Paper.Core.Styles.Length.Px(16), Paper.Core.Styles.Length.Px(14)),
  }, content.ToArray());

  return Widgets.BoxOf("sidebar", new[] { scroll });
}
