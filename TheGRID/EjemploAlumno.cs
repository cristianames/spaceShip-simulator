using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;

using TgcViewer;
using TgcViewer.Example;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils.TgcGeometry;
using TgcViewer.Utils.TgcSceneLoader;
using TgcViewer.Utils.Input;
using TgcViewer.Utils.Terrain;

using AlumnoEjemplos.TheGRID;
using AlumnoEjemplos.TheGRID.Colisiones;
using AlumnoEjemplos.TheGRID.Explosiones;
using AlumnoEjemplos.TheGRID.Shaders;
using AlumnoEjemplos.TheGRID.Camara;
using AlumnoEjemplos.TheGRID.InterfazGrafica;

namespace AlumnoEjemplos.TheGRID
{
    public class EjemploAlumno : TgcExample
    {
        #region TGCVIEWER METADATA
        /// Categor�a a la que pertenece el ejemplo.
        /// Influye en donde se va a ver en el �rbol de la derecha de la pantalla.
        public override string getCategory(){ return "AlumnoEjemplos"; }
        /// Completar nombre del grupo en formato Grupo NN
        public override string getName(){ return "Grupo TheGRID"; }
        /// Completar con la descripci�n del TP
        public override string getDescription() { return "Welcome to TheGRID"+Environment.NewLine+"FLECHAS: Rotaciones"+Environment.NewLine+"WASD: Desplazamiento"+Environment.NewLine+"LeftShift: Efecto Blur"+Environment.NewLine+"LeftCtrl: Modo Crucero"+Environment.NewLine+"Espacio: Disparo Principal"+Environment.NewLine+"RightShift: Disparo Secundario"+Environment.NewLine+"Z: DO A BARREL ROLL!!"; }
        #endregion

        #region ATRIBUTOS
        Escenario scheme;
        internal Escenario Escenario { get { return scheme; } }
        static EjemploAlumno singleton;
        public Nave nave;
        public Dibujable sol;
        public Estrella estrellaControl;
        public List<TgcMesh> estrellas;
        public List<TgcMesh> estrellasNo;
        public bool boundingBoxes;
        public float velocidadBlur = 0;
        bool velocidadCrucero = false;
        public float tiempoBlur=0.3f;
        public Dibujable ObjetoPrincipal { get { return nave; } }
        List<Dibujable> listaDibujable = new List<Dibujable>();
        float timeLaser = 0; //Inicializacion.
        const float betweenTime = 0.15f;    //Tiempo de espera entre cada disparo de laser.
        public float tiempoPupila;
        //lista de meshes para implementar el motion blur
        public List<Dibujable> dibujableCollection = new List<Dibujable>();
        public List<IRenderObject> objectosNoMeshesCollection = new List<IRenderObject>();
        public List<TgcMesh> objetosBrillantes = new List<TgcMesh>();
        //Modificador de la camara del proyecto
        public CambioCamara camara;
        private TgcFrustum currentFrustrum;
        public TgcFrustum CurrentFrustrum { get { return currentFrustrum; } }
        private SkySphere skySphere;
        public SkySphere SkySphere { get { return skySphere; } }
        SuperRender superRender;
        internal SuperRender Shader { get { return superRender; } }
        public Musique music = new Musique();
        //Lazer Azul
        float pressed_time_lazer = 0;
        //Variables para el parpadeo
        float tiempo_acum = 0;
        float periodo_parpadeo = 1.5f;
        public bool parpadeoIzq = true;
        public bool parpadeoDer = false;
        public bool linterna = true;
        public Point mouseCenter;
        public int altoPantalla;
        public int anchoPantalla;
        //GUI
        public bool pausa = false;
        public bool config = false;
        Pausa guiPausa = new Pausa();
        Configuracion guiConfig;
        #endregion

        #region METODOS AUXILIARES
        public static EjemploAlumno workspace() { return singleton; }
        public static void addMesh(Dibujable unDibujable){
            singleton.dibujableCollection.Add(unDibujable);
        }
        public static void addRenderizable(IRenderObject unObjeto)
        {
            singleton.objectosNoMeshesCollection.Add(unObjeto);
        }
        public static void addMesh(TgcMesh unObjeto)
        {
            singleton.objetosBrillantes.Add(unObjeto);
        }
        public TgcFrustum getCurrentFrustrum() { return currentFrustrum; }
        private static string tg_Folder = GuiController.Instance.AlumnoEjemplosMediaDir + "\\TheGrid\\";
        public static string TG_Folder { get { return tg_Folder; } }
        #endregion

        public override void init()
        {
            #region INICIALIZACIONES POCO IMPORTANTES
            
            EjemploAlumno.singleton = this;
            Microsoft.DirectX.Direct3D.Device d3dDevice = GuiController.Instance.D3dDevice;
            string alumnoMediaFolder = GuiController.Instance.AlumnoEjemplosMediaDir;
            GuiController.Instance.CustomRenderEnabled = true;
            #endregion

            //Informacion sobre los movimientos en la parte inferior de la pantalla.
            TgcViewer.Utils.Logger logger = GuiController.Instance.Logger;
            logger.clear();
            //logger.log("Welcome to TheGRID", Color.DarkCyan);
            logger.log("Le damos la bienvenida a este simulador. A continuacion le indicaremos los controles que puede utilizar:");
            logger.log("Paso 1: Para rotar sobre los ejes Z y X utilice las FLECHAS de direccion. Para rotar sobre el eje Y utilice las teclas A y D.");
            logger.log("Paso 2: Para avanzar presione la W pero cuidado con el movimiento inercial. Seguramente se va a dar cuenta de lo que hablo. Para frenar puede presionar la tecla S. Esto estabiliza la nave sin importar que fuerzas tiren de ella.");
            logger.log("Paso 3: Para avanzar con cuidado, acelere o frene hasta la velocidad deseada, pulse una vez LeftCtrl y luego acelere. Esto activa el modo crucero. Para desactivarlo basta con frenar un poco o volver a pulsar LeftCtrl.");
            logger.log("Paso 4: Para activar el Motion Blur debe ir a la maxima velocidad y luego pulsar una vez LeftShift. La desactivacion es de la misma forma. Por ultimo pruebe disparar presionando SpaceBar. -- Disfrute el ejemplo.");

            currentFrustrum = new TgcFrustum();           
            superRender = new SuperRender();
            guiConfig = new Configuracion(music);

            //Crear la nave
            nave = new Nave();

            skySphere = new SkySphere("SkyBox\\skysphere-TgcScene.xml");
            addMesh(skySphere.dibujable_skySphere);

            //Creamos el escenario.
            scheme = new Escenario(nave);

            //Cargamos la nave como objeto principal.
            camara = new CambioCamara(nave);
                        
            //Flecha direccion objetivo
            //arrow = new TgcArrow();
            //arrow.BodyColor = Color.FromArgb(230, Color.Cyan);
            //arrow.HeadColor = Color.FromArgb(230, Color.Yellow);
            //this.objectosNoMeshesCollection.Add(arrow);

            //Cargamos el audio
            //music.playBackgound();

            Control focusWindows = GuiController.Instance.D3dDevice.CreationParameters.FocusWindow;
            mouseCenter = focusWindows.PointToScreen(
                new Point(
                    focusWindows.Width / 2,
                    focusWindows.Height / 2)
                    );

            altoPantalla = focusWindows.Height;
            anchoPantalla = focusWindows.Width;

            #region PANEL DERECHO

            //Cargamos valores en el panel lateral
            GuiController.Instance.UserVars.addVar("Vel-Actual:");
            /*GuiController.Instance.UserVars.addVar("Integtidad Nave:");
            GuiController.Instance.UserVars.addVar("Integridad Escudos:");
            GuiController.Instance.UserVars.addVar("Posicion X:");
            GuiController.Instance.UserVars.addVar("Posicion Y:");
            GuiController.Instance.UserVars.addVar("Posicion Z:");
             */
            //Cargar valor en UserVar
            GuiController.Instance.UserVars.setValue("Vel-Actual:", nave.velocidadActual());
            /*GuiController.Instance.UserVars.setValue("Integtidad Nave:", objetoPrincipal.explosion.vida);
            GuiController.Instance.UserVars.setValue("Integridad Escudos:", objetoPrincipal.explosion.escudo);
            GuiController.Instance.UserVars.setValue("Posicion X:", objetoPrincipal.getPosicion().X);
            GuiController.Instance.UserVars.setValue("Posicion Y:", objetoPrincipal.getPosicion().Y);
            GuiController.Instance.UserVars.setValue("Posicion Z:", objetoPrincipal.getPosicion().Z);
             */
            //Crear un modifier para un valor FLOAT
            //GuiController.Instance.Modifiers.addFloat("Aceleracion", 0f,500f, objetoPrincipal.getAceleracion());  De momento lo saco.
            //GuiController.Instance.Modifiers.addFloat("Frenado", 0f, 1000f, objetoPrincipal.getAcelFrenado());    De momento lo saco.
            //Crear un modifier para un ComboBox con opciones
            //List<int> pistaDeAudio = new List<int>(){0,1,2,3,4,5,6,7,8,9};
            string[] opciones0 = new string[] { "THE OPENING", "IMPULSE DRIVE", "WELCOME HOME" };
            GuiController.Instance.Modifiers.addInterval("Escenario Actual", opciones0, 0);
            string[] opciones1 = new string[] { "Tercera Persona", "Camara FPS", "Libre" };
            GuiController.Instance.Modifiers.addInterval("Tipo de Camara", opciones1, 0);
            string[] opciones2 = new string[] { "Lista Completa", "Castor", "Derezzed", "M4 Part 2", "ME Theme", "New Worlds", "Solar Sailer", "Spectre", "Tali", "The Son of Flynn", "Tron Ending", "Sin Musica" };
            GuiController.Instance.Modifiers.addInterval("Musica de fondo", opciones2, 0);
            //GuiController.Instance.Modifiers.addBoolean("Velocidad Manual", "Activado", true);
            GuiController.Instance.Modifiers.addBoolean("Desplaz. Avanzado", "Activado", true);
            GuiController.Instance.Modifiers.addBoolean("Ver BoundingBox", "Activado", false);
            GuiController.Instance.Modifiers.addBoolean("glow", "Activado", true);
            GuiController.Instance.Modifiers.addBoolean("Linterna", "Activado", true);
            GuiController.Instance.Modifiers.addBoolean("Luces de Posicion", "Activado", true);
            //string[] opciones4 = new string[] { "Activado", "Desactivado" };
            //GuiController.Instance.Modifiers.addInterval("Rotacion Avanzada", opciones4, 1);  De momento lo saco.
            string opcionElegida = (string)GuiController.Instance.Modifiers["Escenario Actual"];
            scheme.chequearCambio(opcionElegida);
            opcionElegida = (string)GuiController.Instance.Modifiers["Musica de fondo"];
            music.chequearCambio(opcionElegida);
            music.refrescar();
            #endregion
        }   

        public override void render(float elapsedTime)
        {
            TgcD3dInput input = GuiController.Instance.D3dInput;
            if (pausa)
            {
                guiPausa.pausa();
                if (input.keyPressed(Key.P)) 
                { 
                    pausa = false;
                    music.playPauseBackgound();
                }
                return;
            }
            if (config)
            {
                guiConfig.operar(elapsedTime);
                return;
            }
            #region -----KEYS-----
            if (input.keyPressed(Key.I)) { music.refrescar(); }
            if (input.keyPressed(Key.P)) { 
                pausa = true;
                music.playPauseBackgound();
            }     //Pausa.
            if (input.keyPressed(Key.C))
            {
                config = true;
                guiConfig.restart();
                music.playPauseBackgound();
            }     //Pausa.

            //Flechas
            
            //int giroX = -1+2*((int)GuiController.Instance.D3dInput.Xpos/anchoPantalla);
            //int giroY = -1+2*((int)GuiController.Instance.D3dInput.Ypos/altoPantalla);
            int sensibilidad =1;
            nave.rotarPorVectorDeAngulos(new Vector3(GuiController.Instance.D3dInput.YposRelative * -sensibilidad, 0, GuiController.Instance.D3dInput.XposRelative * -sensibilidad));
            if (input.keyDown(Key.Left)) { nave.rotacion = 1; }
           // if (GuiController.Instance.D3dInput.XposRelative < 0 && GuiController.Instance.D3dInput.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT)) {nave.rotacion = 1;  }
            if (input.keyDown(Key.Right)) { nave.rotacion = -1; }
            //if (GuiController.Instance.D3dInput.XposRelative > 0 && GuiController.Instance.D3dInput.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT)) { nave.rotacion = -1; }
            if (input.keyDown(Key.Up)) { nave.inclinacion = 1; }
           // if (GuiController.Instance.D3dInput.YposRelative > 0 && GuiController.Instance.D3dInput.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT)) { nave.inclinacion = 1; }
            if (input.keyDown(Key.Down)) { nave.inclinacion = -1; }
           // if (GuiController.Instance.D3dInput.YposRelative < 0 && GuiController.Instance.D3dInput.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT)) { nave.inclinacion = -1; }
            //Letras
            
            if (input.keyDown(Key.A)) { nave.giro = -1; }
            
            if (input.keyDown(Key.D)) { nave.giro = 1; }
            if (input.keyDown(Key.W)) { nave.acelerar(); }
            if (input.keyDown(Key.S)) { if (!superRender.motionBlurActivado)nave.frenar(); }
            if (input.keyPressed(Key.S)) { nave.fisica.desactivarCrucero(); velocidadCrucero = false; }
            if (input.keyDown(Key.Z)) { nave.rotarPorVectorDeAngulos(new Vector3(0, 0, 15)); }
            if (input.keyPressed(Key.LeftControl)) 
            {
                if (velocidadCrucero)
                {
                    nave.fisica.desactivarCrucero();
                    velocidadCrucero = false;
                }
                else
                {
                    nave.fisica.activarCrucero();
                    velocidadCrucero = true;
                }
            }
            if (scheme.escenarioActual == Escenario.TipoModo.IMPULSE_DRIVE)
            {
                if (input.keyPressed(Key.LeftShift))
                {
                    if (superRender.motionBlurActivado)
                    {
                        superRender.motionBlurActivado = false;
                        tiempoBlur = 0.3f;
                        nave.fisica.velocidadMaxima = nave.velMaxNormal;
                        nave.fisica.velocidadCrucero = nave.velMaxNormal;
                        nave.fisica.aceleracion = nave.acelNormal;
                        nave.velocidadRadial = nave.rotNormal;
                        nave.fisica.velocidadInstantanea = nave.velMaxNormal;//Esto esta para cuando el blur se desactiva, desacelrar rapidamente la nave
                        //camara = new CambioCamara(nave);
                    }
                    else
                    {
                        if (nave.velocidadActual() >= nave.fisica.velocidadMaxima)
                        {
                            superRender.motionBlurActivado = true;
                            nave.fisica.velocidadMaxima = nave.velMaxBlur;
                            nave.fisica.desactivarCrucero(); velocidadCrucero = false;
                            nave.velocidadRadial = nave.rotBlur;
                        }

                    }
                }
            }

            


            //if (superRender.motionBlurActivado) nave.acelerar();//Si el Blur esta activado la nave solamente acelera
            //else if (objetoPrincipal.velocidadActual() > objetoPrincipal.fisica.velocidadMaxima) objetoPrincipal.fisica.velocidadInstantanea = nave.velMaxNormal;//Esto esta para cuando el blur se desactiva, desacelrar rapidamente la nave




            //if (input.keyDown(Key.P)) { scheme.asteroidManager.explotaAlPrimero(); }
            if (scheme.escenarioActual == Escenario.TipoModo.THE_OPENING)
            {
                if (input.keyDown(Key.Space))
                {
                    timeLaser += elapsedTime;
                    if (timeLaser > betweenTime)
                    {
                        scheme.dispararLaser();
                        timeLaser = 0;
                    }
                }
                if (input.keyDown(Key.RightShift))
                {
                    pressed_time_lazer += elapsedTime;
                    music.playLazerCarga();
                }
                else
                    if (input.keyUp(Key.RightShift))
                    {
                        scheme.dispararLaserAzul(pressed_time_lazer);
                        pressed_time_lazer = 0;
                        music.playLazer2();
                    }
            }

            #endregion

            #region -----Update------
            tiempoPupila = elapsedTime; //Para el HDRL

            #region Parpadeo de las luces
            tiempo_acum += elapsedTime;
            if(tiempo_acum >= periodo_parpadeo)
            {
                if ((bool)GuiController.Instance.Modifiers["Luces de Posicion"])
                {
                    if (parpadeoIzq)
                    {
                        parpadeoIzq = false;
                        parpadeoDer = true;
                    }
                    else
                    {
                        parpadeoIzq = true;
                        parpadeoDer = false;
                    }
                }
                else
                {
                    parpadeoDer = false;
                    parpadeoIzq = false;
                }
                tiempo_acum = 0;
            }
            #endregion

            //Aceleracion Blur
            if (superRender.motionBlurActivado && tiempoBlur < 5f)
            {
                tiempoBlur += elapsedTime;
                nave.fisica.aceleracion += FastMath.Pow(tiempoBlur, 10);
                nave.acelerar();
            }


            nave.rotarPorTiempo(elapsedTime, listaDibujable);
            nave.desplazarsePorTiempo(elapsedTime, new List<Dibujable>(scheme.CuerposGravitacionales));

            scheme.refrescar(elapsedTime);

            camara.cambiarPosicionCamara();
            currentFrustrum.updateMesh(GuiController.Instance.CurrentCamera.getPosition(),GuiController.Instance.CurrentCamera.getLookAt());
            
            //Si estamos en una vel de warp considerable, le damos sonido
            if (nave.velocidadActual() > 25000f)
                music.playWarp();
            else
                music.stopWarp();

            //Cargar valores de la flecha
            //Vector3 navePos = nave.getPosicion();
            //Vector3 naveDir = Vector3.Subtract(new Vector3(0, 0, 10000), nave.getDireccion());
            //naveDir.Normalize();
            //naveDir.Multiply(75);
            //arrow.PStart = navePos;
            //arrow.PEnd = navePos + naveDir;
            //arrow.Thickness = 0.5f;
            //arrow.HeadSize = new Vector2(2,2);
            //arrow.updateValues();
            
            skySphere.render(nave);     //Solo actualiza pos. Tiene deshabiltiado los render propiamente dicho.
            #endregion
           
            superRender.render(nave, sol, dibujableCollection, objectosNoMeshesCollection, objetosBrillantes); //Redirige todo lo que renderiza dentro del "shader"

            #region Refrescar panel lateral
            string opcionElegida = (string)GuiController.Instance.Modifiers["Tipo de Camara"];
            camara.chequearCambio(opcionElegida);
            opcionElegida = (string)GuiController.Instance.Modifiers["Escenario Actual"];
            scheme.chequearCambio(opcionElegida);
            opcionElegida = (string)GuiController.Instance.Modifiers["Musica de fondo"];
            music.chequearCambio(opcionElegida);
            //objetoPrincipal.velocidadManual = (bool)GuiController.Instance.Modifiers["Velocidad Manual"];
            nave.desplazamientoReal = (bool)GuiController.Instance.Modifiers["Desplaz. Avanzado"];
            boundingBoxes = (bool)GuiController.Instance.Modifiers["Ver BoundingBox"];
            linterna = (bool)GuiController.Instance.Modifiers["Linterna"];
            //opcionElegida = (string)GuiController.Instance.Modifiers["Rotacion Avanzada"];
            //if (String.Compare(opcionElegida, "Activado") == 0) objetoPrincipal.rotacionReal = true; else objetoPrincipal.rotacionReal = false;   De momento lo saco.
            
            //Refrescar User Vars
            GuiController.Instance.UserVars.setValue("Vel-Actual:", nave.velocidadActual());
            /*GuiController.Instance.UserVars.setValue("Posicion X:", objetoPrincipal.getPosicion().X);
            GuiController.Instance.UserVars.setValue("Posicion Y:", objetoPrincipal.getPosicion().Y);
            GuiController.Instance.UserVars.setValue("Posicion Z:", objetoPrincipal.getPosicion().Z);
            GuiController.Instance.UserVars.setValue("Integtidad Nave:", objetoPrincipal.explosion.vida);
            GuiController.Instance.UserVars.setValue("Integridad Escudos:", objetoPrincipal.explosion.escudo);
             */
            //Obtener valores de Modifiers
            //objetoPrincipal.fisica.aceleracion = (float)GuiController.Instance.Modifiers["Aceleracion"];  De momento lo saco.
            //objetoPrincipal.fisica.acelFrenado = (float)GuiController.Instance.Modifiers["Frenado"];      De momento lo saco.
            #endregion
        }

        public override void close()
        {
            scheme.asteroidManager.destruirListas();
            scheme.laserManager.destruirListas();
            scheme.dispose();
            nave.dispose();
            //arrow.dispose();
            skySphere.dispose();
            music.liberarRecursos();
        }
    }
}

