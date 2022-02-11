using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class HUD : MonoBehaviour
{   
    // 필요 컴포넌트
    [SerializeField]
    private GunController theGunCtrl;
    private Gun currentGun;

    // 필요시 HUD 호출, 필요없으면 HUD 비활성화
    [SerializeField]
    private GameObject go_BulletHUD;

    // 텍스트에 총알 개수 반영
    [SerializeField]
    private TMP_Text[] text_Bullet;

    void Start()
    {
        theGunCtrl = FindObjectOfType<GunController>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckBullet();
    }

    private void CheckBullet()
    {
        currentGun = theGunCtrl.GetGun();
        text_Bullet[0].text = currentGun.carryBulletCount.ToString();
        text_Bullet[1].text = currentGun.reloadBulletCount.ToString();
        text_Bullet[2].text = currentGun.currentBulletCount.ToString();
    }
}
