﻿using CustomCompanions.Framework.Models.Companion;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomCompanions.Framework.Companions
{
    public class Companion : NPC
    {
        private Farmer owner;
        private CompanionModel model;

        private int? soundIdleTimer = null;
        private int? soundMovingTimer = null;
        private int? soundAlwaysTimer = null;

        private SoundModel idleSound;
        private SoundModel movingSound;
        private SoundModel alwaysSound;

        private readonly NetVector2 motion = new NetVector2(Vector2.Zero);
        private new readonly NetRectangle nextPosition = new NetRectangle();

        public Companion(CompanionModel model, Farmer owner) : base(new AnimatedSprite(model.TileSheetPath, 0, model.FrameSizeWidth, model.FrameSizeHeight), owner.getTileLocation() * 64f, 2, model.Name)
        {
            base.Breather = false;
            base.speed = model.TravelSpeed;
            base.forceUpdateTimer = 9999;
            base.collidesWithOtherCharacters.Value = false;
            base.farmerPassesThrough = true;
            base.HideShadow = true;

            this.owner = owner;
            this.model = model;
            this.nextPosition.Value = this.GetBoundingBox();

            idleSound = model.Sounds.FirstOrDefault(s => s.WhenToPlay.ToUpper() == "IDLE");
            if (idleSound != null && CustomCompanions.IsSoundValid(idleSound.SoundName, true))
            {
                this.soundIdleTimer = idleSound.TimeBetweenSound;
            }

            movingSound = model.Sounds.FirstOrDefault(s => s.WhenToPlay.ToUpper() == "MOVING");
            if (movingSound != null && CustomCompanions.IsSoundValid(movingSound.SoundName, true))
            {
                this.soundMovingTimer = movingSound.TimeBetweenSound;
            }

            alwaysSound = model.Sounds.FirstOrDefault(s => s.WhenToPlay.ToUpper() == "ALWAYS");
            if (alwaysSound != null && CustomCompanions.IsSoundValid(alwaysSound.SoundName, true))
            {
                this.soundAlwaysTimer = alwaysSound.TimeBetweenSound;
            }
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.currentLocation = location;
            base.update(time, location);
            base.forceUpdateTimer = 99999;

            this.AttemptMovement(time, location);

            // Play any sound(s) that are required
            this.PlayRequiredSounds(time);

        }

        public override bool isMoving()
        {
            return !this.motion.Equals(Vector2.Zero);
        }


        public override bool shouldCollideWithBuildingLayer(GameLocation location)
        {
            if (IsFlying())
            {
                return false;
            }

            return base.shouldCollideWithBuildingLayer(location);
        }

        public override bool collideWith(StardewValley.Object o)
        {
            if (IsFlying())
            {
                return false;
            }

            return base.collideWith(o);
        }

        public override bool isColliding(GameLocation l, Vector2 tile)
        {
            if (IsFlying())
            {
                return false;
            }

            return base.isColliding(l, tile);
        }

        private void AttemptMovement(GameTime time, GameLocation location)
        {

            Farmer f = Utility.isThereAFarmerWithinDistance(base.getTileLocation(), 10, base.currentLocation);
            if (f != null)
            {
                var targetDistance = Vector2.Distance(base.Position, f.Position);
                if (targetDistance > 640f)
                {
                    base.position.Value = f.position;
                }
                else if (targetDistance > 64f)
                {
                    base.Speed = model.TravelSpeed;
                    if (targetDistance > 128f)
                    {
                        base.Speed = model.TravelSpeed + (int)(targetDistance / 64f) - 1;
                    }

                    this.SetMotion(Utility.getVelocityTowardPlayer(new Point((int)base.Position.X, (int)base.Position.Y), base.speed, f));
                }
                else
                {
                    this.motion.Value = Vector2.Zero;
                }
            }

            this.nextPosition.Value = this.GetBoundingBox();
            this.nextPosition.X += (int)this.motion.X;
            if (!location.isCollidingPosition(this.nextPosition, Game1.viewport, this) || IsFlying())
            {
                base.position.X += (int)this.motion.X;
            }
            this.nextPosition.X -= (int)this.motion.X;
            this.nextPosition.Y += (int)this.motion.Y;
            if (!location.isCollidingPosition(this.nextPosition, Game1.viewport, this) || IsFlying())
            {
                base.position.Y += (int)this.motion.Y;
            }

            if (!this.motion.Equals(Vector2.Zero))
            {
                if (model.UniformAnimation != null && !CustomCompanions.CompanionHasFullMovementSet(model))
                {
                    this.Sprite.Animate(time, model.UniformAnimation.StartingFrame, model.UniformAnimation.NumberOfFrames, model.UniformAnimation.Duration);
                    this.FacingDirection = 2;
                }
                else if (CustomCompanions.CompanionHasFullMovementSet(model))
                {
                    int oldDirection = this.FacingDirection;

                    if (Math.Abs(this.motion.Y) > Math.Abs(this.motion.X) && this.motion.Y < 0f)
                    {
                        this.FacingDirection = 0;
                    }
                    else if (Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X > 0f)
                    {
                        this.FacingDirection = 1;
                    }
                    else if (Math.Abs(this.motion.Y) > Math.Abs(this.motion.X) && this.motion.Y > 0f)
                    {
                        this.FacingDirection = 2;
                    }
                    else if (Math.Abs(this.motion.X) > Math.Abs(this.motion.Y) && this.motion.X < 0f)
                    {
                        this.FacingDirection = 3;
                    }

                    switch (this.FacingDirection)
                    {
                        case 0:
                            this.Sprite.Animate(time, model.UpAnimation.StartingFrame, model.UpAnimation.NumberOfFrames, model.UpAnimation.Duration);
                            break;
                        case 1:
                            this.Sprite.Animate(time, model.RightAnimation.StartingFrame, model.RightAnimation.NumberOfFrames, model.RightAnimation.Duration);
                            break;
                        case 2:
                            this.Sprite.Animate(time, model.DownAnimation.StartingFrame, model.DownAnimation.NumberOfFrames, model.DownAnimation.Duration);
                            break;
                        case 3:
                            this.Sprite.Animate(time, model.LeftAnimation.StartingFrame, model.LeftAnimation.NumberOfFrames, model.LeftAnimation.Duration);
                            break;
                    }
                }
            }
            else
            {
                if (model.UniformAnimation != null && !CustomCompanions.CompanionHasFullMovementSet(model))
                {
                    this.Sprite.Animate(time, model.UniformAnimation.IdleAnimation.StartingFrame, model.UniformAnimation.IdleAnimation.NumberOfFrames, model.UniformAnimation.IdleAnimation.Duration);
                }
                else
                {
                    switch (this.FacingDirection)
                    {
                        case 0:
                            this.Sprite.Animate(time, model.UpAnimation.IdleAnimation.StartingFrame, model.UpAnimation.IdleAnimation.NumberOfFrames, model.UpAnimation.IdleAnimation.Duration);
                            break;
                        case 1:
                            this.Sprite.Animate(time, model.RightAnimation.IdleAnimation.StartingFrame, model.RightAnimation.IdleAnimation.NumberOfFrames, model.RightAnimation.IdleAnimation.Duration);
                            break;
                        case 2:
                            this.Sprite.Animate(time, model.DownAnimation.IdleAnimation.StartingFrame, model.DownAnimation.IdleAnimation.NumberOfFrames, model.DownAnimation.IdleAnimation.Duration);
                            break;
                        case 3:
                            this.Sprite.Animate(time, model.LeftAnimation.IdleAnimation.StartingFrame, model.LeftAnimation.IdleAnimation.NumberOfFrames, model.LeftAnimation.IdleAnimation.Duration);
                            break;
                    }
                }
            }
        }

        private void PlayRequiredSounds(GameTime time)
        {
            if (this.soundAlwaysTimer != null)
            {
                this.soundAlwaysTimer = Math.Max(0, (int)this.soundAlwaysTimer - time.ElapsedGameTime.Milliseconds);
                if (soundAlwaysTimer <= 0 && Game1.random.NextDouble() <= alwaysSound.ChanceOfPlaying)
                {
                    Game1.playSound(alwaysSound.SoundName);
                    soundAlwaysTimer = alwaysSound.TimeBetweenSound;
                }
            }

            if (this.isMoving() && this.soundMovingTimer != null)
            {
                this.soundMovingTimer = Math.Max(0, (int)this.soundMovingTimer - time.ElapsedGameTime.Milliseconds);
                if (soundMovingTimer <= 0 && Game1.random.NextDouble() <= movingSound.ChanceOfPlaying)
                {
                    Game1.playSound(movingSound.SoundName);
                    soundMovingTimer = movingSound.TimeBetweenSound;
                }
            }

            if (!this.isMoving() && this.soundIdleTimer != null)
            {
                this.soundIdleTimer = Math.Max(0, (int)this.soundIdleTimer - time.ElapsedGameTime.Milliseconds);
                if (soundIdleTimer <= 0 && Game1.random.NextDouble() <= idleSound.ChanceOfPlaying)
                {
                    Game1.playSound(idleSound.SoundName);
                    soundIdleTimer = idleSound.TimeBetweenSound;
                }
            }
        }

        internal bool IsFlying()
        {
            return this.model.Type.ToUpper() == "FLYING";
        }

        internal void SetMotion(Vector2 motion)
        {
            this.motion.Value = motion;
        }

        internal void ResetForNewLocation(Vector2 position)
        {
            base.Position = position * 64f;
        }
    }
}
