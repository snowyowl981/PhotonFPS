using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    // 스피드 조정 변수
    [SerializeField]
    private float walkSpeed;

    [SerializeField]
    private float runSpeed;
    private float applySpeed;
    
    [SerializeField]
    private float crouchSpeed;

    [SerializeField]
    private float jumpForce;

    // 현재 장착된 총
    [SerializeField]
    private Gun currentGun;

    // 연사 속도 계산
    private float currentFireRate;

    // Vector3 moveDir = Vector3.zero;

    // 상태 변수
    private bool isWalk = false;
    public static bool isRun = false;
    private bool isGround = true;
    private bool isCrouch = false;

    // 움직임 체크 변수(전 프레임의 현재위치)
    private Vector3 lastPos;
    private Vector3 currPos;
    private Quaternion currRot;

    // 땅 착지 여부
    private CapsuleCollider capsuleCollider;

    // 앉았을 때 얼마나 앉을지 결정하는 변수
    private float crouchPosY;
    private float originPosY;
    private float applyCrouchPosY;

    // 카메라 민감도
    [SerializeField]
    private float lookSensitivity;

    // 카메라 한계
    [SerializeField]
    private float cameraRotationLimit;
    private float currentCameraRotationX = 0.0f;

    // 필요 컴포넌트
    [SerializeField]
    private Camera theCamera;
    private Transform tr;
    private Rigidbody myRig;
    private PhotonView pv;
    private Animator anim;
    private AudioSource audioSource;
    private Transform gunTr;
    private GunController gunController;
    private Crosshair theCrosshair;

    // Start is called before the first frame update
    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        myRig = GetComponent<Rigidbody>();
        tr = GetComponent<Transform>();
        pv = GetComponent<PhotonView>();
        anim = GetComponentInChildren<Animator>();
        gunTr = GameObject.FindGameObjectWithTag("GUN").GetComponent<Transform>();
        gunController = FindObjectOfType<GunController>();
        theCrosshair = FindObjectOfType<Crosshair>();

        applySpeed = walkSpeed;
        originPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = originPosY;

        if (!pv.IsMine)
        {   
            GetComponent<Rigidbody>().isKinematic = true;
            Camera[] cameras = tr.GetComponentsInChildren<Camera>();
            theCamera.GetComponent<AudioBehaviour>().enabled = false;
            foreach (var cam in cameras)
            {
                cam.enabled = false;
            }
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(pv.IsMine)
        {
            // 캐릭터 이동 로직
            IsGround();
            Move();
            CameraRotation();
            CharacterRotation();
            TryRun();
            TryJump();
            TryCrouch();
        }

         else
        {
            if((tr.position - receivePos).sqrMagnitude > 10.0f * 10.0f)
            {
                tr.position = receivePos;
            }
            else
            {
                tr.position = Vector3.Lerp(tr.position, receivePos, Time.deltaTime * 10f);
            }
            tr.rotation = Quaternion.Slerp(tr.rotation, receiveRot, Time.deltaTime * 10f);
        }
    }

    void FixedUpdate()
    {
        if (pv.IsMine)
            MoveCheck();
    }

    // 움직임 실행
    private void Move()
    {
        // GetAxis : 부드러운 이동, GetAxisRaw : 즉각적인 이동(서바이벌이므로 Raw 사용)
        // Horizontal 유니티 기본 좌우 제공, Vertical 전후 제공
        float moveDirX = Input.GetAxisRaw("Horizontal");
        float moveDirZ = Input.GetAxisRaw("Vertical");
        Vector3 _moveHorizontal = Vector3.right * moveDirX;
        Vector3 _moveVertical = Vector3.forward * moveDirZ;
        // normalized 벡터 합을 1로 정규화
        // applySpeed를 넣음으로써 같은 코드를 두 번 작성할 필요가 없음
        //Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed;
        // 약 0.0016
        //myRig.MovePosition(tr.position + _velocity * Time.deltaTime);
        tr.Translate((_moveHorizontal + _moveVertical).normalized * Time.deltaTime * applySpeed);
    }

    private void MoveCheck()
    {
        if (!isRun && !isCrouch && isGround)
        {
            if (Vector3.Distance(lastPos, transform.position) >= 0.01f)
            {
                isWalk = true;
                anim.SetBool("Walk", true);
            }
            else
            {
                isWalk = false;
                anim.SetBool("Walk", false);
            }
            theCrosshair.WalkingAnimation(isWalk);
            lastPos = transform.position;
        }
    }

    private void TryRun()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Running();
        }
        if(Input.GetKeyUp(KeyCode.LeftShift))
        {
            RunningCancel();
        }
    }

    private void Running()
    {
        if(isCrouch)
        {
            Crouch();
        }
        gunController.CancelFineSight();
        isRun = true;
        anim.SetBool("Run", true);
        theCrosshair.RunningAnimation(isRun);
        applySpeed = runSpeed;
    }

    private void RunningCancel()
    {
        isRun = false;
        anim.SetBool("Run", false);
        theCrosshair.RunningAnimation(isRun);
        applySpeed = walkSpeed;
    }

    private void IsGround()
    {
        isGround = Physics.Raycast(tr.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f);
        theCrosshair.JumpingAnimation(!isGround);
    }

    private void TryJump()
    {
        if(Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            Jump();
        }
    }

    private void Jump()
    {
        if(isCrouch)
        {
            Crouch();
        }
        myRig.velocity = tr.up * jumpForce;
    }

    // 앉기 시도
    private void TryCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
    }

    // 앉기
    private void Crouch()
    {
        // 스위치 역할
        isCrouch = !isCrouch;
        theCrosshair.CrouchingAnimation(isCrouch);

        if (isCrouch)
        {
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else
        {
            applySpeed = walkSpeed;
            applyCrouchPosY = originPosY;
        }
        if(isWalk)
        {
            isWalk = false;
            theCrosshair.WalkingAnimation(isWalk);
        }
        StartCoroutine(CrouchCoroutine());
    }

    IEnumerator CrouchCoroutine()
    {
        float _posY = theCamera.transform.localPosition.y;
        int count = 0;
        while(_posY != applyCrouchPosY)
        {
            count++;
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.1f);

            theCamera.transform.localPosition = new Vector3(0, _posY, 0);

            if(count > 15)
            {
                break;
            }

            yield return null;
        }
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0f);
    }

    // 상하 카메라 회전
    private void CameraRotation()
    {
        float _xRotation = Input.GetAxisRaw("Mouse Y");
        float _cameraRotationX = _xRotation * lookSensitivity;
        currentCameraRotationX -= _cameraRotationX;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);

        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0.0f, 0.0f);
    }

    private void CharacterRotation()
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        myRig.MoveRotation(myRig.rotation * Quaternion.Euler(_characterRotationY));
    }

    // 네트워크를 통해서 수신받을 변수
    Vector3 receivePos = Vector3.zero;
    Quaternion receiveRot = Quaternion.identity;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting) // PhotonView.IsMine == true;
        {
            stream.SendNext(tr.position);   // 위치
            stream.SendNext(tr.rotation);   // 회전값
        }
        else
        {
            // 두번 보내서 두번 받음
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
