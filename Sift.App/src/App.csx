@import "ScenePanel.csx"
@import "Sidebar.csx"
@import "Transport.csx"
@import "App.csss"

using Sift.App;

UINode App(Props _) {
  var glyph = UI.Image("Assets/header-glyph.png", new Paper.Core.Styles.StyleSheet {
    Width = Paper.Core.Styles.Length.Px(18),
    Height = Paper.Core.Styles.Length.Px(31),
    MinWidth = Paper.Core.Styles.Length.Px(18),
    MinHeight = Paper.Core.Styles.Length.Px(31),
    FlexShrink = 0f,
  });

  return (
    <Box className="root">
      <Box className="header">
        {glyph}
      </Box>
      <Box className="body">
        {UI.Component(ScenePanel)}
        {UI.Component(Sidebar)}
      </Box>
      {UI.Component(Transport)}
    </Box>
  );
}
