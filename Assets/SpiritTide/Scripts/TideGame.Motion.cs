using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private TideRigGraphic articulatedActor,articulatedHair;
        private readonly TideNaturalMotion naturalMotion=new TideNaturalMotion();
        private float naturalCameraX,cameraOriginX,cameraVelocity,cameraClock;
        private int cameraEpoch=-1;
        private bool cameraInBattle;
        private void UpdateMotionCamera()
        {
            if(cameraEpoch!=Session.PresentationEpoch||cameraInBattle!=Session.InBattle)
            {
                cameraEpoch=Session.PresentationEpoch;cameraInBattle=Session.InBattle;naturalCameraX=Session.InBattle?Session.X:0;cameraOriginX=Session.InBattle&&Session.IsCampaign?2.25f:0;cameraVelocity=0;cameraClock=Session.PresentationClock;
            }
            float dt=Mathf.Clamp(Session.PresentationClock-cameraClock,0,.05f);cameraClock=Session.PresentationClock;
            if(!Session.InBattle){naturalCameraX=0;return;}if(dt<=0)return;
            float delta=Session.X-naturalCameraX,target=Session.X-Mathf.Clamp(delta,-1.15f,1.15f);
            naturalCameraX=Mathf.SmoothDamp(naturalCameraX,target,ref cameraVelocity,.13f,16,dt);
        }
        internal TideNaturalMotion NaturalMotion=>naturalMotion;
        internal TideRigGraphic ArticulatedActor=>articulatedActor;
        private void BuildNaturalMotion()
        {
            var back=Node("ArticulatedHair",actor,new Vector2(.5f,0),Vector2.zero,new Vector2(440,320));back.pivot=new Vector2(.5f,0);articulatedHair=back.gameObject.AddComponent<TideRigGraphic>();articulatedHair.Bind(Session.Profile.hero,true);
            var root=Node("ArticulatedActor",actor,new Vector2(.5f,0),Vector2.zero,new Vector2(440,320));root.pivot=new Vector2(.5f,0);
            articulatedActor=root.gameObject.AddComponent<TideRigGraphic>();articulatedActor.raycastTarget=false;
            articulatedActor.Bind(Session.Profile.hero);actorImage.enabled=false;
        }
        private TideMotionInput MotionInput()
        {
            float unit=Session.InBattle?TideHudLayout.UnitX(safe.rect.width,Session.Profile.controlScale):62;
            return TideMotionInput.Capture(Session,unit);
        }
        private void DrawNaturalActor()
        {
            int hero=Session.Profile.hero;var rig=TideRigData.Get(hero);
            if(articulatedActor.Rig!=rig){articulatedActor.Bind(hero);articulatedHair.Bind(hero,true);}
            articulatedHair.gameObject.SetActive(rig.hair_vertices.Length>0);
            if(naturalMotion.Tick(rig,MotionInput()))
            {
                for(int i=0;i<18;i++){articulatedActor.BonePositions[i]=naturalMotion.Positions[i];articulatedActor.BoneAngles[i]=naturalMotion.Angles[i];articulatedHair.BonePositions[i]=naturalMotion.Positions[i];articulatedHair.BoneAngles[i]=naturalMotion.Angles[i];}
                articulatedActor.PoseChanged();articulatedHair.PoseChanged();
            }
            actorImage.enabled=false;actor.pivot=new Vector2(.5f,0);actor.sizeDelta=new Vector2(128,192);
            actor.localScale=new Vector3(Session.Facing,1,1);actor.localRotation=Quaternion.identity;
            actor.anchoredPosition=Project(Session.X,Session.Y)+new Vector2(0,Session.Z*54);
        }
    }
}
