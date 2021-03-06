﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD27
{
    class Engine : IDisposable
    {
        private GraphicsDevice GraphicsDevice;
        private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private Matrix worldMatrix;
        private VertexBuffer vertexBuffer;
        
        private Dictionary<string, PositionedQuad> namedQuads;
        private Microsoft.Xna.Framework.Content.ContentManager Content;
        private Dictionary<string, SpriteSheet> sprites;

        private Dictionary<string, SoundEffect> Sounds;

        public Vector3 Camera { get; set; }

        public Vector3 Target { get; set; }

        public Dictionary<string, Texture2D> Textures { get; set; }

        public BasicEffect Effect { get; set; }

        public WorldMap WorldMap { get; set; }

        private float aspect;

        public Engine(GraphicsDevice GraphicsDevice)
        {
            // TODO: Complete member initialization
            this.GraphicsDevice = GraphicsDevice;
            this.Initialize();
        }

        public Engine(GraphicsDevice GraphicsDevice, Microsoft.Xna.Framework.Content.ContentManager Content)
        {
            this.GraphicsDevice = GraphicsDevice;
            this.Content = Content;
            this.Initialize();
        }

        private List<PositionedQuad> textQuads;

        private void Initialize()
        {            
            this.textQuads = new List<PositionedQuad>();
            this.sprites = new Dictionary<string, SpriteSheet>();
            this.namedQuads = new Dictionary<string, PositionedQuad>();
            this.Sounds = new Dictionary<string, SoundEffect>();
            this.PlayingSounds = new Dictionary<string, SoundEffectInstance>();

            aspect = GraphicsDevice.Viewport.AspectRatio;

            CreateEffect();

            RasterizerState state = new RasterizerState();
            state.CullMode = CullMode.None;
            state.FillMode = FillMode.Solid;
            GraphicsDevice.RasterizerState = state;
            DepthStencilState depthBufferState = new DepthStencilState();
            depthBufferState.DepthBufferEnable = true;
            GraphicsDevice.DepthStencilState = depthBufferState;
            
            
            Textures = new Dictionary<string, Texture2D>();
            Camera = new Vector3(0, 0, 0.2f);
            Target = new Vector3(0, 0, -1);
            //vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(VertexPositionColor), this.namedQuads.Count * 6, BufferUsage.WriteOnly,);
        }

        public Vector2 GetScreenUpperLeft() {
            //FIXME : not fully implemented
            return new Vector2(Camera.X, Camera.Y);
        }

        private void CreateEffect()
        {
            Effect = new BasicEffect(this.GraphicsDevice);
            //Effect.EnableDefaultLighting();
            /*
            Effect.DirectionalLight0.Enabled = false;
            Effect.DirectionalLight1.Enabled = false;
            Effect.DirectionalLight2.Enabled = false;
             */
        }
        public Dictionary<string, SoundEffectInstance> PlayingSounds {get; set; }

        public SoundEffectInstance PlaySound(string sound) {
            if (!Sounds.ContainsKey(sound)) { return null; }
            SoundEffectInstance instance;
            if (!PlayingSounds.ContainsKey(sound))
            {
                instance = Sounds[sound].CreateInstance();
                PlayingSounds[sound] = instance;
            }
            else {
                instance = PlayingSounds[sound];
            }
            instance.Volume = 0.3f;
            instance.IsLooped = false;
            instance.Play();            
            return instance;
        }

        private float  defaultScale = 64f / 480f;
        //private float defaultScale = 10f;
        
        private Vector2 MapAspect;

        internal void AddSound(string name, string resource) 
        { //workaround for Content.Load<SoundEffect> not working.
            var f = System.IO.File.Exists("Content\\" + resource + ".wav");
            var path = System.IO.Directory.GetCurrentDirectory();
            Console.WriteLine(path);

            SoundEffect file = null;            
            if (f)
            {
                file = SoundEffect.FromStream(System.IO.File.OpenRead("Content\\" + resource + ".wav"));
                file.Name = name;                
                int i = file.Duration.Milliseconds;
                Console.WriteLine("lenght:", i);
            }
            else {
                throw new Exception("File not found: " + resource);
            }


            Sounds.Add(name, file);            
        }

        /**
         * LoadContent
         */
        internal void LoadContent()
        {
            int screenw = GraphicsDevice.Viewport.Bounds.Width;
            int screenh = GraphicsDevice.Viewport.Bounds.Height;
            //load textures
            Textures.Add("test", Content.Load<Texture2D>("testTexture"));
            Textures.Add("bmpFont", Content.Load<Texture2D>("bmpFont"));
            Textures.Add("playerspritesheet", Content.Load<Texture2D>("playerspritesheet"));
            Textures.Add("youmaspritesheet", Content.Load<Texture2D>("youmaspritesheet"));
            Textures.Add("tilesheet", Content.Load<Texture2D>("tilesheet"));
            Textures.Add("sfx", Content.Load<Texture2D>("specialeffects"));
            Textures.Add("misc", Content.Load<Texture2D>("misctiles"));

            // load sounds                        
            AddSound("timer", "Timer");
            AddSound("hurt", "Hit_Hurt");
            AddSound("attack", "Laser_Shoot");
            AddSound("flame", "Flame2");
            AddSound("explosion", "Explosion");
            AddSound("beam", "Laser2");
            AddSound("heal", "Heart");
            AddSound("seal", "Sign");


            NewMapTexture(screenw, screenh);

            var cards = AddSpriteSheet("cards", Textures["misc"], Vector2.Zero, true, true);
            cards.SetTileSize(64, 128);
            cards.DefineAnimation("bomb", new int[] { 24 });
            cards.DefineAnimation("heal", new int[] { 25 });
            //cards.DefineAnimation("heal", new int[] { 6*8+1 });
            cards.DefineAnimation("sign", new int[] { 26 });
                                    
            var misc = AddSpriteSheet("misc", Textures["misc"], Vector2.Zero, true, false);
            misc.SetTileSize(128, 128);
            misc.Delay = 0.1f;
            misc.DefineAnimation("bigportal", Enumerable.Range(4, 4).ToArray());
            misc.DefineAnimation("bigportal_destroyed", new int[] { 8, 9 }, 0.200f, true, 0.5f);
            //misc.Animations.Add(misc.AnimationDefinitions.First().Value.Copy());
            
            
            var player = AddSpriteSheet("player", Textures["playerspritesheet"], WorldMap.Player.Location);
            player.DefineAnimation("idle", new int[] { 0, 1 });
            player.DefineAnimation("attack", Enumerable.Range(16, 5).ToArray(), 0.08f, true);
            player.AddAnimation("idle", WorldMap.Viewport, WorldMap.Player.ID);
            
            var tiles = AddSpriteSheet("tiles", Textures["tilesheet"], Vector2.Zero, false, false);
            tiles.DefineAnimation("test", Enumerable.Range(0, 64).ToArray());
            tiles.DefineAnimation("portal", new int[] { 8 });
            //tiles.Animation = "test";
            

            var enemies = AddSpriteSheet("enemies", Textures["youmaspritesheet"], Vector2.Zero, false, false);
            enemies.Delay = 0.5f;
            enemies.DefineAnimation ("small", new int[] { 0, 1}, 0.5f, true, 0.8f);
            enemies.DefineAnimation("medium", new int[] { 8, 9 });
            enemies.DefineAnimation("large", new int[] { 16, 17 }, 0.5f, true, 1.3f);
            enemies.DefineAnimation("itcomes", new int[] { 24, 25, 26 }, 0.4f, true, 1.5f);
            enemies.DefineAnimation("boss", new int[] { 24, 25, 26 }, 0.4f, true, 2.5f);
            enemies.DefineAnimation("small_dead", new int[] { 2, 3 }, 0.5f, false);
            enemies.DefineAnimation("medium_dead", new int[] { 4, 5 }, 0.5f, false);
            enemies.DefineAnimation("large_dead", new int[] { 18, 19 }, 0.5f, false);
            enemies.DefineAnimation("itcomes_dead", new int[] { 27, 28 }, 0.5f, false);
            enemies.DefineAnimation("boss_dead", new int[] { 27,28}, 0.4f, true, 2.5f);
            
            enemies.DefineAnimation("test", Enumerable.Range(0, 64).ToArray());           
            //enemies.Animation  ="test";            
            
            var timer = AddSpriteSheet("timer", Textures["bmpFont"], FixedPixelPositionToVector2(screenw / 2, 32));
            
            var sfx = AddSpriteSheet("sfx", Textures["sfx"], Vector2.Zero, false, false);
            sfx.Delay = 0.200f;
            sfx.DefineAnimation("row1", Enumerable.Range(0, 4).ToArray(), 0.200f, false);
            sfx.DefineAnimation("row2", Enumerable.Range(7,6).ToArray(), 0.150f, false, 1.5f, 0.7f);
            sfx.DefineAnimation("bloody", Enumerable.Range(15, 5).ToArray(), 0.080f, false, 1, 0.8f);
            sfx.DefineAnimation("explosion", Enumerable.Range(0, 7).ToArray(), 0.04f, false, 7, 0.7f);
            sfx.DefineAnimation("beam", Enumerable.Range(23, 8).ToArray(), 0.200f, false, 1, 0.7f);
            //sfx.Animation = "row2";

            
            timer.DefineAnimation("timer", Enumerable.Range(32, 10).ToArray(), 1f);
            System.Diagnostics.Debug.Assert(timer.AnimationDefinitions["timer"].FrameIndexes.Contains(41));            
            timer.Animation = "timer";

            //vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, namedQuads.Count * 6, BufferUsage.WriteOnly);
        }

        private void NewMapTexture(int screenw, int screenh)
        {            
            Textures["mapRender"] =  WorldMap.GetMapImage();
            Effect.Texture = Textures["mapRender"];

            float testScale = 64f / (screenh);
            //defaultScale = testScale;
            defaultScale = 0.08f;

            var map = Textures["mapRender"];
            //var mapx = (map.Width > screenw) ? Textures["mapRender"].Width / screenw : 1;
            //var mapy = (map.Height > screenh) ? Textures["mapRender"].Height / screenh : 1;
            var mapx = Textures["mapRender"].Width / (float)screenw;
            var mapy = Textures["mapRender"].Height / (float)screenh;

            MapAspect = new Vector2(mapx / mapy, mapy / mapx);

            var loc = this.WorldMap.WorldMapLocationToVector2(this.WorldMap.Player.Location);

            namedQuads["map"] = new PositionedQuad(aspect * mapx, 1f * mapy)
            {
                Texture = Textures["mapRender"],
                Position = new Vector2(0,0),
                Show = true
            };
            Camera = new Vector3(loc.X, loc.Y - 0.2f, Camera.Z);
            Target = new Vector3(loc.X, loc.Y, -1000);
           
            WorldMap.MapChanged = false;
        }
        

        private SpriteSheet AddSpriteSheet(string name, Texture2D texture, Vector2 position, bool animated = true, bool show = true)
        {
            //spritesheet is by default a 8x8 grid, so.... 
            // 
            sprites.Add(name, new SpriteSheet(texture, GraphicsDevice) { 
                //ScaleX = aspect, 
                ScaleX = aspect*defaultScale*8,
                ScaleY = 1*defaultScale*8,                      
            }); // , 64, 64, 8, aspect, 1           
            var sprite = sprites.Values.Last();
            sprite.GenerateTiles();
            //sprite.DefineAnimation("default", new int[] { 0, 1 });
            //sprite.DefineAnimation("test1", new int[] { 9 });
            /*if (show)
            {
                sprite.Animation = sprite.AnimationDefinitions.Keys.First();
            } */         
            return sprite;
        }

        internal Vector2 PixelPositionToVector2(int x, int y) {
            float fx = 0;
            float fy = 0;
            
            fx = (float)(aspect * ((2.0 * (double)x / (double)GraphicsDevice.Viewport.Bounds.Width)));
            fy = (float)(-2.0 * (double)y / (double)GraphicsDevice.Viewport.Bounds.Height);
            
            return new Vector2(fx-aspect, fy+1);

        }

        //float newAngle = 0;

        /*
         * Draw
        */
        internal void Draw(GraphicsDevice GraphicsDevice, Microsoft.Xna.Framework.GameTime gameTime)
        {
            //var ticks = DateTime.Now.Ticks;
            int screenw = GraphicsDevice.Viewport.Bounds.Width;
            int screenh = GraphicsDevice.Viewport.Bounds.Height;            
            GraphicsDevice.SetVertexBuffer(null);
            List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();
            
            float seconds = (float)gameTime.TotalGameTime.TotalSeconds - prevSeconds;            
            if (seconds > 10.0f)
            {
                prevSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
            }
            delta = gameTime.TotalGameTime.TotalSeconds;

            // portals rendered in wrong spots; why?  
            foreach (var portal in WorldMap.Portals) {
                //Console.WriteLine("Portal: " + portal.ToString());
                //var v = PixelPositionToVector2((int)((portal.Location.X - WorldMap.X)), (int)((portal.Location.Y - WorldMap.Y)));
                /*var v = PixelPositionToVector2(
                    (int)((portal.Location.X- (WorldMap.MapWidth/2))), 
                    (int)((portal.Location.Y- (WorldMap.MapHeight/2)))
                    );*/
                var v = this.WorldMap.WorldMapLocationToVector2(portal.Location);
                if (portal.isOpen) {                    
                    sprites["tiles"].AddAnimation("portal", v, portal.ID);                     
                }
                if (portal.Destroyed) {
                    sprites["misc"].AddAnimation("bigportal_destroyed", v, portal.ID);
                }
            }
            sprites["tiles"].PruneUnusedAnimations(WorldMap.Portals.Where((x)=>(x.isOpen)).Select((x)=>(x.ID)));
            
            var playerLocation = WorldMap.WorldMapLocationToVector2(WorldMap.Player.Location);
            //playerLocation = new Vector2(loc.X/aspect, loc.Y);
            //renderQuads.AddRange(sprites.Values);
            var player = sprites["player"];

            if (WorldMap.Player.Is("attacking"))
            {                
                if (player.Animation == "idle")
                {
                    player.Animations.Clear();
     
                }
                player.AddAnimation("attack", playerLocation, WorldMap.Player.ID);             
            }
            else 
            {  
                player.Animations.Clear();
                player.AddAnimation("idle", playerLocation, WorldMap.Player.ID);
            }
            
            //sprites["player"].PruneUnusedAnimations(new int [] {9999});
            //sprites["player"].PruneUnusedAnimations(new int[] { WorldMap.Player.ID });




            var enemies = sprites["enemies"];
            enemies.CameraPosition = Camera;
            //enemies.Current = 0;
            //enemies.SetTileToCurrent(GraphicsDevice);            
            sprites["enemies"].PruneUnusedAnimations(WorldMap.Creatures.Select((i) => (i.ID)), true);
            
            foreach (var enemy in WorldMap.Creatures) {
                var enemylocation = WorldMap.WorldMapLocationToVector2(enemy.Location);
                //var enemylocation = PixelPositionToVector2((int)(enemy.Location.X - WorldMap.X), (int)(enemy.Location.Y - WorldMap.Y));
                if (enemy.Is("hurt")) {
                    sprites["sfx"].AddAnimation("bloody", enemylocation, enemy.ID);
                    enemy.Set("hurt", enemy.Get("hurt")-0.1f);
                }

                string type = string.Empty;
                switch (enemy.Type) { 
                    case Creature.Types.PLAYER:
                        // do nothing as player is rendered separately.
                        break;

                    case Creature.Types.SMALL:
                        type = "small";
                        break;
                    case Creature.Types.MEDIUM:
                        type = "medium";
                        break;
                    case Creature.Types.LARGE:
                        type = "large";
                        break;
                    case Creature.Types.BEWARE:
                        type = "itcomes";
                        break;
                    case Creature.Types.BOSS:
                        type = "boss";
                        break;

                    default:
                 
                        break;
                }
                if (!type.Equals(string.Empty))
                {
                    bool reset = false;
                    if (enemy.Is("dead"))
                    {
                        type = string.Format("{0}_dead", type);
                        enemy.Set("dead", enemy.Get("dead") - 0.1f);                        
                        reset = true;
                        enemies.AddAnimation(type, enemylocation, enemy.ID, reset);
                    }
                    else
                    {

                        enemies.AddAnimation(type, enemylocation, enemy.ID, reset);
                    }
                }                
                
            }            
            

            foreach (var loc in WorldMap.Locations) {
                if (WorldMap.EndGame && loc.Type == "EndGame") {
                    var v = WorldMap.WorldMapLocationToVector2(new Vector2(loc.X, loc.Y));
                    var misc = sprites["misc"];
                    misc.AddAnimation("bigportal", v, 1234);                    
                    }
            }
            //sprites["misc"].PruneUnusedAnimations(WorldMap.Locations.Select((i) => (i.ID)));

            var sfx = sprites["sfx"];
            //Console.WriteLine("sfx count: {0}, creature count: {1}", WorldMap.Forces.Count, WorldMap.Creatures.Count);
            foreach (var force in WorldMap.Forces) { 
                //var v = PixelPositionToVector2((int)(force.Location.X), (int)(force.Location.Y));
                var v = WorldMap.WorldMapLocationToVector2(force.Location);
                sfx.Delay = 0.250f;
                string type = string.Empty;
                switch (force.Visual) { 
                    case Force.Visuals.Test:
                        type = "row1";
                        break;
                    case Force.Visuals.Beam:
                        type = "beam";
                        break;
                    case Force.Visuals.Test2:
                        type = "row2";
                        break;
                    case Force.Visuals.Bloody:
                        type = "bloody";
                        v = FixedPixelPositionToVector2(screenw / 2, screenh / 2);
                        break;
                    case Force.Visuals.Explosion:
                        type = "explosion";
                        //v = TenGame.AdjustVector2(WorldMap.Viewport, TenGame.screenw / 2, TenGame.screenh / 2);
                        break;                    
                    default:
                        sfx.Animation = string.Empty;
                        break;
                }
            
                var sfxanim = sfx.AddAnimation(type, v, force.ID);
                var atk = force as Attack;

                if (null != atk) {
                    sfxanim.Angle = atk.Direction;
                }

                if (force.IsApplied) {                    
                    if (!sfxanim.Playing) {
                        force.Remove = true;
                    }                                        
                }
            }
            sprites["sfx"].PruneUnusedAnimations(WorldMap.Forces.Select((i) => (i.ID)));
            
          

            sprites["cards"].ClearAnimations();
            int xshift = 0;
            foreach (var card in WorldMap.Player.Cards) {
                string type = string.Empty;
                switch (card.Type) { 
                    case Card.Types.Bomb:
                        type = "bomb";
                        break;
                    case Card.Types.Heal:
                        type = "heal";
                        break;
                    case Card.Types.Sign:
                        type = "sign";
                        break;
                }
                if (type != string.Empty) {
                    sprites["cards"].AddAnimation(type, FixedPixelPositionToVector2((64*xshift)+64, screenh-67), xshift);
                }
                xshift++;
            }            


            verts.AddRange(namedQuads["map"].Vertices);

            sprites["cards"].AddAnimation("sign", new Vector2(0.0f, 0.0f));

            sprites["cards"].AddAnimation("heal", new Vector2(0.2f, 0.0f));


            sprites["cards"].AddAnimation("bomb", FixedPixelPositionToVector2(screenw - 64, screenh - 64));

            foreach (var sprite in sprites.Values) {
                foreach (var anim in sprite.Animations) {
                    var tile = sprite.GetPositionedTile(sprite.Tiles[anim.getNextAllowedIndex((float)gameTime.TotalGameTime.TotalSeconds)], anim.Position, Camera.Z);
                    verts.AddRange(tile);
                }
            }

            if (null != vertexBuffer)
            {
                if (vertexBuffer.VertexCount < verts.Count)
                {
                    //overflow      
                    Console.WriteLine("Vertexcount:" + verts.Count);
                    vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), verts.Count * 10, BufferUsage.WriteOnly); 
                }
                else
                {
                    vertexBuffer.SetData(verts.ToArray());
                }
            }
            
            //vertexBuffer.SetData<VertexPositionNormalTexture>(0, verts.ToArray(), 0, verts.Count, 32);

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SetVertexBuffer(vertexBuffer);                       

            SetupEffect(); 
            // foreach object in some list 
            if (sprites["timer"].Current == 41)
            {
                this.WorldMap.Disaster = true;
                seconds = 10;
                //sprites["timer"].isAnimated = false;
            }            

            foreach (var quad in namedQuads.Values)
            {
                if (quad.Show)
                {
                    RenderVertices(GraphicsDevice, quad.Texture, quad.Vertices);
                }
            }

            foreach (var kvpair in sprites) {                
                var sheet = kvpair.Value;
                //Console.WriteLine("Animations for {0}: {1}", kvpair.Key, sheet.Animations.Count);
                foreach (var anim in sheet.Animations)
                {
                    RenderVertices(GraphicsDevice, sheet.Sheet, 
                        sheet.GetPositionedTile(sheet.Tiles[anim.CurrentFrame], anim.Position, anim.Scale, anim.Angle, Camera.Z), 
                        anim.Opacity);
                }
            }

            //Console.WriteLine("Engine Draw time: {0}", DateTime.Now.Ticks - ticks);
        }

        private Vector2 FixedPixelPositionToVector2(int x, int y)
        {
            float fx = 0;
            float fy = 0;

            fx = ((Camera.X / (5 * Camera.Z)) + (float)(aspect * (2.0 * (double)x / (double)GraphicsDevice.Viewport.Bounds.Width)));
            fy = ((Camera.Y / (5 * Camera.Z)) + ((float)(-2.0 * (double)y / (double)GraphicsDevice.Viewport.Bounds.Height)));
            return new Vector2((fx - aspect)* (Camera.Z*5), (fy + 1) * Camera.Z*5);

        }        

        private void RenderVertices(Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice,Texture2D texture2D,VertexPositionNormalTexture[] vertices, float alpha = 1f)
        {
 	        Effect.Texture = null;
            Effect.Texture = texture2D;
            if (alpha < 1f) {
                Effect.GraphicsDevice.BlendState = BlendState.Additive;
            }
            Effect.Alpha = alpha;
            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, vertices, 0, 2);
            }
        } 
        
        
        private void SetupEffect()
        {
            Effect.View = viewMatrix;
            Effect.Projection = projectionMatrix;
            Effect.World = worldMatrix;
            Effect.AmbientLightColor = Color.White.ToVector3();
            Effect.TextureEnabled = true;
            
        }

        float prevSeconds = 0;
        double delta = 0;
        /**
         *  Update function
         *  
         */
        internal void Update(GameTime gameTime)
        {
            //var ticks = DateTime.Now.Ticks;
            this.viewMatrix = Matrix.CreateLookAt(Camera, Target, Vector3.Up);
            this.worldMatrix = Matrix.CreateWorld(Vector3.Backward, Vector3.Forward, Vector3.Up);
            //this.projectionMatrix = Matrix.CreatePerspective(GraphicsDevice.Viewport.AspectRatio, 1.0f, 0.1f, 100.0f);
            this.projectionMatrix = Matrix.CreateOrthographic(2f * GraphicsDevice.Viewport.AspectRatio, 2f, 0.1f, 100f);
            

            int screenw = GraphicsDevice.Viewport.Bounds.Width;
            int screenh = GraphicsDevice.Viewport.Bounds.Height;
            if (WorldMap.MapChanged) {
                NewMapTexture(screenw, screenh);
            }

            //if (gameTime.TotalGameTime.TotalSeconds - delta > 0.1)
            //{

            //}
            //namedQuads["map"].Position = new Vector2(Camera.X, Camera.Y);

            WorldMap.Player.Location = WorldMap.ConvertLocationToPixelPosition(new Vector2(Camera.X / aspect, Camera.Y));

            sprites["timer"].Position = FixedPixelPositionToVector2(screenw / 2, 32);
            var seconds = (float)gameTime.TotalGameTime.TotalSeconds - prevSeconds;
            if (WorldMap.Update(seconds, gameTime)) {
                prevSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
                PlaySound("timer");
            }
            if (!WorldMap.Disaster) {
                //sprites["timer"].isAnimated = true;
            }

            //if (null != vertexBuffer) { vertexBuffer.Dispose(); vertexBuffer = null;  }
            var quadCount = this.namedQuads.Count;
            foreach(var sprite in sprites) {
                quadCount += sprite.Value.Animations.Count;
            }

            //6 for quad, 2 for normal, 2 for texture
            vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), quadCount * (6+2+2), BufferUsage.WriteOnly);

            //Console.WriteLine("Engine Update time: {0}", DateTime.Now.Ticks - ticks);
               
            if (WorldMap.Player.Is("hurt")) {
                sprites["sfx"].AddAnimation("bloody", FixedPixelPositionToVector2(screenw/2, screenh/2), 9998);          
                PlaySound("hurt");
                WorldMap.Player.Set("hurt", 0);
            }
            if (WorldMap.Forces.Count > 0) {
                foreach (var force in WorldMap.Forces)
                {
                    string sound = String.Empty;
                    switch (force.Sound) {
                        case Force.Sounds.Default:
                            sound = "attack";
                            break;
                        case Force.Sounds.Flame:
                            sound = "flame";
                            break;
                        case Force.Sounds.Beam:
                            //sound = "beam";
                            break;
                        case Force.Sounds.Explosion:
                            sound = "explosion";
                            break;
                    }

                    if (null == attackSound) {
                        attackSound = new Dictionary<Force.Sounds, SoundEffectInstance>();
                    
                    }

                    if (attackSound.ContainsKey(force.Sound))
                    {
                        if (attackSound[force.Sound].State == SoundState.Stopped)
                        {
                            attackSound[force.Sound].Play();
                        }
                    }
                    else
                    {
                        if (!sound.Equals(string.Empty))
                        {
                            attackSound.Add(force.Sound, PlaySound(sound));
                        }
                    }        
                }
            }
        }

        Dictionary<Force.Sounds, SoundEffectInstance> attackSound=null;

        public void Dispose() {
            Dispose(true);
        }

        ~Engine() {
            Dispose(false);
        }

        protected virtual void Dispose(bool Disposing) {
            if (Disposing) {
                this.vertexBuffer.Dispose();
                this.vertexBuffer = null;
            }
        
        }

        internal PositionedQuad Write(string text, int screenx, int screeny, Vector3 rotation, float scale = 1)
        {
            Texture2D tex = Textures["bmpFont"];
            int columns = 8;
            int pixelwidth = 64;
            Color[] data = new Color[tex.Width * tex.Height];
            //useless padding.
            Color[] letters = new Color[(64 * text.Length) * 64];
            tex.GetData<Color>(data);
            int letterindex = 0;
            foreach (char letter in text.ToLower())
            {
                int gid = 0;
                if (Char.IsNumber(letter))
                {
                    gid = 31 + (int)Char.GetNumericValue(letter);
                }
                else if (letter >= 'a')
                {
                    gid = letter - 'a';
                }
                else if (letter.Equals(' '))
                {
                    gid = 63;
                }
                else
                { //period
                    gid = 30;
                }

                //int xmod = (gid % columns) * pixelwidth;
                //int ymod = (int)(Math.Floor((float)gid / columns)) * (tex.Width);
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < pixelwidth; x++)
                    {
                        //int dataindex = x + xmod + ((int)y * ymod);
                        //int dataindex = x + xmod + (y * tex.Width) + (ymod * 64);
                        int dataindex = x + (gid % columns) * pixelwidth + (y * tex.Width) + ((int)(Math.Floor((float)gid / columns)) * (tex.Width) * 64);
                        int stringTextureIndex = (y * (64 * text.Length)) + (64 * letterindex) + x;
                        letters[stringTextureIndex] = data[dataindex];
                    }

                }
                letterindex++;
            }

            Texture2D texture = new Texture2D(GraphicsDevice, 64 * text.Length, 64);
            texture.SetData<Color>(letters);

            PositionedQuad pq = new PositionedQuad(scale, scale / text.Length)
            {
                Texture = texture,
                Position = PixelPositionToVector2(screenx, screeny)
            };
            return pq;
            //this.positionedQuads.Add(pq);
        }


    }
}
