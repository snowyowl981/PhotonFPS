using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Crosshair : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    // 크로스헤어 상태에 따른 정확도
    private float gunAccuracy;

    // 크로스헤어 비활성화를 위한 부모객체
    [SerializeField]
    private GameObject go_crosshairHUD;
    [SerializeField]
    private GunController theGunCtrl;

    void Start()
    {
        theGunCtrl = FindObjectOfType<GunController>();
    }

    public void WalkingAnimation(bool _flag)
    {
        animator.SetBool("Walking", _flag);
    }

    public void RunningAnimation(bool _flag)
    {
        animator.SetBool("Running", _flag);
    }

    public void JumpingAnimation(bool _flag)
    {
        animator.SetBool("Running", _flag);
    }

    public void CrouchingAnimation(bool _flag)
    {
        animator.SetBool("Crouching", _flag);
    }

    public void FineSightAnimation(bool _flag)
    {
        animator.SetBool("FineSight", _flag);
    }

    public void FireAnimation()
    {
        if(animator.GetBool("Walking"))
        {
            animator.SetTrigger("Walk_Fire");
        }
        else if(animator.GetBool("Crouching"))
        {
            animator.SetTrigger("Crouch_Fire");
        }
        else
        {
            animator.SetTrigger("Idle_Fire");
        }
    }

    public float GetAccuracy()
    {
        if(animator.GetBool("Walking"))
        {
            gunAccuracy = 0.06f;
        }
        else if(animator.GetBool("Crouching"))
        {
            gunAccuracy = 0.017f;
        }
        else if(theGunCtrl.GetFineSight())
        {
            gunAccuracy = 0.001f;
        }
        else
        {
            gunAccuracy = 0.035f;
        }
        return gunAccuracy;
    }
}
