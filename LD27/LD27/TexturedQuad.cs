﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD27
{
    class TexturedQuad
    {
        public Microsoft.Xna.Framework.Graphics.Texture2D Texture { get; set; }

        public Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture[] Vertices { get; set; }

        public TexturedQuad() {
            Vertices = new VertexPositionNormalTexture[6];
            Vertices[0] = genVertice(-1.0f, 1.0f, 0, 0.0f, 0.0f);
            Vertices[1] = genVertice(1.0f, 1.0f, 0, 1.0f, 0.0f);
            Vertices[2] = genVertice(-1.0f, -1.0f, 0, 0.0f, 1.0f);

            Vertices[3] = genVertice(-1.0f, -1.0f, 0, 0.0f, 1.0f);
            Vertices[4] = genVertice(1.0f, 1.0f, 0, 1.0f, 0.0f);
            Vertices[5] = genVertice(1.0f, -1.0f, 0, 1.0f, 1.0f);
        }
        private Vector3 normal = Vector3.Backward;

        private VertexPositionNormalTexture genVertice(float x, float y, float z, float tx, float ty) {
            return new VertexPositionNormalTexture(new Vector3(x, y, z), normal, new Vector2(tx, ty));
        }
    }
}
