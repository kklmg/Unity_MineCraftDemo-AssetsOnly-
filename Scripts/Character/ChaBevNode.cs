﻿using Assets.Scripts.BehaviorTree;
using Assets.Scripts.InputHandler;
using Assets.Scripts.EventManager;
using Assets.Scripts.Pattern;
using Assets.Scripts.WorldComponent;

using UnityEngine;

namespace Assets.Scripts.CharacterSpace
{
    class Control_Cha_Move : BevConditionBase
    {
        private float m_fHor;
        private float m_fVer;
        private ChaBevData m_ThisData;
        E_Cha_TryMove m_ETryMove;

        protected override void VEnter(BevData workData)
        {  
            m_ThisData = workData as ChaBevData;
            m_ETryMove = new E_Cha_TryMove(m_ThisData.Move);
        }  

        public override bool Check(BevData workData)
        {
            IController control = Locator<IController>.GetService();

            //get input
            m_fHor = control.Horizontal();
            m_fVer = control.Vertical();

            //not valid input
            if (Mathf.Approximately(0.0f, m_fHor)
                && Mathf.Approximately(0.0f, m_fVer)) return false;

            //calculate movement
            m_ThisData.Move.Trans_x = m_fHor * Time.deltaTime
                * (m_ThisData.isWalking ? m_ThisData.Character.WalkSpeed : m_ThisData.Character.RunSpeed);
            m_ThisData.Move.Trans_z = m_fVer * Time.deltaTime
            * (m_ThisData.isWalking ? m_ThisData.Character.WalkSpeed : m_ThisData.Character.RunSpeed);


            //notify to global event center
            Locator<IEventPublisher>.GetService().PublishAndHandle(m_ETryMove);
            //notify to other component
            m_ThisData.Communicator.PublishEvent(m_ETryMove);

            return true;
        }
    }
    class Control_Cha_RotateY : BevConditionBase
    {
        private float Cache_Rotate;
        public override bool Check(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;
            IController control = Locator<IController>.GetService();

            //get input
            Cache_Rotate = control.Rotate_Y();

            //not valid input
            if (Mathf.Approximately(0.0f, Cache_Rotate)) return false;

            thisData.Move.Rotation_Y = Cache_Rotate;

            return true;
        }
    }
    class Control_Camera_UpDown : BevConditionBase
    {
        private float Cache_Rotate;
        public override bool Check(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;
            IController control = Locator<IController>.GetService();

            //get input
            Cache_Rotate = control.Rotate_Z();

            //not valid input
            if (Mathf.Approximately(0.0f, Cache_Rotate)) return false;

            //save to blackboard
            thisData.SetValue(KEY_CONTROL.CAMERA_UD, Cache_Rotate);

            return true;
        }


    }
    public class Cha_Move : BevLeaf
    {
        Vector3 Cache_Velocity;
        E_Cha_Moved m_ECha_move = new E_Cha_Moved();
        protected override eRunningState Tick(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;
            thisData.Character.transform.Translate(
                new Vector3(thisData.Move.Trans_x, thisData.Move.Trans_y, thisData.Move.Trans_z));

            //notify orther system
            m_ECha_move.Cha = thisData.Character;
            Locator<IEventPublisher>.GetService().Publish(m_ECha_move);    

            m_enRunningState = eRunningState.Suceed;
            return m_enRunningState;
        }
    }
    public class Cha_Rotate : BevLeaf
    {
        protected override eRunningState Tick(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;

            thisData.Character.transform.Rotate(Vector3.up * thisData.Move.Rotation_Y);

            m_enRunningState = eRunningState.Suceed;
            return m_enRunningState;
        }
    }
    public class CameraUpDown : BevLeaf
    {
        float Camera_UD;
        protected override eRunningState Tick(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;

            thisData.GetValue(KEY_CONTROL.CAMERA_UD, out Camera_UD);
            thisData.Character.Camera.transform.Rotate(Vector3.right *-Camera_UD);

            m_enRunningState = eRunningState.Suceed;
            return m_enRunningState;
        }
    }
    public class Cha_Jump : BevConditionBase
    { 
        public override bool Check(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;
            IController control = Locator<IController>.GetService();

            //get input
            bool trigger = control.Jump();
            if (!trigger) return false;

            //notify subscribed component
            thisData.Communicator.PublishEvent(new E_Cha_Jump(thisData.Character.JumpForce));
            Debug.Log("jump triggerd");
            return true;
        }
    }

    public class Cha_NotGrounded : BevConditionBase
    {
        private World m_refWorld;
        //private E_Cha_TryMove m_ECha_TryMove = new E_Cha_TryMove();

        protected override void VEnter(BevData workData)
        {
            m_refWorld = Locator<World>.GetService();
        }
        public override bool Check(BevData workData)
        {
            ChaBevData thisData = workData as ChaBevData;
            IController control = Locator<IController>.GetService();

            Block adj = m_refWorld.GetBlock(thisData.Character.transform.position + Vector3.down);
            return !(adj != null && adj.IsSolid(eDirection.up));
        }
    }
}
