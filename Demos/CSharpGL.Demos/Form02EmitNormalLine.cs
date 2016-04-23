﻿using CSharpGL.ModelAdapters;
using CSharpGL.Models;
using GLM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSharpGL.Demos
{
    public partial class Form02EmitNormalLine : Form
    {

        public enum GeometryModel
        {
            Cube,
            Sphere,
            Teapot,
        }

        private GeometryModel selectedModel = GeometryModel.Teapot;
        public GeometryModel SelectedModel
        {
            get { return selectedModel; }
            set
            {
                if (value != selectedModel)
                {
                    selectedModel = value;
                    if (this.rendererPropertyGrid != null)
                    { this.rendererPropertyGrid.DisplayObject(this.rendererDict[value]); }
                    this.cameraUpdated = true;
                    this.UpdateMVP(this.rendererDict[this.selectedModel]);
                }
            }
        }

        Dictionary<GeometryModel, ModernRenderer> rendererDict = new Dictionary<GeometryModel, ModernRenderer>();

        ///// <summary>
        ///// 要渲染的对象
        ///// </summary>
        //ModernRenderer renderer;

        bool cameraUpdated = true;

        public bool CameraUpdated
        {
            get { return cameraUpdated; }
        }

        /// <summary>
        /// 控制Camera的旋转、进退
        /// </summary>
        SatelliteRotator rotator;
        /// <summary>
        /// 摄像机
        /// </summary>
        Camera camera;

        private FormBulletinBoard bulletinBoard;
        private FormProperyGrid rendererPropertyGrid;
        private FormProperyGrid cameraPropertyGrid;
        private FormProperyGrid formPropertyGrid;

        public Form02EmitNormalLine()
        {
            InitializeComponent();

            this.glCanvas1.OpenGLDraw += glCanvas1_OpenGLDraw;
            this.glCanvas1.MouseDown += glCanvas1_MouseDown;
            this.glCanvas1.MouseMove += glCanvas1_MouseMove;
            this.glCanvas1.MouseUp += glCanvas1_MouseUp;
            this.glCanvas1.MouseWheel += glCanvas1_MouseWheel;
            // 天蓝色背景
            //GL.ClearColor(0x87 / 255.0f, 0xce / 255.0f, 0xeb / 255.0f, 0xff / 255.0f);
            GL.ClearColor(0, 0, 0, 0);
        }

        RenderModes renderMode;

        public RenderModes RenderMode
        {
            get { return renderMode; }
            set
            {
                if (value != renderMode)
                {
                    renderMode = value;
                    this.UpdateMVP(this.rendererDict[this.selectedModel]);
                }
            }
        }


        private void glCanvas1_OpenGLDraw(object sender, PaintEventArgs e)
        {
            if (this.RenderMode == RenderModes.ColorCodedPicking)
            { GL.ClearColor(1, 1, 1, 1); }
            else if (this.RenderMode == RenderModes.Render)
            { GL.ClearColor(0, 0, 0, 0); }

            GL.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);

            ModernRenderer renderer = this.rendererDict[this.SelectedModel];
            if (renderer != null)
            {
                if (cameraUpdated)
                {
                    UpdateMVP(renderer);
                    cameraUpdated = false;
                }
                renderer.Render(new RenderEventArgs(RenderMode, this.camera));
            }
        }

        private void UpdateMVP(ModernRenderer renderer)
        {
            mat4 projectionMatrix = camera.GetProjectionMat4();
            mat4 viewMatrix = camera.GetViewMat4();
            mat4 modelMatrix = mat4.identity();

            if (this.RenderMode == RenderModes.ColorCodedPicking)
            {
                IColorCodedPicking picking = renderer;
                picking.MVP = projectionMatrix * viewMatrix * modelMatrix;
            }
            else if (this.RenderMode == RenderModes.Render)
            {
                renderer.SetUniformValue("projectionMatrix", projectionMatrix);
                renderer.SetUniformValue("viewMatrix", viewMatrix);
                renderer.SetUniformValue("modelMatrix", modelMatrix);
            }
            else
            { throw new NotImplementedException(); }
        }

        private void glCanvas1_MouseDown(object sender, MouseEventArgs e)
        {
            rotator.SetBounds(this.glCanvas1.Width, this.glCanvas1.Height);
            rotator.MouseDown(e.X, e.Y);
        }

        private void glCanvas1_MouseMove(object sender, MouseEventArgs e)
        {
            if (rotator.MouseDownFlag)
            {
                rotator.MouseMove(e.X, e.Y);
                this.cameraUpdated = true;
            }

            {
                IColorCodedPicking pickable = this.rendererDict[this.SelectedModel];
                pickable.MVP = this.camera.GetProjectionMat4() * this.camera.GetViewMat4();
                IPickedGeometry pickedGeometry = ColorCodedPicking.Pick(
                    this.camera, e.X, e.Y, this.glCanvas1.Width, this.glCanvas1.Height, pickable);
                if (pickedGeometry != null)
                {
                    this.bulletinBoard.SetContent(pickedGeometry.ToString());
                }
                else
                {
                    this.bulletinBoard.SetContent("picked nothing.");
                }
            }
        }

        private void glCanvas1_MouseUp(object sender, MouseEventArgs e)
        {
            rotator.MouseUp(e.X, e.Y);
        }

        void glCanvas1_MouseWheel(object sender, MouseEventArgs e)
        {
            camera.MouseWheel(e.Delta);
            cameraUpdated = true;
        }

        private void Form01ModernRenderer_Load(object sender, EventArgs e)
        {
            {
                var camera = new Camera(CameraType.Perspecitive, this.glCanvas1.Width, this.glCanvas1.Height);
                camera.Position = new vec3(0, 0, 5);
                var rotator = new SatelliteRotator(camera);
                this.camera = camera;
                this.rotator = rotator;
            }
            {
                var bufferables = new IBufferable[]{
                    new CubeModelAdapter(new CubeModel(1.0f)),
                    new SphereModelAdapter(new SphereModel(1.0f)),
                    new TeapotModelAdapter(TeapotModel.GetModel(1.0f)),
                };
                var keys = new GeometryModel[] { GeometryModel.Cube, GeometryModel.Sphere, GeometryModel.Teapot };
                for (int i = 0; i < bufferables.Length; i++)
                {
                    IBufferable bufferable = bufferables[i];
                    GeometryModel key = keys[i];
                    ShaderCode[] shaders = new ShaderCode[3];
                    shaders[0] = new ShaderCode(File.ReadAllText(@"Shaders\EmitNormalLine.vert"), ShaderType.VertexShader);
                    shaders[1] = new ShaderCode(File.ReadAllText(@"Shaders\EmitNormalLine.geom"), ShaderType.GeometryShader);
                    shaders[2] = new ShaderCode(File.ReadAllText(@"Shaders\EmitNormalLine.frag"), ShaderType.FragmentShader);
                    var propertyNameMap = new PropertyNameMap();
                    propertyNameMap.Add("in_Position", "position");
                    propertyNameMap.Add("in_Normal", "normal");
                    string positionNameInIBufferable = "position";
                    var renderer = ModernRendererFactory.GetModernRenderer(bufferable, shaders, propertyNameMap, positionNameInIBufferable);
                    renderer.Initialize();
                    renderer.SetUniformValue("normalLength", 0.5f);
                    renderer.SetUniformValue("showModel", true);
                    renderer.SetUniformValue("showNormal", true);

                    GLSwitch lineWidthSwitch = new LineWidthSwitch(10.0f);
                    renderer.SwitchList.Add(lineWidthSwitch);
                    GLSwitch pointSizeSwitch = new PointSizeSwitch(10.0f);
                    renderer.SwitchList.Add(pointSizeSwitch);
                    this.rendererDict.Add(key, renderer);
                    GLSwitch polygonModeSwitch = new PolygonModeSwitch(PolygonModes.Filled);
                    renderer.SwitchList.Add(polygonModeSwitch);
                    GLSwitch primitiveRestartSwitch = new PrimitiveRestartSwitch(uint.MaxValue);
                    renderer.SwitchList.Add(primitiveRestartSwitch);
                }
                this.SelectedModel = GeometryModel.Teapot;
            }
            {
                var frmBulletinBoard = new FormBulletinBoard();
                frmBulletinBoard.Dump = true;
                frmBulletinBoard.Show();
                this.bulletinBoard = frmBulletinBoard;
            }
            {
                var frmPropertyGrid = new FormProperyGrid();
                frmPropertyGrid.DisplayObject(this.rendererDict[this.SelectedModel]);
                frmPropertyGrid.Show();
                this.rendererPropertyGrid = frmPropertyGrid;
            }
            {
                var frmPropertyGrid = new FormProperyGrid();
                frmPropertyGrid.DisplayObject(this.camera);
                frmPropertyGrid.Show();
                this.cameraPropertyGrid = frmPropertyGrid;
            }
            {
                var frmPropertyGrid = new FormProperyGrid();
                frmPropertyGrid.DisplayObject(this);
                frmPropertyGrid.Show();
                this.formPropertyGrid = frmPropertyGrid;
            }

        }

    }
}
