@import "ScenePanel.csx"
@import "Sidebar.csx"
@import "Transport.csx"
@import "App.csss"

using Sift.App;

UINode App(Props _) {
  var host = Sift.App.SiftHost.Current;

  var glyph = UI.Image("Assets/header-glyph.png", new Paper.Core.Styles.StyleSheet {
    Width = Paper.Core.Styles.Length.Px(18),
    Height = Paper.Core.Styles.Length.Px(31),
    MinWidth = Paper.Core.Styles.Length.Px(18),
    MinHeight = Paper.Core.Styles.Length.Px(31),
    FlexShrink = 0f,
  });

  string title = host.DocumentName + (host.IsDirty ? "  -  Edited" : "");

  return (
    <Box className="root">
      <Box className="header">
        <Box className="header-side"></Box>
        {glyph}
        <Box className="header-side header-right">
          <Text className="header-title">{title}</Text>
        </Box>
      </Box>
      <Box className="body">
        {UI.Component(ScenePanel)}
        {UI.Component(Sidebar)}
      </Box>
      {UI.Component(Transport)}
    </Box>
  );
}
