using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;

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
        public override string getDescription() { return "Welcome to TheGRID                                                                            FLECHAS: Rotaciones              WASD: Desplazamiento              LeftShift: Efecto Blur                     LeftCtrl: Modo Crucero                  Espacio - Disparo Principal"; }
        #endregion

        #region ATRIBUTOS
        Escenario scheme;
        internal Escenario Escenario { get { return scheme; } }
        static EjemploAlumno singleton;
        Nave nave;
        public float velocidadBlur = 0;
        bool velocidadCrucero = false;
        public float tiempoBlur=0.3f;
        private Dibujable objetoPrincipal;  //Este va a ser configurable con el panel de pantalla.
        public Dibujable ObjetoPrincipal { get { return objetoPrincipal; } }
        List<Dibujable> listaDibujable = new List<Dibujable>();
        float timeLaser = 0; //Inicializacion.
        const float betweenTime = 0.15f;    //Tiempo de espera entre cada disparo de laser.

        //lista de meshes para implementar el motion blur
        public List<TgcMesh> meshCollection = new List<TgcMesh>();

        //Modificador de la camara del proyecto
        public CambioCamara camara;
        TgcArrow arrow;
        private TgcFrustum currentFrustrum;
        public TgcFrustum CurrentFrustrum { get { return currentFrustrum; } }
        private SkySphere skySphere;
        public SkySphere SkySphere { get { return skySphere; } }
        //TgcBox suelo;
        //TgcSkyBox skyBox;
        //ManagerLaser laserManager;
        //private ManagerAsteroide asteroidManager;
        SuperRender superRender;
        internal SuperRender Shader { get { return superRender; } }
        #endregion

        #region METODOS AUXILIARES
        public static EjemploAlumno workspace() { return singleton; }
        public static void addMesh(TgcMesh unMesh){
            singleton.meshCollection.Add(unMesh);
        }
        public TgcFrustum getCurrentFrustrum() { return currentFrustrum; }
        private void crearSkyBox()
        {
            /*
            //Crear SkyBox 
            //skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, 0, 0);
            skyBox.Size = new Vector3(15000, 15000, 15000);
            //Crear suelo
            TgcTexture pisoTexture = TgcTexture.createTexture(d3dDevice, alumnoMediaFolder + "TheGrid\\SkyBox\\adelante.jpg");
            suelo = TgcBox.fromSize(new Vector3(0, 0, 9500), new Vector3(1000, 1000, 0), pisoTexture);
            //Configurar color
            //skyBox.Color = Color.OrangeRed;
            //Configurar las texturas para cada una de las 6 caras
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, alumnoMediaFolder + "TheGrid\\SkyBox\\arriba.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, alumnoMediaFolder + "TheGrid\\SkyBox\\abajo.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, alumnoMediaFolder + "TheGrid\\SkyBox\\izquierda.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, alumnoMediaFolder + "TheGrid\\SkyBox\\derecha.jpg");
            //Hay veces es necesario invertir las texturas Front y Back si se pasa de un sistema RightHanded a uno LeftHanded
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, alumnoMediaFolder + "TheGrid\\SkyBox\\adelante.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, alumnoMediaFolder + "TheGrid\\SkyBox\\atras.jpg");
            //Actualizar todos los valores para crear el SkyBox
            skyBox.updateValues();
             */
        }
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

            //Crear la nave
            nave = new Nave();

            skySphere = new SkySphere();

            //Creamos el escenario.
            scheme = new Escenario(nave);
            //scheme.loadChapter2();

            //Cargamos la nave como objeto principal.
            objetoPrincipal = nave;
            camara = new CambioCamara(nave);
            
            /*
            //Flecha direccion objetivo
            arrow = new TgcArrow();
            arrow.BodyColor = Color.FromArgb(230, Color.Cyan);
            arrow.HeadColor = Color.FromArgb(230, Color.Yellow);
            */

            #region PANEL DERECHO

            //Cargamos valores en el panel lateral
            GuiController.Instance.UserVars.addVar("Vel-Actual:");
            GuiController.Instance.UserVars.addVar("Integtidad Nave:");
            GuiController.Instance.UserVars.addVar("Integridad Escudos:");
            GuiController.Instance.UserVars.addVar("Posicion X:");
            GuiController.Instance.UserVars.addVar("Posicion Y:");
            GuiController.Instance.UserVars.addVar("Posicion Z:");
            //Cargar valor en UserVar
            GuiController.Instance.UserVars.setValue("Vel-Actual:", objetoPrincipal.velocidadActual());
            GuiController.Instance.UserVars.setValue("Integtidad Nave:", objetoPrincipal.explosion.vida);
            GuiController.Instance.UserVars.setValue("Integridad Escudos:", objetoPrincipal.explosion.escudo);
            GuiController.Instance.UserVars.setValue("Posicion X:", objetoPrincipal.getPosicion().X);
            GuiController.Instance.UserVars.setValue("Posicion Y:", objetoPrincipal.getPosicion().Y);
            GuiController.Instance.UserVars.setValue("Posicion Z:", objetoPrincipal.getPosicion().Z);
            //Crear un modifier para un valor FLOAT
            //GuiController.Instance.Modifiers.addFloat("Aceleracion", 0f,500f, objetoPrincipal.getAceleracion());  De momento lo saco.
            //GuiController.Instance.Modifiers.addFloat("Frenado", 0f, 1000f, objetoPrincipal.getAcelFrenado());    De momento lo saco.
            //Crear un modifier para un ComboBox con opciones
            string[] opciones0 = new string[] { "THE OPENING", "IMPULSE DRIVE", "WELCOME HOME", "VACUUM" };
            GuiController.Instance.Modifiers.addInterval("Escenario Actual", opciones0, 3);
            string[] opciones1 = new string[] { "Tercera Persona", "Camara FPS", "Libre" };
            GuiController.Instance.Modifiers.addInterval("Tipo de Camara", opciones1, 0);
            string[] opciones2 = new string[] { "Activado", "Desactivado" };
            GuiController.Instance.Modifiers.addInterval("Velocidad Manual", opciones2, 0);
            string[] opciones3 = new string[] { "Activado", "Desactivado" };
            GuiController.Instance.Modifiers.addInterval("Desplaz. Avanzado", opciones3, 0);
            //string[] opciones4 = new string[] { "Activado", "Desactivado" };
            //GuiController.Instance.Modifiers.addInterval("Rotacion Avanzada", opciones4, 1);  De momento lo saco.
            string opcionElegida = (string)GuiController.Instance.Modifiers["Escenario Actual"];
            scheme.chequearCambio(opcionElegida);

            #endregion
        }   

        public override void render(float elapsedTime)
        {
            #region -----KEYS-----
            TgcD3dInput input = GuiController.Instance.D3dInput;

            //Flechas
            if (input.keyDown(Key.Left)) { nave.rotacion = 1; }
            if (input.keyDown(Key.Right)) { nave.rotacion = -1; }
            if (input.keyDown(Key.Up)) { nave.inclinacion = 1; }
            if (input.keyDown(Key.Down)) { nave.inclinacion = -1; }
            //Letras
            if (input.keyDown(Key.A)) { nave.giro = -1; }
            if (input.keyDown(Key.D)) { nave.giro = 1; }
            if (input.keyDown(Key.W)) { nave.acelerar(); }
            if (input.keyDown(Key.S)) { if (!superRender.motionBlurActivado)nave.frenar(); }
            if (input.keyPressed(Key.S)) { objetoPrincipal.fisica.desactivarCrucero(); velocidadCrucero = false; }
            if (input.keyDown(Key.Z)) { nave.rotarPorVectorDeAngulos(new Vector3(0, 0, 15)); }
            if (input.keyPressed(Key.LeftControl)) 
            {
                if (velocidadCrucero)
                {
                    objetoPrincipal.fisica.desactivarCrucero();
                    velocidadCrucero = false;
                }
                else
                {
                    objetoPrincipal.fisica.activarCrucero();
                    velocidadCrucero = true;
                }
            }
            if (input.keyPressed(Key.LeftShift)) 
            {
                if (superRender.motionBlurActivado)
                {
                    superRender.motionBlurActivado = false;
                    tiempoBlur = 0.3f;
                    velocidadBlur = 0;
                }
                else
                {
                    if (objetoPrincipal.velocidadActual() == objetoPrincipal.fisica.velocidadMaxima)
                    {
                        superRender.motionBlurActivado = true;
                        //velocidadBlur = objetoPrincipal.velocidadActual();
                    }

                }
            }
            if (input.keyDown(Key.P)) { scheme.asteroidManager.explotaAlPrimero(); }
            if (input.keyDown(Key.Space))
            {
                timeLaser += elapsedTime;
                if (timeLaser > betweenTime)
                {
                    scheme.dispararLaser();
                    //laserManager.fabricar(nave.getEjes(),nave.getPosicion());                  
                    timeLaser = 0;
                }
            }
            #endregion

            #region -----Update------
            nave.rotarPorTiempo(elapsedTime, listaDibujable);
            nave.desplazarsePorTiempo(elapsedTime, new List<Dibujable>(scheme.cuerpos()));

            scheme.refrescar(elapsedTime);

            camara.cambiarPosicionCamara();
            currentFrustrum.updateMesh(GuiController.Instance.CurrentCamera.getPosition(),GuiController.Instance.CurrentCamera.getLookAt());
            
            /*
            //Cargar valores de la flecha
            Vector3 navePos = nave.getPosicion();
            Vector3 naveDir = Vector3.Subtract(new Vector3(0, 0, 10000), nave.getDireccion());
            naveDir.Normalize();
            naveDir.Multiply(75);
            arrow.PStart = navePos;
            arrow.PEnd = navePos + naveDir;
            arrow.Thickness = 0.5f;
            arrow.HeadSize = new Vector2(2,2);
            arrow.updateValues();
            */
            
            skySphere.render();     //Solo actualiza pos. Tiene deshabiltiado los render propiamente dicho.
            //arrow.render();
            //skySphere.render();
            //suelo.render();
            #endregion

            superRender.render((TgcMesh)nave.objeto, meshCollection, elapsedTime); //Redirige todo lo que renderiza dentro del "shader"

            #region Refrescar panel lateral
            string opcionElegida = (string)GuiController.Instance.Modifiers["Tipo de Camara"];
            camara.chequearCambio(opcionElegida);
            opcionElegida = (string)GuiController.Instance.Modifiers["Escenario Actual"];
            scheme.chequearCambio(opcionElegida);
            opcionElegida = (string)GuiController.Instance.Modifiers["Velocidad Manual"];
            if (String.Compare(opcionElegida, "Activado") == 0) objetoPrincipal.velocidadManual = true; else objetoPrincipal.velocidadManual = false;
            opcionElegida = (string)GuiController.Instance.Modifiers["Desplaz. Avanzado"];
            if (String.Compare(opcionElegida, "Activado") == 0) objetoPrincipal.desplazamientoReal = true; else objetoPrincipal.desplazamientoReal = false;
            //opcionElegida = (string)GuiController.Instance.Modifiers["Rotacion Avanzada"];
            //if (String.Compare(opcionElegida, "Activado") == 0) objetoPrincipal.rotacionReal = true; else objetoPrincipal.rotacionReal = false;   De momento lo saco.
            //Refrescar User Vars
            if (superRender.motionBlurActivado)
            {
                tiempoBlur += elapsedTime;
                velocidadBlur = (float)Math.Pow(100D, tiempoBlur);
                if (velocidadBlur > 299800) velocidadBlur = 299800;
                GuiController.Instance.UserVars.setValue("Vel-Actual:", velocidadBlur + objetoPrincipal.velocidadActual());
            }
            else GuiController.Instance.UserVars.setValue("Vel-Actual:", objetoPrincipal.velocidadActual());            
            GuiController.Instance.UserVars.setValue("Posicion X:", objetoPrincipal.getPosicion().X);
            GuiController.Instance.UserVars.setValue("Posicion Y:", objetoPrincipal.getPosicion().Y);
            GuiController.Instance.UserVars.setValue("Posicion Z:", objetoPrincipal.getPosicion().Z);
            GuiController.Instance.UserVars.setValue("Integtidad Nave:", objetoPrincipal.explosion.vida);
            GuiController.Instance.UserVars.setValue("Integridad Escudos:", objetoPrincipal.explosion.escudo);
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
            arrow.dispose();
            skySphere.dispose();
            //suelo.dispose();
        }
    }
}

