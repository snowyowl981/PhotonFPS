using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public string gunName; // 총의 이름
    public float range; // 사정거리
    public float accuracy; // 정확도
    public float fireRate; // 연사력
    public float reloadTime; // 장전속도
    public float ammoOutReloadTime; // 탄약 다 썼을 시 장전속도

    public float damage; // 피해량

    public int reloadBulletCount; // 재장전할 총알 개수
    public int currentBulletCount; // 장탄수량
    public int maxBulletCount; // 최대 소유 가능 총알개수
    public int carryBulletCount; // 현재 소유하고 있는 총알개수

    public float retroActionForce; // 반동 세기
    public float retroActionFineSightForce; // 정조준 시 반동세기

    public Vector3 fineSightOriginPos; // 정조준 시점
    public Animator anim; // 총기 애니메이션

    public ParticleSystem muzzleFlash; // 총구화염
    public AudioClip fireSound; // 총소리
}