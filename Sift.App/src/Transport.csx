using Sift.App;

UINode Transport(Props _) {
  var host = Sift.App.SiftHost.Current;
  bool isPlane = host.Mode == Sift.App.ViewMode.Plane;

  return (
    <Box className="transport">
      {Widgets.PlayButton(host.IsPlaying, () => host.TogglePlay())}
      {Widgets.Gap(12)}
      <Text className="transport-position">{host.PositionLabel}</Text>
      {Widgets.BeatDots(host.BeatInBar)}
      {Widgets.VerticalDivider()}
      <Box className="transport-group">
        <Text className="transport-value">{host.Tempo.ToString()}</Text>
        <Text className="transport-label">BPM</Text>
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsDash, () => host.NudgeTempo(-1), size: 14f)}
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsPlus, () => host.NudgeTempo(1), size: 14f)}
      </Box>
      {Widgets.VerticalDivider()}
      <Box className="transport-group">
        <Text className="transport-label">GRID</Text>
        {Widgets.Cycle(host.QuantisationLabel, () => host.CycleQuantisation(-1), () => host.CycleQuantisation(1), 36f)}
      </Box>
      <Box className="spacer"></Box>
      <Box className="segmented">
        {Widgets.Segment("2D", 40f, isPlane, () => host.SetMode(Sift.App.ViewMode.Plane))}
        {Widgets.Segment("LAYERS", 68f, !isPlane, () => host.SetMode(Sift.App.ViewMode.Layers))}
      </Box>
      {Widgets.VerticalDivider()}
      <Box className="transport-group">
        <Text className="transport-label">LAYER</Text>
        <Text className="transport-value">{host.Layer.ToString()}</Text>
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsChevronDown, () => host.SetLayer(host.Layer - 1), size: 13f)}
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsChevronUp, () => host.SetLayer(host.Layer + 1), size: 13f)}
      </Box>
      {Widgets.VerticalDivider()}
      {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsArrowCounterclockwise, () => host.Undo(), host.Scene.CanUndo)}
      {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsArrowClockwise, () => host.Redo(), host.Scene.CanRedo)}
    </Box>
  );
}
