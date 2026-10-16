using Sift.App;

UINode Transport(Props _) {
  var host = Sift.App.SiftHost.Current;
  bool isPlane = host.Mode == Sift.App.ViewMode.Plane;

  return (
    <Box className="transport">
      {Widgets.PlayButton(host.IsPlaying, () => host.TogglePlay())}
      <Text className="transport-position">{host.PositionLabel}</Text>
      <Box className="transport-group">
        <Text className="transport-label">TEMPO</Text>
        {Widgets.IconPill(Paper.Icons.IconSets.Bs.BsDash, () => host.NudgeTempo(-1))}
        <Text className="transport-value">{host.Tempo.ToString()}</Text>
        {Widgets.IconPill(Paper.Icons.IconSets.Bs.BsPlus, () => host.NudgeTempo(1))}
      </Box>
      <Box className="transport-group">
        <Text className="transport-label">GRID</Text>
        <Text className="transport-value">{host.QuantisationLabel}</Text>
      </Box>
      <Box className="spacer"></Box>
      <Box className="segmented">
        {Widgets.Segment("2D", 40f, isPlane, () => host.SetMode(Sift.App.ViewMode.Plane))}
        {Widgets.Segment("LAYERS", 68f, !isPlane, () => host.SetMode(Sift.App.ViewMode.Layers))}
      </Box>
      <Box className="transport-group">
        <Text className="transport-label">LAYER</Text>
        {Widgets.IconPill(Paper.Icons.IconSets.Bs.BsDash, () => host.SetLayer(host.Layer - 1))}
        <Text className="transport-value">{host.Layer.ToString()}</Text>
        {Widgets.IconPill(Paper.Icons.IconSets.Bs.BsPlus, () => host.SetLayer(host.Layer + 1))}
      </Box>
      <Box className="transport-group">
        {Widgets.IconPill(Paper.Icons.IconSets.Bs.BsArrowCounterclockwise, () => host.Undo(), host.Scene.CanUndo)}
        {Widgets.IconPill(Paper.Icons.IconSets.Bs.BsArrowClockwise, () => host.Redo(), host.Scene.CanRedo)}
      </Box>
    </Box>
  );
}
