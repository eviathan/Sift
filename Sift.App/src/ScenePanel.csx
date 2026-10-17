using Sift.App;

UINode ScenePanel(Props _) {
  var host = Sift.App.SiftHost.Current;

  var viewport = UI.Viewport(host.SceneTexture, new Paper.Core.Styles.StyleSheet { FlexGrow = 1f },
    onSizeChanged: (w, h) => host.SetViewportSize(w, h));

  // NOTE: A transparent layer over the viewport takes the pointer, as in Prism's scene panel. Focusable so clicking
  // the scene takes focus from the sidebar's fields (committing them) and the keyboard shortcuts work again
  var overlay = UI.Box(new PropsBuilder()
    .TabIndex(-1)
    .Style(new Paper.Core.Styles.StyleSheet {
      Position = Paper.Core.Styles.Position.Absolute,
      Left = Paper.Core.Styles.Length.Px(0),
      Top = Paper.Core.Styles.Length.Px(0),
      Width = Paper.Core.Styles.Length.Percent(100),
      Height = Paper.Core.Styles.Length.Percent(100),
    })
    .OnPointerMove(e => host.PointerMove(e.LocalX, e.LocalY, e.TargetWidth, e.TargetHeight))
    .OnPointerDown(e => host.PointerDown(e.LocalX, e.LocalY, e.TargetWidth, e.TargetHeight, e.Button))
    .OnPointerUp(e => host.PointerUp())
    .OnPointerLeave(e => host.PointerLeave())
    .OnDoubleClick(() => host.DoubleClick())
    .OnWheel(e => host.Wheel(e.LocalX, e.LocalY, e.TargetWidth, e.TargetHeight, e.WheelDeltaX, e.WheelDeltaY, e.Meta || e.Ctrl))
    .Build());

  bool isPlane = host.Mode == Sift.App.ViewMode.Plane;
  string badge = isPlane ? "Layer " + host.Layer.ToString() : "All layers";
  string badgeNote = isPlane ? "" : "Scroll or click to choose, Enter to open";

  return (
    <Box className="scene">
      {viewport}
      {overlay}
      <Box className="scene-badge">
        <Text className="scene-badge-text">{badge}</Text>
        <Text className="scene-badge-note">{badgeNote}</Text>
      </Box>
      <Box className="scene-tools">
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsZoomOut, () => host.Zoom(-1), isPlane, size: 14f)}
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsFullscreen, () => host.FitView(), true, size: 13f)}
        {Widgets.GhostIcon(Paper.Icons.IconSets.Bs.BsZoomIn, () => host.Zoom(1), isPlane, size: 14f)}
      </Box>
    </Box>
  );
}
