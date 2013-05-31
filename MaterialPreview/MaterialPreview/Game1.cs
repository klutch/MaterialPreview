using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using StasisCore;

namespace MaterialPreview
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private MaterialRenderer _materialRenderer;
        private SpriteFont _font;
        private int _selectedIndex = 0;
        private Texture2D _pixel;
        private KeyboardState newKeyState;
        private KeyboardState oldKeyState;
        private bool _inMaterialMenu;
        private string _helpText;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _helpText = @"Material Previewer
---------------------------------
  F1 - Toggle material menu
  F2 - Reload material definitions";
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 512;
            _graphics.PreferredBackBufferWidth = 512;
            base.Initialize();

            ResourceManager.initialize(GraphicsDevice);
            ResourceManager.rootDirectory = @"D:/StasisResources/";
            loadMaterials();

            _materialRenderer = new MaterialRenderer(GraphicsDevice, Content, _spriteBatch);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("arial");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData<Color>(new[] { Color.White });
        }

        protected override void UnloadContent()
        {
        }

        private void loadMaterials()
        {
            using (FileStream stream = new FileStream(ResourceManager.materialPath, FileMode.Open))
                ResourceManager.loadAllMaterials(stream);
        }

        private void drawMaterialsMenu()
        {
            Vector2 offset = new Vector2(22, 46);
            float ySpacing = 15;

            _spriteBatch.DrawString(_font, "Material Menu (press F1 to close)\n----------------------------", new Vector2(16, 16), Color.White);

            for (int i = 0; i < ResourceManager.materialResources.Count; i++)
            {
                XElement data = ResourceManager.materialResources[i];
                Vector2 position = offset + new Vector2(0, ySpacing * i);
                string text = data.Attribute("uid").Value;

                if (_selectedIndex == i)
                {
                    int textWidth = (int)_font.MeasureString(text).X;
                    int textHeight = (int)_font.MeasureString(text).Y;

                    _spriteBatch.Draw(_pixel, position, new Rectangle(0, 0, textWidth, textHeight), Color.DarkRed);
                }

                _spriteBatch.DrawString(_font, text, position, Color.White);
            }
        }

        private void drawHelpText()
        {
            Vector2 offset = new Vector2(16, 16);
            _spriteBatch.DrawString(_font, _helpText, offset, Color.White);
        }

        protected override void Update(GameTime gameTime)
        {
            oldKeyState = newKeyState;
            newKeyState = Keyboard.GetState();

            if (_inMaterialMenu)
            {
                // Material menu
                if (newKeyState.IsKeyDown(Keys.Up) && oldKeyState.IsKeyUp(Keys.Up))
                    _selectedIndex--;
                if (newKeyState.IsKeyDown(Keys.Down) && oldKeyState.IsKeyUp(Keys.Down))
                    _selectedIndex++;
                if (newKeyState.IsKeyDown(Keys.F1) && oldKeyState.IsKeyUp(Keys.F1))
                    _inMaterialMenu = false;
            }
            else
            {
                // Help menu
                if (newKeyState.IsKeyDown(Keys.F1) && oldKeyState.IsKeyUp(Keys.F1))
                    _inMaterialMenu = true;
                if (newKeyState.IsKeyDown(Keys.F2) && oldKeyState.IsKeyUp(Keys.F2))
                    loadMaterials();
            }

            // Validate selected material
            if (_selectedIndex < 0)
                _selectedIndex = ResourceManager.materialResources.Count - 1;
            else if (_selectedIndex > ResourceManager.materialResources.Count - 1)
                _selectedIndex = 0;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            if (_inMaterialMenu)
                drawMaterialsMenu();
            else
                drawHelpText();

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
