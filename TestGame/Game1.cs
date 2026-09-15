using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CyclopsEngine;

namespace TestGame
{
  public class Game1 : Game
  {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    public static InputService Input => Runtime.Input;
    public static TextureService TextureService => Runtime.Textures;

    public Game1()
    {
      _graphics = new GraphicsDeviceManager(this);
      Runtime.Initialize(this);
      Content.RootDirectory = "Content";
      IsMouseVisible = true;
    }

    protected override void Initialize()
    {
      // TODO: Add your initialization logic here
      base.Initialize();

      new SpriteObject(Runtime.Pixel, null, new Vector2(5), null, new Vector2(10));
    }

    protected override void LoadContent()
    {
      _spriteBatch = new SpriteBatch(GraphicsDevice);
      Runtime.LoadContent();

      // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        Exit();

      // TODO: Add your update logic here
      Runtime.Update(gameTime);

      base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
      GraphicsDevice.Clear(Color.CornflowerBlue);

      // TODO: Add your drawing code here
      _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
      Runtime.Draw(gameTime, _spriteBatch);
      _spriteBatch.End();

      base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        Runtime.Shutdown();
      }

      base.Dispose(disposing);
    }
  }
}
