using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Room Info")]
    public TMP_Text roomNameText;
    public TMP_Text connectInfoText;
    public TMP_Text messageText;

    // [Header("Chatting UI")]
    // public TMP_Text chatListText;
    // public TMP_InputField msgIF;

    // public Button exitButton;

    private PhotonView pv;

    // 싱글턴 변수
    public static GameManager instance = null;

    void Awake()
    {
        instance = this;
        
        //PhotonNetwork.IsMessageQueueRunning = true;

        Vector3 pos = new Vector3(Random.Range(-10.0f, 10.0f),
                                  5.0f,
                                  Random.Range(-10.0f, 10.0f));

        // 통신이 가능한 주인공 캐릭터(탱크) 생성
        PhotonNetwork.Instantiate("Player",
                                  pos,
                                  Quaternion.identity, 0);        
    }

    void Start()
    {
        pv = GetComponent<PhotonView>();
        //pv = photonView;
        SetRoomInfo();
    }

    void SetRoomInfo()
    {
        Room currentRoom = PhotonNetwork.CurrentRoom;
        roomNameText.text = currentRoom.Name;
        connectInfoText.text = $"{currentRoom.PlayerCount}/{currentRoom.MaxPlayers}";
    }

    // 
    public void OnExitClick()
    {
        PhotonNetwork.LeaveRoom();
    }

    // CleanUp 끝난 후에 호출되는 콜백
    public override void OnLeftRoom()
    {
        // Lobby 씬으로 되돌아 가기...
        SceneManager.LoadScene("Lobby");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetRoomInfo();
        string msg = $"\n<color=#00ff00>{newPlayer.NickName}</color> is joined room";
        messageText.text += msg;
        
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SetRoomInfo();
        string msg = $"\n<color=#ff0000>{otherPlayer.NickName} left room";
        messageText.text += msg;
    }

    /* public void OnSendClick()
    {
        string _msg = $"<color=#00ff00>[{PhotonNetwork.NickName}]</color> {msgIF.text}";
        pv.RPC("SendChatMessage", RpcTarget.AllBufferedViaServer, _msg);
    }

    [PunRPC]
    void SendChatMessage(string msg)
    {
        chatListText.text += $"{msg}\n";
    } */
}