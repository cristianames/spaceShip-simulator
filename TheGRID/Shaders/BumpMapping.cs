﻿using AlumnoEjemplos.TheGRID.Helpers;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using TgcViewer;
using TgcViewer.Utils.Shaders;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;

namespace AlumnoEjemplos.TheGRID.Shaders
{
    //Estructura para manejar las luces
    struct LightValues
    {
        public Vector3 direccion;
        public float angulo;
        public Vector3 posicion_ParaSol;
        public Vector3 posicion_ParaAsteroide;
        public Vector3 posicion_ParaNave;
        public Color color;
        public float intensidad_sol;
        public float intensidad_asteroide;
        public float intensidad_nave;
        public float atenuacion_sol;
        public float atenuacion_asteroide;
        public float atenuacion_nave;
        public float bumpiness;
    }

    class BumpMapping :IShader
    {
        #region Atributos
        private string ShaderDirectory = EjemploAlumno.TG_Folder + "Shaders\\BumpMapping.fx";
        private SuperRender mainShader;
        TgcTexture normalMapAsteroid;
        private Surface g_pDepthStencil;     // Depth-stencil buffer 
        private Texture g_pRenderTarget;     //Textura Final
        private VertexBuffer g_pVBV3D;

        private Texture g_BumpSol;
        private Texture g_BumpIzq;
        private Texture g_BumpDer;
        private Texture g_BumpFront;

        private LightValues light_sol;
        private LightValues light_izq;
        private LightValues light_der;
        private LightValues light_front;

        private Vector3 eyePosition;

        private Effect bumpEffect_asteroides;
        private Effect bumpEffect_nave;
        private Effect bumpEffect_sol;

        TgcBox[] lightMeshes;

        #endregion

        public BumpMapping(SuperRender main)
        {
            mainShader = main;
            Device d3dDevice = GuiController.Instance.D3dDevice;
            normalMapAsteroid = TgcTexture.createTexture(EjemploAlumno.TG_Folder + "asteroid\\Textures\\3215_Bump.jpg");
            //Inicializo el depth stencil
            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
                                                             d3dDevice.PresentationParameters.BackBufferHeight,
                                                             DepthFormat.D24S8,
                                                             MultiSampleType.None,
                                                             0,
                                                             true);

            // inicializo el render target
            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                    , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                        Format.X8R8G8B8, Pool.Default);

            //Inicializamos las texturas auxiliares
            g_BumpSol = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                    , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                        Format.X8R8G8B8, Pool.Default);
            g_BumpIzq = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                    , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                    Format.X8R8G8B8, Pool.Default);
            g_BumpDer = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                    , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                    Format.X8R8G8B8, Pool.Default);
            g_BumpFront = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                    , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                    Format.X8R8G8B8, Pool.Default);
            //Creamos El vertex auxiliar para el postprocesado
            CustomVertex.PositionTextured[] vertices = new CustomVertex.PositionTextured[]
		    {
    			new CustomVertex.PositionTextured( -1, 1, 1, 0,0), 
			    new CustomVertex.PositionTextured(1,  1, 1, 1,0),
			    new CustomVertex.PositionTextured(-1, -1, 1, 0,1),
			    new CustomVertex.PositionTextured(1,-1, 1, 1,1)
    		};
            //vertex buffer de los triangulos
            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                    4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                        CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);
            //Creamos las luces
            light_sol = crearLuz(Color.White, 1000f, 2000f, 1000f, 0.6f, 0.2f, 0.2f, 1f);
            light_izq = crearLuz(Color.Green, 1f, 1000f, 10f, 0.3f, 0.3f, 0.3f, 1f);
            light_izq.direccion = new Vector3(0, -1, 0);
            light_izq.angulo = -10f;
            light_der = crearLuz(Color.Green, 1f, 1000f, 10f, 0.3f, 0.3f, 0.3f, 1f);
            light_der.direccion = new Vector3(0, 1, 0);
            light_der.angulo = -10f;
            light_front = crearLuz(Color.Yellow, 1f, 1000f, 10f, 0.3f, 0.3f, 0.3f, 1f);
            light_front.direccion = new Vector3(0, 0, 1);
            light_front.angulo = -10f;
            //Cuadraditos que simulan luces
            lightMeshes = new TgcBox[3];
            //Color[] c = new Color[3] { Color.Red, Color.Green, Color.Green };
            for (int i = 0; i < lightMeshes.Length; i++)
            {
                lightMeshes[i] = TgcBox.fromSize(new Vector3(1f, 1f, 1f), Color.Blue);
                EjemploAlumno.workspace().objectosNoMeshesCollection.Add(lightMeshes[i]);
            }
            /*d3dDevice.SetRenderState(RenderStates.Lighting, true);
            d3dDevice.SetRenderState(RenderStates.SpecularEnable, true);
            d3dDevice.Lights[0].Type = LightType.Point;
            d3dDevice.Lights[0].Diffuse = Color.FromArgb(255, 255, 255, 255);
            d3dDevice.Lights[0].Specular = Color.FromArgb(255, 255, 255, 255);
            d3dDevice.Lights[0].Attenuation0 = 0.0f;
            d3dDevice.Lights[0].Range = 50000.0f;
            d3dDevice.Lights[0].Enabled = true;
            d3dDevice.Lights[1].Type = LightType.Point;
            d3dDevice.Lights[1].Diffuse = Color.FromArgb(255, 255, 255, 255);
            d3dDevice.Lights[1].Specular = Color.FromArgb(255, 255, 255, 255);
            d3dDevice.Lights[1].Attenuation0 = 0.0f;
            d3dDevice.Lights[1].Range = 50000.0f;
            d3dDevice.Lights[1].Enabled = true;
            d3dDevice.Lights[1].Type = LightType.Point;
            d3dDevice.Lights[1].Diffuse = Color.FromArgb(255, 255, 255, 255);
            d3dDevice.Lights[1].Specular = Color.FromArgb(255, 255, 255, 255);
            d3dDevice.Lights[1].Attenuation0 = 0.0f;
            d3dDevice.Lights[1].Range = 50000.0f;
            d3dDevice.Lights[1].Enabled = true;*/
            cargarBumpEffect();
        }

        public Texture renderDefault(EstructuraRender parametros)
        {
            throw new NotImplementedException();
        }

        public Texture renderEffect(EstructuraRender parametros)
        {
            actualizarLuces();

            Device device = GuiController.Instance.D3dDevice;

            // guardo el Render target anterior y seteo la textura como render target
            Surface pOldRT = device.GetRenderTarget(0);
            // hago lo mismo con el depthbuffer, necesito el que no tiene multisampling
            Surface pOldDS = device.DepthStencilSurface;

            Surface pSurf;

            //1° Pasada, luz solar
            device.DepthStencilSurface = g_pDepthStencil;
            pSurf = g_BumpSol.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            device.BeginScene();
                renderScene(parametros.meshes, light_sol, "BumpMappingTechnique");
                renderScene(parametros.sol, light_sol, "BumpMappingTechnique");
                    renderScene(parametros.elementosRenderizables);
                    if (!EjemploAlumno.workspace().camara.soyFPS())
                        renderScene(parametros.nave, light_sol, "BumpMappingTechnique");
            device.EndScene();
            pSurf.Dispose();

            //2° Pasada, luz derecha
            device.DepthStencilSurface = g_pDepthStencil;
            pSurf = g_BumpDer.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            device.BeginScene();
            renderScene(parametros.meshes, light_der, "VERTEX_COLOR");
                renderScene(parametros.sol, light_der, "VERTEX_COLOR");
                renderScene(parametros.elementosRenderizables);
                if (!EjemploAlumno.workspace().camara.soyFPS())
                    renderScene(parametros.nave, light_der, "VERTEX_COLOR");
            device.EndScene();
            pSurf.Dispose();
            //3° Pasada, luz izquierda
            device.DepthStencilSurface = g_pDepthStencil;
            pSurf = g_BumpIzq.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            device.BeginScene();
            renderScene(parametros.meshes, light_izq, "VERTEX_COLOR");
                renderScene(parametros.sol, light_izq, "VERTEX_COLOR");
                renderScene(parametros.elementosRenderizables);
                if (!EjemploAlumno.workspace().camara.soyFPS())
                    renderScene(parametros.nave, light_izq, "VERTEX_COLOR");
            device.EndScene();
            pSurf.Dispose();
            //4° Pasada, luz delantera
            device.DepthStencilSurface = g_pDepthStencil;
            pSurf = g_BumpFront.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            device.BeginScene();
            renderScene(parametros.meshes, light_front, "VERTEX_COLOR");
            renderScene(parametros.sol, light_front, "VERTEX_COLOR");
                renderScene(parametros.elementosRenderizables);
                if (!EjemploAlumno.workspace().camara.soyFPS())
                    renderScene(parametros.nave, light_front, "VERTEX_COLOR");
            device.EndScene();
            pSurf.Dispose();
            device.DepthStencilSurface = pOldDS;
            //5° Pasada, join de luces
            pSurf = g_pRenderTarget.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            //device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();
                bumpEffect_asteroides.Technique = "JoinBumpsTechnique";
                device.VertexFormat = CustomVertex.PositionTextured.Format;
                device.SetStreamSource(0, g_pVBV3D, 0);
                bumpEffect_asteroides.SetValue("luzSolarTarget", g_BumpSol);
                bumpEffect_asteroides.SetValue("luzIzqTarget", g_BumpIzq);
                bumpEffect_asteroides.SetValue("luzDerTarget", g_BumpDer);
                bumpEffect_asteroides.SetValue("luzFrontalTarget", g_BumpFront);
                bumpEffect_asteroides.Begin(FX.None);
                    bumpEffect_asteroides.BeginPass(0);
                        device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                    bumpEffect_asteroides.EndPass();
                bumpEffect_asteroides.End();
            device.EndScene();
            pSurf.Dispose();
            //Vuelvo a Setear el depthbuffer y el Render target
            device.SetRenderTarget(0, pOldRT);
            device.DepthStencilSurface = pOldDS;

            //Retorno la textura
            return g_pRenderTarget;
        }

        #region RenderScene

        public void renderScene(Nave nave, LightValues light, string technique)
        {
            actualizarEfect(bumpEffect_nave,light.posicion_ParaNave, light.direccion, light.angulo, light.color, light.intensidad_nave, light.atenuacion_nave, light.bumpiness);
            ((TgcMesh)nave.objeto).Effect = bumpEffect_nave;
            ((TgcMesh)nave.objeto).Technique = technique;
            nave.render();
            /*//Renderizamos las luces de la nave
            foreach(TgcBox cajita in lightMeshes)
            {
                cajita.Effect = bumpEffect_nave;
                cajita.Technique = technique;
                cajita.render();
            }*/
        }

        public void renderScene(Dibujable sol, LightValues light, string technique)
        {
            actualizarEfect(bumpEffect_sol, light.posicion_ParaSol, light.direccion, light.angulo, light.color, light.intensidad_sol, light.atenuacion_sol, light.bumpiness);
            ((TgcMesh)sol.objeto).Effect = bumpEffect_sol;
            ((TgcMesh)sol.objeto).Technique = technique;
            sol.render();
        }

        public void renderScene(List<Dibujable> meshes, LightValues light, string technique)
        {
            actualizarEfect(bumpEffect_asteroides, light.posicion_ParaAsteroide, light.direccion, light.angulo, light.color, light.intensidad_asteroide, light.atenuacion_asteroide, light.bumpiness);
            foreach (Dibujable dibujable in meshes)
            {
                if (dibujable.soyAsteroide())
                {
                    if (mainShader.fueraFrustrum(dibujable)) continue;
                    ((TgcMesh)dibujable.objeto).Effect = bumpEffect_asteroides;
                    ((TgcMesh)dibujable.objeto).Technique = technique;
                }
                else resetearRenderDefault(dibujable);
                dibujable.render();
            }
        }

        public void renderScene(List<IRenderObject> elementosRenderizables)
        {
            foreach (IRenderObject elemento in elementosRenderizables)
            {
                //((TgcArrow)elemento).Effect = bumpEffect_nave;
                //((TgcArrow)elemento).Technique = "BumpMappingTechnique";
                elemento.render();
            }
        }

        #endregion

        private void cargarBumpEffect()
        {
            //ASTEROIDES
            bumpEffect_asteroides = TgcShaders.loadEffect(ShaderDirectory);
            bumpEffect_asteroides.SetValue("texNormalMap", (BaseTexture) normalMapAsteroid.D3dTexture);
            bumpEffect_asteroides.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.Black));
            bumpEffect_asteroides.SetValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            bumpEffect_asteroides.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.White));
            bumpEffect_asteroides.SetValue("materialSpecularColor", ColorValue.FromColor(Color.White));
            bumpEffect_asteroides.SetValue("materialSpecularExp", 7f);

            //NAVE
            bumpEffect_nave = TgcShaders.loadEffect(ShaderDirectory);
            bumpEffect_nave.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.Black));
            bumpEffect_nave.SetValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            bumpEffect_nave.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.White));
            bumpEffect_nave.SetValue("materialSpecularColor", ColorValue.FromColor(Color.White));
            bumpEffect_nave.SetValue("materialSpecularExp", 7f);

            //SOL
            bumpEffect_sol = TgcShaders.loadEffect(ShaderDirectory);
            bumpEffect_sol.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.Black));
            bumpEffect_sol.SetValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            bumpEffect_sol.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.White));
            bumpEffect_sol.SetValue("materialSpecularColor", ColorValue.FromColor(Color.White));
            bumpEffect_sol.SetValue("materialSpecularExp", 7f);
        }
        
        #region Luces
        private LightValues crearLuz(Color color,float i_sol, float i_ast, float i_nave, float a_sol, float a_ast, float a_nave, float bump)
        {
            LightValues light = new LightValues();
            light.color = color;
            light.intensidad_sol = i_sol;
            light.intensidad_asteroide = i_ast;
            light.intensidad_nave = i_nave;
            light.atenuacion_sol = a_sol;
            light.atenuacion_asteroide = a_ast;
            light.atenuacion_nave = a_nave;
            light.bumpiness = bump;

            light.angulo = 0;
            light.direccion = new Vector3(0,0,0);
            return light;
        }

        private void actualizarEfect(Effect effect,Vector3 pos, Vector3 dir, float angle, Color color, float intensidad, float atenuacion, float bump)
        {
            Device d3dDevice = GuiController.Instance.D3dDevice;
            effect.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            effect.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);
            effect.SetValue("lightColor", ColorValue.FromColor(color));
            effect.SetValue("lightIntensity", intensidad);
            effect.SetValue("lightAttenuation", atenuacion);
            effect.SetValue("bumpiness", bump);
            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(pos));
            effect.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(eyePosition));
            effect.SetValue("spotLightDir", TgcParserUtils.vector3ToFloat3Array(dir));
            effect.SetValue("spotLightAngleCos", angle);
            effect.SetValue("spotLightExponent", 7f);
        }
        private void actualizarLuces()
        {
            
            Device device = GuiController.Instance.D3dDevice;

            eyePosition = EjemploAlumno.workspace().camara.PosicionDeCamara;
            Vector3 pos_sol = -EjemploAlumno.workspace().sol.getPosicion();

            light_sol.posicion_ParaNave = pos_sol;
            light_sol.posicion_ParaSol = pos_sol;// +new Vector3(500, 900, -900);
            light_sol.posicion_ParaAsteroide = pos_sol + new Vector3(0, 0, -18000);

            light_izq.color = (Color)GuiController.Instance.Modifiers["lightColor"];
            light_izq.posicion_ParaNave = EjemploAlumno.workspace().nave.puntoLuzIzq();
            light_izq.posicion_ParaAsteroide = light_izq.posicion_ParaNave;
            light_izq.posicion_ParaSol = light_izq.posicion_ParaNave;

            light_der.color = (Color)GuiController.Instance.Modifiers["lightColor"];
            light_der.posicion_ParaNave = EjemploAlumno.workspace().nave.puntoLuzDer();
            light_der.posicion_ParaAsteroide = light_der.posicion_ParaNave;
            light_der.posicion_ParaSol = light_der.posicion_ParaNave;

            light_front.color = (Color)GuiController.Instance.Modifiers["lightColor"];
            light_front.posicion_ParaNave = EjemploAlumno.workspace().nave.puntoLuzCent();
            light_front.posicion_ParaAsteroide = light_der.posicion_ParaNave;
            light_front.posicion_ParaSol = light_der.posicion_ParaNave;

            lightMeshes[2].setPositionSize(light_der.posicion_ParaNave, new Vector3(0.1f, 0.1f, 0.1f));
            lightMeshes[1].setPositionSize(light_izq.posicion_ParaNave, new Vector3(0.1f, 0.1f, 0.1f));
            lightMeshes[0].setPositionSize(light_front.posicion_ParaNave, new Vector3(0.1f, 0.1f, 0.1f));

            /*device.Lights[0].Position = light_der.posicion_ParaNave;
            device.Lights[0].Diffuse = light_der.color;
            device.Lights[0].Specular = light_der.color;
            device.Lights[0].Update();
            device.Lights[1].Position = light_izq.posicion_ParaNave;
            device.Lights[1].Diffuse = light_izq.color;
            device.Lights[1].Specular = light_izq.color;
            device.Lights[1].Update();
            device.Lights[2].Position = light_front.posicion_ParaNave;
            device.Lights[2].Diffuse = light_front.color;
            device.Lights[2].Specular = light_front.color;
            device.Lights[2].Update();
             * */

        }

        #endregion

        #region Auxiliares

        private void resetearRenderDefault(Dibujable dibujable)
        {
            dibujable.objeto.Effect = GuiController.Instance.Shaders.TgcMeshShader;
            dibujable.objeto.Technique = GuiController.Instance.Shaders.getTgcMeshTechnique(((TgcMesh)dibujable.objeto).RenderType);
        }

        public SuperRender.tipo tipoShader()
        {
            return SuperRender.tipo.BUMP;
        }

        public void close()
        {
            g_BumpDer.Dispose();
            g_BumpIzq.Dispose();
            g_BumpSol.Dispose();
            g_BumpFront.Dispose();
            g_pDepthStencil.Dispose();
        }

        #endregion


        #region Parte de la Interfaz no implementada

        public void renderScene(Nave nave, string technique)
        {
            throw new NotImplementedException();
        }

        public void renderScene(Dibujable sol, string technique)
        {
            throw new NotImplementedException();
        }

        public void renderScene(List<Dibujable> dibujables, string technique)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
