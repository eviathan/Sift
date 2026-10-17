using Sift.App;
using Sift.App.Editing;

UINode Sidebar(Props _) {
  var host = Sift.App.SiftHost.Current;
  var node = host.SelectedNode;
  var content = new List<UINode>();

  if (host.PendingDiscard != null) {
    content.Add(Widgets.Section(null, new[] {
      Widgets.Warning("Discard unsaved changes to " + host.DocumentName + "?"),
      Widgets.HStack(
        Widgets.Pill("Discard", () => host.ConfirmDiscard(), Sift.App.Theme.Error),
        Widgets.Pill("Cancel", () => host.CancelDiscard())),
    }));
  }

  if (host.LastError is { } error)
    content.Add(Widgets.Section(null, new[] { Widgets.Warning(error) }));

  if (node != null) {
    content.AddRange(Inspector.Node(host, node));
  }
  else if (host.SelectedCell is { } cell) {
    content.AddRange(Inspector.EmptyCell(host, cell));
  }
  else {
    var (minLayer, maxLayer) = host.Layers;

    content.Add(Widgets.Section(null, new[] {
      Widgets.HStack(
        Widgets.TextOf(host.DocumentName, "title"),
        Widgets.Spacer(),
        host.IsDirty ? Widgets.TextOf("Edited", "badge") : UI.Box(new Paper.Core.Styles.StyleSheet())),
      Widgets.HStack(
        Widgets.IconTextButton(Paper.Icons.IconSets.Bs.BsFileEarmarkPlus, "New", () => host.NewSequence()),
        Widgets.IconTextButton(Paper.Icons.IconSets.Bs.BsFolder2Open, "Open", () => host.OpenSequence()),
        Widgets.IconTextButton(Paper.Icons.IconSets.Bs.BsDownload, "Save", () => host.Save())),
    }));

    content.Add(Widgets.Section("SEQUENCE", new[] {
      Widgets.Row("Tempo", host.Tempo.ToString() + " BPM"),
      Widgets.FieldRow("Grid", Widgets.Cycle(host.QuantisationLabel, () => host.CycleQuantisation(-1), () => host.CycleQuantisation(1))),
      Widgets.Row("Nodes", host.Scene.Nodes.Count.ToString()),
      Widgets.Row("Layers", (maxLayer - minLayer + 1).ToString()),
    }));

    var layerRows = new List<UINode>();

    for (int layer = maxLayer; layer >= minLayer; layer--) {
      int captured = layer;
      int count = host.Scene.Nodes.Count(n => n.Position.Z == captured);
      layerRows.Add(Widgets.LayerRow(captured, count, captured == host.Layer, () => host.SetLayer(captured)));
    }

    content.Add(Widgets.Section("LAYERS", layerRows));

    content.Add(Widgets.Section("SHORTCUTS", new[] {
      Widgets.Row("Play / stop", "Space"),
      Widgets.Row("2D / layers", "Tab"),
      Widgets.Row("Layer up / down", "] / ["),
      Widgets.Row("Add / move node", "Double-click / drag"),
      Widgets.Row("Undo / redo", "Cmd Z / Cmd Shift Z"),
      Widgets.Row("New / open / save", "Cmd N / O / S"),
    }));
  }

  // NOTE: Scrolls when a node's ports run past the bottom
  var scroll = UI.Scroll(new Paper.Core.Styles.StyleSheet {
    FlexGrow = 1f,
    Display = Paper.Core.Styles.Display.Flex,
    FlexDirection = Paper.Core.Styles.FlexDirection.Column,
  }, content.ToArray());

  return Widgets.BoxOf("sidebar", new[] { scroll });
}
