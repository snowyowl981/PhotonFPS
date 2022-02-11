using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GunController : MonoBehaviourPun, IPunObservable
{
    // 현재 장착된 총
    [SerializeField]
    private Gun currentGun;

    // 연사 속도 계산
    private float currentFireRate;

    // 상태 변수
    private bool isReload = false;
    private bool isFineSight = false;

    // 본래 포지션 값
    [SerializeField]
    private Vector3 originPos;
    [SerializeField]
    private Vector3 muzzleOriginPos;
    [SerializeField]
    private Vector3 muzzleAimPos;

    [SerializeField]
    private Camera theCam;
    private AudioSource audioSource;
    private PhotonView pv;
    private Transform tr;
    private Transform muzzleTr;
    private Crosshair theCrosshair;

    // 피격 이펙트
    [SerializeField]
    private GameObject hit_effect_prefab;

    // 충돌 정보 받아옴
    private RaycastHit hitInfo;

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        audioSource = GetComponent<AudioSource>();
        tr = GetComponent<Transform>();
        muzzleTr = currentGun.muzzleFlash.GetComponent<Transform>();
        theCrosshair = FindObjectOfType<Crosshair>();
    }

    // Update is called once per frame
    void Update()
    {
        if(pv.IsMine)
        {
            GunFireRateCalc();
            TryFire();
            TryReload();
            TryFineSight();
        }
        else
        {
            tr.rotation = Quaternion.Slerp(tr.rotation, receiveRot, Time.deltaTime * 10f);
        }
    }

    // 연사속도 재계산
    void GunFireRateCalc()
    {
        if(currentFireRate > 0)
        {   
            // Time.delataTime은 1초에 1씩 감소 약 1/60
            currentFireRate -= Time.deltaTime;
        }
    }

    void TryFire()
    {
        if(Input.GetButton("Fire1") && currentFireRate <= 0 && !isReload && !PlayerController.isRun)
        {
            Fire();
        }
    }

    void Fire()
    {
        if(!isReload)
        {
            if(currentGun.currentBulletCount > 0)
            {
                pv.RPC("Shoot", RpcTarget.All);
            }
            else 
            {
                CancelFineSight();
                pv.RPC("TryReloadAmmoOut", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void Shoot()
    { 
        theCrosshair.FireAnimation();
        currentGun.anim.SetTrigger("Fire");
        currentGun.currentBulletCount--;
        currentFireRate = currentGun.fireRate; // 연사 속도 재계산
        PlaySE(currentGun.fireSound);
        currentGun.muzzleFlash.Play();

        StopAllCoroutines();
        Hit();
    }

    private void Hit()
    {
         if(Physics.Raycast(theCam.transform.position,theCam.transform.forward +
                           new Vector3(Random.Range(-theCrosshair.GetAccuracy() - currentGun.accuracy, theCrosshair.GetAccuracy() + currentGun.accuracy),
                                       Random.Range(-theCrosshair.GetAccuracy() - currentGun.accuracy, theCrosshair.GetAccuracy() + currentGun.accuracy), 
                                       0)
                                       , out hitInfo, currentGun.range))
        {
            var clone = Instantiate(hit_effect_prefab,
                                    hitInfo.point,
                                    Quaternion.LookRotation(hitInfo.normal));
            Destroy(clone, 2f);
        }
    }

    void PlaySE(AudioClip _clip)
    {
        audioSource.clip = _clip;
        audioSource.Play();
    }

    // 재장전 시도
    void TryReload()
    {
        if(Input.GetKeyDown(KeyCode.R) && !isReload && currentGun.currentBulletCount < currentGun.reloadBulletCount)
        {
            CancelFineSight();
            pv.RPC("TryReloadRPC", RpcTarget.All);
        }
    }

    public void CancelReload()
    {
        if(isReload)
        {
            StopAllCoroutines();
            isReload = false;
        }
    }

    [PunRPC]
    void TryReloadRPC()
    {
        StartCoroutine(ReloadCoroutine());
    }

    [PunRPC]
    void TryReloadAmmoOut()
    {
        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        if(currentGun.carryBulletCount > 0)
        {
            isReload = true;

            currentGun.anim.SetTrigger("Reload");

            currentGun.carryBulletCount += currentGun.currentBulletCount;
            currentGun.currentBulletCount = 0;

            yield return new WaitForSeconds(currentGun.reloadTime);

            if(currentGun.carryBulletCount >= currentGun.reloadBulletCount)
            {
                currentGun.currentBulletCount = currentGun.reloadBulletCount;
                currentGun.carryBulletCount -= currentGun.reloadBulletCount;
            }
            else
            {
                currentGun.currentBulletCount = currentGun.carryBulletCount;
                currentGun.carryBulletCount = 0;
            }
            isReload = false;
        }
    }

    void TryFineSight()
    {
        if(Input.GetButtonDown("Fire2") && !isReload)
        {
            FineSight();
        }
    }

    void FineSight()
    {
        isFineSight = !isFineSight;

        currentGun.anim.SetBool("FineSight", isFineSight);
        theCrosshair.FineSightAnimation(isFineSight);

        if(isFineSight)
        {
            StopAllCoroutines();
            StartCoroutine(FineSightActivateCoroutine());
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FineSightDeactivateCoroutine());
        }
    }

    // 정조준 취소
    public void CancelFineSight()
    {
        if(isFineSight)
            FineSight();
    }

    IEnumerator FineSightActivateCoroutine()
    {
        while(tr.localPosition != currentGun.fineSightOriginPos)
        {
            tr.localPosition = Vector3.Lerp(tr.localPosition, currentGun.fineSightOriginPos, 0.2f);
            yield return null;
            muzzleTr.localPosition = muzzleAimPos;
        }
    }

    IEnumerator FineSightDeactivateCoroutine()
    {
        while(tr.localPosition != originPos)
        {
            tr.localPosition = Vector3.Lerp(tr.localPosition, originPos, 0.2f);
            yield return null;
            muzzleTr.localPosition = muzzleOriginPos;
        }
    }

    public Gun GetGun()
    {
        return currentGun;
    }

    public bool GetFineSight()
    {
        return isFineSight;
    }

    Vector3 receivePos = Vector3.zero;
    Quaternion receiveRot = Quaternion.identity;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting) // PhotonView.IsMine == true;
        {
            stream.SendNext(tr.position);
            stream.SendNext(tr.rotation);
        }
        else
        {
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
