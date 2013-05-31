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
using StasisCore.Models;
using Poly2Tri;

namespace MaterialPreview
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        public const float SCALE = 35f;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private MaterialRenderer _materialRenderer;
        private SpriteFont _font;
        private int _selectedIndex = 0;
        private Texture2D _pixel;
        private KeyboardState newKeyState;
        private KeyboardState oldKeyState;
        private Texture2D _materialTexture;
        private List<Vector2> _polygonPoints;
        private List<Vector2> _screenPoints;
        private bool _drawOnPolygon;
        private Effect _primitives;
        private VertexPositionTexture[] _polygonVertices;
        private VertexPositionTexture[] _screenVertices;
        private int _screenPrimitiveCount;
        private int _polygonPrimitiveCount;
        private bool _hideMenu;
        private string _menuText;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _menuText = 
@"F1 -- Hide/show this menu
F2 -- Reload material definitions
Space -- Toggle polygon shape
Enter -- Render material
-------------------------------------";
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 512;
            _graphics.PreferredBackBufferWidth = 512;
            base.Initialize();

            ResourceManager.initialize(GraphicsDevice);
            ResourceManager.rootDirectory = @"D:/StasisResources/";
            loadMaterials();

            // Initialize rectangular points
            float scaledHalfWidth = (float)(GraphicsDevice.Viewport.Width / SCALE) * 0.5f;
            float scaledHalfHeight = (float)(GraphicsDevice.Viewport.Height / SCALE) * 0.5f;
            _screenPoints = new List<Vector2>();
            _screenPoints.Add(new Vector2(-scaledHalfWidth, -scaledHalfHeight));
            _screenPoints.Add(new Vector2(scaledHalfWidth, -scaledHalfHeight));
            _screenPoints.Add(new Vector2(scaledHalfWidth, scaledHalfHeight));
            _screenPoints.Add(new Vector2(-scaledHalfWidth, scaledHalfHeight));

            // Initialize polygon points
            _polygonPoints = new List<Vector2>();
            _polygonPoints.Add(new Vector2(-3.5f, 0));
            _polygonPoints.Add(new Vector2(-1, 1));
            _polygonPoints.Add(new Vector2(0, 3));
            _polygonPoints.Add(new Vector2(2, 2.5f));
            _polygonPoints.Add(new Vector2(3, 0));
            _polygonPoints.Add(new Vector2(4, -1));
            _polygonPoints.Add(new Vector2(3.5f, -3));
            _polygonPoints.Add(new Vector2(1, -3.5f));
            _polygonPoints.Add(new Vector2(0.5f, -3));
            _polygonPoints.Add(new Vector2(-1, -4));
            _polygonPoints.Add(new Vector2(-2.5f, -2.5f));
            _polygonPoints.Add(new Vector2(-3.5f, -3));
            _polygonPoints.Add(new Vector2(-4.5f, -1.5f));

            // Initialize vertices
            _screenVertices = createVerticesFromPoints(_screenPoints, out _screenPrimitiveCount);
            _polygonVertices = createVerticesFromPoints(_polygonPoints, out _polygonPrimitiveCount);

            // Initialize primitives effect
            _primitives.CurrentTechnique = _primitives.Techniques["generic"];
            _primitives.Parameters["world"].SetValue(Matrix.Identity);
            _primitives.Parameters["view"].SetValue(Matrix.CreateScale(new Vector3(SCALE, -SCALE, 1)));
            _primitives.Parameters["projection"].SetValue(Matrix.CreateOrthographic(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 1));

            _materialRenderer = new MaterialRenderer(GraphicsDevice, Content, _spriteBatch);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("arial");
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData<Color>(new[] { Color.White });
            _primitives = Content.Load<Effect>("effects/primitives");
        }

        protected override void UnloadContent()
        {
        }

        private void loadMaterials()
        {
            using (FileStream stream = new FileStream(ResourceManager.materialPath, FileMode.Open))
                ResourceManager.loadAllMaterials(stream);
        }

        private void renderSelectedMaterial()
        {
            XElement data = ResourceManager.materialResources[_selectedIndex];
            Material material = new Material(data);

            _materialTexture = _materialRenderer.renderMaterial(material, _drawOnPolygon ? _polygonPoints : _screenPoints, 1f, false);
        }

        private VertexPositionTexture[] createVerticesFromPoints(List<Vector2> points, out int primitiveCount)
        {
            List<PolygonPoint> p2tPoints = new List<PolygonPoint>();
            Polygon polygon;
            Vector2 topLeft = points[0];
            Vector2 bottomRight = points[0];
            VertexPositionTexture[] vertices;
            int index = 0;

            foreach (Vector2 v in points)
            {
                p2tPoints.Add(new PolygonPoint(v.X, v.Y));
                topLeft = Vector2.Min(v, topLeft);
                bottomRight = Vector2.Max(v, bottomRight);
            }

            polygon = new Polygon(p2tPoints);
            P2T.Triangulate(polygon);
            primitiveCount = polygon.Triangles.Count;
            vertices = new VertexPositionTexture[primitiveCount * 3];

            foreach (DelaunayTriangle triangle in polygon.Triangles)
            {
                Vector2 p1 = new Vector2(triangle.Points[0].Xf, triangle.Points[0].Yf);
                Vector2 p2 = new Vector2(triangle.Points[1].Xf, triangle.Points[1].Yf);
                Vector2 p3 = new Vector2(triangle.Points[2].Xf, triangle.Points[2].Yf);

                vertices[index++] = new VertexPositionTexture(
                    new Vector3(p1, 0),
                    (p1 - topLeft) / (bottomRight - topLeft));
                vertices[index++] = new VertexPositionTexture(
                    new Vector3(p2, 0),
                    (p2 - topLeft) / (bottomRight - topLeft));
                vertices[index++] = new VertexPositionTexture(
                    new Vector3(p3, 0),
                    (p3 - topLeft) / (bottomRight - topLeft));
            }

            return vertices;
        }

        private void drawMaterialsMenu()
        {
            Vector2 offset = new Vector2(16, _font.MeasureString(_menuText).Y + 16);
            float ySpacing = 15;

            _spriteBatch.DrawString(_font, _menuText, new Vector2(17, 17), Color.Black);
            _spriteBatch.DrawString(_font, _menuText, new Vector2(16, 16), Color.White);

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

                _spriteBatch.DrawString(_font, text, position + new Vector2(1, 1), Color.Black);
                _spriteBatch.DrawString(_font, text, position, Color.White);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            oldKeyState = newKeyState;
            newKeyState = Keyboard.GetState();

            // Material menu
            if (newKeyState.IsKeyDown(Keys.Up) && oldKeyState.IsKeyUp(Keys.Up))
                _selectedIndex--;
            if (newKeyState.IsKeyDown(Keys.Down) && oldKeyState.IsKeyUp(Keys.Down))
                _selectedIndex++;
            if (newKeyState.IsKeyDown(Keys.Enter) && oldKeyState.IsKeyUp(Keys.Enter))
                renderSelectedMaterial();
            if (newKeyState.IsKeyDown(Keys.F1) && oldKeyState.IsKeyUp(Keys.F1))
                _hideMenu = !_hideMenu;
            if (newKeyState.IsKeyDown(Keys.F2) && oldKeyState.IsKeyUp(Keys.F2))
                loadMaterials();

            // Toggle polygon shape
            if (newKeyState.IsKeyDown(Keys.Space) && oldKeyState.IsKeyUp(Keys.Space))
            {
                _drawOnPolygon = !_drawOnPolygon;
                renderSelectedMaterial();
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

            // Material
            if (_materialTexture != null)
            {
                _primitives.CurrentTechnique.Passes["textured_primitives"].Apply();
                GraphicsDevice.Textures[0] = _materialTexture;
                if (_drawOnPolygon)
                    GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, _polygonVertices, 0, _polygonPrimitiveCount);
                else
                    GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleList, _screenVertices, 0, _screenPrimitiveCount);
            }

            // GUI
            _spriteBatch.Begin();

            if (!_hideMenu)
                drawMaterialsMenu();

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
