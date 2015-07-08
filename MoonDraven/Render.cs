// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Render.cs" company="ChewyMoon">
//   Copyright (C) 2015 ChewyMoon
//   
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//   
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//   
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   The render class allows you to draw stuff using SharpDX easier.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MoonDraven
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using LeagueSharp;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;

    using SharpDX;
    using SharpDX.Direct3D9;

    using Color = System.Drawing.Color;
    using Font = SharpDX.Direct3D9.Font;
    using Rectangle = SharpDX.Rectangle;

    /// <summary>
    ///     The render class allows you to draw stuff using SharpDX easier.
    /// </summary>
    public static class Render
    {
        /// <summary>
        /// The render objects.
        /// </summary>
        private static readonly List<RenderObject> RenderObjects = new List<RenderObject>();

        /// <summary>
        /// The _render visible objects.
        /// </summary>
        private static List<RenderObject> renderVisibleObjects = new List<RenderObject>();

        /// <summary>
        /// The _cancel thread.
        /// </summary>
        private static bool cancelThread;

        /// <summary>
        /// The render objects lock.
        /// </summary>
        private static readonly object RenderObjectsLock = new object();

        /// <summary>
        /// Initializes static members of the <see cref="Render"/> class.
        /// </summary>
        static Render()
        {
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            Drawing.OnDraw += Drawing_OnDraw;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
            var thread = new Thread(PrepareObjects);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// Gets the device.
        /// </summary>
        public static Device Device
        {
            get
            {
                return Drawing.Direct3DDevice;
            }
        }

        /// <summary>
        /// The on screen.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool OnScreen(Vector2 point)
        {
            return point.X > 0 && point.Y > 0 && point.X < Drawing.Width && point.Y < Drawing.Height;
        }

        /// <summary>
        /// The current domain on domain unload.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="eventArgs">
        /// The event args.
        /// </param>
        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            cancelThread = true;
            foreach (var renderObject in RenderObjects)
            {
                renderObject.Dispose();
            }
        }

        /// <summary>
        /// The drawing on on post reset.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void DrawingOnOnPostReset(EventArgs args)
        {
            foreach (var renderObject in RenderObjects)
            {
                renderObject.OnPostReset();
            }
        }

        /// <summary>
        /// The drawing on on pre reset.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void DrawingOnOnPreReset(EventArgs args)
        {
            foreach (var renderObject in RenderObjects)
            {
                renderObject.OnPreReset();
            }
        }

        /// <summary>
        /// The drawing_ on draw.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Device == null || Device.IsDisposed)
            {
                return;
            }

            foreach (var renderObject in renderVisibleObjects)
            {
                renderObject.OnDraw();
            }
        }

        /// <summary>
        /// The drawing_ on end scene.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Device == null || Device.IsDisposed)
            {
                return;
            }

            Device.SetRenderState(RenderState.AlphaBlendEnable, true);

            foreach (var renderObject in renderVisibleObjects)
            {
                renderObject.OnEndScene();
            }
        }

        /// <summary>
        /// The add.
        /// </summary>
        /// <param name="renderObject">
        /// The render object.
        /// </param>
        /// <param name="layer">
        /// The layer.
        /// </param>
        /// <returns>
        /// The <see cref="RenderObject"/>.
        /// </returns>
        public static RenderObject Add(this RenderObject renderObject, float layer = float.MaxValue)
        {
            renderObject.Layer = !layer.Equals(float.MaxValue) ? layer : renderObject.Layer;
            lock (RenderObjectsLock)
            {
                RenderObjects.Add(renderObject);
            }

            return renderObject;
        }

        /// <summary>
        /// The remove.
        /// </summary>
        /// <param name="renderObject">
        /// The render object.
        /// </param>
        public static void Remove(this RenderObject renderObject)
        {
            lock (RenderObjectsLock)
            {
                RenderObjects.Remove(renderObject);
            }
        }

        /// <summary>
        /// The prepare objects.
        /// </summary>
        private static void PrepareObjects()
        {
            while (!cancelThread)
            {
                try
                {
                    Thread.Sleep(1);
                    lock (RenderObjectsLock)
                    {
                        renderVisibleObjects =
                            RenderObjects.Where(obj => obj.Visible && obj.HasValidLayer())
                                .OrderBy(obj => obj.Layer)
                                .ToList();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Cannot prepare RenderObjects for drawing. Ex:" + e);
                }
            }
        }

        /// <summary>
        /// The circle.
        /// </summary>
        public class Circle : RenderObject
        {
            /// <summary>
            /// The _vertices.
            /// </summary>
            private static VertexBuffer vertices;

            /// <summary>
            /// The _vertex elements.
            /// </summary>
            private static VertexElement[] vertexElements;

            /// <summary>
            /// The _vertex declaration.
            /// </summary>
            private static VertexDeclaration vertexDeclaration;

            /// <summary>
            /// The _effect.
            /// </summary>
            private static Effect effect;

            /// <summary>
            /// The _technique.
            /// </summary>
            private static EffectHandle technique;

            /// <summary>
            /// The _initialized.
            /// </summary>
            private static bool initialized;

            /// <summary>
            /// The _offset.
            /// </summary>
            private static Vector3 offset = new Vector3(0, 0, 0);

            /// <summary>
            /// Initializes a new instance of the <see cref="Circle"/> class.
            /// </summary>
            /// <param name="unit">
            /// The unit.
            /// </param>
            /// <param name="radius">
            /// The radius.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="zDeep">
            /// The z deep.
            /// </param>
            public Circle(GameObject unit, float radius, Color color, int width = 1, bool zDeep = false)
            {
                this.Color = color;
                this.Unit = unit;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Circle"/> class.
            /// </summary>
            /// <param name="unit">
            /// The unit.
            /// </param>
            /// <param name="offset">
            /// The offset.
            /// </param>
            /// <param name="radius">
            /// The radius.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="zDeep">
            /// The z deep.
            /// </param>
            public Circle(GameObject unit, Vector3 offset, float radius, Color color, int width = 1, bool zDeep = false)
            {
                this.Color = color;
                this.Unit = unit;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
                this.Offset = offset;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Circle"/> class.
            /// </summary>
            /// <param name="position">
            /// The position.
            /// </param>
            /// <param name="offset">
            /// The offset.
            /// </param>
            /// <param name="radius">
            /// The radius.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="zDeep">
            /// The z deep.
            /// </param>
            public Circle(
                Vector3 position,
                Vector3 offset,
                float radius,
                Color color,
                int width = 1,
                bool zDeep = false)
            {
                this.Color = color;
                this.Position = position;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
                this.Offset = offset;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Circle"/> class.
            /// </summary>
            /// <param name="position">
            /// The position.
            /// </param>
            /// <param name="radius">
            /// The radius.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="zDeep">
            /// The z deep.
            /// </param>
            public Circle(Vector3 position, float radius, Color color, int width = 1, bool zDeep = false)
            {
                this.Color = color;
                this.Position = position;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
            }

            /// <summary>
            /// Gets or sets the position.
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// Gets or sets the unit.
            /// </summary>
            public GameObject Unit { get; set; }

            /// <summary>
            /// Gets or sets the radius.
            /// </summary>
            public float Radius { get; set; }

            /// <summary>
            /// Gets or sets the color.
            /// </summary>
            public Color Color { get; set; }

            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether z deep.
            /// </summary>
            public bool ZDeep { get; set; }

            /// <summary>
            /// Gets or sets the offset.
            /// </summary>
            public Vector3 Offset
            {
                get
                {
                    return offset;
                }

                set
                {
                    offset = value;
                }
            }

            /// <summary>
            /// The on draw.
            /// </summary>
            public override void OnDraw()
            {
                try
                {
                    if (this.Unit != null && this.Unit.IsValid)
                    {
                        DrawCircle(this.Unit.Position + offset, this.Radius, this.Color, this.Width, this.ZDeep);
                    }
                    else if ((this.Position + offset).ToVector2().IsValid())
                    {
                        DrawCircle(this.Position + offset, this.Radius, this.Color, this.Width, this.ZDeep);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Circle.OnEndScene: " + e);
                }
            }

            /// <summary>
            /// The create vertexes.
            /// </summary>
            public static void CreateVertexes()
            {
                const float X = 6000f;
                vertices = new VertexBuffer(
                    Device,
                    Utilities.SizeOf<Vector4>() * 2 * 6,
                    Usage.WriteOnly,
                    VertexFormat.None,
                    Pool.Managed);

                vertices.Lock(0, 0, LockFlags.None).WriteRange(
                    new[]
                        {
                        // T1
                        new Vector4(-X, 0f, -X, 1.0f), new Vector4(), new Vector4(-X, 0f, X, 1.0f), new Vector4(),
                            new Vector4(X, 0f, -X, 1.0f), new Vector4(),

                        // T2
                        new Vector4(X, 0f, X, 1.0f), new Vector4(), new Vector4(-X, 0f, X, 1.0f), new Vector4(),
                            new Vector4(X, 0f, -X, 1.0f), new Vector4()
                        });
                vertices.Unlock();

                vertexElements = new[]
                                      {
                                          new VertexElement(
                                              0,
                                              0,
                                              DeclarationType.Float4,
                                              DeclarationMethod.Default,
                                              DeclarationUsage.Position,
                                              0),
                                          new VertexElement(
                                              0,
                                              16,
                                              DeclarationType.Float4,
                                              DeclarationMethod.Default,
                                              DeclarationUsage.Color,
                                              0),
                                          VertexElement.VertexDeclarationEnd
                                      };

                vertexDeclaration = new VertexDeclaration(Device, vertexElements);



                try
                {
                    var compiledEffect = new byte[]
                                             {
                                                 0x01, 0x09, 0xFF, 0xFE, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                                                 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00,
                                                 0x50, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x4D, 0x61,
                                                 0x74, 0x72, 0x69, 0x78, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0xA4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x43, 0x69, 0x72, 0x63,
                                                 0x6C, 0x65, 0x43, 0x6F, 0x6C, 0x6F, 0x72, 0x00, 0x03, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x52, 0x61, 0x64, 0x69,
                                                 0x75, 0x73, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x07, 0x00, 0x00, 0x00, 0x42, 0x6F, 0x72, 0x64, 0x65, 0x72, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x01, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00,
                                                 0x7A, 0x45, 0x6E, 0x61, 0x62, 0x6C, 0x65, 0x64, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                                                 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x05, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                                                 0x0F, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                                                 0x50, 0x30, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x4D, 0x61, 0x69, 0x6E,
                                                 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                                                 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x78, 0x00, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0xB4, 0x00, 0x00, 0x00, 0xD0, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x00, 0x00, 0x00,
                                                 0xFC, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x0C, 0x01, 0x00, 0x00, 0x28, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0xF4, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0xEC, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x40, 0x01, 0x00, 0x00, 0x3C, 0x01, 0x00, 0x00, 0x0D, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x60, 0x01, 0x00, 0x00, 0x5C, 0x01, 0x00, 0x00,
                                                 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x01, 0x00, 0x00,
                                                 0x7C, 0x01, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0xA0, 0x01, 0x00, 0x00, 0x9C, 0x01, 0x00, 0x00, 0x92, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0xC0, 0x01, 0x00, 0x00, 0xBC, 0x01, 0x00, 0x00,
                                                 0x93, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD8, 0x01, 0x00, 0x00,
                                                 0xD4, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
                                                 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4C, 0x04, 0x00, 0x00,
                                                 0x00, 0x02, 0xFF, 0xFF, 0xFE, 0xFF, 0x38, 0x00, 0x43, 0x54, 0x41, 0x42,
                                                 0x1C, 0x00, 0x00, 0x00, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x02, 0xFF, 0xFF,
                                                 0x03, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20,
                                                 0xA3, 0x00, 0x00, 0x00, 0x58, 0x00, 0x00, 0x00, 0x02, 0x00, 0x05, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x70, 0x00, 0x00, 0x00,
                                                 0x80, 0x00, 0x00, 0x00, 0x02, 0x00, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x8C, 0x00, 0x00, 0x00, 0x70, 0x00, 0x00, 0x00, 0x9C, 0x00, 0x00, 0x00,
                                                 0x02, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00,
                                                 0x70, 0x00, 0x00, 0x00, 0x42, 0x6F, 0x72, 0x64, 0x65, 0x72, 0x00, 0xAB,
                                                 0x00, 0x00, 0x03, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x43, 0x69, 0x72, 0x63,
                                                 0x6C, 0x65, 0x43, 0x6F, 0x6C, 0x6F, 0x72, 0x00, 0x01, 0x00, 0x03, 0x00,
                                                 0x01, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x52, 0x61, 0x64, 0x69, 0x75, 0x73, 0x00, 0x70, 0x73, 0x5F, 0x32, 0x5F,
                                                 0x30, 0x00, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x20,
                                                 0x28, 0x52, 0x29, 0x20, 0x48, 0x4C, 0x53, 0x4C, 0x20, 0x53, 0x68, 0x61,
                                                 0x64, 0x65, 0x72, 0x20, 0x43, 0x6F, 0x6D, 0x70, 0x69, 0x6C, 0x65, 0x72,
                                                 0x20, 0x39, 0x2E, 0x32, 0x39, 0x2E, 0x39, 0x35, 0x32, 0x2E, 0x33, 0x31,
                                                 0x31, 0x31, 0x00, 0xAB, 0xFE, 0xFF, 0x7C, 0x00, 0x50, 0x52, 0x45, 0x53,
                                                 0x01, 0x02, 0x58, 0x46, 0xFE, 0xFF, 0x30, 0x00, 0x43, 0x54, 0x41, 0x42,
                                                 0x1C, 0x00, 0x00, 0x00, 0x8B, 0x00, 0x00, 0x00, 0x01, 0x02, 0x58, 0x46,
                                                 0x02, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x20,
                                                 0x88, 0x00, 0x00, 0x00, 0x44, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00, 0x5C, 0x00, 0x00, 0x00,
                                                 0x6C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x78, 0x00, 0x00, 0x00, 0x5C, 0x00, 0x00, 0x00, 0x42, 0x6F, 0x72, 0x64,
                                                 0x65, 0x72, 0x00, 0xAB, 0x00, 0x00, 0x03, 0x00, 0x01, 0x00, 0x01, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x43, 0x69, 0x72, 0x63, 0x6C, 0x65, 0x43, 0x6F, 0x6C, 0x6F, 0x72, 0x00,
                                                 0x01, 0x00, 0x03, 0x00, 0x01, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x74, 0x78, 0x00, 0x4D, 0x69, 0x63, 0x72, 0x6F,
                                                 0x73, 0x6F, 0x66, 0x74, 0x20, 0x28, 0x52, 0x29, 0x20, 0x48, 0x4C, 0x53,
                                                 0x4C, 0x20, 0x53, 0x68, 0x61, 0x64, 0x65, 0x72, 0x20, 0x43, 0x6F, 0x6D,
                                                 0x70, 0x69, 0x6C, 0x65, 0x72, 0x20, 0x39, 0x2E, 0x32, 0x39, 0x2E, 0x39,
                                                 0x35, 0x32, 0x2E, 0x33, 0x31, 0x31, 0x31, 0x00, 0xFE, 0xFF, 0x0C, 0x00,
                                                 0x50, 0x52, 0x53, 0x49, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0xFE, 0xFF, 0x1A, 0x00, 0x43, 0x4C, 0x49, 0x54, 0x0C, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0xBF,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0xFE, 0xFF, 0x1F, 0x00, 0x46, 0x58, 0x4C, 0x43, 0x03, 0x00, 0x00, 0x00,
                                                 0x01, 0x00, 0x30, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x02, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x40, 0xA0,
                                                 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                                                 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                                 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                                                 0x04, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x10, 0x01, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00,
                                                 0xF0, 0xF0, 0xF0, 0xF0, 0x0F, 0x0F, 0x0F, 0x0F, 0xFF, 0xFF, 0x00, 0x00,
                                                 0x51, 0x00, 0x00, 0x05, 0x06, 0x00, 0x0F, 0xA0, 0x00, 0x00, 0xE0, 0x3F,
                                                 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80, 0xBF, 0x00, 0x00, 0x00, 0x00,
                                                 0x1F, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x07, 0xB0,
                                                 0x05, 0x00, 0x00, 0x03, 0x00, 0x00, 0x08, 0x80, 0x00, 0x00, 0xAA, 0xB0,
                                                 0x00, 0x00, 0xAA, 0xB0, 0x04, 0x00, 0x00, 0x04, 0x00, 0x00, 0x01, 0x80,
                                                 0x00, 0x00, 0x00, 0xB0, 0x00, 0x00, 0x00, 0xB0, 0x00, 0x00, 0xFF, 0x80,
                                                 0x07, 0x00, 0x00, 0x02, 0x00, 0x00, 0x01, 0x80, 0x00, 0x00, 0x00, 0x80,
                                                 0x06, 0x00, 0x00, 0x02, 0x00, 0x00, 0x01, 0x80, 0x00, 0x00, 0x00, 0x80,
                                                 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x01, 0x80, 0x00, 0x00, 0x00, 0x81,
                                                 0x04, 0x00, 0x00, 0xA0, 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x02, 0x80,
                                                 0x00, 0x00, 0x00, 0x81, 0x05, 0x00, 0x00, 0xA1, 0x58, 0x00, 0x00, 0x04,
                                                 0x00, 0x00, 0x02, 0x80, 0x00, 0x00, 0x55, 0x80, 0x06, 0x00, 0x55, 0xA0,
                                                 0x06, 0x00, 0xAA, 0xA0, 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x04, 0x80,
                                                 0x00, 0x00, 0x00, 0x80, 0x05, 0x00, 0x00, 0xA1, 0x58, 0x00, 0x00, 0x04,
                                                 0x00, 0x00, 0x02, 0x80, 0x00, 0x00, 0xAA, 0x80, 0x06, 0x00, 0x55, 0xA0,
                                                 0x00, 0x00, 0x55, 0x80, 0x05, 0x00, 0x00, 0x03, 0x00, 0x00, 0x04, 0x80,
                                                 0x00, 0x00, 0x00, 0x80, 0x06, 0x00, 0x00, 0xA0, 0x58, 0x00, 0x00, 0x04,
                                                 0x00, 0x00, 0x01, 0x80, 0x00, 0x00, 0x00, 0x80, 0x06, 0x00, 0xAA, 0xA0,
                                                 0x06, 0x00, 0x55, 0xA0, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x08, 0x80,
                                                 0x06, 0x00, 0x55, 0xA0, 0x58, 0x00, 0x00, 0x04, 0x00, 0x00, 0x01, 0x80,
                                                 0x01, 0x00, 0x00, 0xA0, 0x00, 0x00, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x80,
                                                 0x05, 0x00, 0x00, 0x03, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0xAA, 0x80,
                                                 0x00, 0x00, 0x00, 0xA0, 0x23, 0x00, 0x00, 0x02, 0x00, 0x00, 0x04, 0x80,
                                                 0x00, 0x00, 0xAA, 0x80, 0x04, 0x00, 0x00, 0x04, 0x00, 0x00, 0x04, 0x80,
                                                 0x03, 0x00, 0xFF, 0xA0, 0x00, 0x00, 0xAA, 0x81, 0x03, 0x00, 0xFF, 0xA0,
                                                 0x58, 0x00, 0x00, 0x04, 0x00, 0x00, 0x02, 0x80, 0x00, 0x00, 0x55, 0x80,
                                                 0x06, 0x00, 0xFF, 0xA0, 0x00, 0x00, 0xAA, 0x80, 0x58, 0x00, 0x00, 0x04,
                                                 0x00, 0x00, 0x08, 0x80, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x55, 0x80,
                                                 0x03, 0x00, 0xFF, 0xA0, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x07, 0x80,
                                                 0x02, 0x00, 0xE4, 0xA0, 0x01, 0x00, 0x00, 0x02, 0x00, 0x08, 0x0F, 0x80,
                                                 0x00, 0x00, 0xE4, 0x80, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x04, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x4C, 0x01, 0x00, 0x00, 0x00, 0x02, 0xFE, 0xFF,
                                                 0xFE, 0xFF, 0x34, 0x00, 0x43, 0x54, 0x41, 0x42, 0x1C, 0x00, 0x00, 0x00,
                                                 0x9B, 0x00, 0x00, 0x00, 0x00, 0x02, 0xFE, 0xFF, 0x01, 0x00, 0x00, 0x00,
                                                 0x1C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x94, 0x00, 0x00, 0x00,
                                                 0x30, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
                                                 0x44, 0x00, 0x00, 0x00, 0x54, 0x00, 0x00, 0x00, 0x50, 0x72, 0x6F, 0x6A,
                                                 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x4D, 0x61, 0x74, 0x72, 0x69, 0x78,
                                                 0x00, 0xAB, 0xAB, 0xAB, 0x03, 0x00, 0x03, 0x00, 0x04, 0x00, 0x04, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x76, 0x73, 0x5F, 0x32, 0x5F, 0x30, 0x00, 0x4D, 0x69, 0x63, 0x72, 0x6F,
                                                 0x73, 0x6F, 0x66, 0x74, 0x20, 0x28, 0x52, 0x29, 0x20, 0x48, 0x4C, 0x53,
                                                 0x4C, 0x20, 0x53, 0x68, 0x61, 0x64, 0x65, 0x72, 0x20, 0x43, 0x6F, 0x6D,
                                                 0x70, 0x69, 0x6C, 0x65, 0x72, 0x20, 0x39, 0x2E, 0x32, 0x39, 0x2E, 0x39,
                                                 0x35, 0x32, 0x2E, 0x33, 0x31, 0x31, 0x31, 0x00, 0x1F, 0x00, 0x00, 0x02,
                                                 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x0F, 0x90, 0x1F, 0x00, 0x00, 0x02,
                                                 0x0A, 0x00, 0x00, 0x80, 0x01, 0x00, 0x0F, 0x90, 0x09, 0x00, 0x00, 0x03,
                                                 0x00, 0x00, 0x01, 0xC0, 0x00, 0x00, 0xE4, 0x90, 0x00, 0x00, 0xE4, 0xA0,
                                                 0x09, 0x00, 0x00, 0x03, 0x00, 0x00, 0x02, 0xC0, 0x00, 0x00, 0xE4, 0x90,
                                                 0x01, 0x00, 0xE4, 0xA0, 0x09, 0x00, 0x00, 0x03, 0x00, 0x00, 0x04, 0xC0,
                                                 0x00, 0x00, 0xE4, 0x90, 0x02, 0x00, 0xE4, 0xA0, 0x09, 0x00, 0x00, 0x03,
                                                 0x00, 0x00, 0x08, 0xC0, 0x00, 0x00, 0xE4, 0x90, 0x03, 0x00, 0xE4, 0xA0,
                                                 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x0F, 0xD0, 0x01, 0x00, 0xE4, 0x90,
                                                 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x0F, 0xE0, 0x00, 0x00, 0xE4, 0x90,
                                                 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0xE0, 0x00, 0x00, 0x00, 0x00, 0x02, 0x58, 0x46, 0xFE, 0xFF, 0x25, 0x00,
                                                 0x43, 0x54, 0x41, 0x42, 0x1C, 0x00, 0x00, 0x00, 0x5F, 0x00, 0x00, 0x00,
                                                 0x00, 0x02, 0x58, 0x46, 0x01, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00,
                                                 0x00, 0x01, 0x00, 0x20, 0x5C, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00, 0x00,
                                                 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00,
                                                 0x4C, 0x00, 0x00, 0x00, 0x7A, 0x45, 0x6E, 0x61, 0x62, 0x6C, 0x65, 0x64,
                                                 0x00, 0xAB, 0xAB, 0xAB, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00,
                                                 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x74, 0x78, 0x00, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74,
                                                 0x20, 0x28, 0x52, 0x29, 0x20, 0x48, 0x4C, 0x53, 0x4C, 0x20, 0x53, 0x68,
                                                 0x61, 0x64, 0x65, 0x72, 0x20, 0x43, 0x6F, 0x6D, 0x70, 0x69, 0x6C, 0x65,
                                                 0x72, 0x20, 0x39, 0x2E, 0x32, 0x39, 0x2E, 0x39, 0x35, 0x32, 0x2E, 0x33,
                                                 0x31, 0x31, 0x31, 0x00, 0xFE, 0xFF, 0x02, 0x00, 0x43, 0x4C, 0x49, 0x54,
                                                 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0x0C, 0x00, 0x46, 0x58, 0x4C, 0x43,
                                                 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x10, 0x01, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                 0xF0, 0xF0, 0xF0, 0xF0, 0x0F, 0x0F, 0x0F, 0x0F, 0xFF, 0xFF, 0x00, 0x00
                                             };
                    effect = Effect.FromMemory(Device, compiledEffect, ShaderFlags.None);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }



                technique = effect.GetTechnique(0);

                if (!initialized)
                {
                    initialized = true;
                    Drawing.OnPreReset += OnPreReset;
                    Drawing.OnPreReset += OnPostReset;
                    AppDomain.CurrentDomain.DomainUnload += Dispose;
                }
            }

            /// <summary>
            /// The on pre reset.
            /// </summary>
            /// <param name="args">
            /// The args.
            /// </param>
            private static void OnPreReset(EventArgs args)
            {
                if (effect != null && !effect.IsDisposed)
                {
                    effect.OnLostDevice();
                }
            }

            /// <summary>
            /// The on post reset.
            /// </summary>
            /// <param name="args">
            /// The args.
            /// </param>
            private static void OnPostReset(EventArgs args)
            {
                if (effect != null && !effect.IsDisposed)
                {
                    effect.OnResetDevice();
                }
            }

            /// <summary>
            /// The dispose.
            /// </summary>
            /// <param name="sender">
            /// The sender.
            /// </param>
            /// <param name="e">
            /// The e.
            /// </param>
            private static void Dispose(object sender, EventArgs e)
            {
                if (effect != null && !effect.IsDisposed)
                {
                    effect.Dispose();
                }

                if (vertices != null && !vertices.IsDisposed)
                {
                    vertices.Dispose();
                }

                if (vertexDeclaration != null && !vertexDeclaration.IsDisposed)
                {
                    vertexDeclaration.Dispose();
                }
            }

            /// <summary>
            /// The draw circle.
            /// </summary>
            /// <param name="position">
            /// The position.
            /// </param>
            /// <param name="radius">
            /// The radius.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="zDeep">
            /// The z deep.
            /// </param>
            public static void DrawCircle(
                Vector3 position,
                float radius,
                Color color,
                int width = 5,
                bool zDeep = false)
            {
                try
                {
                    if (Device == null || Device.IsDisposed)
                    {
                        return;
                    }

                    if (vertices == null)
                    {
                        CreateVertexes();
                    }

                    if (vertices == null || vertices.IsDisposed || vertexDeclaration.IsDisposed || effect.IsDisposed
                        || technique.IsDisposed)
                    {
                        return;
                    }

                    var olddec = Device.VertexDeclaration;

                    effect.Technique = technique;

                    effect.Begin();
                    effect.BeginPass(0);
                    effect.SetValue(
                        "ProjectionMatrix",
                        Matrix.Translation(new Vector3(position.X, position.Z, position.Y)) * Drawing.View
                        * Drawing.Projection);
                    effect.SetValue(
                        "CircleColor",
                        new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
                    effect.SetValue("Radius", radius);
                    effect.SetValue("Border", 2f + width);
                    effect.SetValue("zEnabled", zDeep);

                    Device.SetStreamSource(0, vertices, 0, Utilities.SizeOf<Vector4>() * 2);
                    Device.VertexDeclaration = vertexDeclaration;

                    Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);

                    effect.EndPass();
                    effect.End();

                    Device.VertexDeclaration = olddec;
                }
                catch (Exception e)
                {
                    vertices = null;
                    Console.WriteLine(@"DrawCircle: " + e);
                }
            }
        }

        /// <summary>
        /// The line.
        /// </summary>
        public class Line : RenderObject
        {
            /// <summary>
            /// The position delegate.
            /// </summary>
            public delegate Vector2 PositionDelegate();

            /// <summary>
            /// The _line.
            /// </summary>
            private readonly SharpDX.Direct3D9.Line line;

            /// <summary>
            /// The _width.
            /// </summary>
            private int width;

            /// <summary>
            /// The color.
            /// </summary>
            public ColorBGRA Color;

            /// <summary>
            /// Initializes a new instance of the <see cref="Line"/> class.
            /// </summary>
            /// <param name="start">
            /// The start.
            /// </param>
            /// <param name="end">
            /// The end.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            public Line(Vector2 start, Vector2 end, int width, ColorBGRA color)
            {
                this.line = new SharpDX.Direct3D9.Line(Device);
                this.Width = width;
                this.Color = color;
                this.Start = start;
                this.End = end;
                Game.OnUpdate += this.GameOnOnUpdate;
            }

            /// <summary>
            /// Gets or sets the start.
            /// </summary>
            public Vector2 Start { get; set; }

            /// <summary>
            /// Gets or sets the end.
            /// </summary>
            public Vector2 End { get; set; }

            /// <summary>
            /// Gets or sets the start position update.
            /// </summary>
            public PositionDelegate StartPositionUpdate { get; set; }

            /// <summary>
            /// Gets or sets the end position update.
            /// </summary>
            public PositionDelegate EndPositionUpdate { get; set; }

            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            public int Width
            {
                get
                {
                    return this.width;
                }

                set
                {
                    this.line.Width = value;
                    this.width = value;
                }
            }

            /// <summary>
            /// The game on on update.
            /// </summary>
            /// <param name="args">
            /// The args.
            /// </param>
            private void GameOnOnUpdate(EventArgs args)
            {
                if (this.StartPositionUpdate != null)
                {
                    this.Start = this.StartPositionUpdate();
                }

                if (this.EndPositionUpdate != null)
                {
                    this.End = this.EndPositionUpdate();
                }
            }

            /// <summary>
            /// The on end scene.
            /// </summary>
            public override void OnEndScene()
            {
                try
                {
                    if (this.line.IsDisposed)
                    {
                        return;
                    }

                    this.line.Begin();
                    this.line.Draw(new[] { this.Start, this.End }, this.Color);
                    this.line.End();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Line.OnEndScene: " + e);
                }
            }

            /// <summary>
            /// The on pre reset.
            /// </summary>
            public override void OnPreReset()
            {
                this.line.OnLostDevice();
            }

            /// <summary>
            /// The on post reset.
            /// </summary>
            public override void OnPostReset()
            {
                this.line.OnResetDevice();
            }

            /// <summary>
            /// The dispose.
            /// </summary>
            public override void Dispose()
            {
                if (!this.line.IsDisposed)
                {
                    this.line.Dispose();
                }

                Game.OnUpdate -= this.GameOnOnUpdate;
            }
        }

        /// <summary>
        /// The rectangle.
        /// </summary>
        public class Rectangle : RenderObject
        {
            /// <summary>
            /// The position delegate.
            /// </summary>
            public delegate Vector2 PositionDelegate();

            /// <summary>
            /// The _line.
            /// </summary>
            private readonly SharpDX.Direct3D9.Line line;

            /// <summary>
            /// The color.
            /// </summary>
            public ColorBGRA Color;

            /// <summary>
            /// Initializes a new instance of the <see cref="Rectangle"/> class.
            /// </summary>
            /// <param name="x">
            /// The x.
            /// </param>
            /// <param name="y">
            /// The y.
            /// </param>
            /// <param name="width">
            /// The width.
            /// </param>
            /// <param name="height">
            /// The height.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            public Rectangle(int x, int y, int width, int height, ColorBGRA color)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
                this.Color = color;
                this.line = new SharpDX.Direct3D9.Line(Device) { Width = height };
                Game.OnUpdate += this.Game_OnUpdate;
            }

            /// <summary>
            /// Gets or sets the x.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            /// Gets or sets the y.
            /// </summary>
            public int Y { get; set; }

            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            /// Gets or sets the position update.
            /// </summary>
            public PositionDelegate PositionUpdate { get; set; }

            /// <summary>
            /// The game_ on update.
            /// </summary>
            /// <param name="args">
            /// The args.
            /// </param>
            private void Game_OnUpdate(EventArgs args)
            {
                if (this.PositionUpdate != null)
                {
                    var pos = this.PositionUpdate();
                    this.X = (int)pos.X;
                    this.Y = (int)pos.Y;
                }
            }

            /// <summary>
            /// The on end scene.
            /// </summary>
            public override void OnEndScene()
            {
                try
                {
                    if (this.line.IsDisposed)
                    {
                        return;
                    }

                    this.line.Begin();
                    this.line.Draw(
                        new[]
                            {
                                new Vector2(this.X, this.Y + this.Height / 2),
                                new Vector2(this.X + this.Width, this.Y + this.Height / 2)
                            },
                        this.Color);
                    this.line.End();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Rectangle.OnEndScene: " + e);
                }
            }

            /// <summary>
            /// The on pre reset.
            /// </summary>
            public override void OnPreReset()
            {
                this.line.OnLostDevice();
            }

            /// <summary>
            /// The on post reset.
            /// </summary>
            public override void OnPostReset()
            {
                this.line.OnResetDevice();
            }

            /// <summary>
            /// The dispose.
            /// </summary>
            public override void Dispose()
            {
                if (!this.line.IsDisposed)
                {
                    this.line.Dispose();
                }

                Game.OnUpdate -= this.Game_OnUpdate;
            }
        }

        /// <summary>
        /// The render object.
        /// </summary>
        public class RenderObject : IDisposable
        {
            /// <summary>
            /// The visible condition delegate.
            /// </summary>
            /// <param name="sender">
            /// The sender.
            /// </param>
            public delegate bool VisibleConditionDelegate(RenderObject sender);

            /// <summary>
            /// The _visible.
            /// </summary>
            private bool visible = true;

            /// <summary>
            /// The layer.
            /// </summary>
            public float Layer;

            /// <summary>
            /// The visible condition.
            /// </summary>
            public VisibleConditionDelegate VisibleCondition;

            /// <summary>
            /// Gets or sets a value indicating whether visible.
            /// </summary>
            public bool Visible
            {
                get
                {
                    return this.VisibleCondition != null ? this.VisibleCondition(this) : this.visible;
                }

                set
                {
                    this.visible = value;
                }
            }

            /// <summary>
            /// The dispose.
            /// </summary>
            public virtual void Dispose()
            {
            }

            /// <summary>
            /// The on draw.
            /// </summary>
            public virtual void OnDraw()
            {
            }

            /// <summary>
            /// The on end scene.
            /// </summary>
            public virtual void OnEndScene()
            {
            }

            /// <summary>
            /// The on pre reset.
            /// </summary>
            public virtual void OnPreReset()
            {
            }

            /// <summary>
            /// The on post reset.
            /// </summary>
            public virtual void OnPostReset()
            {
            }

            /// <summary>
            /// The has valid layer.
            /// </summary>
            /// <returns>
            /// The <see cref="bool"/>.
            /// </returns>
            public bool HasValidLayer()
            {
                return this.Layer >= -5 && this.Layer <= 5;
            }
        }

        /// <summary>
        /// The sprite.
        /// </summary>
        public class Sprite : RenderObject
        {
            /// <summary>
            /// The on resetting.
            /// </summary>
            /// <param name="sprite">
            /// The sprite.
            /// </param>
            public delegate void OnResetting(Sprite sprite);

            /// <summary>
            /// The position delegate.
            /// </summary>
            public delegate Vector2 PositionDelegate();

            /// <summary>
            /// The _sprite.
            /// </summary>
            private readonly SharpDX.Direct3D9.Sprite sprite = new SharpDX.Direct3D9.Sprite(Device);

            /// <summary>
            /// The _crop.
            /// </summary>
            private SharpDX.Rectangle? crop;

            /// <summary>
            /// The _hide.
            /// </summary>
            private bool hide;

            /// <summary>
            /// The _original texture.
            /// </summary>
            private Texture originalTexture;

            /// <summary>
            /// The _texture.
            /// </summary>
            private Texture texture;

            /// <summary>
            /// Prevents a default instance of the <see cref="Sprite"/> class from being created.
            /// </summary>
            private Sprite()
            {
                Game.OnUpdate += this.Game_OnUpdate;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Sprite"/> class.
            /// </summary>
            /// <param name="bitmap">
            /// The bitmap.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            public Sprite(Bitmap bitmap, Vector2 position)
                : this()
            {
                this.UpdateTextureBitmap(bitmap, position);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Sprite"/> class.
            /// </summary>
            /// <param name="texture">
            /// The texture.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            public Sprite(BaseTexture texture, Vector2 position)
                : this()
            {
                this.UpdateTextureBitmap(
                    (Bitmap)Image.FromStream(BaseTexture.ToStream(texture, ImageFileFormat.Bmp)),
                    position);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Sprite"/> class.
            /// </summary>
            /// <param name="stream">
            /// The stream.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            public Sprite(Stream stream, Vector2 position)
                : this()
            {
                this.UpdateTextureBitmap(new Bitmap(stream), position);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Sprite"/> class.
            /// </summary>
            /// <param name="bytesArray">
            /// The bytes array.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            public Sprite(byte[] bytesArray, Vector2 position)
                : this()
            {
                this.UpdateTextureBitmap((Bitmap)Image.FromStream(new MemoryStream(bytesArray)), position);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Sprite"/> class.
            /// </summary>
            /// <param name="fileLocation">
            /// The file location.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            public Sprite(string fileLocation, Vector2 position)
                : this()
            {
                if (!File.Exists(fileLocation))
                {
                    return;
                }

                this.UpdateTextureBitmap(new Bitmap(fileLocation), position);
            }

            /// <summary>
            /// Gets or sets the x.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            /// Gets or sets the y.
            /// </summary>
            public int Y { get; set; }

            /// <summary>
            /// Gets or sets the bitmap.
            /// </summary>
            public Bitmap Bitmap { get; set; }

            /// <summary>
            /// Gets the width.
            /// </summary>
            public int Width
            {
                get
                {
                    return (int)(this.Bitmap.Width * this.Scale.X);
                }
            }

            /// <summary>
            /// Gets the height.
            /// </summary>
            public int Height
            {
                get
                {
                    return (int)(this.Bitmap.Height * this.Scale.Y);
                }
            }

            /// <summary>
            /// Gets the size.
            /// </summary>
            public Vector2 Size
            {
                get
                {
                    return new Vector2(this.Bitmap.Width, this.Bitmap.Height);
                }
            }

            /// <summary>
            /// Gets or sets the position.
            /// </summary>
            public Vector2 Position
            {
                get
                {
                    return new Vector2(this.X, this.Y);
                }

                set
                {
                    this.X = (int)value.X;
                    this.Y = (int)value.Y;
                }
            }

            /// <summary>
            /// Gets or sets the position update.
            /// </summary>
            public PositionDelegate PositionUpdate { get; set; }

            /// <summary>
            /// Gets or sets the scale.
            /// </summary>
            public Vector2 Scale { get; set; } = new Vector2(1, 1);

            /// <summary>
            /// Gets or sets the rotation.
            /// </summary>
            public float Rotation { get; set; }

            /// <summary>
            /// Gets or sets the color.
            /// </summary>
            public ColorBGRA Color { get; set; } = SharpDX.Color.White;

            /// <summary>
            /// The game_ on update.
            /// </summary>
            /// <param name="args">
            /// The args.
            /// </param>
            private void Game_OnUpdate(EventArgs args)
            {
                if (this.PositionUpdate != null)
                {
                    var pos = this.PositionUpdate();
                    this.X = (int)pos.X;
                    this.Y = (int)pos.Y;
                }
            }

            /// <summary>
            /// The on reset.
            /// </summary>
            public event OnResetting OnReset;

            /// <summary>
            /// The crop.
            /// </summary>
            /// <param name="x">
            /// The x.
            /// </param>
            /// <param name="y">
            /// The y.
            /// </param>
            /// <param name="w">
            /// The w.
            /// </param>
            /// <param name="h">
            /// The h.
            /// </param>
            /// <param name="scale">
            /// The scale.
            /// </param>
            public void Crop(int x, int y, int w, int h, bool scale = false)
            {
                this.crop = new SharpDX.Rectangle(x, y, w, h);

                if (scale)
                {
                    this.crop = new SharpDX.Rectangle(
                        (int)(this.Scale.X * x),
                        (int)(this.Scale.Y * y),
                        (int)(this.Scale.X * w),
                        (int)(this.Scale.Y * h));
                }
            }

            /// <summary>
            /// The crop.
            /// </summary>
            /// <param name="rect">
            /// The rect.
            /// </param>
            /// <param name="scale">
            /// The scale.
            /// </param>
            public void Crop(SharpDX.Rectangle rect, bool scale = false)
            {
                this.crop = rect;

                if (scale)
                {
                    this.crop = new SharpDX.Rectangle(
                        (int)(this.Scale.X * rect.X),
                        (int)(this.Scale.Y * rect.Y),
                        (int)(this.Scale.X * rect.Width),
                        (int)(this.Scale.Y * rect.Height));
                }
            }

            /// <summary>
            /// The show.
            /// </summary>
            public void Show()
            {
                this.hide = false;
            }

            /// <summary>
            /// The hide.
            /// </summary>
            public void Hide()
            {
                this.hide = true;
            }

            /// <summary>
            /// The reset.
            /// </summary>
            public void Reset()
            {
                this.UpdateTextureBitmap(
                    (Bitmap)Image.FromStream(BaseTexture.ToStream(this.originalTexture, ImageFileFormat.Bmp)));

                if (this.OnReset != null)
                {
                    this.OnReset(this);
                }
            }

            /// <summary>
            /// The gray scale.
            /// </summary>
            public void GrayScale()
            {
                this.SetSaturation(0.0f);
            }

            /// <summary>
            /// The fade.
            /// </summary>
            public void Fade()
            {
                this.SetSaturation(0.5f);
            }

            /// <summary>
            /// The complement.
            /// </summary>
            public void Complement()
            {
                this.SetSaturation(-1.0f);
            }

            /// <summary>
            /// The set saturation.
            /// </summary>
            /// <param name="saturiation">
            /// The saturiation.
            /// </param>
            public void SetSaturation(float saturiation)
            {
                this.UpdateTextureBitmap(SaturateBitmap(this.Bitmap, saturiation));
            }

            /// <summary>
            /// The saturate bitmap.
            /// </summary>
            /// <param name="original">
            /// The original.
            /// </param>
            /// <param name="saturation">
            /// The saturation.
            /// </param>
            /// <returns>
            /// The <see cref="Bitmap"/>.
            /// </returns>
            private static Bitmap SaturateBitmap(Image original, float saturation)
            {
                const float RWeight = 0.3086f;
                const float GWeight = 0.6094f;
                const float BWeight = 0.0820f;

                var a = (1.0f - saturation) * RWeight + saturation;
                var b = (1.0f - saturation) * RWeight;
                var c = (1.0f - saturation) * RWeight;
                var d = (1.0f - saturation) * GWeight;
                var e = (1.0f - saturation) * GWeight + saturation;
                var f = (1.0f - saturation) * GWeight;
                var g = (1.0f - saturation) * BWeight;
                var h = (1.0f - saturation) * BWeight;
                var i = (1.0f - saturation) * BWeight + saturation;

                var newBitmap = new Bitmap(original.Width, original.Height);
                var gr = Graphics.FromImage(newBitmap);

                // ColorMatrix elements
                float[][] ptsArray =
                    {
                        new[] { a, b, c, 0, 0 }, new[] { d, e, f, 0, 0 }, new[] { g, h, i, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 }
                    };

                // Create ColorMatrix
                var clrMatrix = new ColorMatrix(ptsArray);

                // Create ImageAttributes
                var imgAttribs = new ImageAttributes();

                // Set color matrix
                imgAttribs.SetColorMatrix(clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default);

                // Draw Image with no effects
                gr.DrawImage(original, 0, 0, original.Width, original.Height);

                // Draw Image with image attributes
                gr.DrawImage(
                    original,
                    new System.Drawing.Rectangle(0, 0, original.Width, original.Height),
                    0,
                    0,
                    original.Width,
                    original.Height,
                    GraphicsUnit.Pixel,
                    imgAttribs);
                gr.Dispose();

                return newBitmap;
            }

            /// <summary>
            /// The update texture bitmap.
            /// </summary>
            /// <param name="newBitmap">
            /// The new bitmap.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            public void UpdateTextureBitmap(Bitmap newBitmap, Vector2 position = new Vector2())
            {
                if (position.IsValid())
                {
                    this.Position = position;
                }

                if (this.Bitmap != null)
                {
                    this.Bitmap.Dispose();
                }

                this.Bitmap = newBitmap;

                this.texture = Texture.FromMemory(
                    Device,
                    (byte[])new ImageConverter().ConvertTo(newBitmap, typeof(byte[])),
                    this.Width,
                    this.Height,
                    0,
                    Usage.None,
                    Format.A1,
                    Pool.Managed,
                    Filter.Default,
                    Filter.Default,
                    0);

                if (this.originalTexture == null)
                {
                    this.originalTexture = this.texture;
                }
            }

            /// <summary>
            /// The on end scene.
            /// </summary>
            public override void OnEndScene()
            {
                try
                {
                    if (this.sprite.IsDisposed || this.texture.IsDisposed || !this.Position.IsValid() || this.hide)
                    {
                        return;
                    }

                    this.sprite.Begin();
                    var matrix = this.sprite.Transform;
                    var nMatrix = Matrix.Scaling(this.Scale.X, this.Scale.Y, 0) * Matrix.RotationZ(this.Rotation)
                                  * Matrix.Translation(this.Position.X, this.Position.Y, 0);
                    this.sprite.Transform = nMatrix;
                    this.sprite.Draw(this.texture, this.Color, this.crop);
                    this.sprite.Transform = matrix;
                    this.sprite.End();
                }
                catch (Exception e)
                {
                    this.Reset();
                    Console.WriteLine(@"Common.Render.Sprite.OnEndScene: " + e);
                }
            }

            /// <summary>
            /// The on pre reset.
            /// </summary>
            public override void OnPreReset()
            {
                this.sprite.OnLostDevice();
            }

            /// <summary>
            /// The on post reset.
            /// </summary>
            public override void OnPostReset()
            {
                this.sprite.OnResetDevice();
            }

            /// <summary>
            /// The dispose.
            /// </summary>
            public override void Dispose()
            {
                Game.OnUpdate -= this.Game_OnUpdate;
                if (!this.sprite.IsDisposed)
                {
                    this.sprite.Dispose();
                }

                if (!this.texture.IsDisposed)
                {
                    this.texture.Dispose();
                }

                if (!this.originalTexture.IsDisposed)
                {
                    this.originalTexture.Dispose();
                }
            }
        }

        /// <summary>
        ///     Object used to draw text on the screen.
        /// </summary>
        public class Text : RenderObject
        {
            /// <summary>
            /// The position delegate.
            /// </summary>
            public delegate Vector2 PositionDelegate();

            /// <summary>
            /// The text delegate.
            /// </summary>
            public delegate string TextDelegate();

            /// <summary>
            /// The _text.
            /// </summary>
            // ReSharper disable once InconsistentNaming
            private string _text;

            /// <summary>
            /// The _text font.
            /// </summary>
            private Font textFont;

            /// <summary>
            /// The _x.
            /// </summary>
            private int x;

            /// <summary>
            /// The _x calculated.
            /// </summary>
            private int xCalculated;

            /// <summary>
            /// The _y.
            /// </summary>
            private int y;

            /// <summary>
            /// The _y calculated.
            /// </summary>
            private int yCalculated;

            /// <summary>
            /// The centered.
            /// </summary>
            public bool Centered = false;

            /// <summary>
            /// The offset.
            /// </summary>
            public Vector2 Offset;

            /// <summary>
            /// The out lined.
            /// </summary>
            public bool OutLined = false;

            /// <summary>
            /// The position update.
            /// </summary>
            public PositionDelegate PositionUpdate;

            /// <summary>
            /// The text update.
            /// </summary>
            public TextDelegate TextUpdate;

            /// <summary>
            /// The unit.
            /// </summary>
            public Obj_AI_Base Unit;

            /// <summary>
            /// Initializes a new instance of the <see cref="Render.Text"/> class.
            /// </summary>
            /// <param name="text">
            /// The text.
            /// </param>
            /// <param name="fontName">
            /// The font name.
            /// </param>
            /// <param name="size">
            /// The size.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            private Text(string text, string fontName, int size, ColorBGRA color)
            {
                this.textFont = new Font(
                    Device,
                    new FontDescription
                    {
                        FaceName = fontName,
                        Height = size,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });
                this.Color = color;
                this.text = text;
                Game.OnUpdate += this.Game_OnUpdate;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Render.Text"/> class.
            /// </summary>
            /// <param name="text">
            /// The text.
            /// </param>
            /// <param name="x">
            /// The x.
            /// </param>
            /// <param name="y">
            /// The y.
            /// </param>
            /// <param name="size">
            /// The size.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="fontName">
            /// The font name.
            /// </param>
            public Text(string text, int x, int y, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = x;
                this.y = y;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Render.Text"/> class.
            /// </summary>
            /// <param name="text">
            /// The text.
            /// </param>
            /// <param name="position">
            /// The position.
            /// </param>
            /// <param name="size">
            /// The size.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="fontName">
            /// The font name.
            /// </param>
            public Text(string text, Vector2 position, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = (int)position.X;
                this.y = (int)position.Y;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Render.Text"/> class.
            /// </summary>
            /// <param name="text">
            /// The text.
            /// </param>
            /// <param name="unit">
            /// The unit.
            /// </param>
            /// <param name="offset">
            /// The offset.
            /// </param>
            /// <param name="size">
            /// The size.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="fontName">
            /// The font name.
            /// </param>
            public Text(
                string text,
                Obj_AI_Base unit,
                Vector2 offset,
                int size,
                ColorBGRA color,
                string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.Unit = unit;
                this.Offset = offset;

                var pos = unit.HPBarPosition + offset;

                this.x = (int)pos.X;
                this.y = (int)pos.Y;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Render.Text"/> class.
            /// </summary>
            /// <param name="x">
            /// The x.
            /// </param>
            /// <param name="y">
            /// The y.
            /// </param>
            /// <param name="text">
            /// The text.
            /// </param>
            /// <param name="size">
            /// The size.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="fontName">
            /// The font name.
            /// </param>
            public Text(int x, int y, string text, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = x;
                this.y = y;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Render.Text"/> class.
            /// </summary>
            /// <param name="position">
            /// The position.
            /// </param>
            /// <param name="text">
            /// The text.
            /// </param>
            /// <param name="size">
            /// The size.
            /// </param>
            /// <param name="color">
            /// The color.
            /// </param>
            /// <param name="fontName">
            /// The font name.
            /// </param>
            public Text(Vector2 position, string text, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = (int)position.X;
                this.y = (int)position.Y;
            }

            /// <summary>
            /// Gets or sets the text font description.
            /// </summary>
            public FontDescription TextFontDescription
            {
                get
                {
                    return this.textFont.Description;
                }

                set
                {
                    this.textFont.Dispose();
                    this.textFont = new Font(Device, value);
                }
            }

            /// <summary>
            /// Gets or sets the x.
            /// </summary>
            public int X
            {
                get
                {
                    if (this.PositionUpdate != null)
                    {
                        return this.xCalculated;
                    }

                    return this.x + this.XOffset;
                }

                set
                {
                    this.x = value;
                }
            }

            /// <summary>
            /// Gets or sets the y.
            /// </summary>
            public int Y
            {
                get
                {
                    if (this.PositionUpdate != null)
                    {
                        return this.yCalculated;
                    }

                    return this.y + this.YOffset;
                }

                set
                {
                    this.y = value;
                }
            }

            /// <summary>
            /// Gets the x offset.
            /// </summary>
            private int XOffset
            {
                get
                {
                    return this.Centered ? -this.Width / 2 : 0;
                }
            }

            /// <summary>
            /// Gets the y offset.
            /// </summary>
            private int YOffset
            {
                get
                {
                    return this.Centered ? -this.Height / 2 : 0;
                }
            }

            /// <summary>
            /// Gets the width.
            /// </summary>
            public int Width { get; private set; }

            /// <summary>
            /// Gets the height.
            /// </summary>
            public int Height { get; private set; }

            /// <summary>
            /// Gets or sets the color.
            /// </summary>
            public ColorBGRA Color { get; set; }

            /// <summary>
            /// Gets or sets the text.
            /// </summary>
            // ReSharper disable once InconsistentNaming
            public string text
            {
                get
                {
                    return this._text;
                }

                set
                {
                    if (value != this._text && this.textFont != null && !this.textFont.IsDisposed
                        && !string.IsNullOrEmpty(value))
                    {
                        var size = this.textFont.MeasureText(null, value, 0);
                        this.Width = size.Width;
                        this.Height = size.Height;
                        this.textFont.PreloadText(value);
                    }

                    this._text = value;
                }
            }

            /// <summary>
            /// The game_ on update.
            /// </summary>
            /// <param name="args">
            /// The args.
            /// </param>
            private void Game_OnUpdate(EventArgs args)
            {
                if (this.Visible)
                {
                    if (this.TextUpdate != null)
                    {
                        this.text = this.TextUpdate();
                    }

                    if (this.PositionUpdate != null && !string.IsNullOrEmpty(this.text))
                    {
                        var pos = this.PositionUpdate();
                        this.xCalculated = (int)pos.X + this.XOffset;
                        this.yCalculated = (int)pos.Y + this.YOffset;
                    }
                }
            }

            /// <summary>
            /// The on end scene.
            /// </summary>
            public override void OnEndScene()
            {
                try
                {
                    if (this.textFont.IsDisposed || this.text == string.Empty)
                    {
                        return;
                    }

                    if (this.Unit != null && this.Unit.IsValid)
                    {
                        var pos = this.Unit.HPBarPosition + this.Offset;
                        this.X = (int)pos.X;
                        this.Y = (int)pos.Y;
                    }

                    var xP = this.X;
                    var yP = this.Y;
                    if (this.OutLined)
                    {
                        var outlineColor = new ColorBGRA(0, 0, 0, 255);
                        this.textFont.DrawText(null, this.text, xP - 1, yP - 1, outlineColor);
                        this.textFont.DrawText(null, this.text, xP + 1, yP + 1, outlineColor);
                        this.textFont.DrawText(null, this.text, xP - 1, yP, outlineColor);
                        this.textFont.DrawText(null, this.text, xP + 1, yP, outlineColor);
                    }

                    this.textFont.DrawText(null, this.text, xP, yP, this.Color);
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.text.OnEndScene: " + e);
                }
            }

            /// <summary>
            /// The on pre reset.
            /// </summary>
            public override void OnPreReset()
            {
                this.textFont.OnLostDevice();
            }

            /// <summary>
            /// The on post reset.
            /// </summary>
            public override void OnPostReset()
            {
                this.textFont.OnResetDevice();
            }

            /// <summary>
            /// The dispose.
            /// </summary>
            public override void Dispose()
            {
                Game.OnUpdate -= this.Game_OnUpdate;
                if (!this.textFont.IsDisposed)
                {
                    this.textFont.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// The font extension.
    /// </summary>
    public static class FontExtension
    {
        /// <summary>
        /// The widths.
        /// </summary>
        private static readonly Dictionary<Font, Dictionary<string, Rectangle>> Widths =
            new Dictionary<Font, Dictionary<string, Rectangle>>();

        /// <summary>
        /// The measure text.
        /// </summary>
        /// <param name="font">
        /// The font.
        /// </param>
        /// <param name="sprite">
        /// The sprite.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// The <see cref="Rectangle"/>.
        /// </returns>
        public static Rectangle MeasureText(this Font font, Sprite sprite, string text)
        {
            Dictionary<string, Rectangle> rectangles;
            if (!Widths.TryGetValue(font, out rectangles))
            {
                rectangles = new Dictionary<string, Rectangle>();
                Widths[font] = rectangles;
            }

            Rectangle rectangle;
            if (rectangles.TryGetValue(text, out rectangle))
            {
                return rectangle;
            }

            rectangle = font.MeasureText(sprite, text, 0);
            rectangles[text] = rectangle;
            return rectangle;
        }

        /// <summary>
        /// The measure text.
        /// </summary>
        /// <param name="font">
        /// The font.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// The <see cref="Rectangle"/>.
        /// </returns>
        public static Rectangle MeasureText(this Font font, string text)
        {
            return font.MeasureText(null, text);
        }
    }
}