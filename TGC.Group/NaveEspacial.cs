﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;

namespace TGC.Group
{
    public class NaveEspacial
    {

        public TgcScene Scene { get; set; }

        public TGCMatrix TransformMatix { get; set; }

        public TGCMatrix ScaleFactor { get; set; }
        public TGCVector3 RotationVector { get; set; }
        public TGCVector3 MovementVector { get; set; }
        public float speed = 1000f;
        private bool shouldBarrelRoll = false;
        private bool shouldLeft90Spin = false;
        private bool shouldRight90Spin = false;
        private bool stopSpinning = false;
        public Arma ArmaPrincipal { get; private set; }
        private TGCVector3 shipShotSize = new TGCVector3(2f, 2f, 8f);


        private Stopwatch coolDownMovimientos = null;


        private readonly float COOLDOWNMOVIMIENTOS = 2000;

        public int Vida { get; protected set; }  = 100;

        public float afterBurnFuel { get; set; } = 100;

        public TgcBoundingOrientedBox OOB
        {
            private set; get;
        }

        public NaveEspacial(string MediaDir, string modelToUse, int danio,int cdDisparo)
        {
            this.Scene = new TgcSceneLoader().loadSceneFromFile(MediaDir + "XWing/" + modelToUse, MediaDir + "XWing/");
            this.TransformMatix = TGCMatrix.Identity;
            this.ScaleFactor = TGCMatrix.Identity;
            this.RotationVector = TGCVector3.Empty;
            this.MovementVector = TGCVector3.Empty;

            this.ArmaPrincipal = new Arma(shipShotSize, Color.Red, danio, cdDisparo, this.GetPosition());
            this.coolDownMovimientos = Stopwatch.StartNew();


            this.ActionOnNave((mesh) => {
                mesh.AutoTransform = false; //Desactivar el autotransform para poder usar el mesh.transform.
            });
        }

        //public NaveEspacial(string MediaDir, string modelToUse, int danio): this(MediaDir, modelToUse)
        //{
        //    this.ArmaPrincipal.Danio = danio;
        //}

        public void Daniar(int cantidadDanio)
        {
            if(!this.shouldBarrelRoll)
                this.Vida -= cantidadDanio;
        }

        public void GastarFuel(float cantUsada, Hud hud)
        {
            this.afterBurnFuel = Math.Max(this.afterBurnFuel - cantUsada, 0);
            hud.ReducirFuel(afterBurnFuel);
        }

        public void RecargarFuel(float cantUsada)
        {
            this.afterBurnFuel = Math.Min(this.afterBurnFuel + cantUsada, 100);
        }

        public void Disparar()
        {
            this.ArmaPrincipal.Disparar(this.MovementVector-new TGCVector3(0f,0f,1f));
        }

        public void Disparar(TGCVector3 target)
        {
            this.ArmaPrincipal.Disparar(target);
        }

        public void CreateOOB()
        {
            //Hacemos un OOB a partir de un AABB mas chico porque el original es muy grande.
            this.OOB = TgcBoundingOrientedBox.computeFromAABB(new TgcBoundingAxisAlignBox(TGCVector3.Multiply(Scene.BoundingBox.PMin, 0.5f), TGCVector3.Multiply(Scene.BoundingBox.PMax, 0.5f)));
            this.OOB.move(this.MovementVector);
            this.OOB.rotate(this.RotationVector);
        }

        public TGCMatrix RotationMatrix()
        {
            return TGCMatrix.RotationYawPitchRoll(RotationVector.Y, RotationVector.X, RotationVector.Z);
        }

        public TGCMatrix MovementMatrix()
        {
            return TGCMatrix.Translation(MovementVector);
        }
        private void PerformBarrelRoll(float ElapsedTime)
        {
            /*TGCVector3 origPosition = new TGCVector3(1, 0, 0);

            var v3 = new TGCVector3(-1, 0, 19);
            var v4 = new TGCVector3(3, 0, -5);
            var angle = FastMath.Acos(TGCVector3.Dot(TGCVector3.Normalize(v3), TGCVector3.Normalize(v4)));
            
            float mm = TGCVector3.Dot(this.RotationVector, origPosition);
            if (FastMath.Acos(mm) < FastMath.TWO_PI)*/

           
            if (this.RotationVector.X > -FastMath.TWO_PI)
                this.Rotate(new TGCVector3( FastMath.ToRad(-500 * ElapsedTime), 0, 0));
            else
            {
                this.Rotate(new TGCVector3(-this.RotationVector.X, 0, 0), false);
                this.OOB.setRotation(new TGCVector3(0,FastMath.PI_HALF,0));
                this.shouldBarrelRoll = false;
            }
            
        }

        public void DoBarrelRoll()
        {
            OnCooldown( () => this.shouldBarrelRoll = true );
        }

        private void OnCooldown(Action todo)
        {

            if (coolDownMovimientos.Elapsed.TotalMilliseconds >= COOLDOWNMOVIMIENTOS)
            {
                todo();
                coolDownMovimientos.Restart();
            }
        }

        private void PerformLeft90Spin(float ElapsedTime)
        {
            
            if (stopSpinning)
            {
                if (this.RotationVector.X < 0)
                    this.Rotate(new TGCVector3(FastMath.ToRad(250*ElapsedTime), 0, 0));
                else
                {
                    this.Rotate(new TGCVector3(-this.RotationVector.X, 0, 0), false);
                    this.OOB.setRotation(new TGCVector3(-this.RotationVector.X, FastMath.PI_HALF, 0));
                    this.shouldLeft90Spin = false;
                    this.stopSpinning = false;
                }
            }
            else
            {
                if (this.RotationVector.X < FastMath.PI_HALF)
                    this.Rotate(new TGCVector3(FastMath.ToRad(250 *ElapsedTime), 0, 0));
                else
                {
                    this.Rotate(new TGCVector3(-this.RotationVector.X + FastMath.PI_HALF, 0, 0), false);
                    this.OOB.setRotation(new TGCVector3(FastMath.PI_HALF, FastMath.PI_HALF, 0));
                    this.shouldLeft90Spin = false;
                    this.shouldRight90Spin = true;
                    this.stopSpinning = true;
                }
            }           
        }

        private void PerformRight90Spin(float ElapsedTime)
        {
            if (stopSpinning)
            {
                if (this.RotationVector.X > 0)
                    this.Rotate(new TGCVector3(FastMath.ToRad(-250*ElapsedTime), 0, 0));
                else
                {
                    this.Rotate(new TGCVector3(-this.RotationVector.X, 0, 0), false);
                    this.OOB.setRotation(new TGCVector3(-this.RotationVector.X, FastMath.PI_HALF, 0));
                    this.shouldRight90Spin = false;
                    this.stopSpinning = false;
                }
            }
            else
            {
                if (this.RotationVector.X > -FastMath.PI_HALF)
                {
                    this.Rotate(new TGCVector3(FastMath.ToRad(-250*ElapsedTime), 0, 0));
                }
                else
                {
                    this.Rotate(new TGCVector3(-this.RotationVector.X - FastMath.PI_HALF, 0, 0), false);
                    this.OOB.setRotation(new TGCVector3(-FastMath.PI_HALF, FastMath.PI_HALF, 0));
                    this.shouldRight90Spin = false;
                    this.shouldLeft90Spin = true;
                    this.stopSpinning = true;
                }
            }
        }

        public void DoLeft90Spin()
        {
                this.shouldLeft90Spin = true;
                this.shouldRight90Spin = false;
                this.stopSpinning = false;
        }

        public void DoRight90Spin()
        {
                this.shouldRight90Spin = true;
                this.shouldLeft90Spin = false;
                this.stopSpinning = false;
        }

        public void Rotate(TGCVector3 rotation, bool updateOOB = true)
        {
            if(updateOOB)
                this.OOB.rotate(new TGCVector3(rotation.Z, rotation.Y, -rotation.X));
            this.RotationVector = this.RotationVector + rotation;
        }

        public void MoveTo(TGCVector3 newAbsolutePosition)
        {
            this.OOB.Center = newAbsolutePosition;
            this.MovementVector = newAbsolutePosition;
            ArmaPrincipal.Move(this.GetPosition());
        }

        public void Move(TGCVector3 newOffset)
        {
            this.OOB.move(newOffset * speed);
            this.MovementVector = this.MovementVector + newOffset * speed;
            ArmaPrincipal.Move(this.GetPosition());
        }

        public TGCVector3 GetPosition()
        {
            return MovementVector;
        }


        public int PierdeVidas(int cantVidas)
        {
            Vida = cantVidas;

            return Vida;
        }

        public bool CheckIfMyShotsCollided(NaveEspacial otraNave)
        {
            return this.ArmaPrincipal.CheckShots(otraNave);
        }

        public void Update(float ElapsedTime)
        {
            if (shouldBarrelRoll)
                this.PerformBarrelRoll(ElapsedTime);
            else if (shouldLeft90Spin)
                this.PerformLeft90Spin(ElapsedTime);
            else if (shouldRight90Spin)
                this.PerformRight90Spin(ElapsedTime);

            this.ArmaPrincipal.Update(ElapsedTime);

        }

        public void Render(bool renderBoundingBox = false)
        {
            this.OOB.Render();
            
            this.ActionOnNave((mesh) => {
                mesh.Transform = TransformMatix;
                mesh.Render();
                //if (renderBoundingBox)
                //    mesh.BoundingBox.Render();
            });
            this.ArmaPrincipal.Render();
        }

        public void ActionOnNave(System.Action<TgcMesh> action)
        {
            this.Scene.Meshes.ForEach(action);
        }

        public void UpdateBoundingBox()
        {
            this.ActionOnNave((mesh) => {
                mesh.updateBoundingBox();
            });
        }

        public bool EstaViva()
        {
            return Vida > 0;
        }

        /// <summary>
        /// Mata a la nave, es decir pone su vida en 0.
        /// </summary>
        public void Morir()
        {
            Vida = 0;
        }

        public void Revivir()
        {
            Vida = 100;
        }
    }
}
